using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using SN = System.Numerics;

namespace Sin3d.Extensions.Simd.BitCast;

public static class QuaternionSimdExtensions
{
    extension(Quaternion)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Slerp(
            in Quaternion quaternion1,
            in Quaternion quaternion2,
            float amount,
            out Quaternion result)
        {
            ref SN.Quaternion q1 = ref Unsafe.As<Quaternion, SN.Quaternion>(ref Unsafe.AsRef(in quaternion1));
            ref SN.Quaternion q2 = ref Unsafe.As<Quaternion, SN.Quaternion>(ref Unsafe.AsRef(in quaternion2));

            SN.Quaternion simQuat = SN.Quaternion.Slerp(q1, q2, amount);
            result = Unsafe.BitCast<SN.Quaternion, Quaternion>(simQuat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(
            in Quaternion quaternion1,
            in Quaternion quaternion2,
            out Quaternion result)
        {
            ref SN.Quaternion q1 = ref Unsafe.As<Quaternion, SN.Quaternion>(ref Unsafe.AsRef(in quaternion1));
            ref SN.Quaternion q2 = ref Unsafe.As<Quaternion, SN.Quaternion>(ref Unsafe.AsRef(in quaternion2));

            SN.Quaternion simQuat = q1 * q2;
            result = Unsafe.BitCast<SN.Quaternion, Quaternion>(simQuat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateFromAxisAngle(in Vector3 axis, float angle, out Quaternion result)
        {
            // Note: System.Numerics.Quaternion.CreateFromAxisAngle handles the intrinsic rotation accurately
            SN.Quaternion simQuat = SN.Quaternion.CreateFromAxisAngle(
                Unsafe.BitCast<Vector3, SN.Vector3>(axis),
                angle
            );
            result = Unsafe.BitCast<SN.Quaternion, Quaternion>(simQuat);
        }
    }
}
