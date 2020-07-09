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
using System.Collections.Immutable;
namespace IngameScript
{


    partial class Program
    {
        public enum LaunchState
        {
            PreLaunch,
            Separation,
            Boost,
            Flight,
            Terminal,
            Detonated,
            Aborted
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

    public abstract class MissileCommand<TData> : Command<TData>
    {
    }

    public sealed class RegisterMissileCommand : MissileCommand<string>
    {
        public string UUID { get; set; }


        protected override bool Deserialize(string data)
        {
            UUID = data;
            return true;
        }

        public override string Serialize()
        {
            return UUID;
        }
    }

    public sealed class RegisterLauncherCommand : MissileCommand<bool>
    {
        public override bool Serialize()
        {
            return true;
        }

        protected override bool Deserialize(bool data)
        {
            return true;
        }
    }

    public sealed class LaunchCommand : MissileCommand<Vector3D>
    {
        public Vector3D Destination { get; set; }

        protected override bool Deserialize(Vector3D dest)
        {
            this.Destination = dest;
            return true;
        }

        public override Vector3D Serialize()
        {
            return this.Destination;
        }
    }

    public sealed class ChangeTarget : MissileCommand<Vector3D>
    {

        public Vector3D NewTarget { get; set; }

        protected override bool Deserialize(Vector3D newTarget)
        {
            this.NewTarget = newTarget;
            return true;
        }

        public override Vector3D Serialize()
        {
            return this.NewTarget;
        }
    }

    public sealed class Abort : MissileCommand<bool>
    {
        public bool Detonate { get; set; } = false;
        protected override bool Deserialize(bool detonate)
        {
            this.Detonate = detonate;
            return true;
        }

        public override bool Serialize()
        {
            return Detonate;
        }
    }
}
