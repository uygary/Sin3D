using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sin3d.Extensions;
using Sin3d.Extensions.Simd;

namespace Sin3d;

/// <summary>
/// A 3D model class for handling position, rotation, scale and collision detection.
/// </summary>
public class Model3D
{
    private Transform3D _transform;

    /// <summary>
    /// The spatial transform (position, rotation, scale) of the model.
    /// Changes are NOT automatically reflected in <see cref="WorldMatrix"/>;
    /// call <see cref="UpdateWorldMatrix"/> after modifying.
    /// </summary>
    public ref Transform3D Transform => ref _transform;

    private readonly Model _baseModel;
    /// <summary>
    /// The imported model that the class will use.
    /// </summary>
    public Model BaseModel => _baseModel;

    private List<Texture2D?> _meshTextures = new ();
    /// <summary>
    /// The list of textures that will be mapped to each of the model's meshes.
    /// </summary>
    public List<Texture2D?> MeshTextures { get => _meshTextures; set => _meshTextures = value; }

    private Vector3 _diffuseColor = Vector3.One;
    /// <summary>
    /// The diffuse color tint applied to the model. Defaults to White (1,1,1).
    /// </summary>
    public Vector3 DiffuseColor { get => _diffuseColor; set => _diffuseColor = value; }

    private Vector3 _emissiveColor = Vector3.Zero;
    /// <summary>
    /// The emissive color tint applied to the model. Defaults to Black (0,0,0).
    /// </summary>
    public Vector3 EmissiveColor { get => _emissiveColor; set => _emissiveColor = value; }

    private Matrix _worldMatrix;
    /// <summary>
    /// The model's world matrix.
    /// </summary>
    /// <remarks>
    /// Normally derived from <see cref="Transform"/> via <see cref="UpdateWorldMatrix"/>.
    /// The setter is exposed to handle VR models whose world matrix is provided directly by OpenXR.
    /// After setting this directly, Transform will become invalid until it's re-synced.
    /// This means the Position, Rotation and Scale of the model will be stale.
    /// </remarks>
    public Matrix WorldMatrix { get => _worldMatrix; set => _worldMatrix = value; }

    private List<BoundingBox> _localAxisAlignedBoundingBoxes = new ();
    /// <summary>
    /// The list of local, axis-aligned bounding boxes created from the model's meshes (empty until Build method is called).
    /// </summary>
    public List<BoundingBox> LocalAxisAlignedBoundingBoxes { get => _localAxisAlignedBoundingBoxes; set => _localAxisAlignedBoundingBoxes = value; }

    #region Per-frame scratch buffers

    /* These should be pre-allocated, and never resized. */

    // Shared scratch corners buffer for AABB transforms
    private readonly Vector3[] _scratchCorners = new Vector3[8];

    // Reusable OBB instances for narrow-phase checks
    private readonly OrientedBoundingBox3D _scratchLocalObb = new();
    private readonly OrientedBoundingBox3D _scratchOtherObb = new();

    #endregion Per-frame scratch buffers

    /// <summary>
    /// Creates a new <see cref="Model3D"/> object with the given position, rotation, and scale settings.
    /// </summary>
    /// <param name="position">The initial (x, y, z) position.</param>
    /// <param name="rotation">The initial quaternion rotation.</param>
    /// <param name="scale">The initial scale.</param>
    /// <param name="baseModel">The imported model.</param>
    [Obsolete("Use the overload that takes in a Transform3D.")]
    public Model3D(Vector3 position, Quaternion rotation, float scale, Model baseModel)
        : this(new Transform3D(position, rotation, scale), baseModel)
    {
    }

    /// <summary>
    /// Creates a new <see cref="Model3D"/> object with the given position, rotation, scale and texture settings.
    /// </summary>
    /// <param name="position">The initial (x, y, z) position.</param>
    /// <param name="rotation">The initial quaternion rotation.</param>
    /// <param name="scale">The initial scale.</param>
    /// <param name="baseModel">The imported model.</param>
    /// <param name="meshTextures">The initial list of textures that will be mapped to the model's meshes.</param>
    [Obsolete("Use the overload that takes in a Transform3D.")]
    public Model3D(Vector3 position, Quaternion rotation, float scale, Model baseModel, List<Texture2D?> meshTextures)
        : this (new Transform3D(position, rotation, scale), baseModel, meshTextures)
    {
    }

    /// <summary>
    /// Creates a new <see cref="Model3D"/> object with the given transform.
    /// </summary>
    /// <param name="transform">Transform containing the models position, rotation and scale.</param>
    /// <param name="baseModel">The imported model.</param>
    public Model3D(Transform3D transform, Model baseModel)
    {
        _transform = transform;
        _baseModel = baseModel;

        UpdateWorldMatrix();
    }

