using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    partial class Program : MyGridProgram
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
        static readonly string FIND_STR = "Airtight Hangar Door";
        static readonly string REPLACE_WITH_STR = "Drone Bay Alpha Hangar Door";
        readonly List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo(string.Format("Replacing block names containing '{0}' with '{1}'.", FIND_STR, REPLACE_WITH_STR));
            
            GridTerminalSystem.GetBlocks(blockList);
            foreach(var block in blockList)
            {
                block.CustomName = block.CustomName.Replace(FIND_STR, REPLACE_WITH_STR);
            }
        }
    }
}
