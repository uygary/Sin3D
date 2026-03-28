using Microsoft.Xna.Framework;
using Sin3d.Extensions;

namespace Sin3d;

/// <summary>
/// A 3D oriented bounding box used for SAT-based collision detection.
/// </summary>
/// <remarks>
/// Designed for reuse in object pooling.
/// Call <see cref="Reset"/> with a new AABB each frame instead of creating a new instance.
/// </remarks>
public class OrientedBoundingBox3D
{
    private const float DegenerateAxisGuardThreshold = 1E-05f;

    private readonly Vector3[] _vertices = new Vector3[8];
    /// <summary>
    /// The array of vertices (8 corners) of the box.
    /// </summary>
    /// <remarks>
    /// Filled by <see cref="Reset"/> and transformed by <see cref="TransformVertices"/> to reflect the current state of the box.
    /// </remarks>
    public Vector3[] Vertices => _vertices;

    #region Per-frame scratch buffers
    
    // For use in SAT. Sized for worst case, and reused across updates.
    // 3 local axes per box = 6.
    // + 9 cross products = 15 total candidate axes.
    private readonly Vector3[] _axes = new Vector3[15];
    
    private readonly Vector3[] _thisBoxLocalAxes = new Vector3[3];
    private readonly Vector3[] _otherBoxLocalAxes = new Vector3[3];

    #endregion Per-frame scratch buffers

    /// <summary>
    /// Creates a new oriented bounding box, initialized from an AABB.
    /// </summary>
    /// <param name="aabb">AABB to initialize from.</param>
    public OrientedBoundingBox3D(in BoundingBox aabb)
    {
        aabb.ReadonlyGetCorners(_vertices);
    }

    /// <summary>
    /// Creates an uninitialized OBB.
    /// </summary>
    /// <remarks><see cref="Reset"/> must be called before the new instance can be used.</remarks>
    public OrientedBoundingBox3D()
    {
    }

    /// <summary>
    /// Re-initializes this OBB from an axis-aligned bounding box.
    /// </summary>
    /// <remarks>This allows reuse, thus zero-allocation.
    /// Also, <c>aabb</c> is passed by reference, avoiding a new copy.</remarks>
    public void Reset(in BoundingBox aabb)
    {
        aabb.ReadonlyGetCorners(_vertices);
    }

    /// <summary>
    /// Transforms the vertices of the oriented bounding box by a given transformation.
    /// </summary>
    /// <param name="transform">The transformation that will be applied to the vertices.</param>
    public void TransformVertices(in Matrix transform)
    {
        for (var i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = Vector3.Transform(_vertices[i], transform);
        }
    }

    /// <summary>
    /// Checks if 2 oriented bounding boxes intersect using the Separating Axis Theorem (SAT).
    /// </summary>
    /// <param name="otherBox">The other box.</param>
    /// <returns>Whether an intersection was detected.</returns>
    /// <remarks>Zero-allocation. All scratch buffers are pre-allocated.</remarks>
    public bool Intersects(OrientedBoundingBox3D otherBox)
    {
        // Extract local axes from each box's corners.
        PopulateLocalAxes(_vertices, _thisBoxLocalAxes);
        PopulateLocalAxes(otherBox._vertices, _otherBoxLocalAxes);

        // Build the full set of candidate separating axes:
        // 3 from this box + 3 from the other box + 9 cross products = 15
        int axisCount = 0;

        // Copy local axes of this box.
        for (int i = 0; i < 3; i++)
        {
            var lenSq = _thisBoxLocalAxes[i].LengthSquared();
            if (lenSq > DegenerateAxisGuardThreshold)
            {
                _axes[axisCount++] = Vector3.Normalize(_thisBoxLocalAxes[i]);
            }
        }

        // Copy local axes of the other box.
        for (int i = 0; i < 3; i++)
        {
            var lenSq = _otherBoxLocalAxes[i].LengthSquared();
            if (lenSq > DegenerateAxisGuardThreshold)
            {
                _axes[axisCount++] = Vector3.Normalize(_otherBoxLocalAxes[i]);
            }
        }

        // Calculate cross products.
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Vector3 cross = Vector3.Cross(_thisBoxLocalAxes[i], _otherBoxLocalAxes[j]);
                if (cross.LengthSquared() > DegenerateAxisGuardThreshold)
                {
                    _axes[axisCount++] = Vector3.Normalize(cross);
                }
            }
        }

        // SAT overlap test on each axis.
        for (int a = 0; a < axisCount; a++)
        {
            Vector3 axis = _axes[a];

            // Project this box.
            // Inline min/max to avoid array allocation.
            var minThis = float.MaxValue;
            var maxThis = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var dot = Vector3.Dot(_vertices[i], axis);
                if (dot < minThis)
                {
                    minThis = dot;
                }

                if (dot > maxThis)
                {
                    maxThis = dot;
                }
            }

            // Project the other box.
            var minOther = float.MaxValue;
            var maxOther = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var dot = Vector3.Dot(otherBox._vertices[i], axis);
                if (dot < minOther)
                {
                    minOther = dot;
                }

                if (dot > maxOther)
                {
                    maxOther = dot;
                }
            }

            if (maxThis < minOther || maxOther < minThis)
            {
                // No intersection.
                return false;
            }
        }

        // No separating axis found.
        return true;
    }

    /// <summary>
    /// Extracts the 3 local axes from the OBB corners into the provided buffer.
    /// </summary>
    /// <param name="vertices">The 8 vertices of the OBB.</param>
    /// <param name="localAxes">The array whose contents will be overwritten.</param>
    /// <remarks>Remember that localAxes is being mutated.</remarks>
    private static void PopulateLocalAxes(Vector3[] vertices, Vector3[] localAxes)
    {
        localAxes[0] = vertices[0] - vertices[1]; // local x
        localAxes[1] = vertices[0] - vertices[3]; // local y
        localAxes[2] = vertices[0] - vertices[4]; // local z
    }
}
