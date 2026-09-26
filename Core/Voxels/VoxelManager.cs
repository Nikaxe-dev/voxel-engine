using Godot;
using System.Collections.Generic;

namespace VoxelEngine.Core.Voxels;

public struct VoxelData(Vector3I Position, Color Color)
{
    public Vector3I Position = Position;
    public Color Color = Color;
}

public partial class VoxelManager : Node
{
    [Export] Vector3 WorldSize = Vector3.One*16;
    [Export] float CutOff = 0.25f;

    [Export] public MultiMeshInstance3D MultiMesh;

    [Export] public Color[] Colors;

    public List<VoxelData> Voxels = [];

    public int GetVoxelCount() => Voxels.Count;

    public override void _Ready()
    {
        base._Ready();

        Performance.AddCustomMonitor("game/voxels", new Callable(this, MethodName.GetVoxelCount));

        var RandomGenerator = new FastNoiseLite();

        var startTime = Time.GetTicksUsec();

        for (int x = 0; x < WorldSize.X; x++)
        {
            for (int y = 0; y < WorldSize.Y; y++)
            {
                for (int z = 0; z < WorldSize.Z; z++)
                {
                    var random = RandomGenerator.GetNoise3D(x,y,z);
                    if (random > CutOff)
                    {
                        Voxels.Add(new VoxelData(new Vector3I(x,y,z), Colors[(int)(GD.Randf()*Colors.Length)]));
                    }
                }
            }
        }

        var endTime = Time.GetTicksUsec();
        var genTime = endTime - startTime;

        GD.Print($"Voxels loaded: {GetVoxelCount()}");
        GD.Print($"Gen Time: {genTime}");

        MultiMesh.Multimesh.InstanceCount = Voxels.Count;

        for (int i=0; i<Voxels.Count; i++)
        {
            MultiMesh.Multimesh.SetInstanceTransform(i, new(Basis.Identity, Voxels[i].Position));
            MultiMesh.Multimesh.SetInstanceColor(i, Voxels[i].Color);
        }
    }
}
