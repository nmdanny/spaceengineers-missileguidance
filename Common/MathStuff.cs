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
        public class PIDController
        {
            public double Kp { get; set; }
            public double Ki { get; set; }
            public double Kd { get; set; }

            private double lastError = 0;
            private double integralError = 0;

            public double GetCorrection(double error, double deltaT)
            {
                var derivativeTerm = (error - this.lastError) / deltaT;
                this.integralError += error * deltaT;
                this.lastError = error;
                return Kp * error + Ki * this.integralError + Kd * derivativeTerm;
            }
        }
        public class MathStuff
        {
            //Whip's ApplyGyroOverride Method v11 - 3/22/19
            public static void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, 
                                                 IEnumerable<IMyGyro> gyroList, MatrixD worldMatrix)
            {
                var rotationVec = new Vector3D(-pitchSpeed, yawSpeed, rollSpeed); //because keen does some weird stuff with signs 
                // gets the rotation vector by world's frame of reference
                var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

                // here we calculate the rotation vector by each gyro's frame of reference, converting back from the world's frame.
                foreach (var thisGyro in gyroList)
                {
                    var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));

                    thisGyro.Pitch = (float)transformedRotationVec.X;
                    thisGyro.Yaw = (float)transformedRotationVec.Y;
                    thisGyro.Roll = (float)transformedRotationVec.Z;
                    thisGyro.GyroOverride = true;
                }
            }

            /*
            /// Whip's Get Rotation Angles Method v16 - 9/25/18 ///
                Dependencies: AngleBetween
                Note: Set desiredUpVector to Vector3D.Zero if you don't care about roll
            */
            public static void GetRotationAngles(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double pitch, out double yaw, out double roll)
            {
                var localTargetVector = Vector3D.Rotate(desiredForwardVector, MatrixD.Transpose(worldMatrix));
                var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

                yaw = AngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
                if (Math.Abs(yaw) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                    yaw = Math.PI;

                if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                    pitch = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
                else
                    pitch = AngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive

                if (Vector3D.IsZero(desiredUpVector))
                {
                    roll = 0;
                    return;
                }
                var localUpVector = Vector3D.Rotate(desiredUpVector, MatrixD.Transpose(worldMatrix));
                var flattenedUpVector = new Vector3D(localUpVector.X, localUpVector.Y, 0);
                roll = AngleBetween(flattenedUpVector, Vector3D.Up) * Math.Sign(Vector3D.Dot(Vector3D.Right, flattenedUpVector));
            }

            /// <summary>
            /// Computes angle between 2 vectors
            /// </summary>
            public static double AngleBetween(Vector3D a, Vector3D b) //returns radians
            {
                if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                    return 0;
                else
                    return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }


        }
    }
}
