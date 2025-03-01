using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        class ComponentOutputGenerator : OutputGeneratorBase
        {
            public ComponentOutputGenerator(IMyInventory storage) : base (storage)
            {
                title = "COMPONENTS";
            }
            readonly string title;
            protected override string Title => title;
            readonly Dictionary<MyItemType, string> typeLabels = new Dictionary<MyItemType, string>();
            protected override Dictionary<MyItemType, string> TypeLabels => typeLabels;
        }
    }
}