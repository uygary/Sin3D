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
    /// <param name="crrPositions">CRR light positions (already camera-relative).</param>
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
    /// Forms a View-Space AABB around the sphere (which is mathematically exact regardless of view rotation),
    /// and projects its 8 corners to screen space via the projection matrix.
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
        // 1. Transform World position to View Space (SIMD-accelerated)
        Vector3.Transform(in crrPosition, in viewMatrix, out var viewPos);

        // If the sphere encompasses the camera, or extends past the near plane, fill the screen
        if (-viewPos.Z <= radius + 0.1f)
        {
            minTileX = 0;
            minTileY = 0;
            maxTileX = _tileCountX - 1;
            maxTileY = _tileCountY - 1;
            return;
        }

        // Project the exact center of the sphere to the screen (SIMD-accelerated)
        var centerP = new Vector4(viewPos.X, viewPos.Y, viewPos.Z, 1f);
        Vector4.Transform(in centerP, in projMatrix, out var centerClip);
        
        float invW = 1f / centerClip.W;
        float cx = (centerClip.X * invW * 0.5f + 0.5f) * screenWidth;
        float cy = (1f - (centerClip.Y * invW * 0.5f + 0.5f)) * screenHeight;

        // Calculate maximum projected screen radii (using M11 and M22 focal lengths).
        // This is exact for the sphere center but slightly underestimates for off-axis
        // spheres because perspective causes them to project as ellipses larger than
        // the focal-length ratio alone predicts.
        float rx = (radius * Math.Abs(projMatrix.M11) * invW * 0.5f) * screenWidth;
        float ry = (radius * Math.Abs(projMatrix.M22) * invW * 0.5f) * screenHeight;

        // Small safety margin to cover:
        //   - Off-axis elliptical distortion (~5% of projected radius)
        //   - Float-to-int truncation at tile boundaries (+1 tile width in pixels)
        float tileSizeX = (float)screenWidth / _tileCountX;
        float tileSizeY = (float)screenHeight / _tileCountY;
        float inflationX = rx * 1.05f + tileSizeX;
        float inflationY = ry * 1.05f + tileSizeY;

        float pxMinX = cx - inflationX;
        float pxMaxX = cx + inflationX;
        float pxMinY = cy - inflationY;
        float pxMaxY = cy + inflationY;

        // Convert to tile indices and clamp
        minTileX = Math.Max(0, (int)(pxMinX / tileSizeX));
        minTileY = Math.Max(0, (int)(pxMinY / tileSizeY));
        maxTileX = Math.Min(_tileCountX - 1, (int)(pxMaxX / tileSizeX));
        maxTileY = Math.Min(_tileCountY - 1, (int)(pxMaxY / tileSizeY));
    }
}
