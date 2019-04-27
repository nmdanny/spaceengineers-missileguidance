﻿using Sandbox.Game.EntityComponents;
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
        

        private void LogLine(string msg, bool broadcast=true)
        {
            IMyTextSurfaceProvider prov = Me;
            var disp = Me.GetSurface(0);
            disp.ContentType = ContentType.TEXT_AND_IMAGE;
            disp.WriteText(msg + Environment.NewLine, true);
            Echo(msg + Environment.NewLine);
            if (cmdListener != null && broadcast)
            {
                IGC.SendBroadcastMessage(cmdListener.Tag, msg);
            }
        }

        private const double SWITCH_WAY_POINT_DISTANCE = 100;
        private const double ARM_DISTANCE = 100;
        private const double BLOW_DISTANCE = 25;
        private const double BOOST_MANEUVER_RANGE = 250;
        private const double MIN_ANGLE_FOR_ENGINE_RESTART = 25;
        private const string STATUS_DISPLAY_SECTION = "MissileStatus";
        private const string SETTINGS_SECTION = "Settings";

        private Logger statusLogger;

        private PIDController pitchPID = new PIDController();
        private PIDController yawPID = new PIDController();
        private PIDController rollPID = new PIDController();
        private const double PID_TIME_DELTA = 10.0 / 60.0;
        private const double DEFAULT_KP = 5;
        private const double DEFAULT_KI = 1.5;
        private const double DEFAULT_KD = 0.5;
        private bool applyGyro = true;

        private LaunchState state = LaunchState.PreLaunch;
        private IMyShipMergeBlock separator;
        private List<IMyThrust> fwdThrusters = new List<IMyThrust>();
        private List<IMyGyro> gyros = new List<IMyGyro>();
        private List<IMyThrust> allThrusters = new List<IMyThrust>();
        private List<IMyWarhead> warheads = new List<IMyWarhead>();
        private IMyRemoteControl remote; // assumed to be pointing forward
        private IMyBroadcastListener cmdListener;
        private MyWaypointInfo finalTarget;
        private bool armed = false;
        private bool enginesActive = false;


        private List<MyWaypointInfo> trajectory = new List<MyWaypointInfo>();
        private int trajectoryStage = -1;

        private Vector3D launchPos;
        private Vector3D launchVec;

        private void UpdateSettings()
        {
            var parser = new MyIni();
            if (!parser.TryParse(Me.CustomData))
            {
                throw new InvalidOperationException("Couldn't parse CustomData as INI!");
            }
            var kp = parser.Get(SETTINGS_SECTION, "kp").ToDouble(DEFAULT_KP);
            var ki = parser.Get(SETTINGS_SECTION, "ki").ToDouble(DEFAULT_KI);
            var kd = parser.Get(SETTINGS_SECTION, "kd").ToDouble(DEFAULT_KD);
            this.applyGyro = parser.Get(SETTINGS_SECTION, "applyGyro").ToBoolean(true);
            InitializePIDs(kp, ki, kd);
            LogLine($"Using KP: {kp}, KI: {ki}, KD: {kd}, applyGyro :{this.applyGyro}");

            if (!this.applyGyro)
            {
                foreach (var gyro in this.gyros)
                {
                    gyro.GyroOverride = false;
                    gyro.Pitch = gyro.Yaw = gyro.Roll = 0;
                }
            }
            
        }

        private void InitializePIDs(double kp, double ki, double kd)
        {
            // gyroscopes all have the same PID constants, since they are symmetric in all axes.    
            foreach (var pid in new[] { pitchPID, yawPID, rollPID })
            {
                pid.Kp = kp;
                pid.Ki = ki;
                pid.Kd = kd;

            }
        }

        public Program()
        {
            try
            {
                UpdateSettings();
                ResetDisplay();

                this.remote = GridTerminalSystem.GetBlockOfType<IMyRemoteControl>(rm => rm.CubeGrid == Me.CubeGrid);
                if (remote == null)
                {
                    throw new InvalidOperationException("Missile MUST have a remote. (That is forward facing)");
                }
                LogLine($"Found remote {remote.CustomName}");

                this.separator = GridTerminalSystem.GetBlockOfType<IMyShipMergeBlock>(mb => mb.CubeGrid == Me.CubeGrid);
                if (this.separator != null)
                {
                    LogLine($"Found separator merge-block {separator.CustomName}");

                }

                this.statusLogger = new Logger(this, STATUS_DISPLAY_SECTION, true);


                GridTerminalSystem.GetBlocksOfType(allThrusters, th => th.CubeGrid == Me.CubeGrid);

                foreach (var thruster in allThrusters)
                {
                    if (thruster.WorldMatrix.Forward == remote.WorldMatrix.Backward)
                    {
                        fwdThrusters.Add(thruster);
                        LogLine($"Found forward thruster {thruster.CustomName}");
                    }
                    thruster.Enabled = false;
                    thruster.ThrustOverridePercentage = 0;
                }

                GridTerminalSystem.GetBlocksOfType(warheads);
                LogLine($"Found {warheads.Count} warheads.");

                foreach (var warhead in warheads)
                {
                    warhead.IsArmed = false;
                }

                GridTerminalSystem.GetBlocksOfType(gyros);
                foreach (var gyro in gyros)
                {
                    gyro.Enabled = false;
                }

                registerTag(MissileCommons.DEFAULT_TAG);
                LogLine("Missile is ready to launch");
                LogStatus("Missile: Pre-flight");
            }
            catch (Exception ex)
            {
                LogLine($"Program() expection: {ex}\nStacktrace: \n{ex.StackTrace}");
            }
        }

        private List<MyWaypointInfo> GenerateTrajectory(Vector3D from, Vector3D to, int points, Vector3D planet)
        {
            return new List<MyWaypointInfo>()
            {
                new MyWaypointInfo("DIRECT-WP-0", to)
            };
            /*
            // We shall define a basis for the plane defined by 'from, to, planet'

            // The normal to our plane, which is basically z-axis, don't really care where its pointing towards
            var zAxis = (to - from) * (planet - from);
            zAxis.Normalize();

            // 'to' and 'from' shall be the roots of our parabola, hence why I want them to be on the x-axis
            var xAxis = to - from;
            xAxis.Normalize();

            // the y axis shall be the the perpendicular vector from 'planet' to the xAxis.
            // TODO: determine appropriate direction, maybe reverse cross IDK
            var yAxis = (to - from) * zAxis;
            yAxis.Normalize();

            var realToXY = new MatrixD(
                xAxis.X, yAxis.X, zAxis.X,
                xAxis.Y, yAxis.Y, zAxis.Y,
                xAxis.Z, yAxis.Z, zAxis.Z);

            var XYtoReal = MatrixD.Invert(realToXY);
            */



        }

        private void Launch()
        {
            this.state = LaunchState.Boost;
            this.launchPos = this.Position;
            this.launchVec = this.remote.WorldMatrix.Forward;
            LogLine($"Launching to '{finalTarget.Name}'\nOrigin coords: {launchPos}\nDestination coords: {finalTarget.Coords}");

            if (separator != null)
            {
                separator.Enabled = false;
            }
            foreach (var thrust in allThrusters)
            {
                thrust.Enabled = true;
            }
            foreach (var thrust in fwdThrusters)
            {
                thrust.ThrustOverridePercentage = 1;
            }
            foreach (var gyros in gyros)
            {
                gyros.Enabled = true;
            }
            this.remote.DampenersOverride = true;
            this.enginesActive = true;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }

        public Vector3D Position
        {
            get { return this.remote.GetPosition(); }
        }

        public Vector3D Target { get
            {
                if (this.state == LaunchState.Boost)
                {
                    return this.launchPos + BOOST_MANEUVER_RANGE * this.launchVec;
                }
                if (this.state == LaunchState.Flight)
                {
                    return this.trajectory[this.trajectoryStage].Coords;
                }
                return finalTarget.Coords;
            } }


        private void Tick()
        {
            var status = new MissileStatus()
            {
                Position = this.Position,
                State = state,
            };
            if (state == LaunchState.Boost)
            {
                if ((this.Position - this.launchPos).LengthSquared() >= BOOST_MANEUVER_RANGE * BOOST_MANEUVER_RANGE)
                {
                    LogLine($"Boost phase finished, transitioning to parabolic flight mode");
                    this.state = LaunchState.Flight;
                    
                    foreach (var gyro in gyros)
                    {
                        gyro.Enabled = true;
                    }

                    this.enginesActive = false;
                    Vector3D planet = Vector3D.Zero;
                    if (!remote.TryGetPlanetPosition(out planet))
                    {
                        LogLine("Couldn't find planet!");
                    }

                    LogLine($"Generating parabolic trajectory");
                    this.trajectory = GenerateTrajectory(this.Position, this.finalTarget.Coords, 10, planet);
                    this.trajectoryStage = 0;
                    this.state = LaunchState.Flight;

                    // temporarily stop forward thrust in order to allow missile to align to target
                    foreach (var thruster in fwdThrusters)
                    {
                        thruster.ThrustOverride = 0;
                    }

                }
            }

            if (state == LaunchState.Flight)
            {

                double pitch, yaw, roll;
                var desiredFwdVec = Vector3D.Normalize(Target - Position);
                MathStuff.GetRotationAngles(desiredFwdVec, Vector3D.Zero, remote.WorldMatrix, out pitch, out yaw, out roll);
                status.DistToTarget = (Target - Position).Length();
                status.Pitch = pitch;
                status.Yaw = yaw;
                status.Roll = roll;

                if (!this.enginesActive && (Math.Abs(pitch) < MIN_ANGLE_FOR_ENGINE_RESTART || 
                    Math.Abs(yaw) < MIN_ANGLE_FOR_ENGINE_RESTART || Math.Abs(roll) < MIN_ANGLE_FOR_ENGINE_RESTART))
                {
                    foreach (var thrust in fwdThrusters)
                    {
                        thrust.ThrustOverridePercentage = 1;
                    }
                    this.enginesActive = true;
                }

                var pitchCorrection = pitchPID.GetCorrection(pitch, PID_TIME_DELTA);
                var yawCorrection = yawPID.GetCorrection(yaw, PID_TIME_DELTA);
                var rollCorrection = rollPID.GetCorrection(roll, PID_TIME_DELTA);
                status.ExtraData.Append($"PID:\nPitchCorrection: {MathHelperD.ToDegrees(pitchCorrection):F2}\n" +
                    $"YawCorrection: {MathHelperD.ToDegrees(yawCorrection):F2}\n" +
                    $"RollCorrection: {MathHelperD.ToDegrees(rollCorrection):F2}\n");
                if (applyGyro)
                {
                    MathStuff.ApplyGyroOverride(pitchCorrection, yawCorrection, rollCorrection, this.gyros, this.remote.WorldMatrix);
                }

                if ((this.Position - Target).LengthSquared() <= SWITCH_WAY_POINT_DISTANCE * SWITCH_WAY_POINT_DISTANCE)
                {
                    this.trajectoryStage++;
                } 

                if (this.trajectoryStage >= this.trajectory.Count ||
                    (this.Position - finalTarget.Coords).LengthSquared() <= ARM_DISTANCE * ARM_DISTANCE)
                {
                    ArmMissile();
                    this.trajectoryStage++;
                    this.state = LaunchState.Terminal;
                }
            }
            if (state == LaunchState.Terminal)
            {
                if ((this.Position - finalTarget.Coords).LengthSquared() <= BLOW_DISTANCE * BLOW_DISTANCE)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    this.cmdListener.DisableMessageCallback();
                    LogLine("Reached detonation threshold. Detonating and disabling script.");
                    LogStatus("Detonated.");
                    foreach (var wh in warheads)
                    {
                        wh.Detonate();
                    }
                }
            }
            status.State = this.state;
            LogStatus(status.ToString());
        }

        private void ArmMissile()
        {
            if (this.armed)
            {
                return;
            }
            this.armed = true;
            foreach (var wh in warheads)
            {
                wh.IsArmed = true;
            }
            LogLine($"Entering terminal mode, warheads armed");
        }

        private void registerTag(string tag)
        {
            cmdListener = IGC.RegisterBroadcastListener(tag);
            cmdListener.SetMessageCallback();
            LogLine($"Missile IGC registered with tag \"{cmdListener.Tag}\"");

        }

        private void LogStatus<TData>(TData status)
        {
            var statusString = status.ToString();
            IGC.SendBroadcastMessage(MissileCommons.STATUS_TAG, statusString);
            this.statusLogger.OutputLine(statusString, false);

        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if ((updateSource & UpdateType.Terminal) == UpdateType.Terminal)
                {
                    LogLine("Updating settings...");
                    UpdateSettings();
                }
                if ((updateSource & UpdateType.Script) == UpdateType.Script)
                {
                    registerTag(argument);
                }
                else if ((updateSource & UpdateType.IGC) == UpdateType.IGC)
                {
                    if (cmdListener.HasPendingMessage)
                    {
                        var msg = cmdListener.AcceptMessage();
                        var data = (string)msg.Data;
                        MyWaypointInfo tgt;
                        if (MyWaypointInfo.TryParse(data, out tgt))
                        {
                            this.finalTarget = tgt;
                            Launch();
                        }
                        else
                        {
                            // prevent spam
                            // LogLine($"Missile IGC received unknown msg \"{msg.Data}\" from {msg.Source}");
                        }
                    }
                }
                else
                {
                    Tick();
                }
            }
            catch (Exception ex)
            {
                LogLine($"Main exception: {ex}\nStacktrace:\n{ex.StackTrace}");
            }
        }
    }
}