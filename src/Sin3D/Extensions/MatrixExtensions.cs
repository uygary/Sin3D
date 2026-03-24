using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace Sin3d.Extensions;

public static class MatrixExtensions 
{
    extension(Matrix)
    {
        /// <summary>
        /// Creates a new rotation <see cref="T:Microsoft.Xna.Framework.Matrix" /> from a <see cref="T:Microsoft.Xna.Framework.Quaternion" />.
        /// </summary>
        /// <param name="quaternion"><see cref="T:Microsoft.Xna.Framework.Quaternion" /> of rotation moment.</param>
        /// <param name="result">The rotation <see cref="T:Microsoft.Xna.Framework.Matrix" /> as an output parameter.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("This method cannot be inlined due to its size, and should be replaced with the SIMD-accelerated version in MatrixSimdExtensions for better performance.")]
        public static void CreateFromQuaternion(in Quaternion quaternion, out Matrix result)
        {
            float num1 = quaternion.X * quaternion.X;
            float num2 = quaternion.Y * quaternion.Y;
            float num3 = quaternion.Z * quaternion.Z;
            float num4 = quaternion.X * quaternion.Y;
            float num5 = quaternion.Z * quaternion.W;
            float num6 = quaternion.Z * quaternion.X;
            float num7 = quaternion.Y * quaternion.W;
            float num8 = quaternion.Y * quaternion.Z;
            float num9 = quaternion.X * quaternion.W;
            result.M11 = (float)(1.0 - 2.0 * ((double)num2 + (double)num3));
            result.M12 = (float)(2.0 * ((double)num4 + (double)num5));
            result.M13 = (float)(2.0 * ((double)num6 - (double)num7));
            result.M14 = 0.0f;
            result.M21 = (float)(2.0 * ((double)num4 - (double)num5));
            result.M22 = (float)(1.0 - 2.0 * ((double)num3 + (double)num1));
            result.M23 = (float)(2.0 * ((double)num8 + (double)num9));
            result.M24 = 0.0f;
            result.M31 = (float)(2.0 * ((double)num6 + (double)num7));
            result.M32 = (float)(2.0 * ((double)num8 - (double)num9));
            result.M33 = (float)(1.0 - 2.0 * ((double)num2 + (double)num1));
            result.M34 = 0.0f;
            result.M41 = 0.0f;
            result.M42 = 0.0f;
            result.M43 = 0.0f;
            result.M44 = 1f;
        }

