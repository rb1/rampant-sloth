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
        public Program()
        {
            destinationContainer = GridTerminalSystem.GetBlockWithName("Ore Storage Container") as IMyCargoContainer;
            destinationInventory = destinationContainer.GetInventory();
            refineryInputSorter = GridTerminalSystem.GetBlockWithName("Refinery Master Input Sorter") as IMyConveyorSorter;
            refineryFlushSorter = GridTerminalSystem.GetBlockWithName("Refinery Ore Flush Sorter") as IMyConveyorSorter;
            GridTerminalSystem.GetBlocksOfType(myRefineries, refinery => refinery.CustomData.Contains("FlushableRefinery"));
            
        }
        readonly IMyCargoContainer destinationContainer;
        readonly IMyInventory destinationInventory;
        readonly IMyConveyorSorter refineryInputSorter;
        readonly IMyConveyorSorter refineryFlushSorter;
        readonly List<IMyRefinery> myRefineries = new List<IMyRefinery>();
        readonly List<IMyInventory> oreSources = new List<IMyInventory>();
        readonly List<MyInventoryItem> ores = new List<MyInventoryItem>();

        public void Main(string argument, UpdateType updateSource)
        {
            if(destinationContainer == null)
            {
                Echo("Didn't find 'Ore Storage Container'.");
                return;                
            }

            if(refineryInputSorter == null)
            {
                Echo("Didn't find 'Refinery Master Input Sorter'.");
                return;
            }

            if(refineryFlushSorter == null)
            {
                Echo("Didn't find 'Refinery Ore Flush Sorter'.");
                return;
            }

            if(myRefineries.Count == 0)
            {
                Echo("Didn't find any Refineries with FlushableRefinery CustomData.");
                return;
            }


            oreSources.Clear();
            GetInventoriesWithOre(oreSources);

            if(oreSources.Count() == 0)
            {
                Echo("No ore found in refineries.");
                return;
            }

            SetSortersForFlush();
            TransferOresToDestination();
            SetSortersForNormalOperation();
        }

        void GetInventoriesWithOre(List<IMyInventory> ores)
        {
            foreach(var refinery in myRefineries)
            {
                if(refinery.InputInventory.ItemCount > 0)
                {
                    oreSources.Add(refinery.InputInventory);
                }
            }
        }

        void SetSortersForFlush()
        {
            if(refineryInputSorter.Enabled == false)
            {
                Echo("'Refinery Master Input Sorter' is disabled already.");
            }
            else
            {
                refineryInputSorter.Enabled = false;
            }


            if(refineryFlushSorter.Enabled)
            {
                Echo("'Refinery Ore Flush Sorter' is enabled already.");
            }
            else
            {
                refineryFlushSorter.Enabled = true;
            }

        }

        void TransferOresToDestination()
        {
            foreach(var oreSource in oreSources)
            {
                ores.Clear();
                oreSource.GetItems(ores);
                foreach(var ore in ores)
                {
                    destinationInventory.TransferItemFrom(oreSource, ore);
                }
            }
        }

        void SetSortersForNormalOperation()
        {
            refineryFlushSorter.Enabled = false;
            refineryInputSorter.Enabled = true;
        }
    }
}
