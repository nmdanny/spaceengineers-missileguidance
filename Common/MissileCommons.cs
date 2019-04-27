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
    partial class Program
    {
        public enum LaunchState
        {
            PreLaunch,
            Boost,
            Flight,
            Terminal
        }
        public class MissileStatus
        {
            public LaunchState State { get; set; }
            public Vector3D Position {get; set; }

            public double DistToTarget { get; set; }

            public double Pitch { get; set; }
            public double Yaw { get; set; }
            public double Roll { get; set; }

            public double PitchDeg => MathHelperD.ToDegrees(this.Pitch);
            public double YawDeg => MathHelperD.ToDegrees(this.Yaw);
            public double RollDeg => MathHelperD.ToDegrees(this.Roll);

            public StringBuilder ExtraData { get; private set; } = new StringBuilder();

            public override string ToString()
            {
                return $"State: {State}\nPosition: {Position}\nDist: {DistToTarget:F2}\n" +
                    $"Pitch: {PitchDeg:F2} Yaw: {YawDeg:F2} Roll: {RollDeg:F2}\n{ExtraData}";
            }
        }
        public static class MissileCommons
        {
            public const string DEFAULT_TAG = "SRBM_T";
            public const string STATUS_TAG = "SRBM_STATUS";

        }
    }
}
