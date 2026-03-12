using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// Display provider for desktop (flat screen) rendering.
/// Applies Panini projection correction to <see cref="RenderLayerType.Scene3d"/> layers
/// during compositing to fix perspective distortion of spheres at screen edges.
/// <see cref="RenderLayerType.Overlay2d"/> layers are composited as-is.
/// </summary>
public class DesktopDisplayProvider : IDisplayProvider
{
    private readonly float _targetFov;
    private readonly float _nearPlaneDist;
    private readonly float _farPlaneDist;
    private readonly float _paniniD;
    private float _paniniFov;

    private Matrix _projectionMatrix;
    private Effect? _paniniEffect;

    /// <inheritdoc/>
    public int EyeCount => 1;

    /// <summary>
    /// Creates a new <see cref="DesktopDisplayProvider"/>.
    /// </summary>
    /// <param name="targetFov">The field of view in radians.</param>
    /// <param name="renderedFov">The wider field of view before a Panini projection is applied, in radians.</param>
    /// <param name="nearPlaneDist">The near plane render distance.</param>
    /// <param name="farPlaneDist">The far plane render distance.</param>
    /// <param name="paniniD">Panini compression strength (0.0 = rectilinear, 1.0 = full Panini).</param>
    public DesktopDisplayProvider(float targetFov, float nearPlaneDist, float farPlaneDist, float paniniD = 1.0f)
    {
        _targetFov = targetFov;
        _nearPlaneDist = nearPlaneDist;
        _farPlaneDist = farPlaneDist;
        _paniniD = paniniD;
    }

    /// <inheritdoc/>
    public Vector3 GetEyeOffset(int eyeIndex) => Vector3.Zero;

    /// <inheritdoc/>
    public Matrix GetProjectionMatrix(int eyeIndex) => _projectionMatrix;

    /// <inheritdoc/>
    public void Initialize(GraphicsDevice graphicsDevice)
    {
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            _targetFov,
            graphicsDevice.Viewport.AspectRatio,
            _nearPlaneDist,
            _farPlaneDist
        );

        _paniniFov = CalculatePaniniFov(_targetFov, graphicsDevice.Viewport.AspectRatio, _paniniD);
    }

    /// <summary>
    /// Loads the Panini composite shader from a content manager.
    /// Must be called after <see cref="Initialize"/> and after content is available.
    /// </summary>
    /// <param name="contentManager">The content manager to load the shader from.</param>
    /// <param name="paniniCompositeShaderName">The asset name of the Panini composite shader (without extension).</param>
    public void LoadContent(ContentManager contentManager, string paniniCompositeShaderName = "PaniniComposite")
    {
        _paniniEffect = contentManager.Load<Effect>(paniniCompositeShaderName);
    }

    public float CalculatePaniniFov(float targetFov, float aspectRatio, float paniniD)
    {
        float targetHalfTan = (float)Math.Tan(targetFov * 0.5f);
        float xEdge = targetHalfTan * aspectRatio;

        float dPlus1 = paniniD + 1.0f;
        float inner = (dPlus1 * dPlus1) - (paniniD * xEdge * xEdge);

        // Prevent math errors if Panini parameter is pushed too high
        if (inner <= 0f) return targetFov;

        float xSource = (xEdge * dPlus1) / (float)Math.Sqrt(inner);
        float requiredVerticalHalfTan = xSource / aspectRatio;

        return 2.0f * (float)Math.Atan(requiredVerticalHalfTan);
    }

    /// <inheritdoc/>
    public void CompositeLayers(SpriteBatch spriteBatch, IReadOnlyList<RenderLayer> layers)
    {
        // Sort layers by draw order (ascending — lower values drawn first)
        Span<int> sortedIndices = stackalloc int[layers.Count];
        for (int i = 0; i < layers.Count; i++)
        {
            sortedIndices[i] = i;
        }

        // Simple insertion sort for small layer counts
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

        for (int s = 0; s < sortedIndices.Length; s++)
        {
            RenderLayer layer = layers[sortedIndices[s]];

            if (layer.LayerType == RenderLayerType.Scene3d && _paniniEffect is not null)
            {
                // Apply Panini projection correction
                _paniniEffect.Parameters["PaniniD"]?.SetValue(_paniniD);
                _paniniEffect.Parameters["TargetFOV"]?.SetValue(_targetFov);
                _paniniEffect.Parameters["RenderedFOV"]?.SetValue(_paniniFov);
                _paniniEffect.Parameters["AspectRatio"]?.SetValue(
                    (float)layer.Target.Width / layer.Target.Height);

                spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    null,
                    null,
                    _paniniEffect
                );
            }
            else
            {
                // Overlay2D or no shader available — straight blit
                spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    null,
                    null,
                    null
                );
            }

            spriteBatch.Draw(
                layer.Target,
                new Rectangle(0, 0,
                    spriteBatch.GraphicsDevice.Viewport.Width,
                    spriteBatch.GraphicsDevice.Viewport.Height),
                Color.White
            );

            spriteBatch.End();
        }
    }
}
