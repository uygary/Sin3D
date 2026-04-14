using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Sin3D.Extensions.Simd;

namespace Sin3D;

/// <summary>
/// A zero-allocation helper that assigns lights to screen-space tiles
/// for Forward+ rendering via projection-based bounding-rect overlap.
/// </summary>
public class TileFrustumCuller
{
    private readonly int _tileCountX;
    private readonly int _tileCountY;
    private readonly int _maxLightsPerTile;

    /// <summary>
    /// Creates a new instance of the culler. Arrays are allocated once to prevent GC pressure.
    /// </summary>
    public TileFrustumCuller(int tileCountX, int tileCountY, int maxLightsPerTile)
    {
        _tileCountX = tileCountX;
        _tileCountY = tileCountY;
        _maxLightsPerTile = maxLightsPerTile;
    }

    /// <summary>
    /// Projects each light's bounding sphere into screen-space tile coordinates
    /// and writes overlapping light indices into the tile grid buffer.
    /// </summary>
    /// <param name="viewMatrix">The combined view matrix (CRR-adjusted).</param>
    /// <param name="projectionMatrix">The projection matrix.</param>
    /// <param name="crrPositions">CRR light positions (pre-calculated to be camera-relative).</param>
    /// <param name="radii">Light radii in world units.</param>
    /// <param name="activeLightCount">Number of active lights this frame.</param>
    /// <param name="tileIndexBuffer">Output: flat [tileIndex * maxLightsPerTile + slot] buffer.</param>
    /// <param name="screenWidth">Viewport width in pixels.</param>
    /// <param name="screenHeight">Viewport height in pixels.</param>
    public void CullLights(
        in Matrix viewMatrix,
        in Matrix projectionMatrix,
        in Vector3[] crrPositions,
        float[] radii,
        int activeLightCount,
        float[] tileIndexBuffer,
        int screenWidth,
        int screenHeight)
    {
        var totalTiles = _tileCountX * _tileCountY;

        // Pre-compute per-light screen-space AABBs
        // (minTileX, minTileY, maxTileX, maxTileY) packed per light
        var lightTileBounds = new int[activeLightCount * 4];

        for (var i = 0; i < activeLightCount; i++)
        {
            ComputeLightTileBounds(
                in viewMatrix,
                in projectionMatrix,
                in crrPositions[i],
                radii[i],
                screenWidth,
                screenHeight,
                out var minTileX,
                out var minTileY,
                out var maxTileX,
                out var maxTileY);

            lightTileBounds[i * 4 + 0] = minTileX;
            lightTileBounds[i * 4 + 1] = minTileY;
            lightTileBounds[i * 4 + 2] = maxTileX;
            lightTileBounds[i * 4 + 3] = maxTileY;
        }

        // Fill tile grid
        Parallel.For(0, totalTiles, tileIndex =>
        {
            var tileX = tileIndex % _tileCountX;
            var tileY = tileIndex / _tileCountX;
            var bufferOffset = tileIndex * _maxLightsPerTile;
            var count = 0;

            for (var i = 0; i < activeLightCount; i++)
            {
                if (count >= _maxLightsPerTile)
                {
                    break;
                }

                var minTileX = lightTileBounds[i * 4 + 0];
                var minTileY = lightTileBounds[i * 4 + 1];
                var maxTileX = lightTileBounds[i * 4 + 2];
                var maxTileY = lightTileBounds[i * 4 + 3];

                // Skip lights that were culled (behind camera or off-screen)
                if (minTileX < 0)
                {
                    continue;
                }

                // AABB overlap test
                if (tileX >= minTileX
                    && tileX <= maxTileX
                    && tileY >= minTileY
                    && tileY <= maxTileY)
                {
                    tileIndexBuffer[bufferOffset + count] = i;
                    count++;
                }
            }

            // Pad remainder with -1
            for (var i = count; i < _maxLightsPerTile; i++)
            {
                tileIndexBuffer[bufferOffset + i] = -1f;
            }
        });
    }

