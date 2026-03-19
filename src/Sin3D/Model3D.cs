using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sin3d;

/// <summary>
/// A 3D model class for handling position, rotation, scale and collision detection.
/// </summary>
public class Model3d
{
    private Vector3 _position;
    /// <summary>
    /// The (x, y, z) position of the model.
    /// </summary>
    public Vector3 Position { get => _position; set => _position = value; }

    private Quaternion _rotation;
    /// <summary>
    /// The quaternion rotation of the model.
    /// </summary>
    public Quaternion Rotation { get => _rotation; set => _rotation = value; }

    private float _scale;
    /// <summary>
    /// The scale of the model.
    /// </summary>
    public float Scale { get => _scale; set => _scale = value; }

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

    private Matrix _worldMatrix;
    /// <summary>
    /// The model's world matrix.
    /// </summary>
    /// <remarks>
    /// This should ideally be a readonly property that is only updated through the UpdateWorldMatrix method,
    /// but we made it settable to handle virtual models in VR based on updates provided by OpenXR.
    /// TODO: I think we need a better way of handling that.
    /// </remarks>
    public Matrix WorldMatrix { get => _worldMatrix; set => _worldMatrix = value; }

    private List<BoundingBox> _localAxisAlignedBoundingBoxes = new ();
    /// <summary>
    /// The list of local, axis-aligned bounding boxes created from the model's meshes (empty until Build method is called).
    /// </summary>
    public List<BoundingBox> LocalAxisAlignedBoundingBoxes { get => _localAxisAlignedBoundingBoxes; set => _localAxisAlignedBoundingBoxes = value; }

    /// <summary>
    /// Creates a new <see cref="Model3d"/> object with position, rotation, and scale settings.
    /// </summary>
    /// <param name="position">The initial (x, y, z) position.</param>
    /// <param name="rotation">The initial quaternion rotation.</param>
    /// <param name="scale">The initial scale.</param>
    /// <param name="baseModel">The imported model.</param>
    public Model3d(Vector3 position, Quaternion rotation, float scale, Model baseModel)
    {
        _position = position;
        _rotation = rotation;
        _scale = scale;
        _baseModel = baseModel;

        UpdateWorldMatrix();
    }

    /// <summary>
    /// Creates a new <see cref="Model3d"/> object with position, rotation, scale and texture settings.
    /// </summary>
    /// <param name="position">The initial (x, y, z) position.</param>
    /// <param name="rotation">The initial quaternion rotation.</param>
    /// <param name="scale">The initial scale.</param>
    /// <param name="baseModel">The imported model.</param>
    /// <param name="meshTextures">The initial list of textures that will be mapped to the model's meshes.</param> 
    public Model3d(Vector3 position, Quaternion rotation, float scale, Model baseModel, List<Texture2D?> meshTextures)
    {
        _position = position;
        _rotation = rotation;
        _scale = scale;
        _baseModel = baseModel;
        _meshTextures = meshTextures;

        UpdateWorldMatrix();
    }

    /// <summary>
    /// Updates the model's world matrix (to be used after changing position, rotation or scale).
    /// </summary>
    public void UpdateWorldMatrix()
    {
        _worldMatrix = (
            Matrix.CreateScale(_scale)
            * Matrix.CreateFromQuaternion(_rotation)
            * Matrix.CreateTranslation(_position)
        );
    }

    /// <summary>
    /// Builds the local axis-aligned bounding boxes from the model's meshes (must be done for collision detection to work).
    /// </summary>
    public void BuildLocalAxisAlignedBoundingBoxes()
    {
        _localAxisAlignedBoundingBoxes.Clear();
        //Creating an AABB for each mesh
        foreach (ModelMesh mesh in _baseModel.Meshes)
        {
            _localAxisAlignedBoundingBoxes.Add(CreateBoundingBox(mesh));
        }
    }

