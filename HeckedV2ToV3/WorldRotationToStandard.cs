using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HeckedV2ToV3
{
    internal static class WorldRotationToStandard
    {
        internal static void ConvertPositions(
            List<object>? positionList,
            List<object>? worldRotationList,
            List<object>? localRotationList,
            out Vector3? finalLocalPosition,
            out Vector3? finalLocalEuler)
        {
            Vector3? position = positionList?.ToVector3();
            Quaternion? worldRotation = worldRotationList?.ToVector3().ToQuaternion();
            Quaternion? localRotation = localRotationList?.ToVector3().ToQuaternion();

            finalLocalPosition = null;
            Quaternion? finalLocalRotation = null;

            if (position.HasValue)
            {
                finalLocalPosition = position.Value * 0.6f;
            }

            if (worldRotation.HasValue)
            {
                Quaternion value = worldRotation.Value;
                if (finalLocalPosition.HasValue)
                {
                    finalLocalPosition = Vector3.Transform(finalLocalPosition.Value, value);
                }

                finalLocalRotation = value;
            }

            if (localRotation.HasValue)
            {
                finalLocalRotation = finalLocalRotation.HasValue ? Quaternion.Concatenate(finalLocalRotation.Value, localRotation.Value) : localRotation.Value;
            }

            finalLocalEuler = finalLocalRotation?.ToEulerAngles();
        }

        // https://stackoverflow.com/questions/70462758/c-sharp-how-to-convert-quaternions-to-euler-angles-xyz
        public static Quaternion ToQuaternion(this Vector3 v)
        {
            float cy = (float)Math.Cos(v.Z * 0.5);
            float sy = (float)Math.Sin(v.Z * 0.5);
            float cp = (float)Math.Cos(v.Y * 0.5);
            float sp = (float)Math.Sin(v.Y * 0.5);
            float cr = (float)Math.Cos(v.X * 0.5);
            float sr = (float)Math.Sin(v.X * 0.5);

            return new Quaternion
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };
        }

        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            Vector3 angles = new();

            // roll / x
            double sinrCosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosrCosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinrCosp, cosrCosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double sinyCosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosyCosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(sinyCosp, cosyCosp);

            return angles;
        }

        public static Vector3 ToVector3(this IEnumerable<object> list)
        {
            List<float> floats = list.Select(Convert.ToSingle).ToList();
            return new Vector3(floats[0], floats[1], floats[2]);
        }
    }
}
