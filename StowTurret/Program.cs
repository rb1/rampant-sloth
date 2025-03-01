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
        readonly List<IMyTerminalBlock> turrets = new List<IMyTerminalBlock>();
        public Program()
        {
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(turrets);
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(updateSource != UpdateType.Trigger && updateSource != UpdateType.Terminal) return;
            foreach (var block in turrets)
            {
                var turret = block as IMyLargeTurretBase;
                if(turret == null) 
                {
                    continue;
                }
                ResetTurret(turret);
            }
        }
        void ResetTurret(IMyLargeTurretBase turret)
        {
            if( !turret.HasTarget )
            {
                turret.Elevation = 0;
                turret.Azimuth = 0;
            }
        }
    }
}
