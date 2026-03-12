using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// A named render layer wrapping a <see cref="RenderTarget2D"/> with metadata
/// about its content type and compositing order.
/// </summary>
public class RenderLayer
{
    /// <summary>
    /// A descriptive name for this layer (e.g. "SolarSystem", "Fleet", "GalaxyMap").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of content this layer contains, which determines
    /// whether projection correction is applied during compositing.
    /// </summary>
    public RenderLayerType LayerType { get; }

    /// <summary>
    /// The compositing draw order (lower values are drawn first / further back).
    /// </summary>
    public int DrawOrder { get; }

    /// <summary>
    /// The render target that this layer draws into.
    /// </summary>
    public RenderTarget2D Target { get; }

    /// <summary>
    /// Creates a new <see cref="RenderLayer"/>.
    /// </summary>
    /// <param name="name">A descriptive name for this layer.</param>
    /// <param name="layerType">The type of content this layer contains.</param>
    /// <param name="drawOrder">The compositing draw order (lower = drawn first).</param>
    /// <param name="graphicsDevice">The graphics device used to create the render target.</param>
    /// <param name="width">The width of the render target in pixels.</param>
    /// <param name="height">The height of the render target in pixels.</param>
    public RenderLayer(string name, RenderLayerType layerType, int drawOrder,
        GraphicsDevice graphicsDevice, int width, int height)
    {
        Name = name;
        LayerType = layerType;
        DrawOrder = drawOrder;
        Target = new RenderTarget2D(graphicsDevice, width, height, false,
            SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
    }
}
