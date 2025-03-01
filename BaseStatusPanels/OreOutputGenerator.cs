using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        class OreOutputGenerator : OutputGeneratorBase
        {
            public OreOutputGenerator(IMyInventory storage, int page) : base (storage)
            {
                if(page == 1)
                {
                    title = " ORES (1/2) ";
                    typeLabels.Add(MyItemType.MakeOre("Iron"), "Fe");
                    typeLabels.Add(MyItemType.MakeOre("Nickel"), "Ni");
                    typeLabels.Add(MyItemType.MakeOre("Silicon"), "Si");
                    typeLabels.Add(MyItemType.MakeOre("Cobalt"), "Co");
                    typeLabels.Add(MyItemType.MakeOre("Gold"), "Au");
                    typeLabels.Add(MyItemType.MakeOre("Silver"), "Ag");
                }
                else
                {
                    title = " ORES (2/2) ";
                    typeLabels.Add(MyItemType.MakeOre("Magnesium"), "Mg");
                    typeLabels.Add(MyItemType.MakeOre("Platinum"), "Pt");
                    typeLabels.Add(MyItemType.MakeOre("Uranium"), "U");
                    typeLabels.Add(MyItemType.MakeOre("Stone"), "stn");
                    typeLabels.Add(MyItemType.MakeOre("Ice"), "ice");
                }
            }
            readonly string title;
            protected override string Title => title;
            protected override Dictionary<MyItemType, string> TypeLabels => typeLabels;
            readonly Dictionary<MyItemType, string> typeLabels = new Dictionary<MyItemType, string>();
        }
    }
}