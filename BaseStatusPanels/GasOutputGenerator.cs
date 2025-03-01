using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        class GasOutputGenerator : IOutputGenerator
        {
            public GasOutputGenerator()
            {
                title = " GAS & ICE ";
            }
            private readonly string title;
            public string GetStatusText()
            {
                return title;
            }
        }

    }
}