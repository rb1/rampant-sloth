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

        List<MyDetectedEntityInfo> m_detectedEntities;
        List<ICamera> m_cameras;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            m_cameras = DetectCameras();
            m_detectedEntities = new List<MyDetectedEntityInfo>();
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

        }

        // interface IWeapon
        // {
        //     string DisplayName {get;}
        //     bool IsOn{get;}
        //     bool IsDamaged{get;}
        //     float AmmoLevel{get;}
        // }
        // interface ITurret
        // {
        //     IWeapon[] Weapons{get;}
        //     bool IsAIOn {get;}
        //     bool WeaponsDamaged {get;}
        //     bool CameraDamaged{get;}
        //     bool RotorDamaged{get;}
        //     bool HingeDamaged{get;}


        // }

        // void UpdateWeaponStates()
        // {

        // }

        List<ICamera> DetectCameras()
        {
            var retVal = new List<ICamera>();
            throw new NotImplementedException();
        }

        interface ICamera
        {
            void ConfigureForScanning();
            bool DoScan(List<MyDetectedEntityInfo> detectedEntities);
            double ScanDistance {get; set;}
        }
        void UpdateRadarResults(List<ICamera> cameras, double scanDistance, List<MyDetectedEntityInfo> detectedEntities)
        {
            detectedEntities.Clear();
            foreach(var camera in cameras)
            {
                camera.ScanDistance = scanDistance;
                camera.DoScan(detectedEntities);
            }
        }



        class Camera : ICamera
        {
            public Camera(IMyCameraBlock camera, double scanDistance)
            {
                this.m_camera = camera;
                ScanDistance = scanDistance;
            }
            public double ScanDistance 
            {
                get { return m_scanDistance; }
                set{ m_scanDistance = Math.Min(value, m_camera.RaycastDistanceLimit); }
            }

            public void ConfigureForScanning()
            {
                m_camera.Enabled = true;
                m_camera.EnableRaycast = true;
            }

            public bool DoScan(List<MyDetectedEntityInfo> detectedEntities)
            {

                throw new NotImplementedException();
            }

            readonly IMyCameraBlock m_camera;
            double m_scanDistance;

            float m_yaw = 0;
            float m_pitch = 0;
            short m_quadrant = 0;

        }

    }
}
