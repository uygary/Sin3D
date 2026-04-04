using Microsoft.Xna.Framework;
using Sin3D.Extensions.Simd;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Sin3D;

/// <summary>
/// A shared spatial transform representing position, rotation and uniform scale.
/// Rotation is stored as a <see cref="Quaternion"/> (the canonical representation).
/// </summary>
/// <remarks>
/// <para>
/// This struct is the single authoritative representation of an object's spatial state
/// within Sin3D. Both <see cref="Model3D"/> and <see cref="Camera3D"/> expose it so that
/// game code can read/write transform data through a unified type.
/// </para>
/// <para>
/// <see cref="ToWorldMatrix"/> computes on every call.
/// It should only be called once per frame, and cached if necessary.
/// </para>
/// </remarks>
[DataContract]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Transform3D
{
    /// <summary>The world-space position.</summary>
    [DataMember]
    public Vector3 Position;

    /// <summary>The rotation as a unit quaternion.</summary>
    [DataMember]
    public Quaternion Rotation;

    /// <summary>The uniform scale factor.</summary>
    [DataMember]
    public float Scale;

    /// <summary>
    /// Creates a new <see cref="Transform3D"/> with the specified position, rotation and scale.
    /// </summary>
    public Transform3D(Vector3 position, Quaternion rotation, float scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    /// <summary>
    /// Creates a new <see cref="Transform3D"/> at the given position with identity rotation and unit scale.
    /// </summary>
    public Transform3D(Vector3 position)
    {
        Position = position;
        Rotation = Quaternion.Identity;
        Scale = 1f;
    }

    #region Static Factories

    /// <summary>Position at origin, identity rotation, unit scale.</summary>
    public static Transform3D Identity => new(Vector3.Zero, Quaternion.Identity, 1f);

    /// <summary>
    /// Creates a <see cref="Transform3D"/> from Euler angles (yaw, pitch, roll).
    /// </summary>
    /// <param name="position">The world-space position.</param>
    /// <param name="yaw">Yaw in radians (rotation around Y axis).</param>
    /// <param name="pitch">Pitch in radians (rotation around X axis).</param>
    /// <param name="roll">Roll in radians (rotation around Z axis).</param>
    /// <param name="scale">The uniform scale factor.</param>
    public static Transform3D FromYawPitchRoll(
        Vector3 position, float yaw, float pitch, float roll, float scale = 1f)
    {
        return new Transform3D(
            position,
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll),
            scale);
    }

    /// <summary>
    /// Attempts to decompose an arbitrary matrix into position, rotation and uniform scale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is useful for consuming OpenXR grip poses and other externally-provided matrices.
    /// The decomposition is reliable for matrices built from uniform Scale × Rotation × Translation.
    /// </para>
    /// <para>
    /// <b>Caveat:</b> Non-uniform scale or skewed matrices will produce approximate results that will not adhere to the actual scale.
    /// For those cases, prefer setting <see cref="Model3D.WorldMatrix"/> directly.
    /// </para>
    /// </remarks>
    /// <param name="matrix">The matrix to decompose.</param>
    public static Transform3D FromMatrix(in Matrix matrix)
    {
        matrix.ReadOnlyDecompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);

        // Use the average of the three scale axes as the uniform scale.
        // For uniform matrices this is exact; for non-uniform it's a best-effort average.
        // TODO: Do OpenXR grip poses etc. or anything that comes out of OpenXR have non-uniform scaling? Is that a thing?:
        // Maybe we need a separate Transform3D type that supports non-uniform scale?
        // TODO: Do we have a use-case for it ourselves? Is it even worth it?:
        // Like a spaceship flying through a wormhole or flying by a black hole?
        // Or the spacetime distortions around an Alcubierre drive?
        float uniformScale = (scale.X + scale.Y + scale.Z) / 3f;

        return new Transform3D(translation, rotation, uniformScale);
    }

    #endregion Static Factories

    #region Matrices

    /// <summary>
    /// Computes the world matrix: Scale × Rotation × Translation.
    /// </summary>
    /// <remarks>
    /// This is NOT cached. Call once per frame and store the result if you need it in multiple places.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix ToWorldMatrix()
    {
        Matrix.CreateScale(Scale, out var scaleMatrix);
        Matrix.CreateFromQuaternion(in Rotation, out var rotationMatrix);
        Matrix.Multiply(in scaleMatrix, in rotationMatrix, out var srMatrix);
        Matrix.CreateTranslation(in Position, out var translationMatrix);
        Matrix.Multiply(in srMatrix, in translationMatrix, out var worldMatrix);

        return worldMatrix;
    }

    /// <summary>
    /// Computes the world matrix into an output parameter, avoiding a stack copy on return.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToWorldMatrix(out Matrix result)
    {
        Matrix.CreateScale(Scale, out var scaleMatrix);
        Matrix.CreateFromQuaternion(in Rotation, out var rotationMatrix);
        Matrix.Multiply(in scaleMatrix, in rotationMatrix, out var srMatrix);
        Matrix.CreateTranslation(in Position, out var translationMatrix);
        Matrix.Multiply(in srMatrix, in translationMatrix, out result);
    }

    /// <summary>
    /// Returns just the rotation component as a matrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Matrix ToRotationMatrix()
    {
        return Matrix.CreateFromQuaternion(Rotation);
    }

    #endregion Matrices

    #region Euler

    /// <summary>
    /// Extracts approximate yaw, pitch and roll (in radians) from the current <see cref="Rotation"/> quaternion.
    /// </summary>
    /// <remarks>
    /// Euler extraction from a quaternion is inherently lossy near gimbal lock, so this is non-canonical.
    /// For FPS camera controls, prefer storing Euler angles directly (as <see cref="Camera3D"/> does).
    /// </remarks>
    public readonly void GetYawPitchRoll(out float yaw, out float pitch, out float roll)
    {
        // Convert quaternion to rotation matrix, then extract Euler angles.
        var rotMatrix = Matrix.CreateFromQuaternion(Rotation);

        // Pitch (X rotation)
        // Clamp to avoid NaN from asin.
        float sinPitch = -rotMatrix.M32;
        pitch = MathF.Asin(Math.Clamp(sinPitch, -1f, 1f));

        // Check for gimbal lock
        if (MathF.Abs(sinPitch) > 0.9999f)
        {
            // Gimbal lock: yaw and roll become coupled
            yaw = MathF.Atan2(-rotMatrix.M13, rotMatrix.M11);
            roll = 0f;
        }
        else
        {
            yaw = MathF.Atan2(rotMatrix.M31, rotMatrix.M33);
            roll = MathF.Atan2(rotMatrix.M12, rotMatrix.M22);
        }
    }

    #endregion Euler

    #region Direction

    private static readonly Vector3 ForwardUnit = Vector3.Forward;
    private static readonly Vector3 BackwardUnit = Vector3.Backward;
    private static readonly Vector3 DownUnit = Vector3.Down;
    private static readonly Vector3 UpUnit = Vector3.Up;
    private static readonly Vector3 LeftUnit = Vector3.Left;
    private static readonly Vector3 RightUnit = Vector3.Right;

    /// <summary>The forward direction (-Z in local space, transformed by rotation).</summary>
    public readonly Vector3 Forward
    {
        // TODO: Check if inlining these getters actually work.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3.Transform(in ForwardUnit, in Rotation, out var result);
            return result;
        }
    }

    /// <summary>The backward direction (+Z in local space, transformed by rotation).</summary>
    public readonly Vector3 Backward
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3.Transform(in BackwardUnit, in Rotation, out var result);
            return result;
        }
    }

    /// <summary>The down direction (-Y in local space, transformed by rotation).</summary>
    public readonly Vector3 Down
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3.Transform(in DownUnit, in Rotation, out var result);
            return result;
        }
    }

    /// <summary>The up direction (+Y in local space, transformed by rotation).</summary>
    public readonly Vector3 Up
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3.Transform(in UpUnit, in Rotation, out var result);
            return result;
        }
    }

    /// <summary>The right direction (-X in local space, transformed by rotation).</summary>
    public readonly Vector3 Left
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3.Transform(in LeftUnit, in Rotation, out var result);
            return result;
        }
    }

    /// <summary>The right direction (+X in local space, transformed by rotation).</summary>
    public readonly Vector3 Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3.Transform(in RightUnit, in Rotation, out var result);
            return result;
        }
    }

    #endregion Direction

    #region Mutation

    /// <summary>
    /// Applies an incremental rotation around an arbitrary axis.
    /// </summary>
    /// <param name="axis">The rotation axis (will be normalized internally).</param>
    /// <param name="radians">The rotation angle in radians.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RotateAxis(in Vector3 axis, float radians)
    {
        Quaternion.CreateFromAxisAngle(in axis, radians, out Quaternion delta);
        Quaternion.Multiply(in delta, in Rotation, out Rotation);
    }

    /// <summary>
    /// Sets the rotation so that the transform faces the given target position.
    /// </summary>
    /// <param name="target">The world-space position to look at.</param>
    /// <param name="up">The up vector (typically <see cref="Vector3.Up"/>).</param>
    public void LookAt(in Vector3 target, in Vector3 up)
    {
        // TODO: Keep adding SIMD helpers for all these.
        Matrix lookMatrix = Matrix.CreateLookAt(Position, target, up);
        // CreateLookAt produces a view matrix (inverted world). We need to invert it
        // and then extract the rotation. Since it's orthonormal, transpose == inverse.
        Matrix worldFromLook = Matrix.Transpose(lookMatrix);
        Rotation = Quaternion.CreateFromRotationMatrix(worldFromLook);
    }

    #endregion Mutation

    #region Interpolation

    /// <summary>
    /// Interpolates between two transforms: Lerp for position/scale, Slerp for rotation.
    /// </summary>
    /// <param name="a">The start transform.</param>
    /// <param name="b">The end transform.</param>
    /// <param name="t">The interpolation factor (0 = a, 1 = b).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform3D Lerp(in Transform3D a, in Transform3D b, float t)
    {
        // TODO: I was sure I added Lerp SIMD extension. Fix this.
        return new Transform3D(
            Vector3.Lerp(a.Position, b.Position, t),
            Quaternion.Lerp(a.Rotation, b.Rotation, t), // TODO: I think we should use Slerp here instead?
            MathHelper.Lerp(a.Scale, b.Scale, t));
    }

    #endregion Interpolation

    /// <inheritdoc/>
    public readonly override string ToString() =>
        $"{nameof(Transform3D)}(P={Position}, R={Rotation}, S={Scale:F3})";
    

    internal string DebugDisplayString =>
        $"P:{(object)Position}  R:{(object)Rotation}  S:{(object)Scale}";
}
