using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GUI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyTurretControlBlock = SpaceEngineers.Game.ModAPI.Ingame.IMyTurretControlBlock;
using Vector2 = VRageMath.Vector2;
using RectangleF = VRageMath.RectangleF;
using System.Runtime.Remoting.Messaging;
using System.Runtime.InteropServices;
using VRageRender;
using EmptyKeys.UserInterface.Generated.DataTemplatesStoreBlock_Bindings;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        internal static class Logger
        {
            public static void Log(string formatStr, params object[] args)
            {
                LogFn?.Invoke(string.Format(formatStr, args));
            }
            public static Action<string> LogFn = null;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Logger.LogFn = Echo;

            var turretControlBlocks = new List<IMyTurretControlBlock>();
            var groups = new List<TurretGroupPanelManager>{new TurretGroupPanelManager("Port Lateral"), new TurretGroupPanelManager("Starboard Lateral"), new TurretGroupPanelManager("Tail")};
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            foreach(var block in blocks)
            {
                for(int groupIndex = 0; groupIndex < groups.Count; ++groupIndex)
                {
                    if(block.DisplayNameText.ToLower().StartsWith(groups[groupIndex].Prefix.ToLower()))
                    {
                        var group = groups[groupIndex];
                        groupIndex = groups.Count;

                        if(block is IMyTurretControlBlock)
                        {
                            //Logger.Log("Found TCB '{0}'", block.DisplayNameText);
                            group.AddTurret(block as IMyTurretControlBlock);
                            continue;
                        }
                        if(block is IMyTextPanel)
                        {
                            if (block.DisplayNameText.ToLower().Contains("turret control"))
                            {
                                //Logger.Log("Found Main Panel '{0}'", block.DisplayNameText);
                                group.SetOutputPanel(block as IMyTextPanel);
                                continue;
                            }
                        }
                        if(block is IMyCockpit)
                        {
                            //Logger.Log("Found Control Seat '{0}'", block.DisplayNameText);
                            group.SetControlSeat(block as IMyCockpit);
                        }
                    }
                }
            }
            m_groups = groups;
        }


        class TurretGun
        {
            public TurretGun(IMySmallGatlingGun gunBlock)
            {
                m_gun = gunBlock;
            }
            readonly IMySmallGatlingGun m_gun;
            static readonly MyItemType m_ammoType = MyItemType.MakeAmmo("AutocannonClip");
            public string DisplayNameText => m_gun.DisplayNameText;
            public float AmmoCount => (float)m_gun.GetInventory().GetItemAmount(m_ammoType);
            public int MaxAmmoCount => 3;
            public enum GunState { Firing, Ready, Hacked, Offline };
            public GunState Status 
            {
                get
                {
                    if(m_gun.IsShooting) return GunState.Firing;
                    if(m_gun.IsWorking) return GunState.Ready;
                    if(m_gun.IsBeingHacked) return GunState.Hacked;
                        return GunState.Offline;
                }
            }

        }


        class MiniCannonTurret
        {
            public MiniCannonTurret(IMyTurretControlBlock controlBlock)
            {
                var tools = new List<IMyFunctionalBlock> ();
                var guns = new List<TurretGun>();
                controlBlock.GetTools(tools);
                foreach(var weapon in tools)
                {
                    if(weapon is IMySmallGatlingGun)
                    {
                        guns.Add(new TurretGun(weapon as IMySmallGatlingGun));
                    }
                }
                ControlBlock =  controlBlock;
                Guns = guns;
                Number = GetNumberFromString(controlBlock.DisplayNameText);
            }
            public readonly IMyTurretControlBlock ControlBlock;
            public IReadOnlyList<TurretGun> Guns;
            public readonly int Number;
            public enum AIState { Enabled, Manual, Offline};
            public AIState AIStatus 
            {   get
                {
                    if(!ControlBlock.Enabled) return AIState.Offline;
                    if(ControlBlock.IsUnderControl) return AIState.Manual;
                    if(ControlBlock.AIEnabled)return AIState.Enabled;
                    return AIState.Offline;
                }
            }
            public bool IsAnyAmmoLow
            {
                get
                {
                    foreach (var gun in Guns)
                    {
                        if(gun.AmmoCount < gun.MaxAmmoCount/3) return true;
                    }
                    return false;
                }
            }

            public int GetAmmoLowGunNumber()
            {
                foreach(var gun in Guns)
                {
                    if(gun.AmmoCount < gun.MaxAmmoCount/3) return GetNumberFromString(gun.DisplayNameText);
                }
                return 0;
            }
            public int GetFiringGunNumber()
            {
                foreach(var gun in Guns)
                {
                    if(gun.Status == TurretGun.GunState.Firing) return GetNumberFromString(gun.DisplayNameText);
                }
                return 0;
            }
            public int GetOfflineGunNumber()
            {
                foreach(var gun in Guns)
                {
                    if(gun.Status == TurretGun.GunState.Offline) return GetNumberFromString(gun.DisplayNameText);
                }
                return 0;
            }
        }


        class TurretGroupStatus
        {
            public TurretGroupStatus(string prefix, IReadOnlyList<MiniCannonTurret> turrets)
            {
                GroupPrefix = prefix;
                m_turrets = turrets;
                OverallStatus = OverallState.OK;
                AmmoStatus = AmmoState.OK;
                OnlineOfflineStatus = OnlineOfflineState.Online;
                
            }
            public enum OverallState { OK, Warning, Offline, AllOffline}
            public enum AmmoState { OK, LowOrEmpty }
            public enum OnlineOfflineState { Online, PartiallyOffline, AllOffline }
            readonly string GroupPrefix;
            readonly IReadOnlyList<MiniCannonTurret> m_turrets;
            public string OnlineOfflineStatusMessage { get; private set; } 
            public string AmmoStatusMessage { get; private set; }
            public OverallState OverallStatus { get; private set; }
            public AmmoState AmmoStatus {get; private set;}
            public OnlineOfflineState OnlineOfflineStatus {get; private set;}
            public void Update()
            {
                uint offlineCount = 0;
                uint lowAmmoCount = 0;
                OverallStatus = OverallState.OK;
                foreach (var turret in m_turrets)
                {
                    if (turret.AIStatus == MiniCannonTurret.AIState.Offline)
                    {
                        OverallStatus = OverallState.Offline;
                        ++offlineCount;
                    }
                    else if ((OverallStatus != OverallState.Offline || OverallStatus != OverallState.AllOffline) && turret.IsAnyAmmoLow)
                    {
                        OverallStatus = OverallState.Warning;
                        ++lowAmmoCount;
                    }
                }
                if (offlineCount == m_turrets.Count)
                {
                    OverallStatus = OverallState.AllOffline;
                }
                
                OnlineOfflineStatusMessage = GetOfflineCountStatusMessage(offlineCount);
                UpdateOnlineOfflineStatus(offlineCount);
                AmmoStatusMessage = GetAmmoStatusMessage(lowAmmoCount);
                UpdateAmmoStatus(lowAmmoCount);
            }
            private void UpdateAmmoStatus(uint lowAmmoCount)
            {
                if (lowAmmoCount > 0)
                {
                    AmmoStatus = AmmoState.LowOrEmpty;
                }
                else
                {
                    AmmoStatus = AmmoState.OK;
                }
            }
            private void UpdateOnlineOfflineStatus(uint offlineCount)
            {
                if (offlineCount == m_turrets.Count)
                {
                    OnlineOfflineStatus = OnlineOfflineState.AllOffline;
                }
                else if (offlineCount > 0)
                {
                    OnlineOfflineStatus = OnlineOfflineState.PartiallyOffline;
                }
                else
                {
                    OnlineOfflineStatus = OnlineOfflineState.Online;
                }
            }
            private string GetAmmoStatusMessage(uint lowAmmoCount)
            {
                if (lowAmmoCount == m_turrets.Count())
                {
                    return "All ammo low";
                }
                if (lowAmmoCount > 0)
                {
                    return string.Format("{0} ammo low", lowAmmoCount);
                }
                return "Ammo OK";
            }

            private string GetOfflineCountStatusMessage(uint offlineCount)
            {
                if (offlineCount == m_turrets.Count)
                {
                    OverallStatus = OverallState.AllOffline;
                    return "All turrets offline";
                }
                else if (offlineCount > 0)
                {
                    return string.Format("{0} turrets offline", offlineCount);
                }
                return "All turrets online";
            }

        }


        class MiniCannonTurretPanelManager
        {
            public MiniCannonTurretPanelManager(MiniCannonTurret turret, string groupPrefix)
            {
                Turret = turret;
                m_mainSurface = (turret.ControlBlock as IMyTextSurfaceProvider).GetSurface(0);
                
                m_topRightSurface = (turret.ControlBlock as IMyTextSurfaceProvider).GetSurface(1);
                m_bottomLeftSurface =(turret.ControlBlock as IMyTextSurfaceProvider).GetSurface(2);
                m_bottomRightSurface =(turret.ControlBlock as IMyTextSurfaceProvider).GetSurface(3);

                displayNameLines = groupPrefix + Environment.NewLine + "Turret #" + string.Format("{0}", Turret.Number);
                
                ConfigureSurfaceForTextOutput(m_mainSurface);
                m_mainSurface.WriteText(displayNameLines);

                ConfigureSurfaceForTextOutput(m_topRightSurface);
                m_topRightSurface.Alignment = TextAlignment.CENTER;
                m_topRightSurface.FontSize = 10;
                m_topRightSurface.Font = "Monospace";
                ConfigureSurfaceForTextOutput(m_bottomRightSurface);
                m_bottomRightSurface.Alignment = TextAlignment.CENTER;
                m_bottomRightSurface.FontSize = 10;
                m_bottomRightSurface.Font = "Monospace";
                m_bottomRightSurface.FontColor = Color.Black;
                ConfigureSurfaceForSpriteOutput(m_bottomLeftSurface);
                
            }
            public void UpdatePanels()
            {
                var gunStates = new SortedDictionary<int, string>();
                foreach(var gun in Turret.Guns)
                {
                    var gunId = GetNumberFromString(gun.DisplayNameText);
                    gunStates[gunId] = string.Format("Gun #{0}: {1} {2}", gunId, GetGunAmmoStatus(gun), GetGunStatusText(gun));
                }
                m_mainSurface.WriteText(string.Format("{1} {3}{0}{2}", Environment.NewLine, displayNameLines,
                                        string.Join(Environment.NewLine, gunStates.Values), GetAIStatusText()));
                
                
                m_topRightSurface.FontColor = GetTopRightPanelColor();
                m_topRightSurface.WriteText(GetAIStatusText());
                string gunNumber;
                m_bottomRightSurface.BackgroundColor = GetBottomRightColorAndText(out gunNumber);
                m_bottomRightSurface.WriteText(gunNumber);
            }
            Color GetTopRightPanelColor()
            {
                switch(Turret.AIStatus)
                {
                    case MiniCannonTurret.AIState.Enabled: return Color.Green;
                    case MiniCannonTurret.AIState.Offline: return Color.Red;
                    case MiniCannonTurret.AIState.Manual: return Color.Orange;
                    default: return Color.Black;
                }
            }
            Color GetBottomRightColorAndText(out string displayText)
            {
                var gunNumber = Turret.GetFiringGunNumber();
                if(gunNumber > 0)
                {
                    displayText = string.Format("{0}", gunNumber);
                    return Color.BlueViolet;
                }
                gunNumber = Turret.GetOfflineGunNumber();
                if(gunNumber > 0)
                {
                    displayText = string.Format("{0}", gunNumber);
                    return Color.Red;
                }
                gunNumber = Turret.GetAmmoLowGunNumber();
                if(gunNumber > 0)
                {
                    displayText = string.Format("{0}", gunNumber);
                    return Color.Orange;
                }
                displayText = "";
                return Color.Green;
            }
            public string GetAIStatusText()
            {
                switch(Turret.AIStatus)
                {
                    case MiniCannonTurret.AIState.Enabled: return "A";
                    case MiniCannonTurret.AIState.Manual: return "M";
                    default: return "U";
                }
            }


            string GetGunStatusText(TurretGun gun)
            {
                switch(gun.Status)
                {
                    case TurretGun.GunState.Firing: return "Firing";
                    case TurretGun.GunState.Ready: return "Ready";
                    case TurretGun.GunState.Hacked: return "Hacked";
                    case TurretGun.GunState.Offline: return "Offline";
                    default: return "Unknown";
                }
            }

            string GetGunAmmoStatus(TurretGun gun)
            {
                string ammoIndicator = new string('l', (int)gun.AmmoCount).PadRight(gun.MaxAmmoCount, ' ');
                return string.Format("{0}/{1} {2}", (int)gun.AmmoCount , gun.MaxAmmoCount, ammoIndicator);
            }


            private readonly string displayNameLines;
            public readonly MiniCannonTurret Turret;
            readonly IMyTextSurface m_mainSurface;
            readonly List<MySprite> m_mainSurfaceSprites = new List<MySprite>();
            readonly IMyTextSurface m_topRightSurface;
            readonly IMyTextSurface m_bottomRightSurface;
            readonly IMyTextSurface m_bottomLeftSurface;
        }


        class TurretGroupPanelManager
        {
            public TurretGroupPanelManager(string prefix)
            {
                Prefix = prefix;
                m_turretPanelManagers = new List<MiniCannonTurretPanelManager>();
                m_turrets = new List<MiniCannonTurret>();
                m_turretGroupStatus = new TurretGroupStatus(Prefix, m_turrets);
            }
            public readonly string Prefix;
            readonly TurretGroupStatus m_turretGroupStatus;
            public void AddTurret(IMyTurretControlBlock turretBlock)
            {
                var turret = new MiniCannonTurret(turretBlock);
                m_turretPanelManagers.Add(new MiniCannonTurretPanelManager(turret, Prefix));
                m_turrets.Add(turret);
            }
            readonly List<MiniCannonTurretPanelManager> m_turretPanelManagers;
            readonly List<MiniCannonTurret> m_turrets;
            public void SetOutputPanel(IMyTextPanel panel)
            {
                m_mainPanelSurface = new GroupOutputPanel((panel as IMyTextSurfaceProvider).GetSurface(0), m_turretGroupStatus);
            }
            SpritePanel m_mainPanelSurface;
            IMyTextSurface  m_controlSeatMainSurface;

            public void SetControlSeat(IMyCockpit controlSeat)
            {
                var provider = controlSeat as IMyTextSurfaceProvider;
                if(provider == null) return;
                m_controlSeatMainSurface = provider.GetSurface(0);
                ConfigureSurfaceForTextOutput(m_controlSeatMainSurface);
            }

            public void UpdatePanels()
            {
                UpdateControlSeatPanel();
                foreach(var turretPanels in m_turretPanelManagers)
                {
                    turretPanels.UpdatePanels();
                }
                m_mainPanelSurface.UpdatePanel();

            }
            void UpdateControlSeatPanel()
            {
                if(m_controlSeatMainSurface == null) return;

                m_controlSeatMainSurface.WriteText(string.Format("{1}{0}Turret Control{0}Station", Environment.NewLine, Prefix));
                
            }
        }


        class GroupOutputPanel : SpritePanel
        {
            public GroupOutputPanel(IMyTextSurface surface, TurretGroupStatus groupStatus) : base(surface)
            {
                m_groupStatus = groupStatus;
                ConfigureSurfaceForSpriteOutput(surface);
                m_statusIconSprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "MyObjectBuilder_AmmoMagazine/AutocannonClip",
                    Size = ViewportSize/3,
                    Color = Color.Green                    
                };
                m_statusIconSpritePosition = new Vector2((SurfaceCenter.X*2) - m_statusIconSprite.Size.Value.X, m_statusIconSprite.Size.Value.Y/2);
                m_statusMessageSprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = m_groupStatus.OnlineOfflineStatusMessage,
                    FontId = "Debug",
                    Alignment = TextAlignment.LEFT,
                    Color = OverallStatusColor,
                    RotationOrScale = 2
                };
                m_statusMesssageSpritePosition = new Vector2(0, m_statusIconSprite.Size.Value.Y);
                m_ammoStatusMessageSprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = m_groupStatus.AmmoStatusMessage,
                    FontId = "Debug",
                    Alignment = TextAlignment.LEFT,
                    Color = AmmoStatusColor,
                    RotationOrScale = 2
                };
                m_ammoStatusMessageSpritePosition = new Vector2(0, m_statusIconSprite.Size.Value.Y * 2);
            }
            readonly TurretGroupStatus m_groupStatus;
            MySprite m_statusIconSprite;
            readonly Vector2 m_statusIconSpritePosition;
            MySprite m_statusMessageSprite;
            readonly Vector2 m_statusMesssageSpritePosition;
            MySprite m_ammoStatusMessageSprite;
            readonly Vector2 m_ammoStatusMessageSpritePosition;
            protected override void DrawSprites(MySpriteDrawFrame frame)
            {
                m_groupStatus.Update();
                UpdateSpriteColors();
                UpdateTextSprites();
                DrawSprite(m_statusIconSprite, frame, m_statusIconSpritePosition);
                DrawSprite(m_statusMessageSprite, frame, m_statusMesssageSpritePosition);
                DrawSprite(m_ammoStatusMessageSprite, frame, m_ammoStatusMessageSpritePosition);
            }
            void UpdateTextSprites()
            {
                m_statusMessageSprite.Data = m_groupStatus.OnlineOfflineStatusMessage;
                m_ammoStatusMessageSprite.Data = m_groupStatus.AmmoStatusMessage;
            }
            void UpdateSpriteColors()
            {
                m_statusIconSprite.Color = OverallStatusColor;
                m_statusMessageSprite.Color = OnlineOfflineStatusColor;
                m_ammoStatusMessageSprite.Color = AmmoStatusColor;
            }
            Color OnlineOfflineStatusColor
            {
                get 
                {
                    switch (m_groupStatus.OnlineOfflineStatus)
                    {
                        case TurretGroupStatus.OnlineOfflineState.AllOffline: return Color.Red;
                        case TurretGroupStatus.OnlineOfflineState.PartiallyOffline: return Color.Orange;
                        case TurretGroupStatus.OnlineOfflineState.Online: return Color.Green;
                        default: 
                            Logger.Log("Unexpected OnlineOfflineStatus: {0}", m_groupStatus.OnlineOfflineStatus);
                            return Color.White;                       
                        
                    }
                }
            }
            Color OverallStatusColor 
            {
                get 
                {
                    switch(m_groupStatus.OverallStatus)
                    {
                        case TurretGroupStatus.OverallState.Offline: return Color.DarkOrange;
                        case TurretGroupStatus.OverallState.OK: return Color.Green;
                        case TurretGroupStatus.OverallState.Warning: return Color.Orange;
                        case TurretGroupStatus.OverallState.AllOffline: return Color.Red;
                        default: return Color.White;
                    }
                }
            }
            Color AmmoStatusColor
            {
                get
                {
                    if(m_groupStatus.AmmoStatus ==  TurretGroupStatus.AmmoState.LowOrEmpty) return Color.OrangeRed;
                    return Color.Green;
                }
            }

        }


        abstract class SpritePanel
        {
            public SpritePanel(IMyTextSurface surface)
            {
                m_surface = surface;
                ConfigureSurfaceForSpriteOutput(m_surface);
                m_viewport = new RectangleF((m_surface.TextureSize - m_surface.SurfaceSize)/2,
                                            m_surface.SurfaceSize);
            }
            protected void DrawSprite(MySprite sprite, MySpriteDrawFrame frame, Vector2 position)
            {
                sprite.Position = GetViewportAdjustedPosition(position);
                frame.Add(sprite);
            }
            protected Vector2 GetViewportAdjustedPosition(Vector2 position)
            {
                return position + m_viewport.Position;
            }
            protected Vector2 SurfaceCenter => m_surface.SurfaceSize/2;
            
            protected abstract void DrawSprites(MySpriteDrawFrame frame);
            public void UpdatePanel()
            {
                using(var frame = m_surface.DrawFrame())
                {
                    DrawSprites(frame);
                }
            }
            
            public Vector2 ViewportSize => m_viewport.Size;
            readonly RectangleF m_viewport;
            readonly IMyTextSurface m_surface;
        }


        readonly List<TurretGroupPanelManager> m_groups;


        static int GetNumberFromString(string str)
        {
            string result = "";
            for(int i = str.Length - 1; i > 0; --i)
            {
                if(Char.IsDigit(str[i]))
                {
                    result = str[i] + result;
                }
                else if(result.Length > 0)
                {
                    return int.Parse(result);
                }
            }
            return -1;
        }

        static void ConfigureSurfaceForSpriteOutput(IMyTextSurface surface)
        {
            surface.ContentType = ContentType.SCRIPT;
            surface.Script = "";
            surface.ScriptBackgroundColor = Color.Black;
            surface.ScriptForegroundColor = Color.DarkGray;
        }

        static void ConfigureSurfaceForTextOutput(IMyTextSurface surface)
        {
            surface.ContentType = ContentType.TEXT_AND_IMAGE;
            surface.Font = "Monospace";
            surface.FontSize = 1.8f;
            surface.BackgroundColor = Color.Black;
            surface.FontColor = Color.Green;
            surface.Alignment = TextAlignment.LEFT;
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
            if(updateSource.HasFlag(UpdateType.Update100))
            {
                foreach(var group in m_groups)
                {
                    group.UpdatePanels();
                }
            }
        }

        // static class SpriteHelper
        // {
        //     public const string DefaultFontId = "Debug";
        //     public static float GetMaxFontSizeForLine(StringBuilder lineText, string fontId, IMyTextSurface surface, out float lineHeight)
        //     {
        //         float fontSize = 1;
        //         Vector2 size = surface.MeasureStringInPixels(lineText, fontId, fontSize);
        //         if(size.X > surface.SurfaceSize.X || size.Y > surface.SurfaceSize.Y)
        //         {
        //             return ShrinkToLength(lineText, fontId, fontSize, surface, out lineHeight);
        //         }
        //         if (size.X < surface.SurfaceSize.X && size.Y < surface.SurfaceSize.Y)
        //         {
        //             return GrowToLength(lineText, fontId, fontSize, surface, out lineHeight);
        //         }
        //         lineHeight = size.Y;
        //         return fontSize;
        //     }

        //     static float GrowToLength(StringBuilder lineText, string fontId, float fontSize, IMyTextSurface surface, out float lineHeight, float targetWidth = 0)
        //     {
        //         const float FONT_SIZE_STEP = 0.1f;
        //         targetWidth = Math.Min(targetWidth, surface.SurfaceSize.X);
        //         if(targetWidth <= 0.0f)
        //             targetWidth = surface.SurfaceSize.X;

        //         var targetSize = new Vector2(targetWidth, surface.SurfaceSize.Y);
        //         Vector2 size = surface.MeasureStringInPixels(lineText, fontId, fontSize);
        //         Vector2 lastGoodSize = size;
        //         while(SizeFitsSurface(size, targetSize))
        //         {
        //             lastGoodSize = size;
        //             fontSize += FONT_SIZE_STEP;
        //             size = surface.MeasureStringInPixels(lineText, fontId, fontSize);
        //         }
        //         lineHeight = lastGoodSize.Y;
        //         return fontSize - FONT_SIZE_STEP;
        //     }
        //     static float ShrinkToLength(StringBuilder lineText, string fontId, float fontSize, IMyTextSurface surface, out float lineHeight, float targetWidth = 0)
        //     {
        //         const float FONT_SIZE_STEP = 0.1f;
        //         targetWidth = Math.Min(targetWidth, surface.SurfaceSize.X);
        //         if(targetWidth <= 0.0f)
        //             targetWidth = surface.SurfaceSize.X;

        //         var targetSize = new Vector2(targetWidth, surface.SurfaceSize.Y);
        //         Vector2 size = surface.MeasureStringInPixels(lineText, fontId, fontSize);
        //         while(SizeFitsSurface(size, targetSize) == true && fontSize > FONT_SIZE_STEP)
        //         {
        //             fontSize -= FONT_SIZE_STEP;
        //             size = surface.MeasureStringInPixels(lineText, fontId, fontSize);
        //         }
        //         lineHeight = size.Y;
        //         return fontSize;
        //     }

        //     static bool SizeFitsSurface(Vector2 size, Vector2 surfaceSize)
        //     {
        //         return size.X < surfaceSize.X && size.Y < surfaceSize.Y;
        //     }
        // }


    }
}
