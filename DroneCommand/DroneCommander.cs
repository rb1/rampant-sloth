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
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        static readonly string RECALL_ORDER = "RTB";
        static readonly string LAUNCH_ORDER = "GO";
        
        static readonly string STATUS_REPORTING_CHANNEL = "Home carrier drone status";

        readonly IMyBroadcastListener m_statusListener;
        readonly Dictionary<string, Drone> m_drones;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            m_statusListener = IGC.RegisterBroadcastListener(STATUS_REPORTING_CHANNEL);
            m_drones = new Dictionary<string, Drone>();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            while(m_statusListener.HasPendingMessage)
            {
                var message = m_statusListener.AcceptMessage().As<String>();
                HandleStatusUpdate(message);
            }
            if(argument.StartsWith(RECALL_ORDER))
            {
                SendRecallOrder(argument.Substring(RECALL_ORDER.Length + 1));

            }
            else if(argument.StartsWith(LAUNCH_ORDER))
            {
                SendLaunchOrder(argument.Substring(LAUNCH_ORDER.Length + 1));
            }
        }

        void SendLaunchOrder(string droneName)
        {
            IGC.SendBroadcastMessage(m_drones[droneName].OrdersChannel, LAUNCH_ORDER);
        }

        void SendRecallOrder(string droneName)
        {
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
            m_drones[status.DroneName] = new Drone(status, null);
            Echo("Registered new drone: " + status.DroneName);
        }
    }

    class Drone
    {
        static readonly string ORDERS_CHANNEL_FMT_STR = "{0} orders";
        public Drone(DroneStatus status, IMyTextSurface outputSurface)
        {
            LastStatus = status;
            OrdersChannel = string.Format(ORDERS_CHANNEL_FMT_STR, status.DroneName);
        }
        public DroneStatus LastStatus;
        public readonly IMyTextSurface OutputSurface;
        public readonly string OrdersChannel;
    }

    class DroneStatus
    {
        public DroneStatus(string statusStr)
        {
            var parts = statusStr.Split(',');
            DroneName = parts[DroneNameIndex];
            FlightStatus = parts[FlightStatusIndex];
            IsDamaged = bool.Parse(parts[IsDamagedIndex]);
            FuelLevel = float.Parse(parts[FuelLevelIndex]);
            BatteryLevel = float.Parse(parts[BatteryLevelIndex]);
            MagazineLevel = float.Parse(parts[MagazineLevelIndex]);
        }
        static readonly int DroneNameIndex = 0;
        public readonly string DroneName;
        public readonly int FlightStatusIndex = 1;
        public readonly string FlightStatus;
        public readonly int IsDamagedIndex = 2;
        public readonly bool IsDamaged;
        public readonly int FuelLevelIndex = 3;
        public readonly float FuelLevel;
        public readonly int BatteryLevelIndex = 4;
        public readonly float BatteryLevel;
        public readonly int MagazineLevelIndex = 5;
        public readonly float MagazineLevel;

    }
}
