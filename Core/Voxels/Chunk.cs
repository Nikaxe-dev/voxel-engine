using System;
using System.Collections.Generic;
using Godot;
using VoxelEngine.Core.Data;
using VoxelEngine.Core.Enums;

namespace VoxelEngine.Core.Voxels;

public partial class Chunk : Node3D
{
    public MeshInstance3D VoxelMeshInstance = new();
    public Mesh VoxelMesh = null;

    public Vector3I ChunkPosition;
    public Vector3I VoxelPosition;

    public override void _Ready()
    {
        base._Ready();
        AddChild(VoxelMeshInstance);
    }

    private Dictionary<Vector3I, VoxelData> Generate()
    {
        Dictionary<Vector3I, VoxelData> voxels = [];

        Noise Noise = new FastNoiseLite();
        int MaxHeight = 128;

        Vector3 ChunkSize = ChunkManager.Instance.ChunkSize;

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
                        voxels[new Vector3I(x,y,z)] = new VoxelData();
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
                    voxels[new Vector3I(x,y,z)] = new VoxelData();
                }
            }
        }

        return voxels;
    }

    public Dictionary<Vector3I, VoxelData> Construct() => Generate();

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

    private void AddFaceMeshData(ArrayMeshData meshData, Dictionary<Vector3I, VoxelData> voxels, CubeFace face, Vector3I position, VoxelData voxel)
    {
        if (FaceHasNeighbour(voxels, face, position)) return;

        var indices = FaceIndices[face];
        
        foreach (var triangle in indices)
        {
            foreach (var index in triangle)
            {
                meshData.Vertices.Add(CubeVertices[index] + position);
                meshData.Normals.Add(FaceNormals[face]);
                meshData.Colors.Add(FaceColors[face]);
            }
        }
    }

    private bool FaceHasNeighbour(Dictionary<Vector3I, VoxelData> voxels, CubeFace face, Vector3I position)
    {
        Vector3I neighbourPosition = position + FaceNormals[face];
        return voxels.TryGetValue(neighbourPosition, out _);
    }

    private void AddVoxelMeshData(ArrayMeshData meshData, Dictionary<Vector3I, VoxelData> voxels, VoxelData voxel, Vector3I position)
    {
        AddFaceMeshData(meshData, voxels, CubeFace.Front, position, voxel);
        AddFaceMeshData(meshData, voxels, CubeFace.Back, position, voxel);
        AddFaceMeshData(meshData, voxels, CubeFace.Left, position, voxel);
        AddFaceMeshData(meshData, voxels, CubeFace.Right, position, voxel);
        AddFaceMeshData(meshData, voxels, CubeFace.Top, position, voxel);
        AddFaceMeshData(meshData, voxels, CubeFace.Bottom, position, voxel);
    }

    private ArrayMeshData CreateMeshData(Dictionary<Vector3I, VoxelData> voxels)
    {
        ArrayMeshData meshData = new();

        foreach (KeyValuePair<Vector3I, VoxelData> kvp in voxels)
        {
            AddVoxelMeshData(meshData, voxels, kvp.Value, kvp.Key);
        }

        return meshData;
    }

    public Mesh GenerateMesh(Dictionary<Vector3I, VoxelData> voxels) => CreateMeshData(voxels).GenerateMesh();
}