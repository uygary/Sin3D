using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using SN = System.Numerics;

namespace Sin3d.Extensions.Simd;

public static class MatrixSimdExtensions 
{
    extension(Matrix)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateFromQuaternion(in Quaternion quaternion, out Matrix result)
        {
            // Cast strictly without allocations straight into hardware accelerated System.Numerics
            SN.Matrix4x4 simMat = SN.Matrix4x4.CreateFromQuaternion(
                Unsafe.As<Quaternion, SN.Quaternion>(ref Unsafe.AsRef(in quaternion))
            );
            result = Unsafe.As<SN.Matrix4x4, Matrix>(ref simMat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(in Matrix matrix1, in Matrix matrix2, out Matrix result)
        {
            // System.Numerics naturally utilizes modern hardware Intrinsics (AVX2, AVX512, Neon) without branching
            ref SN.Matrix4x4 m1 = ref Unsafe.As<Matrix, SN.Matrix4x4>(ref Unsafe.AsRef(in matrix1));
            ref SN.Matrix4x4 m2 = ref Unsafe.As<Matrix, SN.Matrix4x4>(ref Unsafe.AsRef(in matrix2));
            
            SN.Matrix4x4 simMat = m1 * m2;
            result = Unsafe.As<SN.Matrix4x4, Matrix>(ref simMat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateTranslation(in Vector3 position, out Matrix result)
        {
            SN.Matrix4x4 simMat = SN.Matrix4x4.CreateTranslation(
                Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in position))
            );
            result = Unsafe.As<SN.Matrix4x4, Matrix>(ref simMat);
        }
    }

    extension(in Matrix matrix)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadOnlyDecompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            ref SN.Matrix4x4 snMatrix = ref Unsafe.As<Matrix, SN.Matrix4x4>(ref Unsafe.AsRef(in matrix));

            var success = SN.Matrix4x4.Decompose(snMatrix, out SN.Vector3 snScale, out SN.Quaternion snRotation, out SN.Vector3 snTranslation);

            scale = Unsafe.As<SN.Vector3, Vector3>(ref snScale);
            rotation = Unsafe.As<SN.Quaternion, Quaternion>(ref snRotation);
            translation = Unsafe.As<SN.Vector3, Vector3>(ref snTranslation);

            return success;
        }
    }
}
