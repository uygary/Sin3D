using Microsoft.Xna.Framework;

namespace Sin3d.Extensions;

public static class QuaternionExtensions
{
    extension(Quaternion)
    {

        /// <summary>
        /// Performs a spherical linear blend between two quaternions.
        /// </summary>
        /// <param name="quaternion1">Source <see cref="T:Microsoft.Xna.Framework.Quaternion" />.</param>
        /// <param name="quaternion2">Source <see cref="T:Microsoft.Xna.Framework.Quaternion" />.</param>
        /// <param name="amount">The blend amount where 0 returns <paramref name="quaternion1" /> and 1 <paramref name="quaternion2" />.</param>
        /// <param name="result">The result of spherical linear blending between two quaternions as an output parameter.</param>
        public static void Slerp(
            in Quaternion quaternion1,
            in Quaternion quaternion2,
            float amount,
            out Quaternion result)
        {
            float num1 = amount;
            float x = (float)((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W);
            bool flag = false;
            if ((double)x < 0.0)
            {
                flag = true;
                x = -x;
            }
            float num2;
            float num3;
            if ((double)x > 0.99999898672103882)
            {
                num2 = 1f - num1;
                num3 = flag ? -num1 : num1;
            }
            else
            {
                float a = MathF.Acos(x);
                float num4 = (float)(1.0 / Math.Sin((double)a));
                num2 = MathF.Sin((1f - num1) * a) * num4;
                num3 = flag ? -MathF.Sin(num1 * a) * num4 : MathF.Sin(num1 * a) * num4;
            }
            result.X = (float)((double)num2 * (double)quaternion1.X + (double)num3 * (double)quaternion2.X);
            result.Y = (float)((double)num2 * (double)quaternion1.Y + (double)num3 * (double)quaternion2.Y);
            result.Z = (float)((double)num2 * (double)quaternion1.Z + (double)num3 * (double)quaternion2.Z);
            result.W = (float)((double)num2 * (double)quaternion1.W + (double)num3 * (double)quaternion2.W);
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Xna.Framework.Quaternion" /> that contains a multiplication of two quaternions.
        /// </summary>
        /// <param name="quaternion1">Source <see cref="T:Microsoft.Xna.Framework.Quaternion" />.</param>
        /// <param name="quaternion2">Source <see cref="T:Microsoft.Xna.Framework.Quaternion" />.</param>
        /// <param name="result">The result of the quaternion multiplication as an output parameter.</param>
        public static void Multiply(
            in Quaternion quaternion1,
            in Quaternion quaternion2,
            out Quaternion result)
        {
            float x1 = quaternion1.X;
            float y1 = quaternion1.Y;
            float z1 = quaternion1.Z;
            float w1 = quaternion1.W;
            float x2 = quaternion2.X;
            float y2 = quaternion2.Y;
            float z2 = quaternion2.Z;
            float w2 = quaternion2.W;
            float num1 = (float)((double)y1 * (double)z2 - (double)z1 * (double)y2);
            float num2 = (float)((double)z1 * (double)x2 - (double)x1 * (double)z2);
            float num3 = (float)((double)x1 * (double)y2 - (double)y1 * (double)x2);
            float num4 = (float)((double)x1 * (double)x2 + (double)y1 * (double)y2 + (double)z1 * (double)z2);
            result.X = (float)((double)x1 * (double)w2 + (double)x2 * (double)w1) + num1;
            result.Y = (float)((double)y1 * (double)w2 + (double)y2 * (double)w1) + num2;
            result.Z = (float)((double)z1 * (double)w2 + (double)z2 * (double)w1) + num3;
            result.W = w1 * w2 - num4;
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Xna.Framework.Quaternion" /> from the specified axis and angle.
        /// </summary>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle in radians.</param>
        /// <param name="result">The new quaternion builded from axis and angle as an output parameter.</param>
        public static void CreateFromAxisAngle(in Vector3 axis, float angle, out Quaternion result)
        {
            double x = (double)angle * 0.5;
            float num1 = MathF.Sin((float)x);
            float num2 = MathF.Cos((float)x);
            result.X = axis.X * num1;
            result.Y = axis.Y * num1;
            result.Z = axis.Z * num1;
            result.W = num2;
        }
    }
}