        /// <summary>
        /// Creates a new <see cref="T:Microsoft.Xna.Framework.Matrix" /> that contains a multiplication of two matrix.
        /// </summary>
        /// <param name="matrix1">Source <see cref="T:Microsoft.Xna.Framework.Matrix" />.</param>
        /// <param name="matrix2">Source <see cref="T:Microsoft.Xna.Framework.Matrix" />.</param>
        /// <param name="result">Result of the matrix multiplication as an output parameter.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("This method cannot be inlined due to its size, and should be replaced with the SIMD-accelerated version in MatrixSimdExtensions for better performance.")]
        public static void Multiply(in Matrix matrix1, in Matrix matrix2, out Matrix result)
        {
            float num1 = (float)((double)matrix1.M11 * (double)matrix2.M11 + (double)matrix1.M12 * (double)matrix2.M21 + (double)matrix1.M13 * (double)matrix2.M31 + (double)matrix1.M14 * (double)matrix2.M41);
            float num2 = (float)((double)matrix1.M11 * (double)matrix2.M12 + (double)matrix1.M12 * (double)matrix2.M22 + (double)matrix1.M13 * (double)matrix2.M32 + (double)matrix1.M14 * (double)matrix2.M42);
            float num3 = (float)((double)matrix1.M11 * (double)matrix2.M13 + (double)matrix1.M12 * (double)matrix2.M23 + (double)matrix1.M13 * (double)matrix2.M33 + (double)matrix1.M14 * (double)matrix2.M43);
            float num4 = (float)((double)matrix1.M11 * (double)matrix2.M14 + (double)matrix1.M12 * (double)matrix2.M24 + (double)matrix1.M13 * (double)matrix2.M34 + (double)matrix1.M14 * (double)matrix2.M44);
            float num5 = (float)((double)matrix1.M21 * (double)matrix2.M11 + (double)matrix1.M22 * (double)matrix2.M21 + (double)matrix1.M23 * (double)matrix2.M31 + (double)matrix1.M24 * (double)matrix2.M41);
            float num6 = (float)((double)matrix1.M21 * (double)matrix2.M12 + (double)matrix1.M22 * (double)matrix2.M22 + (double)matrix1.M23 * (double)matrix2.M32 + (double)matrix1.M24 * (double)matrix2.M42);
            float num7 = (float)((double)matrix1.M21 * (double)matrix2.M13 + (double)matrix1.M22 * (double)matrix2.M23 + (double)matrix1.M23 * (double)matrix2.M33 + (double)matrix1.M24 * (double)matrix2.M43);
            float num8 = (float)((double)matrix1.M21 * (double)matrix2.M14 + (double)matrix1.M22 * (double)matrix2.M24 + (double)matrix1.M23 * (double)matrix2.M34 + (double)matrix1.M24 * (double)matrix2.M44);
            float num9 = (float)((double)matrix1.M31 * (double)matrix2.M11 + (double)matrix1.M32 * (double)matrix2.M21 + (double)matrix1.M33 * (double)matrix2.M31 + (double)matrix1.M34 * (double)matrix2.M41);
            float num10 = (float)((double)matrix1.M31 * (double)matrix2.M12 + (double)matrix1.M32 * (double)matrix2.M22 + (double)matrix1.M33 * (double)matrix2.M32 + (double)matrix1.M34 * (double)matrix2.M42);
            float num11 = (float)((double)matrix1.M31 * (double)matrix2.M13 + (double)matrix1.M32 * (double)matrix2.M23 + (double)matrix1.M33 * (double)matrix2.M33 + (double)matrix1.M34 * (double)matrix2.M43);
            float num12 = (float)((double)matrix1.M31 * (double)matrix2.M14 + (double)matrix1.M32 * (double)matrix2.M24 + (double)matrix1.M33 * (double)matrix2.M34 + (double)matrix1.M34 * (double)matrix2.M44);
            float num13 = (float)((double)matrix1.M41 * (double)matrix2.M11 + (double)matrix1.M42 * (double)matrix2.M21 + (double)matrix1.M43 * (double)matrix2.M31 + (double)matrix1.M44 * (double)matrix2.M41);
            float num14 = (float)((double)matrix1.M41 * (double)matrix2.M12 + (double)matrix1.M42 * (double)matrix2.M22 + (double)matrix1.M43 * (double)matrix2.M32 + (double)matrix1.M44 * (double)matrix2.M42);
            float num15 = (float)((double)matrix1.M41 * (double)matrix2.M13 + (double)matrix1.M42 * (double)matrix2.M23 + (double)matrix1.M43 * (double)matrix2.M33 + (double)matrix1.M44 * (double)matrix2.M43);
            float num16 = (float)((double)matrix1.M41 * (double)matrix2.M14 + (double)matrix1.M42 * (double)matrix2.M24 + (double)matrix1.M43 * (double)matrix2.M34 + (double)matrix1.M44 * (double)matrix2.M44);
            result.M11 = num1;
            result.M12 = num2;
            result.M13 = num3;
            result.M14 = num4;
            result.M21 = num5;
            result.M22 = num6;
            result.M23 = num7;
            result.M24 = num8;
            result.M31 = num9;
            result.M32 = num10;
            result.M33 = num11;
            result.M34 = num12;
            result.M41 = num13;
            result.M42 = num14;
            result.M43 = num15;
            result.M44 = num16;
        }

        /// <summary>
        /// Creates a new translation <see cref="T:Microsoft.Xna.Framework.Matrix" />.
        /// </summary>
        /// <param name="position">X,Y and Z coordinates of translation.</param>
        /// <param name="result">The translation <see cref="T:Microsoft.Xna.Framework.Matrix" /> as an output parameter.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("This method cannot be inlined due to its size, and should be replaced with the SIMD-accelerated version in MatrixSimdExtensions for better performance.")]
        public static void CreateTranslation(in Vector3 position, out Matrix result)
        {
            result.M11 = 1f;
            result.M12 = 0.0f;
            result.M13 = 0.0f;
            result.M14 = 0.0f;
            result.M21 = 0.0f;
            result.M22 = 1f;
            result.M23 = 0.0f;
            result.M24 = 0.0f;
            result.M31 = 0.0f;
            result.M32 = 0.0f;
            result.M33 = 1f;
            result.M34 = 0.0f;
            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            result.M44 = 1f;
        }
    }
}