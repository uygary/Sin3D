using Microsoft.Xna.Framework;

namespace Sin3d;

/// <summary>
/// A group of properties for a directional light - used to set the DirectionalLight fields of a <see cref="Renderer3d"/> object.
/// </summary>
public struct DirectionalLightPropertyGroup
{
    private bool _enabled;
    /// <summary>
    /// Whether the directional light is enabled.
    /// </summary>
    public bool Enabled { get => _enabled; set => _enabled = value; }

    private Vector3 _direction;
    /// <summary>
    /// The direction of the directional light.
    /// </summary>
    public Vector3 Direction { get => _direction; set => _direction = value; }

    private Vector3 _diffuseColor;
    /// <summary>
    /// The diffuse color of the directional light.
    /// </summary>
    public Vector3 DiffuseColor { get => _diffuseColor; set => _diffuseColor = value; }

    private Vector3 _specularColor;
    /// <summary>
    /// The specular color of the directional light.
    /// </summary>
    public Vector3 SpecularColor { get => _specularColor; set => _specularColor = value; }

    /// <summary>
    /// Creates a new DirectionalLightPropertyGroup
    /// </summary>
    /// <param name="enabled">Whether the directional light is enabled.</param>
    /// <param name="direction">The direction of the directional light.</param>
    /// <param name="diffuseColor">The diffuse color of the directional light.</param>
    /// <param name="specularColor">The specular color of the directional light.</param>
    public DirectionalLightPropertyGroup(bool enabled, Vector3 direction, Vector3 diffuseColor, Vector3 specularColor)
    {
        _enabled = enabled;
        _direction = direction;
        _diffuseColor = diffuseColor;
        _specularColor = specularColor;
    }
}