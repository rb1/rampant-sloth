using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders.Voxels;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This script should be triggered by an event controller that fires when the rotors have completed
        // 1 lap (there and back again).
        // It will then retract the vertical pistons.
        // The script should then be triggered with the argument "resume" from an event block that triggers when
        // all the vertical pistons are retracted.

        public Program()
        {
            if(int.TryParse(Storage, out cycleCount) == false)
            {
                cycleCount = 0;
                Storage = string.Format("{0}", cycleCount);
            }
            var blockGroup = GridTerminalSystem.GetBlockGroupWithName(KnownBlockNames.FirstBase.Mine2.VerticalPistonsGroup);
            blockGroup.GetBlocksOfType(verticalPistons);
            blockGroup = GridTerminalSystem.GetBlockGroupWithName(KnownBlockNames.FirstBase.Mine2.HorizontalPistonsGroup);
            blockGroup.GetBlocksOfType(horizontalPistons);
            blockGroup = GridTerminalSystem.GetBlockGroupWithName(KnownBlockNames.FirstBase.Mine2.AllRotors);
            blockGroup.GetBlocksOfType(rotors);
            blockGroup = GridTerminalSystem.GetBlockGroupWithName(KnownBlockNames.FirstBase.Mine2.Drills);
            blockGroup.GetBlocksOfType(drills);
        }
        int cycleCount = 0;
        readonly List<IMyPistonBase> verticalPistons = new List<IMyPistonBase>();
        readonly List<IMyPistonBase> horizontalPistons = new List<IMyPistonBase>();
        readonly List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();
        readonly List<IMyShipDrill> drills = new List<IMyShipDrill>();

        public void Save()
        {
            Storage = string.Format("{0}", cycleCount);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(updateSource.HasFlag(UpdateType.Trigger) == false)
            {
                Echo("Unexpected update, ignoring.");
                return;
            }

            ReverseRotors();
            
            if(argument.ToLower() == "resume")
            {
                FinishResetAndResume();
                return;
            }

            if(TimeToAct() == false)
            {
                return;
            }

            if(CanExtendVertically())
            {
                ExtendVertically();
            }
            else
            {
                StartReset();
            }
        }


        private bool TimeToAct()
        {
            if(cycleCount < 4)
            {
                ++cycleCount;
                return false;
            }
            cycleCount = 0;
            return true;
        }

        bool CanExtendVertically()
        {
            foreach(var piston in verticalPistons)
             if(piston.MaxLimit < piston.HighestPosition)
                return true;
            return false;
        }
        bool AreAllVerticalPistonsFullyRetracted()
        {
            foreach(var piston in verticalPistons)
                if(piston.MaxLimit != piston.MinLimit)
                    return false;
            return true;
        }

        void ReverseRotors()
        {
            foreach(var rotor in rotors)
            {
                rotor.TargetVelocityRPM = rotor.TargetVelocityRPM *-1f;
            }
        }

        void ExtendVertically()
        {
            float extensionAmount = 2f/verticalPistons.Count;
            foreach(var piston in verticalPistons)
            {
                piston.MaxLimit += extensionAmount;
            }
        }
        void ExtendHorizontally()
        {
            float extensionAmount = 2.5f/horizontalPistons.Count;
            foreach(var piston in horizontalPistons)
            {
                piston.MaxLimit += extensionAmount;
            }
        }

        void RetractVertically()
        {
            foreach(var piston in verticalPistons)
            {
                var oldVelocity = piston.Velocity;
                piston.Velocity = piston.MaxVelocity * -1;
                piston.MaxLimit = piston.MinLimit;

            }
        }

        void LockRotors()
        {
            foreach(var rotor in rotors)
            {
                rotor.RotorLock = true;
            }
        }

        void UnlockRotors()
        {
            foreach(var rotor in rotors)
            {
                rotor.RotorLock = false;
            }
        }

        void StopDrills()
        {
            foreach(var drill in drills)
            {
                drill.Enabled = false;
            }
        }

        void StartDrills()
        {
            foreach(var drill in drills)
            {
                drill.Enabled = true;
            }
        }

        void StartReset()
        {
            LockRotors();
            StopDrills();
            RetractVertically();
        }
        void FinishResetAndResume()
        {
            if(AreAllVerticalPistonsFullyRetracted() == false)
            {
                Echo("Resume requested, but the vertical pistons are not all fully retracted.");
                return;
            }
            ExtendHorizontally();
            StartDrills();
            UnlockRotors();
        }
    }
}
