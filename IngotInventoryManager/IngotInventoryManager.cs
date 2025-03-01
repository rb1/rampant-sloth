using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            
            if(int.TryParse(Storage, out itemsPerLCDLine) == false)
            {
                itemsPerLCDLine = 2;
                Storage = "2";
            }

            refineryInputSorter = GridTerminalSystem.GetBlockWithName("Refinery Master Input Sorter") as IMyConveyorSorter;
            ingotContainer = GridTerminalSystem.GetBlockWithName("Ingot Storage Container") as IMyCargoContainer;

            var ingot = MakeIngot(10000, 15000, "Iron", "Fe");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            stoneOres[0] = ingot.oreFilter;
            ingot = MakeIngot(500, 1000, "Silicon", "Si");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            stoneOres[1] = ingot.oreFilter;
            ingot = MakeIngot(10000, 12500, "Nickel", "Ni");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            stoneOres[2] = ingot.oreFilter;

            stoneOreFilter = new MyInventoryItemFilter("MyObjectBuilder_Ore/Stone");

            ingot = MakeIngot(250, 500, "Cobalt", "Co");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            ingot = MakeIngot(500, 1000, "Silver", "Ag");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            ingot = MakeIngot(500, 1000, "Gold", "Au");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            ingot = MakeIngot(500, 1000, "Platinum", "Pt");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            ingot = MakeIngot(500, 1000, "Uranium", "U ");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);
            ingot = MakeIngot(500, 1000, "Magnesium", "Mg");
            ingotsByTypeId.Add(ingot.ingotItemType, ingot);

            thisBlock = GridTerminalSystem.GetBlockWithName(Me.CustomName) as IMyProgrammableBlock;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        readonly int itemsPerLCDLine = 2;
        readonly IMyProgrammableBlock thisBlock;

        readonly IMyConveyorSorter refineryInputSorter;
        readonly IMyCargoContainer ingotContainer;
        readonly List<MyInventoryItem> ingots = new List<MyInventoryItem>();
        readonly List<MyInventoryItemFilter> oresToAllow = new List<MyInventoryItemFilter>();
        readonly Dictionary<MyItemType, Ingot> ingotsByTypeId = new Dictionary<MyItemType, Ingot>();

        readonly MyInventoryItemFilter[] stoneOres = new MyInventoryItemFilter[3];
        readonly MyInventoryItemFilter stoneOreFilter;
        
        public void Save()
        {
            Storage = string.Format("{0}", itemsPerLCDLine);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(ingotContainer == null)
            {
                Echo("Didn't find 'Ingot Storage Container'.");
                return;
            }
            if(refineryInputSorter == null)
            {
                Echo("Didn't find 'Refinery Master Input Sorter'.");
                return;
            }

            
            ingots.Clear();
            ingotContainer.GetInventory().GetItems(ingots, ingot => ingotsByTypeId.ContainsKey(ingot.Type));
            oresToAllow.Clear();
            refineryInputSorter.GetFilterList(oresToAllow);
            foreach(var ingot in ingots)
            {
                //Echo(String.Format("Type: {0}, SubType: {1}", ingot.Type.TypeId, ingot.Type.SubtypeId));
                var ingotData = ingotsByTypeId[ingot.Type];
                if(ingot.Amount > ingotData.highCount && oresToAllow.Contains(ingotData.oreFilter))
                {
                    oresToAllow.Remove(ingotData.oreFilter);
                }
                else if(ingot.Amount < ingotData.lowCount && (oresToAllow.Contains(ingotData.oreFilter) == false))
                {
                    oresToAllow.Add(ingotData.oreFilter);
                }
            }

            if(oresToAllow.Intersect(stoneOres).Count() > 0)
            {
                oresToAllow.Add(stoneOreFilter);
            }
            else
            {
                oresToAllow.Remove(stoneOreFilter);
            }

            refineryInputSorter.SetFilter(MyConveyorSorterMode.Whitelist, oresToAllow);
            UpdateLCD();
        }
        void UpdateLCD()
        {
            var surface1 = thisBlock.GetSurface(0);
            string statusText = "";
            byte count = 0;
            foreach(var ingot in ingotsByTypeId.Values)
            {
                statusText += string.Format("{0}:\t{1}  ", ingot.lcdLabel, oresToAllow.Contains(ingot.oreFilter)?"On ":"Off");
                count++;
                if(count%itemsPerLCDLine==0)
                {
                    statusText += Environment.NewLine;
                }
            }
            surface1.WriteText(statusText);
        }
    }
}
