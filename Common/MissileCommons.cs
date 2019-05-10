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

    public enum MissileCommandTag : int
    {
        Launch = 1337,
        ChangeTarget = 1338
    }


    
    public abstract class MissileCommand<TData> : Command<TData>
    {
    }
    public sealed class LaunchCommand : MissileCommand<MyTuple<Vector3D>>
    {
        
        public Vector3D Destination { get; private set; }

        protected override CommandFactory LocalFactory => FactoryInstance;
        public static readonly Factory<LaunchCommand> FactoryInstance = new Factory<LaunchCommand>((int)MissileCommandTag.Launch);

        public LaunchCommand() { }

        public LaunchCommand(Vector3D coord)
        {
            this.Destination = coord;
        }



        protected override void Deserialize(MyTuple<Vector3D> arr)
        {
            this.Destination = arr.Item1;
        }

        protected override MyTuple<Vector3D> Serialize()
        {
            return MyTuple.Create(this.Destination);
        }
    }

    public sealed class ChangeTarget : MissileCommand<MyTuple<Vector3D>>
    {

        public Vector3D NewTarget { get; private set; }

        protected override CommandFactory LocalFactory => FactoryInstance;
        public static readonly Factory<ChangeTarget> FactoryInstance = new Factory<ChangeTarget>((int)MissileCommandTag.ChangeTarget);


        public ChangeTarget() { }
        public ChangeTarget(Vector3D newTarget)
        {
            this.NewTarget = newTarget;
        }



        protected override void Deserialize(MyTuple<Vector3D> tup)
        {
            this.NewTarget = tup.Item1;
        }

        protected override MyTuple<Vector3D> Serialize()
        {
            return MyTuple.Create(this.NewTarget);
        }
    }
}
