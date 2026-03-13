using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// Abstracts how the rendered scene is presented to the user.
/// Desktop providers apply projection correction (e.g. Panini) to 3D layers;
/// VR providers submit per-eye render targets to the VR compositor.
/// </summary>
public interface IDisplayProvider
{
    /// <summary>
    /// The number of eyes to render (1 for desktop, 2 for VR).
    /// </summary>
    int EyeCount { get; }

    /// <summary>
    /// The target visual field of view in radians.
    /// </summary>
    float TargetFov { get; }

    /// <summary>
    /// The actual rendered field of view in radians (may be wider than TargetFov for Panini).
    /// </summary>
    float RenderedFov { get; }

    /// <summary>
    /// The Panini compression parameter (0.0 = rectilinear).
    /// </summary>
    float PaniniD { get; }

    /// <summary>
    /// Gets the camera position offset for the given eye in world space.
    /// Returns <see cref="Vector3.Zero"/> for desktop (single eye).
    /// </summary>
    /// <param name="eyeIndex">The eye index (0 or 1).</param>
    /// <returns>The position offset to apply to the camera for this eye.</returns>
    Vector3 GetEyeOffset(int eyeIndex);

    /// <summary>
    /// Gets the projection matrix for the given eye.
    /// </summary>
    /// <param name="eyeIndex">The eye index (0 or 1).</param>
    /// <returns>The projection matrix.</returns>
    Matrix GetProjectionMatrix(int eyeIndex);

    /// <summary>
    /// Gets the desired resolution for render layers (e.g. half-width for VR eyes).
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <returns>The width and height for a render layer target.</returns>
    Point GetLayerResolution(GraphicsDevice graphicsDevice);

    /// <summary>
    /// Initializes the display provider (loads shaders, creates resources, etc.).
    /// Must be called after the graphics device is ready.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    void Initialize(GraphicsDevice graphicsDevice);

    /// <summary>
    /// Composites the given render layers to the current render target (typically the backbuffer),
    /// applying per-layer post-processing as appropriate (e.g. Panini for <see cref="RenderLayerType.Scene3d"/> on Desktop).
    /// Layers are drawn in ascending <see cref="RenderLayer.DrawOrder"/>.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use for drawing fullscreen quads.</param>
    /// <param name="layers">The render layers to composite, in any order (will be sorted by DrawOrder).</param>
    void CompositeLayers(SpriteBatch spriteBatch, IReadOnlyList<RenderLayer> layers);
}
