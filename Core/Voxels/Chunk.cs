using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using VoxelEngine.Core.Enums;
using VoxelEngine.Core.Visuals;

namespace VoxelEngine.Core.Voxels;

public partial class Chunk : Node3D
{
    public Dictionary<Vector3I, VoxelData> Voxels = [];

    public Vector3I VoxelPosition;

    public MeshInstance3D MeshInstance = new()
    {
        Name = "ChunkMesh"
    };
    
    public StaticBody3D StaticBody = new()
    {
        Name = "ChunkBody"
    };

    public CollisionShape3D CollisionShape = new()
    {
        Name = "ChunkCollision"
    };

    public MeshInstance3D DebugOutline = GizmoCreator.CreateOutlineBox(-Vector3.One/2, Basis.Identity, VoxelManager.Instance.ChunkSize, new(1,1,1), false);

    public override void _Ready()
    {
        StaticBody.AddChild(MeshInstance);
        StaticBody.AddChild(CollisionShape);
        AddChild(StaticBody);

        AddChild(DebugOutline);
        DebugOutline.Name = "DebugOutline";
        DebugOutline.Visible = VoxelManager.Instance.DebugShowChunkOutline;
    }

    private void Generate()
    {
        var startTime = Time.GetTicksUsec();

        int Seed = VoxelManager.Instance.WorldSeed;

        Noise Noise = VoxelManager.Instance.Noise;
        int MaxHeight = VoxelManager.Instance.MaxHeight;

        Vector3 ChunkSize = VoxelManager.Instance.ChunkSize;
        Color[] Colors = VoxelManager.Instance.Colors;

        RandomNumberGenerator randomNumberGenerator = new();

        for (int x = 0; x < ChunkSize.X; x++)
        {
            for (int y = 0; y < ChunkSize.Y; y++)
            {
                for (int z = 0; z < ChunkSize.Z; z++)
                {
                    var rand = randomNumberGenerator.Randf();

                    if (rand>0.99999f)
                    {
                        Voxels[new Vector3I(x,y,z)] = new VoxelData(Colors[0]);
                    }
                }
            }
        }

        for (int x = 0; x < ChunkSize.X; x++)
        {
            for (int z = 0; z < ChunkSize.Z; z++)
            {
                int gX = VoxelPosition.X+x;
                int gZ = VoxelPosition.Z+z;

                float rand = ((Noise.GetNoise2D(gX,gZ) + 0.5f * Noise.GetNoise2D(2*gX,2*gZ) + 0.25f * Noise.GetNoise2D(4*gX,4*gZ)) / 1.75f + 1) / 2;
                float randP = (float)Math.Pow(rand, 2.1);
                int height = (int)(randP * MaxHeight);

                if (height < VoxelPosition.Y) continue;

                int localHeight = height - VoxelPosition.Y;

                for (int y = 0; y<Math.Min(localHeight,ChunkSize.Y); y++)
                {
                    Voxels[new Vector3I(x,y,z)] = new VoxelData(Colors[y % Colors.Length]);
                }
            }
        }

        var endTime = Time.GetTicksUsec();
        var genTime = endTime - startTime;
    }

    public void Construct()
    {
        Voxels = [];
        CallDeferred(MethodName.SetShapeNull);
        Generate();
    }

    public void SetShapeNull()
    {
        CollisionShape.Shape = null;
    }

    private static readonly Vector3[] CubeVertices = [
        new(-0.5f,-0.5f,0.5f),
        new(0.5f,-0.5f,0.5f),
        new(0.5f,-0.5f,-0.5f),
        new(-0.5f,-0.5f,-0.5f),
        new(-0.5f,0.5f,0.5f),
        new(0.5f,0.5f,0.5f),
        new(0.5f,0.5f,-0.5f),
        new(-0.5f,0.5f,-0.5f)
    ];

