using Microsoft.Xna.Framework;

namespace Sin3d.Extensions;

public static class Vector3Extensions
{
    extension(Vector3)
    {
        /// <summary>
        /// Performs vector addition on <paramref name="value1" /> and
        /// <paramref name="value2" />, storing the result of the
        /// addition in <paramref name="result" />.
        /// </summary>
        /// <param name="value1">The first vector to add.</param>
        /// <param name="value2">The second vector to add.</param>
        /// <param name="result">The result of the vector addition.</param>
        public static void Add(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
        }

        /// <summary>Returns the distance between two vectors.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The distance between two vectors as an output parameter.</param>
        public static void Distance(in Vector3 value1, in Vector3 value2, out float result)
        {
            Vector3.DistanceSquared(in value1, in value2, out result);
            result = MathF.Sqrt(result);
        }

        /// <summary>Returns the squared distance between two vectors.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The squared distance between two vectors as an output parameter.</param>
        public static void DistanceSquared(in Vector3 value1, in Vector3 value2, out float result)
        {
            result = (float)(((double)value1.X - (double)value2.X) * ((double)value1.X - (double)value2.X) + ((double)value1.Y - (double)value2.Y) * ((double)value1.Y - (double)value2.Y) + ((double)value1.Z - (double)value2.Z) * ((double)value1.Z - (double)value2.Z));
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Xna.Framework.Vector3" /> that contains a transformation of 3d-vector by the specified <see cref="T:Microsoft.Xna.Framework.Matrix" />.
        /// </summary>
        /// <param name="position">Source <see cref="T:Microsoft.Xna.Framework.Vector3" />.</param>
        /// <param name="matrix">The transformation <see cref="T:Microsoft.Xna.Framework.Matrix" />.</param>
        /// <param name="result">Transformed <see cref="T:Microsoft.Xna.Framework.Vector3" /> as an output parameter.</param>
        public static void Transform(in Vector3 position, in Matrix matrix, out Vector3 result)
        {
            float num1 = (float)((double)position.X * (double)matrix.M11 + (double)position.Y * (double)matrix.M21 + (double)position.Z * (double)matrix.M31) + matrix.M41;
            float num2 = (float)((double)position.X * (double)matrix.M12 + (double)position.Y * (double)matrix.M22 + (double)position.Z * (double)matrix.M32) + matrix.M42;
            float num3 = (float)((double)position.X * (double)matrix.M13 + (double)position.Y * (double)matrix.M23 + (double)position.Z * (double)matrix.M33) + matrix.M43;
            result.X = num1;
            result.Y = num2;
            result.Z = num3;
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Xna.Framework.Vector3" /> that contains a transformation of 3d-vector by the specified <see cref="T:Microsoft.Xna.Framework.Quaternion" />, representing the rotation.
        /// </summary>
        /// <param name="value">Source <see cref="T:Microsoft.Xna.Framework.Vector3" />.</param>
        /// <param name="rotation">The <see cref="T:Microsoft.Xna.Framework.Quaternion" /> which contains rotation transformation.</param>
        /// <param name="result">Transformed <see cref="T:Microsoft.Xna.Framework.Vector3" /> as an output parameter.</param>
        public static void Transform(in Vector3 value, in Quaternion rotation, out Vector3 result)
        {
            float num1 = (float)(2.0 * ((double)rotation.Y * (double)value.Z - (double)rotation.Z * (double)value.Y));
            float num2 = (float)(2.0 * ((double)rotation.Z * (double)value.X - (double)rotation.X * (double)value.Z));
            float num3 = (float)(2.0 * ((double)rotation.X * (double)value.Y - (double)rotation.Y * (double)value.X));
            result.X = (float)((double)value.X + (double)num1 * (double)rotation.W + ((double)rotation.Y * (double)num3 - (double)rotation.Z * (double)num2));
            result.Y = (float)((double)value.Y + (double)num2 * (double)rotation.W + ((double)rotation.Z * (double)num1 - (double)rotation.X * (double)num3));
            result.Z = (float)((double)value.Z + (double)num3 * (double)rotation.W + ((double)rotation.X * (double)num2 - (double)rotation.Y * (double)num1));
        }
    }
}