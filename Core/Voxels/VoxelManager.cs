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

    [Export] public int RenderDistance = 4;
    [Export] public int VerticalRenderDistance = 4;

    [Export] public Color[] Colors;

    public Dictionary<Vector3I, Chunk> Chunks = [];

    private List<Chunk> ChunkPool = [];

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
    }

    private Vector3I oldPlayerChunkPosition = Vector3I.Zero;
    private Vector3I lastChunkUpdatePosition = Vector3I.Zero;

    private Task recalculateTask;
    private CancellationTokenSource recalculateCancelToken;

    [Export] public int ChunkUpdateThreshold = 2;

    public override void _Process(double delta)
    {
        Vector3I playerChunkPosition = (Vector3I)(Player.Instance.Position / ChunkSize).Floor();

        if (playerChunkPosition != oldPlayerChunkPosition && (playerChunkPosition-lastChunkUpdatePosition).Length() >= ChunkUpdateThreshold)
        {
            lastChunkUpdatePosition = playerChunkPosition;

            recalculateCancelToken?.Cancel();

            recalculateCancelToken = new();
            recalculateTask = Task.Run(() =>
            {
                try {
                    RecalculateChunks(playerChunkPosition, recalculateCancelToken.Token);
                } catch (Exception err)
                {
                    GD.PrintErr("Encountered error when recalculating chunks: ", err);
                }
            });
        }

        oldPlayerChunkPosition = playerChunkPosition;
    }

    private Vector3I[] GenerateChunksSpiral(Vector3I center)
    {
        List<Vector3I> result = [];

        int maxY = center.Y + VerticalRenderDistance; // center.Y + RenderDistance;
        int minY = center.Y - VerticalRenderDistance; // center.Y - RenderDistance;

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

    private void RecalculateChunks(Vector3I playerChunkPosition, CancellationToken cancelToken)
    {
        var chunkPositions = GenerateChunksSpiral(playerChunkPosition);

        foreach (Vector3I chunkPosition in Chunks.Keys.ToArray())
        {
            if (!chunkPositions.Contains(chunkPosition))
            {
                // CallDeferred(MethodName.UnloadChunk, chunkPosition);
                UnloadChunk(chunkPosition);
            }
        }

        foreach (Vector3I chunkPosition in chunkPositions)
        {
            LoadChunk(chunkPosition);
            if (cancelToken.IsCancellationRequested) break;
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

    private Chunk GetNewChunk()
    {
        if (ChunkPool.Count > 0)
        {
            var chunk = ChunkPool[0];
            ChunkPool.Remove(chunk);
            return chunk;
        } else
        {
            Chunk chunk = new() {Visible = false};
            CallDeferred(Node.MethodName.AddChild, chunk);
            return chunk;
        }
    }

    private void ReleaseChunkToPool(Chunk chunk)
    {
        ChunkPool.Add(chunk);
        chunk.CallDeferred(Node3D.MethodName.SetVisible, false);
    }

    private void LoadChunk(Vector3I chunkPosition)
    {
        if (!Chunks.TryGetValue(chunkPosition, out _))
        {
            Chunk chunk = GetNewChunk();
            chunk.VoxelPosition = GetChunkGlobalPosition(chunkPosition);

            chunk.CallDeferred(Node3D.MethodName.SetPosition, GetChunkGlobalPosition(chunkPosition));
            chunk.CallDeferred(Node.MethodName.SetName, $"Chunk@X{chunkPosition.X}@Y{chunkPosition.Y}@Z{chunkPosition.Z}");

            Chunks[chunkPosition] = chunk;
            
            chunk.Construct();
            chunk.Update();

            chunk.CallDeferred(Node3D.MethodName.SetVisible, true);
        }
    }

    public void AddChunkToTree(Vector3I chunkPosition)
    {
        if (Chunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            if (chunk == null || !IsInstanceValid(chunk)) return;
            AddChild(chunk);
        }
    }

    private void UnloadChunk(Vector3I chunkPosition)
    {
        if (Chunks.TryGetValue(chunkPosition, out Chunk chunk))
        {
            // chunk.QueueFree();
            Chunks.Remove(chunkPosition);
            ReleaseChunkToPool(chunk);
        }
    }
}
