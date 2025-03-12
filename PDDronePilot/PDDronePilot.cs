using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.
        static readonly string DRONE_NAME = "Peashooter 1";
        static readonly string OLD_DRONE_NAME = DRONE_NAME;
        static readonly string AMMO_STORAGE_TAG = "[Ammo]";
        static readonly string RECALL_TIMER_TAG = "[Recall]";
        static readonly string LAUNCH_TAG = "[Launch]";
        static readonly string DOCKING_TAG = "[Dock]";
        static readonly string STATUS_REPORTING_CHANNEL = "Home carrier drone status";
        static readonly string ORDERS_CHANNEL = DRONE_NAME + " orders";
        static readonly float LOW_FUEL_PERCENT = .30f;
        static readonly float HIGH_FUEL_PERCENT = .99f;
        static readonly float LOW_BATTERY_PERCENT = .20f;
        static readonly float HIGH_BATTERY_PERCENT = .90f;
        static readonly float LOW_AMMO_PERCENT = .0f;
        static readonly float HIGH_AMMO_PERCENT = .80f;
        static readonly int LAUNCH_TIME_IN_TICKS = 18;
        static readonly float LAUNCH_THRUST_OVERRIDE = .8f;
        static readonly string RECALL_ORDER = "RTB";
        static readonly string LAUNCH_ORDER = "GO";

