using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace VoxelEngine.Core.Voxels;

public struct VoxelData(Color Color)
{
    public Color Color = Color;
}

public partial class VoxelManager : Node
{
    public static VoxelManager Instance {get; private set;}

    [Export] public bool DebugShowChunkOutline = false;

    [Export] public int WorldSeed = 0;
    [Export] public Vector3I ChunkSize = Vector3I.One*16;

    [Export] public int MaxHeight = 128;
    [Export] public Noise Noise;

    [Export] public Color[] Colors;

    [Export] public int RenderDistance = 4;
    [Export] public int AmountOfLoadingThreads = 4;

    public Dictionary<Vector3I, Chunk> Chunks = [];

    private List<Chunk> OrphanedChunks = [];

    private Task LoadingTask;
    private List<List<Vector3I>> LoadingThreadChunks = [];

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        // LoadingTask = Task.Run(async () =>
        // {
        //     var oldPlayerChunkPosition = Vector3I.Zero;

        //     try {

        //         while (true)
        //         {
        //             // Vector3I playerChunkPosition = (Vector3I)(Player.Instance.Position / ChunkSize).Floor();

        //             // foreach (KeyValuePair<Vector3I, Chunk> kvp in Chunks.ToArray())
        //             // {
        //             //     Vector3I chunkPosition = kvp.Key;
        //             //     Chunk chunk = kvp.Value;
                        
        //             //     Vector3I relativeChunkPosition = chunkPosition - playerChunkPosition;
        //             //     if (Math.Abs(relativeChunkPosition.X)>ChunkLoadArea.X/2 || Math.Abs(relativeChunkPosition.Y)>ChunkLoadArea.Y/2 || Math.Abs(relativeChunkPosition.Z)>ChunkLoadArea.Z/2)
        //             //     {
        //             //         UnloadChunk(chunkPosition);
        //             //     }
        //             // }

        //             // LoadChunks(ChunkLoadArea, playerChunkPosition - ChunkLoadArea/2, playerChunkPosition);

        //             // await Task.Delay(50);

        //             Vector3I playerChunkPosition = (Vector3I)(Player.Instance.Position / ChunkSize).Floor();

        //             if (playerChunkPosition != oldPlayerChunkPosition)
        //             {
        //                 RecalculateChunks(playerChunkPosition);
        //             }

        //             oldPlayerChunkPosition = playerChunkPosition;
        //         }

        //     } catch (Exception ex)
        //     {
        //         GD.PrintErr($"Chunk loading thread failed: ${ex}");
        //     }
        // });

        for (int i=0; i<AmountOfLoadingThreads; i++)
        {
            LoadingThreadChunks.Add([]);
            StartLoadingThread(i);
        }
    }

    private void StartLoadingThread(int threadIndex)
    {
        Task.Run(async () =>
        {
            try {
            while (true)
            {
                var chunks = LoadingThreadChunks[threadIndex];
                while (chunks.Count > 0)
                {
                    LoadChunk(chunks[0]);
                    chunks.RemoveAt(0);
                }
            }
            } catch (Exception err)
            {
                GD.PrintErr($"Chunk loading thread {threadIndex} encountered error: {err}");
            }
        });
    }

    private Vector3I oldPlayerChunkPosition = Vector3I.Zero;
    private Task recalculateTask;

    public override void _Process(double delta)
    {
        Vector3I playerChunkPosition = (Vector3I)(Player.Instance.Position / ChunkSize).Floor();

        if (recalculateTask is { IsCompleted: false })
            return;

        if (playerChunkPosition != oldPlayerChunkPosition)
        {
            recalculateTask = Task.Run(async () =>
            {
                try {
                    RecalculateChunks(playerChunkPosition);
                } catch (Exception err)
                {
                    GD.PrintErr("Encountered error when recalculating chunks: ", err);
                }
            });
        }

        oldPlayerChunkPosition = playerChunkPosition;

        while (OrphanedChunks.Count > 0)
        {
            AddChild(OrphanedChunks[0]);
            OrphanedChunks.RemoveAt(0);
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

    private void RecalculateChunks(Vector3I playerChunkPosition)
    {
        var chunkPositions = GenerateChunksSpiral(playerChunkPosition);
        List<Vector3I> loadedChunkPositions = [];

        foreach (Vector3I chunkPosition in chunkPositions)
        {
            ThreadLoadChunk(chunkPosition);
            loadedChunkPositions.Add(chunkPosition);
        }

        foreach (Vector3I chunkPosition in Chunks.Keys.ToArray())
        {
            if (!loadedChunkPositions.Contains(chunkPosition))
            {
                CallDeferred(MethodName.UnloadChunk, chunkPosition);
            }
        }
    }
    
    public void ApplyChunkMesh(Chunk chunk, Mesh mesh, Shape3D shape)
    {
        if (!IsInstanceValid(chunk)) return;

        if (!IsInstanceValid(chunk.MeshInstance)) return;
        chunk.MeshInstance.Mesh = mesh;

        if (!IsInstanceValid(chunk.CollisionShape)) return;
        chunk.CollisionShape.Shape = shape;
    }

    private Vector3I GetChunkGlobalPosition(Vector3I chunkPosition) => chunkPosition * ChunkSize;

    private void ThreadLoadChunk(Vector3I chunkPosition)
    {
        int threadIndex = (int)(GD.Randf() * LoadingThreadChunks.Count);
        LoadingThreadChunks[threadIndex].Add(chunkPosition);
    }

    private void LoadChunk(Vector3I chunkPosition)
    {
        if (!Chunks.TryGetValue(chunkPosition, out _))
        {
            Chunk chunk = new()
            {
                Position = GetChunkGlobalPosition(chunkPosition),
                Name = $"Chunk@X{chunkPosition.X}@Y{chunkPosition.Y}@Z{chunkPosition.Z}",
                VoxelPosition = chunkPosition*ChunkSize
            };

            Chunks[chunkPosition] = chunk;

            OrphanedChunks.Add(chunk);
            
            chunk.Construct();
            chunk.Update();
        }
    }

    private void UnloadChunk(Vector3I chunkPosition)
    {
        if (Chunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            chunk.QueueFree();
            Chunks.Remove(chunkPosition);
        }
    }

    // private void LoadChunks(Vector3I area, Vector3I chunkPosition, Vector3I centerChunk)
    // {
    //     List<Vector3I> chunkPositions = [];
    //     for (int x=0; x<area.X; x++)
    //     {
    //         for (int y=0; y<area.Y; y++)
    //         {
    //             for (int z=0; z<area.Z; z++)
    //             {
    //                 chunkPositions.Add(new Vector3I(x,y,z) + chunkPosition);
    //             }
    //         }
    //     }

    //     IEnumerable<Vector3I> ordered = from position in chunkPositions
    //                         orderby (position-centerChunk).Length()
    //                         select position;
        
    //     foreach (Vector3I position in ordered)
    //     {
    //         LoadChunk(position);
    //     }
    // }
}
