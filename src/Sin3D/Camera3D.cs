using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// A 3D camera class that handles view and projection matrices.
/// </summary>
public class Camera3d
{
    private Vector3 _position;
    /// <summary>
    /// The (x, y, z) position.
    /// </summary>
    public Vector3 Position { get => _position; set => _position = value; } 

    private float _yaw;
    /// <summary>
    /// The yaw (in radians).
    /// </summary>
    public float Yaw { get => _yaw; set => _yaw = value; }

    private float _pitch;
    /// <summary>
    /// The pitch (in radians).
    /// </summary>
    public float Pitch { get => _pitch; set => _pitch = value; }

    private float _roll;
    /// <summary>
    /// The roll (in radians).
    /// </summary>
    public float Roll { get => _roll; set => _roll = value; }

    private float _fov;
    /// <summary>
    /// The field of view (in radians), (the setter will only assign fov values between 0 and PI).
    /// </summary>
    public float Fov
    {
        get => _fov;
        set
        {
            if (value > 0 && value < Math.PI)
            {
                _fov = value;
                // TODO: maybe automatically update the projection matrix here? (currently left to the user to call UpdateProjectionMatrix after changing fov or near/far plane distance settings)
            }
        }
    }

    private float _nearPlaneDist;
    /// <summary>
    /// The near plane render distance (very small values could impact depth buffer precision).
    /// </summary>
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
    public BoundingFrustum Frustum { get; } = new(Matrix.Identity);

    /// <summary>
    /// Creates a new <see cref="Camera3d"/> object with position, rotation, fov and near/far plane render distance settings.
    /// </summary>
    /// <param name="position">The initial (x, y, z) position.</param>
    /// <param name="rotation">The initial (yaw, pitch, roll) rotation.</param>
    /// <param name="fov">The initial field of view.</param>
    /// <param name="nearPlaneDist">The initial near plane render distance.</param>
    /// <param name="farPlaneDist">The initial far plane render distance.</param>
    /// <param name="graphicsDevice">The graphics device, used in creating the projection matrix.</param>
    public Camera3d(Vector3 position, Vector3 rotation, float fov, float nearPlaneDist, float farPlaneDist, GraphicsDevice graphicsDevice)
    {
        _position = position;

        _yaw = rotation.X;
        _pitch = rotation.Y;
        _roll = rotation.Z;

        _fov = fov;
        _nearPlaneDist = nearPlaneDist;
        _farPlaneDist = farPlaneDist;

        //setting up the view and projection matrices
        UpdateViewMatrix();
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(fov, graphicsDevice.Viewport.AspectRatio, nearPlaneDist, farPlaneDist);
        UpdateFrustum();
    }

    /// <summary>
    /// Updates the camera's view matrix (to be used after changing position or rotation).
    /// </summary>
    /// <param name="eyeOffset">Optional offset (e.g. for VR eyes) in local camera space.</param>
    public void UpdateViewMatrix(Vector3 eyeOffset = default)
    {
        //getting cam target
        Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
        Vector3 direction = Vector3.Transform(Vector3.Forward, rotationMatrix);

        // Apply rotation to the eye offset to get it in world space relative to camera orientation
        Vector3 worldEyeOffset = Vector3.Transform(eyeOffset, rotationMatrix);
        Vector3 shiftedPosition = _position + worldEyeOffset;

        Vector3 target = direction + shiftedPosition;

        _viewMatrix = Matrix.CreateLookAt(shiftedPosition, target, Vector3.Up);
        UpdateFrustum();
    }

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
    /// Sets the view matrix directly (e.g. for VR head tracking).
    /// </summary>
    /// <param name="viewMatrix">The view matrix to use.</param>
    public void SetViewMatrix(Matrix viewMatrix)
    {
        _viewMatrix = viewMatrix;
        UpdateFrustum();
    }

    /// <summary>
    /// Updates the camera's projection matrix (to be used after changing fov or the near/far plane distance).
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used in the creation of the projection matrix.</param>
    public void UpdateProjectionMatrix(GraphicsDevice graphicsDevice)
    {
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(_fov, graphicsDevice.Viewport.AspectRatio, _nearPlaneDist, _farPlaneDist);
        UpdateFrustum();
    }

    private void UpdateFrustum()
    {
        // Internal BoundingFrustum bounds are rebuilt without allocating new objects 
        // whenever the 'Matrix' property setter is invoked.
        Frustum.Matrix = _viewMatrix * _projectionMatrix;
    }
}