    /// <summary>
    /// Creates a new <see cref="Model3D"/> object with the given transform and texture settings.
    /// </summary>
    /// <param name="transform">Transform containing the models position, rotation and scale.</param>
    /// <param name="baseModel">The imported model.</param>
    /// <param name="meshTextures">The initial list of textures that will be mapped to the model's meshes.</param> 
    public Model3D(Transform3D transform, Model baseModel, List<Texture2D?> meshTextures)
    {
        _transform = transform;
        _baseModel = baseModel;
        _meshTextures = meshTextures;

        UpdateWorldMatrix();
    }

    /// <summary>
    /// Updates the model's world matrix (to be used after changing position, rotation or scale).
    /// </summary>
    public void UpdateWorldMatrix()
    {
        _transform.ToWorldMatrix(out _worldMatrix);
    }

    #region Collision Detection

    /// <summary>
    /// Builds the local axis-aligned bounding boxes from the model's meshes (must be done for collision detection to work).
    /// </summary>
    /// <remarks>
    /// Call once at load time. The local AABBs are in model-space and do not change;
    /// world-space transformation is applied per-frame via the cached scratch buffers.
    /// </remarks>
    public void BuildLocalAxisAlignedBoundingBoxes()
    {
        _localAxisAlignedBoundingBoxes.Clear();
        for (var i = 0; i < _baseModel.Meshes.Count; i++)
        {
            _localAxisAlignedBoundingBoxes.Add(CreateBoundingBox(_baseModel.Meshes[i]));
        }
    }

    // Creates an axis-aligned bounding box around a mesh (load-time only, allocation is fine here)
    private static BoundingBox CreateBoundingBox(ModelMesh mesh)
    {
        Vector3 minVert = new Vector3(float.MaxValue);
        Vector3 maxVert = new Vector3(float.MinValue);

        for (var p = 0; p < mesh.MeshParts.Count; p++)
        {
            ModelMeshPart meshPart = mesh.MeshParts[p];
            var stride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[meshPart.NumVertices];
            meshPart.VertexBuffer.GetData(meshPart.VertexOffset * stride, vertices, 0, meshPart.NumVertices, stride);
            for (var v = 0; v < vertices.Length; v++)
            {
                Vector3 vertPoint = vertices[v].Position;
                minVert = Vector3.Min(minVert, vertPoint);
                maxVert = Vector3.Max(maxVert, vertPoint);
            }
        }

        return new BoundingBox(minVert, maxVert);
    }

