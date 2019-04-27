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
        private IMyProgrammableBlock missilePb;
        private IMyBroadcastListener missileMsgListener;
        private IMyBroadcastListener missileStatusListener;
        private Logger statusLogger;
        private Logger missileDiagLogger;

        private const string STATUS_DISPLAY_SECTION = "MissileStatus";
        private const string LOG_DISPLAY_SECTION = "MissileLog";
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

            missilePb = GridTerminalSystem.GetBlockOfType<IMyProgrammableBlock>(pb => pb.CubeGrid != Me.CubeGrid);
            if (missilePb != null && missilePb.TryRun(MissileCommons.DEFAULT_TAG))
            {
                LogLine($"Using '{missilePb.CustomName}' as missile PB.");
            }
            else
            {
                LogLine($"Lacking direct access to PB - will only work if missile uses {MissileCommons.DEFAULT_TAG} as its default tag.");
            }
            missileMsgListener = IGC.RegisterBroadcastListener(MissileCommons.DEFAULT_TAG);
            missileMsgListener.SetMessageCallback();

            missileStatusListener = IGC.RegisterBroadcastListener(MissileCommons.STATUS_TAG);
            missileStatusListener.SetMessageCallback();
        }

        public void Save()
        {

        }



        public void Main(string argument, UpdateType updateSource)
        {

            if (argument.Contains("launch"))
            {
                var coord = argument.Replace("launch ", "");
                LogLine($"Sending launch signal with the following coordinate: {coord}");
                IGC.SendBroadcastMessage(MissileCommons.DEFAULT_TAG, coord);
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
        }
    }
}