using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace Sin3D;

/// <summary>
/// A 6-degrees-of-freedom camera backed by <see cref="Transform3D"/>.
/// Unlike <see cref="Camera3D"/>, which uses Euler angles and assumes a gravity-aligned world,
/// Camera6Dof stores orientation as a quaternion.
/// This makes it suitable for Elite: Dangerous style space sims, zero-G environments, etc.
/// </summary>
/// <remarks>
/// This is yet untested.   // TODO: Test it!
/// <para>
/// The view matrix is computed as inverse of the camera's world transform,
/// avoiding <c>CreateLookAt</c> and its hard-0coded up vector assumption.
/// This means the camera can orient freely in any direction without gimbal lock.
/// </para>
/// <para>
/// Incremental rotation should be applied via <see cref="RotateLocal"/>
/// or by mutating <see cref="Camera6Dof.Transform"/> directly with <see cref="Transform3D.RotateAxis"/>.
/// </para>
/// <para>
/// For FPS or RTS games with a gravity-constrained camera, prefer <see cref="Camera3D"/>.
/// </para>
/// </remarks>
public class Camera6Dof : ICamera
{
    private Transform3D _transform;

    /// <summary>
    /// The spatial transform (position, rotation, scale) of the camera.
    /// Modify this directly for 6DOF control, then call <see cref="UpdateViewMatrix"/>.
    /// </summary>
    /// <remarks>
    /// Scale is ignored, since it wouldn't serve a purpose in a camera.
    /// // TODO: Is there anything we can actually meaningfully tie scale to in a camera? Likely not, but think about this. Or else, should we have a base transform struct without scale?
    /// </remarks>
    public ref Transform3D Transform => ref _transform;

    /// <summary>
    /// The camera's world-space position.
    /// </summary>
    /// <remarks>
    /// This is a shortcut for <c>Transform.Position</c>
    /// </remarks>
    public Vector3 Position
    {
        // TODO: Again, are these inlining hints actually necessary or helpful? Especially in the case of AOT. Profile them.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _transform.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _transform.Position = value;
    }

    private float _fov;
    /// <summary>
    /// The field of view in radians.
    /// </summary>
    /// <remarks>
    /// The setter rejects values outside the (0, π) range.
    /// </remarks>
    public float Fov
    {
        get => _fov;
        set
        {
            if (value > 0 && value < Math.PI)
            {
                _fov = value;
            }
        }
    }

    private float _nearPlaneDist;
    /// <summary>
    /// The near plane render distance.
    /// </summary>
    /// <remarks>
    /// Note to self:
    /// Very small values can impact <see href="https://docs.monogame.net/articles/getting_to_know/whatis/graphics/WhatIs_DepthBuffer.html">depth buffer precision</see> in the already too large galaxy.
    /// </remarks>
    public float NearPlaneDist { get => _nearPlaneDist; set => _nearPlaneDist = value; }

    private float _farPlaneDist;
    /// <summary>
    /// The far plane render distance.
    /// </summary>
    public float FarPlaneDist { get => _farPlaneDist; set => _farPlaneDist = value; }

    private Matrix _viewMatrix;
    /// <summary>
    /// The view matrix.
    /// </summary>
    public Matrix ViewMatrix => _viewMatrix;

    private Matrix _projectionMatrix;
    /// <summary>
    /// The projection matrix.
    /// </summary>
    public Matrix ProjectionMatrix => _projectionMatrix;

    /// <summary>
    /// The camera's viewing frustum.
    /// Used for culling out-of-bounds objects to increase FPS.
    /// </summary>
    public BoundingFrustum Frustum { get; } = new(Matrix.Identity); // TODO: Can we also somehow perform partial model rendering to increase perf further? Is that a thing?

    /// <summary>
    /// Creates a new <see cref="Camera6Dof"/> with the given transform and projection settings.
    /// </summary>
    /// <param name="transform">The initial spatial transform (position and rotation).</param>
    /// <param name="fov">The initial field of view in radians.</param>
    /// <param name="nearPlaneDist">The near plane render distance.</param>
    /// <param name="farPlaneDist">The far plane render distance.</param>
    /// <param name="graphicsDevice">The graphics device, used for computing the projection matrix aspect ratio.</param>
    public Camera6Dof(Transform3D transform,
        float fov,
        float nearPlaneDist,
        float farPlaneDist,
        GraphicsDevice graphicsDevice)
    {
        _transform = transform;
        _fov = fov;
        _nearPlaneDist = nearPlaneDist;
        _farPlaneDist = farPlaneDist;

        UpdateViewMatrix();

        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            fov, graphicsDevice.Viewport.AspectRatio, nearPlaneDist, farPlaneDist);

