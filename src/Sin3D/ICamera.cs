using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3D;

/// <summary>
/// Contract for all camera types.
/// Exposes what's needed by <see cref="Renderer3D"/> to render a scene:
/// view/projection matrices, world-space position, and a frustum for culling.
/// </summary>
/// <remarks>
/// <para>
/// Implemented by <see cref="Camera3D"/> (Euler-based, gravity-constrained)
/// and <see cref="Camera6Dof"/> (quaternion-based, 6DOF).
/// </para>
/// <para>
/// According to Google, performance cost of interface dispatch is negligible at ~0.005% of a 90 Hz frame budget,
/// and it's further mitigated by the <see href="https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-6/#:~:text=The%20JIT%20optimizes%20for%20PGO%20data,to%2C%20well%2C%20learn.">JIT's GDV</see>.
/// </para>
/// </remarks>
public interface ICamera
{
    /// <summary>
    /// The camera's world-space position.
    /// Used by <see cref="Renderer3D"/> for camera-relative rendering (CRR).
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// The view matrix (world-to-camera transform).
    /// </summary>
    Matrix ViewMatrix { get; }

    /// <summary>
    /// The projection matrix (camera-to-clip transform).
    /// </summary>
    Matrix ProjectionMatrix { get; }

    /// <summary>
    /// The camera's viewing frustum, used for culling out-of-bounds meshes.
    /// </summary>
    BoundingFrustum Frustum { get; }
}
