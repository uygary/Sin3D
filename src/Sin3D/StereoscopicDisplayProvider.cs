using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// A basic VR display provider that supports stereo rendering with two eyes.
/// This implementation provides standard symmetric projection matrices with an IPD offset.
/// In a real VR implementation (e.g. OpenXR), this would use asymmetric frustums provided by the VR runtime.
/// </summary>
public class StereoscopicDisplayProvider : IDisplayProvider
{
    public bool IsVr => true;
    public bool IsFlat => true;
    private readonly float _fov;
    private readonly float _nearPlaneDist;
    private readonly float _farPlaneDist;
    private readonly float _ipd;

    private Matrix _projectionMatrix;

    /// <inheritdoc/>
    public int EyeCount => 2;

    /// <inheritdoc/>
    public float TargetFov => _fov;

    /// <inheritdoc/>
    public float RenderedFov => _fov;

    /// <inheritdoc/>
    public float PaniniD => 0.0f;

    /// <summary>
    /// Creates a new <see cref="VrDisplayProvider"/>.
    /// </summary>
    /// <param name="fov">The field of view in radians.</param>
    /// <param name="nearPlaneDist">The near plane render distance.</param>
    /// <param name="farPlaneDist">The far plane render distance.</param>
    /// <param name="ipd">Interpupillary distance in world units (e.g., 0.064 for 64mm if 1 unit = 1 meter).</param>
    public StereoscopicDisplayProvider(float fov, float nearPlaneDist, float farPlaneDist, float ipd = 0.064f)
    {
        _fov = fov;
        _nearPlaneDist = nearPlaneDist;
        _farPlaneDist = farPlaneDist;
        _ipd = ipd;
    }

    /// <inheritdoc/>
    public Vector3 GetEyeOffset(int eyeIndex)
    {
        // Eye 0 is Left (-ipd/2), Eye 1 is Right (+ipd/2)
        float offset = (eyeIndex == 0) ? -_ipd * 0.5f : _ipd * 0.5f;
        return new Vector3(offset, 0, 0);
    }

    /// <inheritdoc/>
    public Matrix GetProjectionMatrix(int eyeIndex) => _projectionMatrix;

    /// <inheritdoc/>
    public Point GetLayerResolution(GraphicsDevice graphicsDevice)
    {
        // For VR, we render both eyes Side-by-Side into a single full-width target.
        // This avoids creating/switching targets per eye and fixes pass-clearing issues on Vulkan.
        return new Point(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
    }

    /// <inheritdoc/>
    public bool Initialize(GraphicsDevice graphicsDevice)
    {
        // For a basic implementation, we use a single symmetric projection for both eyes.
        // In real VR, these would be asymmetric.
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            _fov,
            graphicsDevice.Viewport.AspectRatio * 0.5f, // Half-width for Side-by-Side
            _nearPlaneDist,
            _farPlaneDist
        );

        return true;
    }

    /// <inheritdoc/>
    public void CompositeLayers(SpriteBatch spriteBatch, IReadOnlyList<RenderLayer> layers)
    {
        // Basic VR compositing: Side-by-Side (SBS)
        // This assumes the layers were rendered for the current eye.
        // NOTE: In a more complex setup, the layers themselves might need to store data for both eyes.
        // For now, we assume this is called twice (once per eye) or the Draw loop handles the viewport.

        // Sort layers by draw order
        Span<int> sortedIndices = stackalloc int[layers.Count];
        for (int i = 0; i < layers.Count; i++)
        {
            sortedIndices[i] = i;
        }

        for (int i = 1; i < sortedIndices.Length; i++)
        {
            int key = sortedIndices[i];
            int j = i - 1;
            while (j >= 0 && layers[sortedIndices[j]].DrawOrder > layers[key].DrawOrder)
            {
                sortedIndices[j + 1] = sortedIndices[j];
                j--;
            }
            sortedIndices[j + 1] = key;
        }

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);

        for (int s = 0; s < sortedIndices.Length; s++)
        {
            RenderLayer layer = layers[sortedIndices[s]];

            // Draw the layer to the current viewport (expected to be set to the left or right half of the screen)
            spriteBatch.Draw(
                layer.Target,
                new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height),
                Color.White
            );
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
    }
}