        UpdateFrustum();
    }

    /// <summary>
    /// Creates a new <see cref="Camera6Dof"/> at the given position with identity rotation.
    /// </summary>
    /// <param name="position">The initial world-space position.</param>
    /// <param name="fov">The initial field of view in radians.</param>
    /// <param name="nearPlaneDist">The near plane render distance.</param>
    /// <param name="farPlaneDist">The far plane render distance.</param>
    /// <param name="graphicsDevice">The graphics device, used for computing the projection matrix aspect ratio.</param>
    public Camera6Dof(Vector3 position,
        float fov,
        float nearPlaneDist,
        float farPlaneDist,
        GraphicsDevice graphicsDevice)
        : this(new Transform3D(position), fov, nearPlaneDist, farPlaneDist, graphicsDevice)
    {
    }

    #region Rotation

    /// <summary>
    /// Applies an incremental rotation around a local-space axis.
    /// </summary>
    /// <remarks>
    /// Common local axes:
    /// <list type="bullet">
    /// <item><see cref="Vector3.UnitY"/> — yaw (turn left/right)</item>
    /// <item><see cref="Vector3.UnitX"/> — pitch (nose up/down)</item>
    /// <item><see cref="Vector3.UnitZ"/> — roll (bank left/right)</item>
    /// </list>
    /// Because the rotation is applied in local space (pre-multiplied quaternion),
    /// these always rotate relative to the camera's current orientation, not the world axes.
    /// </remarks>
    /// <param name="localAxis">The local-space rotation axis.</param>
    /// <param name="radians">The rotation angle in radians.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RotateLocal(in Vector3 localAxis, float radians)
    {
        _transform.RotateAxis(in localAxis, radians);
    }

    /// <summary>
    /// Sets the camera's rotation so that it faces the given target position.
    /// </summary>
    /// <param name="target">The world-space position to look at.</param>
    /// <param name="up">The reference up vector used to orient the camera's roll.
    /// In space, this is typically the ship's local up or the galaxy plane normal.</param>
    public void LookAt(in Vector3 target, in Vector3 up)
    {
        _transform.LookAt(in target, in up);
    }

    #endregion Rotation

    #region View Matrix

    /// <summary>
    /// Updates the camera's view matrix from the current <see cref="Transform"/>.
    /// Call after modifying transform properties.
    /// </summary>
    /// <param name="eyeOffset">Optional offset (e.g. for VR eyes) in local camera space.</param>
    public void UpdateViewMatrix(Vector3 eyeOffset = default)
    {
        _transform.ToWorldMatrix(out var worldMatrix);

        // Apply VR eye offset in the camera's local space
        if (eyeOffset != default)
        {
            var worldEyeOffset = Vector3.Transform(eyeOffset, _transform.Rotation);
            worldMatrix.Translation += worldEyeOffset;
        }

        // View = inverse of world transform.
        // For an orthonormal rotation matrix (Scale == 1), this is equivalent
        // to transposing the 3×3 rotation block and negating the translation.
        _viewMatrix = Matrix.Invert(worldMatrix);

        UpdateFrustum();
    }

    /// <summary>
    /// Sets the view matrix directly (e.g. for VR head tracking via OpenXR).
    /// </summary>
    /// <param name="viewMatrix">The view matrix to use.</param>
    public void SetViewMatrix(Matrix viewMatrix)
    {
        _viewMatrix = viewMatrix;

        UpdateFrustum();
    }

    #endregion View Matrix

    #region Projection Matrix

    /// <summary>
    /// Sets the projection matrix directly.
    /// </summary>
    /// <param name="projection">The projection matrix to use.</param>
    public void SetProjection(Matrix projection)
    {
        _projectionMatrix = projection;

        UpdateFrustum();
    }

    /// <summary>
    /// Updates the camera's projection matrix from current <see cref="Fov"/>,
    /// <see cref="NearPlaneDist"/> and <see cref="FarPlaneDist"/> settings.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for aspect ratio calculation.</param>
    public void UpdateProjectionMatrix(GraphicsDevice graphicsDevice)
    {
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            _fov, graphicsDevice.Viewport.AspectRatio, _nearPlaneDist, _farPlaneDist);

        UpdateFrustum();
    }

    #endregion Projection Matrix

    private void UpdateFrustum()
    {
        // BoundingFrustum planes are rebuilt without allocation
        // whenever the Matrix property setter is invoked.
        Frustum.Matrix = _viewMatrix * _projectionMatrix;
    }
}
