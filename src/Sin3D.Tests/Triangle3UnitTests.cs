using Microsoft.Xna.Framework;
using Sin3D;
using Xunit;

namespace Sin3D.Tests;

public class Triangle3UnitTests
{
    /// <summary>
    /// A simple flat triangle on the XZ plane at Y=0.
    /// </summary>
    private static readonly Triangle3 FlatTriangle = new(
        new Vector3(0, 0, 0),
        new Vector3(10, 0, 0),
        new Vector3(5, 0, 10));

    [Fact]
    public void RayDown_HitsCenter_ReturnsDistance()
    {
        // Ray from Y=100 straight down at the centroid
        Vector3 origin = new Vector3(5, 100, 3);
        Vector3 direction = -Vector3.UnitY;

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.NotNull(t);
        Assert.Equal(100f, t!.Value, 0.001f);
    }

    [Fact]
    public void RayDown_HitsVertex_ReturnsDistance()
    {
        // Ray aimed at V0 (0,0,0)
        Vector3 origin = new Vector3(0, 50, 0);
        Vector3 direction = -Vector3.UnitY;

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.NotNull(t);
        Assert.Equal(50f, t!.Value, 0.001f);
    }

    [Fact]
    public void RayDown_Miss_ReturnsNull()
    {
        // Ray completely outside the triangle
        Vector3 origin = new Vector3(20, 100, 20);
        Vector3 direction = -Vector3.UnitY;

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.Null(t);
    }

    [Fact]
    public void RayUp_NoHit_WhenBelowTriangle()
    {
        // Ray going up from below the triangle — should not hit (wrong direction)
        Vector3 origin = new Vector3(5, -10, 3);
        Vector3 direction = -Vector3.UnitY; // Still going down, away from the triangle

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.Null(t); // Ray goes away from triangle
    }

    [Fact]
    public void RayParallel_ReturnsNull()
    {
        // Ray parallel to the triangle plane
        Vector3 origin = new Vector3(0, 5, 0);
        Vector3 direction = Vector3.UnitX;

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.Null(t);
    }

    [Fact]
    public void RayDown_ElevatedTriangle_ReturnsCorrectHeight()
    {
        // Triangle at Y=25
        var tri = new Triangle3(
            new Vector3(-5, 25, -5),
            new Vector3(5, 25, -5),
            new Vector3(0, 25, 5));

        Vector3 origin = new Vector3(0, 100, 0);
        Vector3 direction = -Vector3.UnitY;

        float? t = tri.RayIntersects(in origin, in direction);

        Assert.NotNull(t);
        // Height = origin.Y - t = 100 - 75 = 25
        float hitY = origin.Y - t!.Value;
        Assert.Equal(25f, hitY, 0.001f);
    }

    [Fact]
    public void RayDown_TiltedTriangle_ReturnsInterpolatedHeight()
    {
        // Triangle tilted: V0 at Y=0, V1 at Y=10, V2 at Y=5
        var tri = new Triangle3(
            new Vector3(0, 0, 0),
            new Vector3(10, 10, 0),
            new Vector3(5, 5, 10));

        // Ray at the centroid should hit at Y ≈ 5
        Vector3 origin = new Vector3(5, 100, 3.33f);
        Vector3 direction = -Vector3.UnitY;

        float? t = tri.RayIntersects(in origin, in direction);

        Assert.NotNull(t);
        float hitY = origin.Y - t!.Value;
        Assert.InRange(hitY, 4.0f, 6.0f); // approximately 5
    }

    [Fact]
    public void RayDown_NearEdge_StillHits()
    {
        // Point very close to an edge but inside
        Vector3 origin = new Vector3(2.5f, 50, 0.01f);
        Vector3 direction = -Vector3.UnitY;

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.NotNull(t);
    }

    [Fact]
    public void RayDown_JustOutsideEdge_Misses()
    {
        // Point just outside the triangle
        Vector3 origin = new Vector3(-0.1f, 50, -0.1f);
        Vector3 direction = -Vector3.UnitY;

        float? t = FlatTriangle.RayIntersects(in origin, in direction);

        Assert.Null(t);
    }
}
