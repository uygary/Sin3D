using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sin3d.Extensions.Simd;

namespace Sin3d;

/// <summary>
/// A 3D renderer class used for drawing <see cref="Model3d"/> objects and handling ambient lighting, directional lighting and fog.
/// </summary>
public class Renderer3d
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly bool _useCrr;

    private float _effectAlpha;
    /// <summary>
    /// The alpha value that will be used in rendering.
    /// </summary>
    public float EffectAlpha { get => _effectAlpha; set => _effectAlpha = value; }

    private bool _defaultLightingEnabled;
    /// <summary>
    /// Whether default lighting is enabled for rendering (overrides all other lighting settings).
    /// </summary>
    public bool DefaultLightingEnabled { get => _defaultLightingEnabled; set => _defaultLightingEnabled = value; }

    private bool _lightingEnabled;
    /// <summary>
    /// Whether lighting is enabled for rendering.
    /// </summary>
    public bool LightingEnabled { get => _lightingEnabled; set => _lightingEnabled = value; }

    private Vector3 _ambientLightColor;
    /// <summary>
    /// The floating point ambient light color for rendering (lighting must be enabled).
    /// </summary>
    public Vector3 AmbientLightColor { get => _ambientLightColor; set => _ambientLightColor = value; }

    private DirectionalLightPropertyGroup _directionalLight0;
    /// <summary>
    /// The 1st directional light that can be used in rendering (lighting must be enabled).
    /// </summary>
    public DirectionalLightPropertyGroup DirectionalLight0 { get => _directionalLight0; set => _directionalLight0 = value; }

    private DirectionalLightPropertyGroup _directionalLight1;
    /// <summary>
    /// The 2nd directional light that can be used in rendering (lighting must be enabled).
    /// </summary>
    public DirectionalLightPropertyGroup DirectionalLight1 { get => _directionalLight1; set => _directionalLight1 = value; }

    private DirectionalLightPropertyGroup _directionalLight2;
    /// <summary>
    /// The 3rd directional light that can be used in rendering (lighting must be enabled).
    /// </summary>
    public DirectionalLightPropertyGroup DirectionalLight2 { get => _directionalLight2; set => _directionalLight2 = value; }

    private bool _fogEnabled;
    /// <summary>
    /// Whether fog is enabled.
    /// </summary>
    public bool FogEnabled { get => _fogEnabled; set => _fogEnabled = value; }

    private Vector3 _fogColor;
    /// <summary>
    /// The floating point fog color.
    /// </summary>
    public Vector3 FogColor { get => _fogColor; set => _fogColor = value; }

    private float _fogStart;
    /// <summary>
    /// The distance that fog rendering will start at.
    /// </summary>
    public float FogStart { get => _fogStart; set => _fogStart = value; }

    private float _fogEnd;
    /// <summary>
    /// The distance that fog rendering will end at.
    /// </summary>
    public float FogEnd { get => _fogEnd; set => _fogEnd = value; }

    /// <summary>
    /// Creates a new <see cref="Renderer3d"/> object.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device that the renderer will target.</param>
    /// <param name="useCrr">Whether to use the camera-relative rendering projection matrix.</param>
    public Renderer3d(GraphicsDevice graphicsDevice, bool useCrr)
    {
        _graphicsDevice = graphicsDevice;
        _useCrr = useCrr;
        ResetRenderingSettings();
    }

    /// <summary>
    /// Resets the rendering settings (alpha = 1f, all lighting disabled, fog disabled) (should be called before/after drawing a new object (unless you want settings to carry over)).
    /// </summary>
    public void ResetRenderingSettings()
    {
        //resetting depth blend state/depth stencil
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;

        //resetting light fields
        _effectAlpha = 1f;
        _defaultLightingEnabled = false;
        _lightingEnabled = false;
        _ambientLightColor = Vector3.Zero;

        //resetting directional light fields
        _directionalLight0 = new DirectionalLightPropertyGroup();
        _directionalLight1 = new DirectionalLightPropertyGroup();
        _directionalLight2 = new DirectionalLightPropertyGroup();

        //resetting fog fields
        _fogEnabled = false;
        _fogColor = Vector3.Zero;
        _fogStart = 0f;
        _fogEnd = 1f;
    }

    /// <summary>
    /// Draws a Model3d object (all opaque objects should be drawn before transparent objects).
    /// </summary>
    /// <param name="model">The model that will be drawn.</param>
    /// <param name="camera">The camera that will be used as the viewpoint from which to draw from.</param>
    public void DrawModel3d(Model3d model, Camera3d camera)
    {
        //handling transparency
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        if (_effectAlpha != 1f)
        {
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }

        //drawing
        for (int i = 0; i < model.BaseModel.Meshes.Count; i++)
        {
            ModelMesh mesh = model.BaseModel.Meshes[i];
            for (int j = 0; j < mesh.Effects.Count; j++)
            {
                BasicEffect effect = (BasicEffect)mesh.Effects[j];
                // Setting up the effect to draw the model
                
                if (_useCrr)
                {
                    // Camera-relative rendering for VR jitter reduction:
                    // Perform rendering relative to the camera position by adjust the world and view matrices.
                    // This keeps World*View math near the origin, where float precision is highest.
                    Vector3 translation = -camera.Position;
                    Matrix.CreateTranslation(in translation, out Matrix translationMatrix);
                    Matrix worldMatrix = model.WorldMatrix;
                    Matrix.Multiply(in worldMatrix, in translationMatrix, out Matrix relativeWorld);
                    Matrix relativeView = camera.ViewMatrix;

                    // Clear the translation from the view matrix since we moved the world instead.
                    // This prevents double-translation and ensures we stay at origin.
                    relativeView.M41 = 0;
                    relativeView.M42 = 0;
                    relativeView.M43 = 0;

                    effect.World = relativeWorld;
                    effect.View = relativeView;
                }
                else
                {
                    effect.World = model.WorldMatrix;
                    effect.View = camera.ViewMatrix;
                }
                effect.Projection = camera.ProjectionMatrix;

                //handling effect texture
                effect.TextureEnabled = false; //ensuring texture is reset
                if (model.MeshTextures is not null && i < model.MeshTextures.Count)
                {
                    effect.TextureEnabled = true;
                    effect.Texture = model.MeshTextures[i];
                }

                //handling effect lighting
                effect.Alpha = _effectAlpha;
                if (_defaultLightingEnabled)
                {
                    effect.EnableDefaultLighting();
                }
                else //if default lighting is not enabled - set up manual lighting
                {
                    effect.LightingEnabled = _lightingEnabled;
                    effect.AmbientLightColor = _ambientLightColor;

                    //handling directional effect lighting (and resetting the directional lights if EnableDefaultLighting() modified them)
                    effect.DirectionalLight0.Enabled = _directionalLight0.Enabled;
                    effect.DirectionalLight0.Direction = _directionalLight0.Direction;
                    effect.DirectionalLight0.DiffuseColor = _directionalLight0.DiffuseColor;
                    effect.DirectionalLight0.SpecularColor = _directionalLight0.SpecularColor;

                    effect.DirectionalLight1.Enabled = _directionalLight1.Enabled;
                    effect.DirectionalLight1.Direction = _directionalLight1.Direction;
                    effect.DirectionalLight1.DiffuseColor = _directionalLight1.DiffuseColor;
                    effect.DirectionalLight1.SpecularColor = _directionalLight1.SpecularColor;
                    
                    effect.DirectionalLight2.Enabled = _directionalLight2.Enabled;
                    effect.DirectionalLight2.Direction = _directionalLight2.Direction;
                    effect.DirectionalLight2.DiffuseColor = _directionalLight2.DiffuseColor;
                    effect.DirectionalLight2.SpecularColor = _directionalLight2.SpecularColor;
                }

                //handling effect fog
                effect.FogEnabled = _fogEnabled;
                effect.FogColor = _fogColor;
                effect.FogStart = _fogStart;
                effect.FogEnd = _fogEnd;
            }
            mesh.Draw();
        }
    }
}