readonly IMyBroadcastListener m_ordersListener;

        readonly List<IMyCargoContainer> m_cargoContainers = new List<IMyCargoContainer>(5);
        readonly List<IMyTerminalBlock> m_droneBlocks = new List<IMyTerminalBlock>(20);
        readonly List<IMyThrust> m_launchThrusters = new List<IMyThrust>(3);
        readonly List<IMyThrust> m_allThrusters = new List<IMyThrust>(12);
        readonly List<IMyGasTank> m_fuelTanks = new List<IMyGasTank>(3);
        readonly List<IMyBatteryBlock> m_batteries = new List<IMyBatteryBlock>(2);
        
        readonly IMyTimerBlock m_recallTimerBlock;
        readonly IMyShipConnector m_dockingConnector;
        readonly IMyFlightMovementBlock m_AIMoveBlock;
        readonly IMyBasicMissionBlock m_AIBasicWanderBlock;
        readonly IMyDefensiveCombatBlock m_AIDefenseBlock;
        readonly IMyPathRecorderBlock m_AIDockingPathRecorderBlock;

        bool m_readyToLaunch = false;
        bool m_recallTriggered = false;
        int m_ticksSinceLaunch = int.MinValue;
        enum StatusReportAction
        {
            RECALL,
            AS_YOU_WERE,
            CAN_LAUNCH
        }
        public Program()
        {

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            LoadFromStorage();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            foreach(var block in blocks)
            {
                if(block.IsSameConstructAs(Me))
                {
                    RenameBlock(block);
                    m_droneBlocks.Add(block);
                    if (block is IMyTimerBlock && block.CustomData.Contains(RECALL_TIMER_TAG))
                    {
                        m_recallTimerBlock = block as IMyTimerBlock;
                    }
                    else if (block is IMyCargoContainer && block.CustomData.Contains(AMMO_STORAGE_TAG))
                    {
                        m_cargoContainers.Add(block as IMyCargoContainer);
                        block.ShowInTerminal = false;
                    }
                    else if (block is IMyThrust)
                    {
                        block.ShowInTerminal = false;
                        if(block.CustomData.Contains(LAUNCH_TAG))
                        {
                            m_launchThrusters.Add(block as IMyThrust);
                        }
                        m_allThrusters.Add(block as IMyThrust);
                    }
                    else if(block is IMyGasTank && block.DetailedInfo.Contains("Hydrogen"))
                    {
                        m_fuelTanks.Add(block as IMyGasTank);
                    }
                    else if(block is IMyBatteryBlock)
                    {
                        m_batteries.Add(block as IMyBatteryBlock);
                    }
                    else if(block is IMyShipConnector && block.CustomData.Contains(DOCKING_TAG))
                    {
                        m_dockingConnector = block as IMyShipConnector;
                    }
                    else if(block is IMyFlightMovementBlock)
                    {
                        m_AIMoveBlock = block as IMyFlightMovementBlock;
                    }
                    else if(block is IMyBasicMissionBlock)
                    {
                        m_AIBasicWanderBlock = block as IMyBasicMissionBlock;
                    }
                    else if(block is IMyDefensiveCombatBlock)
                    {
                        m_AIDefenseBlock = block as IMyDefensiveCombatBlock;
                    }
                    else if(block is IMyPathRecorderBlock && block.CustomData.Contains(DOCKING_TAG))
                    {
                        m_AIDockingPathRecorderBlock = block as IMyPathRecorderBlock;
                    }
                }
            }
            m_ordersListener = IGC.RegisterBroadcastListener(ORDERS_CHANNEL);
            m_readyToLaunch = DoStatusReporting() == StatusReportAction.CAN_LAUNCH && m_dockingConnector.IsConnected;
        }

        private void LoadFromStorage()
        {
            if(string.IsNullOrWhiteSpace(Storage))
            {
                Save();
                return;
            }

            var values = Storage.Split(',');
            if(values.Length < 2)
            {
                Echo("Unexpected value from storage: " + Storage);
                Save();
                return;
            }
            m_recallTriggered = values[0].Length > 0;
            m_ticksSinceLaunch = int.Parse(values[1]);
        }

        private void RenameBlock(IMyTerminalBlock block)
        {
            if(DRONE_NAME == OLD_DRONE_NAME)
            {
                if(block.CustomName.StartsWith(DRONE_NAME) == false)
                {
                    block.CustomName = DRONE_NAME + " " + block.DisplayNameText;
                }
            }
            else
            {
                if(block.CustomName.StartsWith(OLD_DRONE_NAME))
                {
                    block.CustomName = block.CustomName.Replace(OLD_DRONE_NAME, DRONE_NAME);
                }
                else if(block.CustomName.StartsWith(DRONE_NAME) == false)
                {
                    block.CustomName = DRONE_NAME + " " + block.DisplayNameText;
                }
            }
        }

        public void Save()
        {
            Storage = m_recallTriggered ? "1" : "";
            Storage += string.Format(",{0}", m_ticksSinceLaunch);
        }

        bool m_recallOrderReceived = false;
        bool m_launchOrderReceived = false;
        void CheckForOrders()
        {
            m_recallOrderReceived = false;
            m_launchOrderReceived = false;
            while(m_ordersListener.HasPendingMessage)
            {
                var msg = m_ordersListener.AcceptMessage().As<string>();
                if(msg == RECALL_ORDER)
                    m_recallOrderReceived = true;
                else if(msg == LAUNCH_ORDER)
                    m_launchOrderReceived = true;
            }
        }
        void DumpDebug(string command)
        {
            if(string.IsNullOrWhiteSpace(command))
            {
                
                Echo(string.Format("Thrusters: {0}({1})", m_allThrusters.Count, m_launchThrusters.Count));
                Echo(string.Format("Batteries: {0}", m_batteries.Count));
                Echo(string.Format("Fuel Tanks: {0}", m_fuelTanks.Count));
                Echo(string.Format("Ammo Containers: {0}", m_cargoContainers.Count));

                if(m_dockingConnector == null)
                {
                    Echo("Docking Connector NOT FOUND");
                }
                if(m_recallTimerBlock == null)
                {
                    Echo("Recall Timer NOT FOUND");
                }
                
            }
        }
        public void Main(string argument, UpdateType updateSource)
        {
            if(string.IsNullOrWhiteSpace(argument) == false && argument.StartsWith("dd"))
                DumpDebug(argument.Substring(2).Trim());
            var statusReportAction = DoStatusReporting();
            CheckForOrders();
            if(m_dockingConnector.IsConnected == false)
            {
                m_launchOrderReceived = false;
                if(LaunchIsInProgress())
                {
                    ContinueLaunch();
                }
                else
                {
                    if(m_recallTriggered == false && m_recallOrderReceived)
                    {
                        TriggerRecall();
                    }
                    if(statusReportAction == StatusReportAction.RECALL && m_recallTriggered == false)
                    {
                        TriggerRecall();
                    }
                }
            }
            else
            {
                if(m_recallTriggered)
                {
                    SetBatteriesRechargeState(true);
                    SetFuelTankStockpile(true);
                    TurnOffThrusters();
                    TurnOffAIBlocks();
                    m_recallTriggered = false;
                }
                
                m_recallOrderReceived = false;
                m_readyToLaunch = statusReportAction == StatusReportAction.CAN_LAUNCH;
                if(m_readyToLaunch && m_launchOrderReceived)
                {
                    TriggerLaunch();
                }
            }
        }
        void TriggerLaunch()
        {
            m_ticksSinceLaunch = 0;
            SetBatteriesRechargeState(false);
            SetFuelTankStockpile(false);
            PrepareThrustersForLaunch();
            m_dockingConnector.Disconnect();
        }

        void TriggerRecall()
        {
            m_recallTriggered = true;
            if(m_recallTimerBlock != null)
            {
                m_recallTimerBlock.Trigger();
                return;
            }
            ResetThrusterOverrides();
            HandOverToRecallAI();
        }

        void TurnOffThrusters()
        {
            foreach(var thruster in m_allThrusters)
            {
                thruster.Enabled = false;
            }
        }

        void PrepareThrustersForLaunch()
        {
            foreach(var thruster in m_allThrusters)
            {
                if(thruster.CustomData.Contains(LAUNCH_TAG))
                {
                    thruster.ThrustOverridePercentage = LAUNCH_THRUST_OVERRIDE;
                    thruster.Enabled = true;
                }
                else
                {
                    thruster.Enabled = false;
                }
            }
        }

        void ResetThrusterOverrides(bool enabled = true)
        {
            foreach(var thruster in m_allThrusters)
            {
                thruster.ThrustOverridePercentage = 0;
                thruster.Enabled = enabled;
            }
        }

        void SetFuelTankStockpile(bool stockpileOn)
        {
            foreach(var tank in m_fuelTanks)
            {
                tank.Stockpile = stockpileOn;
            }
        }
        void SetBatteriesRechargeState(bool rechargeOn)
        {
            foreach(var battery in m_batteries)
            {
                battery.ChargeMode = rechargeOn ? ChargeMode.Recharge : ChargeMode.Auto;
            }
        }

        bool LaunchIsInProgress()
        {
            return m_ticksSinceLaunch >= 0;
        }
        void ContinueLaunch()
        {
            m_ticksSinceLaunch++;
            if(m_ticksSinceLaunch >= LAUNCH_TIME_IN_TICKS)
            {
                ResetThrusterOverrides();
                HandOverToAIBlocks();
                EnableWeaponsSystems();
                m_ticksSinceLaunch = int.MinValue;
            }
        }
        void HandOverToRecallAI()
        {
            m_AIMoveBlock.CollisionAvoidance = true;
            m_AIMoveBlock.PrecisionMode = false;
            m_AIMoveBlock.Enabled = true;
            m_AIMoveBlock.ApplyAction("ActivateBehavior_On");
            
            m_AIDefenseBlock.Enabled = false;
            m_AIDefenseBlock.ApplyAction("ActivateBehavior_Off");
            
            m_AIDockingPathRecorderBlock.Enabled = true;
            m_AIDockingPathRecorderBlock.ApplyAction("ActivateBehavior_On");
            m_AIDockingPathRecorderBlock.SetValueBool("ID_PLAY_CHECKBOX", true);

            m_AIBasicWanderBlock.Enabled = false;
            m_AIBasicWanderBlock.ApplyAction("ActivateBehavior_Off");
        }
        void HandOverToAIBlocks()
        {
            m_AIMoveBlock.CollisionAvoidance = true;
            m_AIMoveBlock.PrecisionMode = false;
            m_AIMoveBlock.Enabled = true;
            m_AIMoveBlock.ApplyAction("ActivateBehavior_On");
            
            m_AIDefenseBlock.Enabled = true;
            m_AIDefenseBlock.ApplyAction("ActivateBehavior_On");
            
            m_AIDockingPathRecorderBlock.Enabled = false;
            m_AIDockingPathRecorderBlock.ApplyAction("ActivateBehavior_Off");

            m_AIBasicWanderBlock.Enabled = true;
            m_AIBasicWanderBlock.ApplyAction("ActivateBehavior_On");
        }
        void TurnOffAIBlocks()
        {
            m_AIMoveBlock.ApplyAction("ActivateBehavior_Off");
            m_AIDefenseBlock.ApplyAction("ActivateBehavior_Off");
            m_AIDockingPathRecorderBlock.ApplyAction("ActivateBehavior_Off");
            m_AIBasicWanderBlock.ApplyAction("ActivateBehavior_Off");

        }
        void EnableWeaponsSystems()
        {
            //TODO
            //wibble
        }


        readonly List<IMyTerminalBlock> m_damagedBlocks = new List<IMyTerminalBlock>();
        StatusReportAction DoStatusReporting()
        {
            StatusReportAction retVal = StatusReportAction.AS_YOU_WERE;
            bool isDamaged = false;
            var tankLevel = GetFuelLevel();
            var batteryLevel = GetBatteryLevel();
            var ammoLevel = GetAmmoLevel();

            if(ShouldRecall(isDamaged, tankLevel, batteryLevel, ammoLevel))
            {
                retVal = StatusReportAction.RECALL;
            }
            else if(CanLaunch(isDamaged, tankLevel, batteryLevel, ammoLevel))
            {
                retVal = StatusReportAction.CAN_LAUNCH;
            }
            TransmitStatus(isDamaged, tankLevel, batteryLevel, ammoLevel, GetFlightStatus(retVal));
            return retVal;
        }

        bool ShouldRecall(bool isDamaged, double fuelLevel, float batteryLevel, float ammoLevel)
        {
            if(isDamaged)
                return true;
            if(fuelLevel <= LOW_FUEL_PERCENT)
                return true;
            if(batteryLevel <= LOW_BATTERY_PERCENT)
                return true;
            if(ammoLevel <= LOW_AMMO_PERCENT)
                return true;
            return false;
        }
        bool CanLaunch(bool isDamaged, double fuelLevel, float batteryLevel, float ammoLevel)
        {
            if(isDamaged == false && fuelLevel >= HIGH_FUEL_PERCENT && batteryLevel >= HIGH_BATTERY_PERCENT && ammoLevel <= HIGH_AMMO_PERCENT)
                return true;
            return false;
        }

        float GetBatteryLevel()
        {
            float totalMax = 0;
            float totalStored = 0;
            foreach(var battery in m_batteries)
            {
                totalMax += battery.MaxStoredPower;
                totalStored += battery.CurrentStoredPower;
            }
            return totalStored/totalMax;
        }

        float GetAmmoLevel()
        {
            float totalVolumeFillFactors = 0;
            foreach(var container in m_cargoContainers)
            {
                totalVolumeFillFactors += container.GetInventory().VolumeFillFactor;
            }
            return totalVolumeFillFactors/m_cargoContainers.Count;
        }

        enum FlightStatus
        {
            DOCKED_R,
            DOCKED_READY,
            LAUNCHING,
            CRUISING,
            RETURNING
        }

        FlightStatus GetFlightStatus(StatusReportAction statusAction)
        {
            if(m_dockingConnector.IsConnected)
            { 
                if(statusAction == StatusReportAction.CAN_LAUNCH)
                    return FlightStatus.DOCKED_READY;
                return FlightStatus.DOCKED_R;
            }
            if(LaunchIsInProgress()) return FlightStatus.LAUNCHING;
            if(m_recallTriggered) return FlightStatus.RETURNING;
            return FlightStatus.CRUISING;
        }

        string FlightStatusToDisplayString(FlightStatus status)
        {
            switch(status)
            {
                case FlightStatus.CRUISING: return "Cruising";
                case FlightStatus.DOCKED_R: return "Docked R&R";
                case FlightStatus.DOCKED_READY: return "Docked Ready";
                case FlightStatus.LAUNCHING: return "Launching";
                case FlightStatus.RETURNING: return "Returning";
                default: 
                    return string.Format("{0}", status);
            }
            
        }

        void TransmitStatus(bool isDamaged, double fuelLevel, double batteryLevel, double ammoLevel, FlightStatus flightStatus)
        {
            string data = string.Format("{0},{5},{1},{2:P2},{3:P2},{4:P2}", DRONE_NAME, isDamaged, fuelLevel, batteryLevel, ammoLevel, FlightStatusToDisplayString(flightStatus));
            IGC.SendBroadcastMessage(STATUS_REPORTING_CHANNEL, data);
        }

        double GetFuelLevel()
        {
            double total = 0;
            foreach(var tank in m_fuelTanks)
            {
                total += tank.FilledRatio;
            }
            return total / m_fuelTanks.Count;
        }
    }
}
