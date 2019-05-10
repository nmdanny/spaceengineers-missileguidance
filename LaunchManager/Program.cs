using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private IMyBroadcastListener missileMsgListener;
        private IMyBroadcastListener missileStatusListener;
        private IMyLargeTurretBase directorTurret;
        private Logger statusLogger;
        private Logger missileDiagLogger;

        private const string STATUS_DISPLAY_SECTION = "MissileStatus";
        private const string LOG_DISPLAY_SECTION = "MissileLog";
        private const string SETTINGS_SECTION = "Settings";
        private const string DIRECTOR_TURRET_SECTION = "MissileDirector";
        private string tag;
        private string statusTag;
        private bool directing;

        private readonly MessageSender msgSender;
        private void UpdateSettings()
        {
            var parser = new MyIni();
            if (!parser.TryParse(Me.CustomData))
            {
                throw new InvalidOperationException("Couldn't parse CustomData as INI!");
            }
            this.tag = parser.Get(SETTINGS_SECTION, "tag").ToString(MissileCommons.DEFAULT_TAG);
            this.statusTag = parser.Get(SETTINGS_SECTION, "statusTag").ToString(MissileCommons.STATUS_TAG);
            this.directing = parser.Get(SETTINGS_SECTION, "directing").ToBoolean(true);
            if (this.directing)
            {
                this.directorTurret = GridTerminalSystem.GetBlockOfType<IMyLargeTurretBase>(t => MyIni.HasSection(t.CustomData, DIRECTOR_TURRET_SECTION));
                if (this.directorTurret != null)
                {
                    LogLine($"Director mode on, found director turret {directorTurret.CustomName}");
                }
                else
                {
                    LogLine($"No director turret was found. If you have one, add the [{DIRECTOR_TURRET_SECTION}] line to its Custom Data. Disabling director mode.");
                    this.directing = false;
                }

            }

            LogLine($"Using command tag \"{tag}\" and status tag \"{statusTag}\".");

            if (missileMsgListener != null)
            {
                missileMsgListener.DisableMessageCallback();
            }
            missileMsgListener = IGC.RegisterBroadcastListener(this.tag);
            missileMsgListener.SetMessageCallback();

            if (missileStatusListener != null)
            {
                missileStatusListener.DisableMessageCallback();
            }
            missileStatusListener = IGC.RegisterBroadcastListener(MissileCommons.STATUS_TAG);
            missileStatusListener.SetMessageCallback();

        }

        private void LogLine(String msg)
        {
            IMyTextSurfaceProvider prov = Me; 
            var disp = Me.GetSurface(0);
            disp.ContentType = ContentType.TEXT_AND_IMAGE;
            Echo(msg);
            this.missileDiagLogger.OutputLine(msg, true);
        }

        public Program()
        {
            this.statusLogger = new Logger(this, STATUS_DISPLAY_SECTION, true);
            this.missileDiagLogger = new Logger(this, LOG_DISPLAY_SECTION, true);
            this.msgSender = new MessageSender(IGC);
            UpdateSettings();
        }

        public void Save()
        {

        }



        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if ((updateSource & UpdateType.Terminal) == UpdateType.Terminal)
                {
                    UpdateSettings();
                }
                if (argument.Contains("launch"))
                {
                    var coord = argument.Replace("launch ", "");
                    MyWaypointInfo wp;
                    if (MyWaypointInfo.TryParse(coord, out wp))
                    {
                        LogLine($"Sending launch signal with the following coordinate: {wp}");
                        this.msgSender.Broadcast(new LaunchCommand(wp.Coords), tag);
                        this.Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    }
                    else
                    {
                        LogLine($"Invalid GPS coordinates \"{coord}\", cannot initiate launch.");
                    }
                }

                if ((updateSource & UpdateType.IGC) == UpdateType.IGC)
                {
                    if (missileMsgListener.HasPendingMessage)
                    {
                        var msg = missileMsgListener.AcceptMessage();
                        LogLine($">: {msg.Data}");

                    }
                    if (missileStatusListener.HasPendingMessage)
                    {
                        var msg = missileStatusListener.AcceptMessage();
                        statusLogger.OutputLine(msg.Data.ToString());
                    }

                }
                if ((updateSource & UpdateType.Update100) == UpdateType.Update100)
                {
                    if (this.directing)
                    {
                        var azimuth = this.directorTurret.Azimuth;
                        var elev = this.directorTurret.Elevation;
                    }
                }
            }
            catch (Exception ex)
            {
                LogLine($"Program() expection: {ex}\nStacktrace: \n{ex.StackTrace}");
            }
        }
    }
}