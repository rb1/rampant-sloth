using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Library.Net;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        static readonly string RECALL_ORDER = "RTB";
        static readonly string LAUNCH_ORDER = "GO";
        
        static readonly string STATUS_REPORTING_CHANNEL = "Home carrier drone status";
        static readonly string DEBUG_PANEL_TAG = "[DebugPanel]";
        static readonly string DRONE_TAG = "[Drone]";
        static readonly string MAX_LINES_TAG_START = "[MaxLines:";
        static readonly int DEFAULT_MAX_LINES = 6;

        readonly IMyBroadcastListener m_statusListener;
        readonly Dictionary<string, Drone> m_drones;
        readonly IMyTextSurface m_myTextSurface;
        readonly IMyTextSurface m_debugTextSurface;
        readonly List<IMyTextSurfaceProvider> m_droneStatusSurfaceProviders = new List<IMyTextSurfaceProvider>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            m_statusListener = IGC.RegisterBroadcastListener(STATUS_REPORTING_CHANNEL);
            m_drones = new Dictionary<string, Drone>();
            m_myTextSurface = Me.GetSurface(0);
            List<IMyTextSurfaceProvider> surfaceProviders = new List<IMyTextSurfaceProvider>();
            GridTerminalSystem.GetBlocksOfType(surfaceProviders);
            foreach(var provider in surfaceProviders)
            {
                var block = provider as IMyTerminalBlock;
                if(block != null) 
                {
                    if(block.CustomData.Contains(DEBUG_PANEL_TAG))
                    {
                        m_debugTextSurface = provider.GetSurface(0);
                        m_debugTextSurface.ContentType = ContentType.TEXT_AND_IMAGE;

                        int debugLineCount = DEFAULT_MAX_LINES;
                        var start = block.CustomData.IndexOf(MAX_LINES_TAG_START);
                        if(start >= 0)
                        {
                            start += MAX_LINES_TAG_START.Length;
                            var end = block.CustomData.IndexOf("]", start);
                            if(end >= 0)
                            {
                                if(int.TryParse(block.CustomData.Substring(start, end - start), out debugLineCount) == false)
                                {
                                    debugLineCount = DEFAULT_MAX_LINES;
                                }
                            }
                        }
                        m_debugPanelLines = new TextPanelBuffer(debugLineCount);
                    }
                    else if(block.CustomData.Contains(DRONE_TAG))
                    {
                        m_droneStatusSurfaceProviders.Add(provider);
                    }
                }
            }
        }

        IMyTextSurface FindStatusSurfaceForDrone(string droneName)
        {
            foreach(var provider in m_droneStatusSurfaceProviders)
            {
                var customData = (provider as IMyTerminalBlock).CustomData;
                var searchStr = "[" + droneName + ":";
                var start = customData.IndexOf(searchStr);
                if(start >= 0)
                {
                    start += searchStr.Length;
                    var end = customData.IndexOf("]", start);
                    if(end >=0)
                    {
                        int surfaceIndex = 0;
                        if(int.TryParse(customData.Substring(start, end - start), out surfaceIndex) == false)
                        {
                            surfaceIndex = 0;
                        }
                        return provider.GetSurface(surfaceIndex);
                    }
                }
                else if(customData.Contains("[" + droneName))
                {
                    return provider.GetSurface(0);
                }
            }
            Log("No panel found for " + droneName);
            return null;
        }

        readonly TextPanelBuffer m_debugPanelLines;

        private void Log(string msg)
        {
            if(m_debugTextSurface != null)
            {
                m_debugPanelLines.AddLine(msg);
                m_debugTextSurface.WriteText(m_debugPanelLines.GetLines());
            }
            Echo(msg);
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(argument.StartsWith(RECALL_ORDER))
            {
                SendRecallOrder(argument.Substring(RECALL_ORDER.Length + 1));
                return;
            }
            else if(argument.StartsWith(LAUNCH_ORDER))
            {
                SendLaunchOrder(argument.Substring(LAUNCH_ORDER.Length + 1));
                return;
            }
            //Process any status updates from drones
            while(m_statusListener.HasPendingMessage)
            {
                var message = m_statusListener.AcceptMessage().As<String>();
                HandleStatusUpdate(message);
            }

            //Update PB Panel
            if(m_drones.Count > 0)
            {
                m_myTextSurface.WriteText(string.Join("\r\n", m_drones.Values.Select(x => x.Get1LineStatusText())));
            }
            else
            {
                m_myTextSurface.WriteText("NO DRONES");
            }

            //Update drone status panels
            foreach(var drone in m_drones.Values)
            {
                if(drone.OutputSurface == null)
                    continue;
                drone.OutputSurface.WriteText(drone.GetDetailedPanelText());
            }
        }

        void SendLaunchOrder(string droneName)
        {
            Log("Launching on " + m_drones[droneName].OrdersChannel);
            IGC.SendBroadcastMessage(m_drones[droneName].OrdersChannel, LAUNCH_ORDER);
        }

        void SendRecallOrder(string droneName)
        {
            Log("Recalling on " + m_drones[droneName].OrdersChannel);
            IGC.SendBroadcastMessage(m_drones[droneName].OrdersChannel, RECALL_ORDER);
        }

        void HandleStatusUpdate(string updateMessage)
        {
            var status = new DroneStatus(updateMessage);
            if(m_drones.ContainsKey(status.DroneName) == false)
            {
                HandleNewDrone(status);
            }
            m_drones[status.DroneName].LastStatus = status;
        }
        void HandleNewDrone(DroneStatus status)
        {
            //TODO Find output surface
            m_drones[status.DroneName] = new Drone(status, FindStatusSurfaceForDrone(status.DroneName));
            Log("Registered new drone: " + status.DroneName);
        }
    }

    class Drone
    {
        static readonly string ORDERS_CHANNEL_FMT_STR = "{0} orders";
        public Drone(DroneStatus status, IMyTextSurface outputSurface)
        {
            LastStatus = status;
            OrdersChannel = string.Format(ORDERS_CHANNEL_FMT_STR, status.DroneName);
            OutputSurface = outputSurface;
        }
        public DroneStatus LastStatus;
        public readonly IMyTextSurface OutputSurface;
        public readonly string OrdersChannel;
        public string GetDetailedPanelText()
        {
            return string.Format("{0}\n{1}\n{2}" +
                                 "Fuel:  {3:P2}\n" +
                                 "Power: {4:P2}\n" +
                                 "Ammo:  {5:P2}\n", 
                                 LastStatus.DroneName,
                                 LastStatus.FlightStatus,
                                 LastStatus.IsDamaged?"DAMAGED\n":"",
                                 LastStatus.FuelLevel,
                                 LastStatus.BatteryLevel,
                                 LastStatus.MagazineLevel);
        }
        public string Get1LineStatusText()
        {
            return string.Format("{0}{1} - {2}: F{3:P2} P{4:P2} A{5:P2} ", LastStatus.DroneName, LastStatus.IsDamaged?"(D)":"", LastStatus.FlightStatus, LastStatus.FuelLevel, LastStatus.BatteryLevel, LastStatus.MagazineLevel);
        }
    }

    class DroneStatus
    {
        public DroneStatus(string statusStr)
        {
            var parts = statusStr.Split(',');
            DroneName = parts[DroneNameIndex];
            FlightStatus = parts[FlightStatusIndex];
            IsDamaged = bool.Parse(parts[IsDamagedIndex]);
            FuelLevel = float.Parse(parts[FuelLevelIndex].Replace("%", "")) / 100;
            BatteryLevel = float.Parse(parts[BatteryLevelIndex].Replace("%", "")) / 100;
            MagazineLevel = float.Parse(parts[MagazineLevelIndex].Replace("%", "")) / 100;
            RawMessage = statusStr;

        }
        readonly int DroneNameIndex = 0;
        public readonly string DroneName;
        readonly int FlightStatusIndex = 1;
        public readonly string FlightStatus;
        readonly int IsDamagedIndex = 2;
        public readonly bool IsDamaged;
        readonly int FuelLevelIndex = 3;
        public readonly float FuelLevel;
        readonly int BatteryLevelIndex = 4;
        public readonly float BatteryLevel;
        readonly int MagazineLevelIndex = 5;
        public readonly float MagazineLevel;
        public readonly string RawMessage;

    }

    class TextPanelBuffer
    {
        public TextPanelBuffer(int capacity)
        {
            m_lines = new string[capacity];
            m_lastAddedIndex = capacity;
        }
        readonly string[] m_lines;
        private int m_lastAddedIndex;
        
        public void AddLine(string line)
        {
            m_lastAddedIndex++;
            if(m_lastAddedIndex >= m_lines.Length)
            {
                m_lastAddedIndex = 0;
            }
            m_lines[m_lastAddedIndex] = line;
        }

        public string GetLines()
        {
            string outStr = "";
            for(int lineCount = 0; lineCount < m_lines.Length; lineCount++)
            {
                m_lastAddedIndex++;
                if(m_lastAddedIndex >= m_lines.Length)
                {
                    m_lastAddedIndex = 0;
                }
                outStr += m_lines[m_lastAddedIndex] + "\n";
            }
            return outStr;
        }

    }

}
