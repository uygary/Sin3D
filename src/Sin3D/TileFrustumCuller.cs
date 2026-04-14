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

        // 2. Extrude the 8 corners of the sphere's bounding box in View Space.
        // This flawlessly absorbs any asymmetric or offset properties of VR projection matrices 
        // without relying on derived geometric conic tangents which can suffer from projection space mapping differences.
        var minX = viewPos.X - radius;
        var maxX = viewPos.X + radius;
        var minY = viewPos.Y - radius;
        var maxY = viewPos.Y + radius;
        var minZ = viewPos.Z - radius;
        var maxZ = viewPos.Z + radius;

        Span<Vector4> corners = stackalloc Vector4[8]
        {
            new Vector4(minX, minY, minZ, 1f),
            new Vector4(maxX, minY, minZ, 1f),
            new Vector4(minX, maxY, minZ, 1f),
            new Vector4(maxX, maxY, minZ, 1f),
            new Vector4(minX, minY, maxZ, 1f),
            new Vector4(maxX, minY, maxZ, 1f),
            new Vector4(minX, maxY, maxZ, 1f),
            new Vector4(maxX, maxY, maxZ, 1f)
        };

        float ndc_minX = float.MaxValue;
        float ndc_maxX = float.MinValue;
        float ndc_minY = float.MaxValue;
        float ndc_maxY = float.MinValue;

        // 3. Project all 8 points to NDC and find absolute bounding rectangle.
        for (int i = 0; i < 8; i++)
        {
            Vector4.Transform(in corners[i], in projMatrix, out var clip);
            float invW = 1f / clip.W;
            float ndcX = clip.X * invW;
            float ndcY = clip.Y * invW;

            if (ndcX < ndc_minX) ndc_minX = ndcX;
            if (ndcX > ndc_maxX) ndc_maxX = ndcX;
            if (ndcY < ndc_minY) ndc_minY = ndcY;
            if (ndcY > ndc_maxY) ndc_maxY = ndcY;
        }

        // 4. Convert NDC [-1, 1] mapped to Screen [0, width].
        // Y is flipped (NDC Y is up, screen Y is down), so ndc_maxY dictates pxMinY.
        float pxMinX = (ndc_minX * 0.5f + 0.5f) * screenWidth;
        float pxMaxX = (ndc_maxX * 0.5f + 0.5f) * screenWidth;
        float pxMinY = (1f - (ndc_maxY * 0.5f + 0.5f)) * screenHeight;
        float pxMaxY = (1f - (ndc_minY * 0.5f + 0.5f)) * screenHeight;

        // Add exactly 1 tile padding to cover partial overlaps and float truncation.
        float tileSizeX = (float)screenWidth / _tileCountX;
        float tileSizeY = (float)screenHeight / _tileCountY;
        
        pxMinX -= tileSizeX;
        pxMaxX += tileSizeX;
        pxMinY -= tileSizeY;
        pxMaxY += tileSizeY;

        // 6. Convert to tile indices and clamp
        minTileX = Math.Max(0, (int)(pxMinX / tileSizeX));
        minTileY = Math.Max(0, (int)(pxMinY / tileSizeY));
        maxTileX = Math.Min(_tileCountX - 1, (int)(pxMaxX / tileSizeX));
        maxTileY = Math.Min(_tileCountY - 1, (int)(pxMaxY / tileSizeY));
    }
}
