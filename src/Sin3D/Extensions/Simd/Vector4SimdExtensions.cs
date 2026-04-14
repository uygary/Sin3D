using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using SN = System.Numerics;

namespace Sin3D.Extensions.Simd;

public static class Vector4SimdExtensions
{
    extension(Vector4)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Vector4 vector, in Matrix matrix, out Vector4 result)
        {
            ref SN.Vector4 v = ref Unsafe.As<Vector4, SN.Vector4>(ref Unsafe.AsRef(in vector));
            ref SN.Matrix4x4 m = ref Unsafe.As<Matrix, SN.Matrix4x4>(ref Unsafe.AsRef(in matrix));

            SN.Vector4 simVec = SN.Vector4.Transform(v, m);
            result = Unsafe.As<SN.Vector4, Vector4>(ref simVec);
        }
    }
}
