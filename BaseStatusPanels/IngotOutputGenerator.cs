using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Reflection;

namespace IngameScript
{
    partial class Program
    {
        class IngotOutputGenerator : OutputGeneratorBase
        {
            public IngotOutputGenerator(IMyInventory storage, int page) : base (storage)
            {
                if(page == 1)
                {
                    title = " INGOTS(1/2)";
                    typeLabels.Add(MyItemType.MakeIngot("Iron"), "Fe");
                    typeLabels.Add(MyItemType.MakeIngot("Nickel"), "Ni");
                    typeLabels.Add(MyItemType.MakeIngot("Silicon"), "Si");
                    typeLabels.Add(MyItemType.MakeIngot("Cobalt"), "Co");
                    typeLabels.Add(MyItemType.MakeIngot("Gold"), "Au");
                }
                else
                {
                    title = " INGOTS(2/2)";
                    typeLabels.Add(MyItemType.MakeIngot("Silver"), "Ag");
                    typeLabels.Add(MyItemType.MakeIngot("Magnesium"), "Mg");
                    typeLabels.Add(MyItemType.MakeIngot("Platinum"), "Pt");
                    typeLabels.Add(MyItemType.MakeIngot("Uranium"), "U");
                    typeLabels.Add(MyItemType.MakeIngot("Stone"), "stn");
                }
            }
            readonly string title;
            protected override string Title => title;
            readonly Dictionary<MyItemType, string> typeLabels = new Dictionary<MyItemType, string>();
            protected override Dictionary<MyItemType, string> TypeLabels => typeLabels;
        }

    }
}