    //Creates an axis-aligned bounding box around a mesh
    private BoundingBox CreateBoundingBox(ModelMesh mesh)
    {
        Vector3 minVert = new Vector3(float.MaxValue);
        Vector3 maxVert = new Vector3(float.MinValue);
        
        foreach (ModelMeshPart meshPart in mesh.MeshParts)
        {
            int stride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
            VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[meshPart.NumVertices];
            meshPart.VertexBuffer.GetData(meshPart.VertexOffset * stride, vertices, 0, meshPart.NumVertices, stride);
            foreach (VertexPositionNormalTexture vert in vertices)
            {
                Vector3 vertPoint = vert.Position;
                minVert = Vector3.Min(minVert, vertPoint);
                maxVert = Vector3.Max(maxVert, vertPoint);
            }
        }
        return new BoundingBox(minVert, maxVert);
    }

    /// <summary>
    /// Checks if the bounding spheres of 2 models intersect.
    /// </summary>
    /// <param name="model2">The other model.</param>
    /// <returns>boolean - whether an intersection was detected.</returns>
    public bool BoundingSphereIntersects(Model3d model2)
    {
        foreach (ModelMesh mesh1 in _baseModel.Meshes)
        {
            foreach (ModelMesh mesh2 in model2.BaseModel.Meshes)
            {
                if (mesh1.BoundingSphere.Transform(_worldMatrix).Intersects(mesh2.BoundingSphere.Transform(model2.WorldMatrix)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the axis-aligned bounding boxes of 2 models intersect (local axis-aligned bounding boxes must be built).
    /// </summary>
    /// <param name="model2">The other model.</param>
    /// <returns>boolean - whether an intersection was detected.</returns>
    public bool AxisAlignedBoundingBoxIntersects(Model3d model2)
    {
        //creating the transformed axis-aligned bounding boxes and checking if they collide
        foreach (BoundingBox box1 in _localAxisAlignedBoundingBoxes)
        {
            BoundingBox transformedBox1 = GetTransformedAxisAlignedBoundingBox(box1, _worldMatrix);
            foreach (BoundingBox box2 in model2.LocalAxisAlignedBoundingBoxes)
            {
                BoundingBox transformedBox2 = GetTransformedAxisAlignedBoundingBox(box2, model2.WorldMatrix);
                if (transformedBox1.Intersects(transformedBox2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    //method for getting a transformed axis-aligned bounding box given a local AABB and a transformation matrix
    BoundingBox GetTransformedAxisAlignedBoundingBox(BoundingBox localBox, Matrix transform)
    {
        Vector3[] localBoxVertices = localBox.GetCorners();
        Vector3[] transformedBoxVertices = new Vector3[localBoxVertices.Length];
        for (int i = 0; i < localBoxVertices.Length; i++)
        {
            transformedBoxVertices[i] = Vector3.Transform(localBoxVertices[i], transform);
        }

        return BoundingBox.CreateFromPoints(transformedBoxVertices);
    }

    /// <summary>
    /// Checks if the oriented bounding boxes of 2 models intersect (local axis-aligned bounding boxes must be built).
    /// </summary>
    /// <param name="model2">The other model.</param>
    /// <returns>boolean - whether an intersection was detected.</returns>
    public bool OrientedBoundingBoxIntersects(Model3d model2)
    {
        //creating the oriented bounding boxes (from the local AABBs) and checking if they collide
        foreach (BoundingBox box1 in _localAxisAlignedBoundingBoxes)
        {
            OrientedBoundingBox3d obb1 = new OrientedBoundingBox3d(box1);
            obb1.TransformVertices(_worldMatrix);

            foreach (BoundingBox box2 in model2.LocalAxisAlignedBoundingBoxes)
            {
                OrientedBoundingBox3d obb2 = new OrientedBoundingBox3d(box2);
                obb2.TransformVertices(model2.WorldMatrix);

                if (obb1.Intersects(obb2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if 2 models intersect using the optimized hierarchy of methods: bounding spheres -> AABB -> OBB (local axis-aligned bounding boxes must be built).
    /// </summary>
    /// <param name="model2">The other model.</param>
    /// <returns>boolean - whether an intersection was detected.</returns>
    public bool Intersects(Model3d model2)
    {
        if (!BoundingSphereIntersects(model2))
        {
            return false;
        }
        else if (!AxisAlignedBoundingBoxIntersects(model2))
        {
            return false;
        }
        else if (!OrientedBoundingBoxIntersects(model2))
        {
            return false;
        }

        return true;
    }
}
