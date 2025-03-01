using Sandbox.Game.EntityComponents;
using Sandbox.Game.GUI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
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
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        [Flags] enum OutputType 
        {
            OreLevelsP1 = 1,
            OreLevelsP2 = 2,
            IngotLevelsP1 = 4,
            IngotLevelsP2 = 8,
            GasLevels = 16,
            ComponentLevels = 32
        }
       

        public Program()
        {
            Echo("Constructing");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            List<IMyTextPanel> tmpPanels = new List<IMyTextPanel>();

            GridTerminalSystem.GetBlocksOfType(tmpPanels, panel => panel.CustomData.Contains("StatusPanel") );
            statusPanels = tmpPanels.Select(x => new OutputPanel(x));

            var generators = new Dictionary<OutputType, IOutputGenerator>();
            generators.Add(OutputType.OreLevelsP1, new OreOutputGenerator((GridTerminalSystem.GetBlockWithName("Ore Storage Container") as IMyCargoContainer).GetInventory(), 1));
            generators.Add(OutputType.OreLevelsP2, new OreOutputGenerator((GridTerminalSystem.GetBlockWithName("Ore Storage Container") as IMyCargoContainer).GetInventory(), 2));
            generators.Add(OutputType.ComponentLevels, new ComponentOutputGenerator((GridTerminalSystem.GetBlockWithName("Component Storage Container") as IMyCargoContainer).GetInventory()));
            generators.Add(OutputType.IngotLevelsP1, new IngotOutputGenerator((GridTerminalSystem.GetBlockWithName("Ingot Storage Container") as IMyCargoContainer).GetInventory(), 1));
            generators.Add(OutputType.IngotLevelsP2, new IngotOutputGenerator((GridTerminalSystem.GetBlockWithName("Ingot Storage Container") as IMyCargoContainer).GetInventory(), 2));
            generators.Add(OutputType.GasLevels, new GasOutputGenerator());

            outputCache = new OutputCache(generators);
        }
        readonly IEnumerable<OutputPanel> statusPanels;
        readonly OutputCache outputCache;
        
        public void Main(string argument, UpdateType updateSource)
        {
            outputCache.ClearCache();
            foreach(var panel in statusPanels)
            {
                if(updateSource.HasFlag(UpdateType.Update100))
                {
                    panel.MoveToNextOutput();
                }
                panel.Block.WriteText(outputCache.GetOutput(panel.CurrentOutput));
            }
            counter++;
            
        }
        int counter = 0;
    }
}
