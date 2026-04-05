using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Sin3D;

/// <summary>
/// A simple triangle defined by three vertices in world space.
/// Used for terrain height sampling via ray-triangle intersection.
/// </summary>
/// <remarks>
/// Each vertex (V0, V1, V2) is an absolute position in world space, not relative to one another.
/// Together they define the three corners of the triangle in counter-clockwise or clockwise winding order.
/// </remarks>
[DataContract]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public readonly struct Triangle3(Vector3 v0, Vector3 v1, Vector3 v2) : IEquatable<Triangle3>
{
    private const float Epsilon = 1e-6f;

    /// <summary>First vertex of the triangle (absolute world-space position).</summary>
    [DataMember]
    public readonly Vector3 V0 = v0;
    /// <summary>Second vertex of the triangle (absolute world-space position).</summary>
    [DataMember]
    public readonly Vector3 V1 = v1;    //TODO: Should we make V1 and V2 relative instead? Would that make sense?
    /// <summary>Third vertex of the triangle (absolute world-space position).</summary>
    [DataMember]
    public readonly Vector3 V2 = v2;

    /// <summary>
    /// Performs a Möller–Trumbore ray-triangle intersection test.
    /// Returns the distance along the ray if it hits, or <c>null</c> if it misses.
    /// </summary>
    /// <param name="rayOrigin">Origin of the ray.</param>
    /// <param name="rayDirection">Normalized direction of the ray.</param>
    /// <returns>The parametric distance <c>t</c> along the ray, or <c>null</c> if no intersection.</returns>
    public float? RayIntersects(in Vector3 rayOrigin, in Vector3 rayDirection)
    {

        Vector3 edge1 = V1 - V0;
        Vector3 edge2 = V2 - V0;

        Vector3 h = Vector3.Cross(rayDirection, edge2);
        float a = Vector3.Dot(edge1, h);

        // Ray is parallel to the triangle
        if (a is > -Epsilon and < Epsilon)
        {
            return null;
        }

        float f = 1.0f / a;
        Vector3 s = rayOrigin - V0;
        float u = f * Vector3.Dot(s, h);

        if (u is < 0.0f or > 1.0f)
        {
            return null;
        }

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(rayDirection, q);

        if (v < 0.0f || u + v > 1.0f)
        {
            return null;
        }

        float t = f * Vector3.Dot(edge2, q);

        // Only count hits in the positive ray direction
        if (t > Epsilon)
        {
            return t;
        }

        return null;
    }

    /// <inheritdoc />
    public bool Equals(Triangle3 other) =>
        V0 == other.V0
        && V1 == other.V1
        && V2 == other.V2;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Triangle3 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(V0, V1, V2);

    public static bool operator ==(Triangle3 left, Triangle3 right) =>
        left.Equals(right);

    public static bool operator !=(Triangle3 left, Triangle3 right) =>
        !left.Equals(right);

    public override string ToString() =>
        $"{nameof(Triangle3)}(V0={V0}, V1={V1}, V2={V2})";

    internal string DebugDisplayString =>
        $"{(object)V0}  {(object)V1}  {(object)V2}";
}
