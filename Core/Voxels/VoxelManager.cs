using Godot;
using System.Collections.Generic;

namespace VoxelEngine.Core.Voxels;

public struct VoxelData(Color Color)
{
    public Color Color = Color;
}

public partial class VoxelManager : Node
{
    public static VoxelManager Instance {get; private set;}

    [Export] public int WorldSeed = 0;

    [Export] public bool DebugSpaceOutChunks = false;
    [Export] public Vector3I DebugSpaceOutChunksSpacing = Vector3I.One*8;

    [Export] public bool DebugShowChunkOutline = false;

    [Export] public Vector3I ChunkSize = Vector3I.One*16;
    [Export] public float CutOff = 0.25f;

    [Export] public Vector3I WorldChunkSize = Vector3I.One*4;

    [Export] public Color[] Colors;

    public List<VoxelData> Voxels = [];

    public int GetVoxelCount() => Voxels.Count;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        // Chunk chunk = new();
        // chunk.Construct();
        // chunk.Update();
        // AddChild(chunk);

        for (int x=0; x<WorldChunkSize.X; x++)
        {
            for (int y=0; y<WorldChunkSize.Y; y++)
            {
                for (int z=0; z<WorldChunkSize.Z; z++)
                {
                    Chunk chunk = new()
                    {
                        Position = new Vector3(x,y,z) * (ChunkSize + (DebugSpaceOutChunks ? DebugSpaceOutChunksSpacing : Vector3I.Zero)),
                        Name = $"Chunk@X{x}@Y{y}@Z{z}"
                    };

                    chunk.Construct();
                    chunk.Update();
                    AddChild(chunk);
                }
            }
        }
    }
}
