using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using SN = System.Numerics;

namespace Sin3d.Extensions.Simd.BitCast;

public static class Vector3SimdExtensions
{
    extension(Vector3)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Vector3 position, in Matrix matrix, out Vector3 result)
        {
            ref SN.Vector3 pos = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in position));
            ref SN.Matrix4x4 mat = ref Unsafe.As<Matrix, SN.Matrix4x4>(ref Unsafe.AsRef(in matrix));
            
            SN.Vector3 simVec = SN.Vector3.Transform(pos, mat);
            result = Unsafe.BitCast<SN.Vector3, Vector3>(simVec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(in Vector3 value1, in Vector3 value2, out Vector3 result)
        {
            ref SN.Vector3 v1 = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value1));
            ref SN.Vector3 v2 = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value2));

            SN.Vector3 simVec = v1 + v2;
            result = Unsafe.BitCast<SN.Vector3, Vector3>(simVec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Distance(in Vector3 value1, in Vector3 value2, out float result)
        {
            ref SN.Vector3 v1 = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value1));
            ref SN.Vector3 v2 = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value2));

            result = SN.Vector3.Distance(v1, v2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DistanceSquared(in Vector3 value1, in Vector3 value2, out float result)
        {
            ref SN.Vector3 v1 = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value1));
            ref SN.Vector3 v2 = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value2));

            result = SN.Vector3.DistanceSquared(v1, v2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Vector3 value, in Quaternion rotation, out Vector3 result)
        {
            ref SN.Vector3 v = ref Unsafe.As<Vector3, SN.Vector3>(ref Unsafe.AsRef(in value));
            ref SN.Quaternion q = ref Unsafe.As<Quaternion, SN.Quaternion>(ref Unsafe.AsRef(in rotation));

            SN.Vector3 simVec = SN.Vector3.Transform(v, q);
            result = Unsafe.BitCast<SN.Vector3, Vector3>(simVec);
        }
    }
}
