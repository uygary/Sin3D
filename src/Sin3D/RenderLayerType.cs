namespace Sin3D;

/// <summary>
/// Describes what kind of content a render layer contains,
/// which determines whether projection correction is applied during compositing.
/// </summary>
public enum RenderLayerType // TODO: I don't think we really need this, and at the very least, I'm confident it shouldn't be in Sin3D. Think.
{
    /// <summary>
    /// 3D scene content that may receive projection correction (e.g. Panini on Desktop).
    /// </summary>
    Scene3D,

    /// <summary>
    /// Flat/2D content that is always composited as-is (rectilinear).
    /// </summary>
    Overlay2D,
}
