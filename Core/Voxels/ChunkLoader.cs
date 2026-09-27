using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace VoxelEngine.Core.Voxels;

public partial class ChunkLoader : Node
{
    public Vector3I ChunkToVoxelPosition(Vector3I chunkPosition) => chunkPosition * ChunkManager.Instance.ChunkSize;

    private Chunk CreateChunk(Vector3I chunkPosition)
    {
        Chunk chunk = new()
        {
            Name = $"Chunk@{chunkPosition.X}@{chunkPosition.Y}@{chunkPosition.Z}",
            Position = ChunkToVoxelPosition(chunkPosition),
            VoxelPosition = ChunkToVoxelPosition(chunkPosition),
            ChunkPosition = chunkPosition
        };

        return chunk;
    }

    public Chunk RequestChunkLoad(Vector3I chunkPosition)
    {
        Chunk chunk = CreateChunk(chunkPosition);
        RequestedChunks.Enqueue(chunk);
        AddChild(chunk);
        return chunk;
    }

    private readonly ConcurrentQueue<Chunk> RequestedChunks = [];

    public override void _Ready()
    {
        base._Ready();

        Task.Run(() =>
        {
            try {
                while (true)
                {
                    while (RequestedChunks.TryDequeue(out Chunk chunk))
                    {
                        chunk.VoxelMesh = chunk.GenerateMesh(chunk.Construct());
                        ChunkManager.Instance.ChunkAddQueue.Enqueue(chunk);
                    }
                }
            } catch (Exception err)
            {
                GD.PrintErr("A ChunkLoader thread encountered an error while loading chunks: ", err);
            }
        });
    }
}