    /// <summary>
    /// Checks if the bounding spheres of 2 models intersect.
    /// </summary>
    /// <param name="otherModel">The other model.</param>
    /// <returns>Whether an intersection was detected.</returns>
    public bool BoundingSphereIntersects(Model3D otherModel)
    {
        for (var i = 0; i < _baseModel.Meshes.Count; i++)
        {
            BoundingSphere s1 = _baseModel.Meshes[i].BoundingSphere.Transform(_worldMatrix);
            for (var j = 0; j < otherModel.BaseModel.Meshes.Count; j++)
            {
                BoundingSphere s2 = otherModel.BaseModel.Meshes[j].BoundingSphere.Transform(otherModel.WorldMatrix);
                if (s1.Intersects(s2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the axis-aligned bounding boxes of 2 models intersect.
    /// </summary>
    /// <param name="otherModel">The other model.</param>
    /// <returns>Whether an intersection was detected.</returns>
    /// <remarks>
    /// AABBs must be built prior to calling this.
    /// Zero-allocation. Uses a shared scratch buffer for corner transformations.
    /// </remarks>
    public bool AxisAlignedBoundingBoxIntersects(Model3D otherModel)
    {
        for (var i = 0; i < _localAxisAlignedBoundingBoxes.Count; i++)
        {
            BoundingBox transformedLocalBox = TransformAabb(_localAxisAlignedBoundingBoxes[i], in _worldMatrix);
            for (var j = 0; j < otherModel._localAxisAlignedBoundingBoxes.Count; j++)
            {
                var otherWorld = otherModel._worldMatrix;
                BoundingBox transformedOtherBox = TransformAabb(otherModel._localAxisAlignedBoundingBoxes[j], in otherWorld);
                if (transformedLocalBox.Intersects(transformedOtherBox))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Transforms a local AABB by a world matrix using the shared scratch buffer.
    /// Returns a new axis-aligned bounding box that encloses the transformed corners.
    /// </summary>
    private BoundingBox TransformAabb(in BoundingBox localBox, in Matrix transform)
    {
        localBox.ReadonlyGetCorners(_scratchCorners);

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        for (var i = 0; i < 8; i++)
        {
            Vector3.Transform(in _scratchCorners[i], in transform, out var transformed);
            min = Vector3.Min(min, transformed);
            max = Vector3.Max(max, transformed);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Checks if the oriented bounding boxes of 2 models intersect.
    /// </summary>
    /// <param name="otherModel">The other model.</param>
    /// <returns>Whether an intersection was detected.</returns>
    /// <remarks>
    /// AABBs must be built prior to calling this.
    /// Zero-allocation. Reuses two cached <see cref="OrientedBoundingBox3D"/> instances.
    /// </remarks>
    public bool OrientedBoundingBoxIntersects(Model3D otherModel)
    {
        for (var i = 0; i < _localAxisAlignedBoundingBoxes.Count; i++)
        {
            _scratchLocalObb.Reset(_localAxisAlignedBoundingBoxes[i]);
            _scratchLocalObb.TransformVertices(in _worldMatrix);

            for (var j = 0; j < otherModel._localAxisAlignedBoundingBoxes.Count; j++)
            {
                _scratchOtherObb.Reset(otherModel._localAxisAlignedBoundingBoxes[j]);
                _scratchOtherObb.TransformVertices(in otherModel._worldMatrix);

                if (_scratchLocalObb.Intersects(_scratchOtherObb))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if 2 models intersect using a broad-phase -> narrow-phase cascade:
    /// Bounding spheres -> AABB -> OBB (local axis-aligned bounding boxes must be built).
    /// </summary>
    /// <param name="otherModel">The other model.</param>
    /// <returns>Whether an intersection was detected.</returns>
    /// <remarks>AABBs must be built prior to calling this.</remarks>
    public bool Intersects(Model3D otherModel)
    {
        return BoundingSphereIntersects(otherModel)
               && AxisAlignedBoundingBoxIntersects(otherModel)
               && OrientedBoundingBoxIntersects(otherModel);
    }

    /// <summary>
    /// Extracts all triangles from the model, transformed by the given world matrix.
    /// </summary>
    /// <param name="worldMatrix">The world matrix to transform vertices by.</param>
    /// <returns>An array of world-space triangles.</returns>
    /// <remarks>
    /// This is a helper designed to be used on load-time, or sparingly.
    /// It allocates freely and should NOT be called per-frame.
    /// <para>
    /// Used for building terrain height samplers from map geometry, or similar cases.
    /// It is obviously very, very high-cost.
    /// </para>
    /// </remarks>
    public Triangle3[] ExtractTransformedTriangles(in Matrix worldMatrix)
    {
        var triangles = new List<Triangle3>();

        for (var m = 0; m < _baseModel.Meshes.Count; m++)
        {
            ModelMesh mesh = _baseModel.Meshes[m];
            for (var p = 0; p < mesh.MeshParts.Count; p++)
            {
                ModelMeshPart part = mesh.MeshParts[p];
                int stride = part.VertexBuffer.VertexDeclaration.VertexStride;

                // Extract vertex positions
                var vertices = new VertexPositionNormalTexture[part.NumVertices];
                part.VertexBuffer.GetData(
                    part.VertexOffset * stride,
                    vertices, 0, part.NumVertices, stride);

                // Extract indices
                var indexElementSize =
                    part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits
                        ? 2
                        : 4;

                var indexCount = part.PrimitiveCount * 3;
                var indices = new int[indexCount];

                if (indexElementSize == 2)
                {
                    var shortIndices = new short[indexCount];
                    part.IndexBuffer.GetData(
                        part.StartIndex * 2,
                        shortIndices, 0, indexCount);

                    for (int i = 0; i < indexCount; i++)
                    {
                        indices[i] = shortIndices[i];
                    }
                }
                else
                {
                    part.IndexBuffer.GetData(
                        part.StartIndex * 4,
                        indices, 0, indexCount);
                }

                // Build triangles, transformed into world space
                for (int i = 0; i < indexCount; i += 3)
                {
                    Vector3.Transform(in vertices[indices[i]].Position, in worldMatrix, out var v0);
                    Vector3.Transform(in vertices[indices[i + 1]].Position, in worldMatrix, out var v1);
                    Vector3.Transform(in vertices[indices[i + 2]].Position, in worldMatrix, out var v2);

                    triangles.Add(new Triangle3(v0, v1, v2));
                }
            }
        }

        return triangles.ToArray();
    }

    #endregion Collision Detection
}
