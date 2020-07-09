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
        private MessageHandler messageHandler;
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


        private Dictionary<long, MissileStatus> portToMissileStatus = new Dictionary<long, MissileStatus>();

        private void RegisterMissile(RegisterMissileCommand command, long source)
        {
            if (portToMissileStatus.ContainsKey(source))
            {
                LogLine($"Warning: tried to register missile at address {source} twice");
                return;
            }
            LogLine($"Registered missile of UUID = {command.UUID} at address {source}");
            portToMissileStatus.Add(source, new MissileStatus() { 
                State = LaunchState.PreLaunch
            });
        }

        private void LaunchMissiles(MyWaypointInfo tgt)
        {
            int launchCount = 0;
            foreach (var kvp in portToMissileStatus)
            {
                if (kvp.Value.State == LaunchState.PreLaunch)
                {
                    IGC.SendUnicast(kvp.Key, new LaunchCommand() { Destination = tgt.Coords });
                    ++launchCount;
                }
            }
            LogLine($"Launched {launchCount} missiles to {tgt.Name} at {tgt.Coords}");
        }

        private void AbortMissiles(bool detonate)
        {
            int abortedMissiles = 0;
            foreach (var kvp in portToMissileStatus)
            {
                if (kvp.Value.State != LaunchState.PreLaunch)
                {
                    IGC.SendUnicast(kvp.Key, new Abort() { Detonate = detonate});
                    ++abortedMissiles;
                }
            }
            LogLine($"Aborted {abortedMissiles}, detonate: {detonate}");
        }

        private MessageHandler CreateMessageHandler()
        {
            var handler = new MessageHandler(IGC, LogLine);
            handler.RegisterHandler<RegisterMissileCommand>(new LambdaCommandHandler<RegisterMissileCommand>(RegisterMissile), acceptBroadcasts: true);
            return handler;
        }

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
            this.messageHandler = CreateMessageHandler();
            
            UpdateSettings();
            LogLine("Finished LaunchManager initialization");
        }

        public void Save()
        {

        }

            

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal | UpdateType.Script)) != 0)
                {
                    UpdateSettings();
                    if (argument.Contains("launch"))
                    {
                        var coord = argument.Replace("launch ", "");
                        MyWaypointInfo wp;
                        if (MyWaypointInfo.TryParse(coord, out wp))
                        {
                            LaunchMissiles(wp);
                        }
                        else
                        {
                            LogLine($"Invalid GPS coordinates \"{coord}\", cannot initiate launch.");
                        }
                    }
                    else if (argument.Contains("abort"))
                    {
                        AbortMissiles(detonate: false);
                    }
                    else if (argument.Contains("detonate"))
                    {
                        AbortMissiles(detonate: true);
                    }
                }
                if ((updateSource & UpdateType.IGC) != 0)
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
                    messageHandler.Tick();

                }
                if ((updateSource & UpdateType.Update100) != 0)
                {
                    if (this.directing && this.directorTurret.IsUnderControl)
                    {
                        var azimuth = this.directorTurret.Azimuth;
                        var elev = this.directorTurret.Elevation;

                    }
                }
            }
            catch (Exception ex)
            {
                LogLine($"Launch main expection: {ex}\nStacktrace: \n{ex.StackTrace}");
            }
        }
    }
}