    /// <summary>
    /// Projects a light's bounding sphere to find which screen tiles it overlaps.
    /// Uses cone-tangent projection to compute the projected silhouette of the sphere,
    /// giving the tightest possible axis-aligned bounding rectangle in normalized device coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ComputeLightTileBounds(
        in Matrix viewMatrix,
        in Matrix projMatrix,
        in Vector3 crrPosition,
        float radius,
        int screenWidth,
        int screenHeight,
        out int minTileX,
        out int minTileY,
        out int maxTileX,
        out int maxTileY)
    {
        // 1. Transform World position to View Space (SIMD-accelerated).
        Vector3.Transform(in crrPosition, in viewMatrix, out var viewPos);

        // If the sphere encompasses the camera, or extends past the near plane, fill the screen.
        if (-viewPos.Z <= radius + 0.1f)
        {
            minTileX = 0;
            minTileY = 0;
            maxTileX = _tileCountX - 1;
            maxTileY = _tileCountY - 1;
            return;
        }

        // 2. Compute exact view-space tangents on the Z = -1 plane.
        var depth = -viewPos.Z;
        var depthSquared = depth * depth;
        var radiusSquared = radius * radius;

        // Denominator is guaranteed to be > 0 because of the depth > radius check above.
        var denominator = depthSquared - radiusSquared;

        var xSquared = viewPos.X * viewPos.X;
        var dx = radius * MathF.Sqrt(xSquared + denominator);
        var minTangentX = (viewPos.X * depth - dx) / denominator;
        var maxTangentX = (viewPos.X * depth + dx) / denominator;

        var ySquared = viewPos.Y * viewPos.Y;
        var dy = radius * MathF.Sqrt(ySquared + denominator);
        var minTangentY = (viewPos.Y * depth - dy) / denominator;
        var maxTangentY = (viewPos.Y * depth + dy) / denominator;

        // 3. Project the minimum and maximum tangent bounds to clip space.
        var p1 = new Vector4(minTangentX, minTangentY, -1f, 1f);
        var p2 = new Vector4(maxTangentX, maxTangentY, -1f, 1f);

        Vector4.Transform(in p1, in projMatrix, out var clip1);
        Vector4.Transform(in p2, in projMatrix, out var clip2);

        // 4. Convert to NDC
        var inverseOfPerspectiveDepth1 = 1f / clip1.W;
        var inverseOfPerspectiveDepth2 = 1f / clip2.W;

        var ndcX1 = clip1.X * inverseOfPerspectiveDepth1;
        var ndcX2 = clip2.X * inverseOfPerspectiveDepth2;
        var ndcY1 = clip1.Y * inverseOfPerspectiveDepth1;
        var ndcY2 = clip2.Y * inverseOfPerspectiveDepth2;

        var ndcMinX = Math.Min(ndcX1, ndcX2);
        var ndcMaxX = Math.Max(ndcX1, ndcX2);
        var ndcMinY = Math.Min(ndcY1, ndcY2);
        var ndcMaxY = Math.Max(ndcY1, ndcY2);

        // 5. Convert NDC [-1, 1] mapped to Screen [0, width].
        // Y is flipped (NDC Y is up, screen Y is down), so ndcMaxY dictates pxMinY.
        var pixelMinXBount = (ndcMinX * 0.5f + 0.5f) * screenWidth;
        var pixelMaxXBound = (ndcMaxX * 0.5f + 0.5f) * screenWidth;
        var pixelMinYBound = (1f - (ndcMaxY * 0.5f + 0.5f)) * screenHeight;
        var pixelMaxYBound = (1f - (ndcMinY * 0.5f + 0.5f)) * screenHeight;

        // Add exactly 1 tile padding to cover partial overlaps and float truncation.
        var tileWidth = (float)screenWidth / _tileCountX;
        var tileHeight = (float)screenHeight / _tileCountY;
        
        pixelMinXBount -= tileWidth;
        pixelMaxXBound += tileWidth;
        pixelMinYBound -= tileHeight;
        pixelMaxYBound += tileHeight;

        // 6. Convert to tile indices and clamp
        minTileX = Math.Max(0, (int)(pixelMinXBount / tileWidth));
        minTileY = Math.Max(0, (int)(pixelMinYBound / tileHeight));
        maxTileX = Math.Min(_tileCountX - 1, (int)(pixelMaxXBound / tileWidth));
        maxTileY = Math.Min(_tileCountY - 1, (int)(pixelMaxYBound / tileHeight));
    }
}
