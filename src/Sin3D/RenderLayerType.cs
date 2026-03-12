namespace Sin3d;

/// <summary>
/// Describes what kind of content a render layer contains,
/// which determines whether projection correction is applied during compositing.
/// </summary>
public enum RenderLayerType
{
    /// <summary>
    /// 3D scene content that may receive projection correction (e.g. Panini on Desktop).
    /// </summary>
    Scene3d,

    /// <summary>
    /// Flat/2D content that is always composited as-is (rectilinear).
    /// </summary>
    Overlay2d,
}
