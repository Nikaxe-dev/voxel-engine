using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace VoxelEngine.Core.Voxels;

public partial class ChunkManager : Node
{
    public static ChunkManager Instance {get; private set;}

    [Export] public Vector3I ChunkSize = Vector3I.One*16;
    [Export] public int RenderDistance = 4;

    [Export] public int ChunkLoadingThreads = 1;
    [Export] public int ChunkUpdateThreshold = 2;

    private readonly List<ChunkLoader> ChunkLoaders = [];
    public readonly ConcurrentQueue<Chunk> ChunkAddQueue = [];

    private readonly Dictionary<Vector3I, Chunk> Chunks = [];

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        for (int loaderIndex=0; loaderIndex<ChunkLoadingThreads; loaderIndex++)
        {
            ChunkLoader loader = new();
            AddChild(loader);
            ChunkLoaders.Add(loader);
        }
    }

    private Vector3I oldPlayerChunkPosition = Vector3I.One*10000;
    private Vector3I lastChunkUpdatePosition = Vector3I.One*10000;

    private Vector3I GetPlayerChunkPosition()
    {
        return (Vector3I)(Player.Instance.Position / ChunkSize).Floor();
    }

    public override void _Process(double delta)
    {
        while (ChunkAddQueue.TryDequeue(out Chunk chunk))
        {
            chunk.VoxelMeshInstance.Mesh = chunk.VoxelMesh;
            Chunks[chunk.ChunkPosition] = chunk;
        }

        var playerChunkPosition = GetPlayerChunkPosition();
        
        if (playerChunkPosition != oldPlayerChunkPosition && (playerChunkPosition-lastChunkUpdatePosition).Length() >= ChunkUpdateThreshold)
        {
            lastChunkUpdatePosition = playerChunkPosition;
            RecalculateChunks(playerChunkPosition);
        }

        oldPlayerChunkPosition = playerChunkPosition;
    }

    private void LoadChunk(Vector3I chunkPosition)
    {
        if (!Chunks.TryGetValue(chunkPosition, out _))
            ChunkLoaders[(int)(GD.Randf() * ChunkLoaders.Count)].RequestChunkLoad(chunkPosition);
    }

    private void RecalculateChunks(Vector3I playerChunkPosition)
    {
        var chunkPositions = GenerateChunksSpiral(playerChunkPosition);

        foreach (Vector3I chunkPosition in chunkPositions)
        {
            LoadChunk(chunkPosition);
        }
    }
    
    private Vector3I[] GenerateChunksSpiral(Vector3I center)
    {
        List<Vector3I> result = [];

        int maxY = center.Y + RenderDistance;
        int minY = center.Y - RenderDistance;

        int x = 0;
        int z = 0;

        int dx = 0;
        int dz = -1;

        int size = RenderDistance * 2 + 1;
        int maxSteps = size * size;

        for (int i=0; i<maxSteps; i++)
        {
            if (Math.Abs(x) <= RenderDistance && Math.Abs(z) <= RenderDistance)
            {
                for (int y=maxY; y>minY-1; y--)
                {
                    result.Add(new(
                        center.X+x,
                        y,
                        center.Z+z
                    ));
                }
            }

            if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
            {
                var temp = dx;
                dx = -dz;
                dz = temp;
            }

            x += dx;
            z += dz;
        }

        return [.. result];
    }
}