    private static readonly Dictionary<CubeFace, int[][]> FaceIndices = new()
    {
        [CubeFace.Front] = [[0,4,5], [0,5,1]],
        [CubeFace.Back] = [[2,7,3], [2,6,7]],
        [CubeFace.Left] = [[3,7,4], [3,4,0]],
        [CubeFace.Right] = [[1,5,6], [1,6,2]],
        [CubeFace.Bottom] = [[0,1,2], [0,2,3]],
        [CubeFace.Top] = [[4,7,6], [4,6,5]]
    };

    private static readonly Dictionary<CubeFace, Vector3I> FaceNormals = new()
    {
        [CubeFace.Front] = new(0,0,1),
        [CubeFace.Back] = new(0,0,-1),
        [CubeFace.Left] = new(-1,0,0),
        [CubeFace.Right] = new(1,0,0),
        [CubeFace.Bottom] = new(0,-1,0),
        [CubeFace.Top] = new(0,1,0)
    };

    private static readonly Dictionary<CubeFace, Color> FaceColors = new()
    {
        [CubeFace.Bottom] = new(1,0,0),
        [CubeFace.Front] = new(1,0.25f,0),
        [CubeFace.Right] = new(1,1,0),
        [CubeFace.Top] = new(0,1,0),
        [CubeFace.Left] = new(0,0,1),
        [CubeFace.Back] = new(1,0,1),
    };

    private Godot.Collections.Array SurfaceArray = [];
    private List<Vector3> Vertices = [];
    private List<Vector3> Normals = [];
    private List<Color> Colors = [];

    private void AddFace(CubeFace face, Vector3I position, VoxelData voxel)
    {
        if (FaceHasNeighbour(face, position)) return;

        var indices = FaceIndices[face];
        
        foreach (var triangle in indices)
        {
            foreach (var index in triangle)
            {
                Vertices.Add(CubeVertices[index] + position);
                Normals.Add(FaceNormals[face]);
                Colors.Add(voxel.Color);
            }
        }
    }

    private bool FaceHasNeighbour(CubeFace face, Vector3I position)
    {
        Vector3I neighbourPosition = position + FaceNormals[face];
        return Voxels.TryGetValue(neighbourPosition, out _);
    }

    private void ResetMeshData()
    {
        Vertices = [];
        Normals = [];
        Colors = [];
    }

    private void AddVoxel(VoxelData voxel, Vector3I position)
    {
        AddFace(CubeFace.Front, position, voxel);
        AddFace(CubeFace.Back, position, voxel);
        AddFace(CubeFace.Left, position, voxel);
        AddFace(CubeFace.Right, position, voxel);
        AddFace(CubeFace.Top, position, voxel);
        AddFace(CubeFace.Bottom, position, voxel);
    }

    private void GenerateMesh()
    {
        ResetMeshData();

        foreach (KeyValuePair<Vector3I, VoxelData> kvp in Voxels)
        {
            AddVoxel(kvp.Value, kvp.Key);
        }
    }

    private void CommitMesh()
    {
        ArrayMesh mesh = new();

        SurfaceArray = [];
        SurfaceArray.Resize((int)Mesh.ArrayType.Max);

        SurfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
        SurfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        SurfaceArray[(int)Mesh.ArrayType.Color] = Colors.ToArray();

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, SurfaceArray);

        mesh.SurfaceSetMaterial(0, new StandardMaterial3D()
        {
            VertexColorUseAsAlbedo = true,
        });

        // if (!IsInstanceValid(MeshInstance) || !IsInstanceValid(CollisionShape)) return;

        MeshInstance.CallDeferred(MeshInstance3D.MethodName.SetMesh, mesh);
        CollisionShape.CallDeferred(CollisionShape3D.MethodName.SetShape, mesh.CreateTrimeshShape());

        // if (!IsInstanceValid(this)) return;
        // CallDeferred(MethodName.SetMeshAndCollisionShape, mesh, mesh.CreateTrimeshShape());

        // VoxelManager.Instance.CallDeferred(VoxelManager.MethodName.ApplyChunkMesh, this, mesh, mesh.CreateTrimeshShape());
    }
    public void Update()
    {
        if (Voxels.Count < 1)
        {
            MeshInstance.Mesh = null;
            return;
        }

        GenerateMesh();
        CommitMesh();
    }
}