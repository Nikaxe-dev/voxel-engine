using System.Collections.Generic;
using Godot;

namespace VoxelEngine.Core.Data;

public struct ArrayMeshData
{
    public List<Vector3> Vertices = [];
    public List<Vector3> Normals = [];
    public List<Color> Colors = [];

    public readonly ArrayMesh GenerateMesh()
    {
        if (Vertices.Count == 0) return null;

        ArrayMesh mesh = new();

        Godot.Collections.Array surfaceArray = [];
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        surfaceArray[(int)Mesh.ArrayType.Vertex] = Vertices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Normal] = Normals.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = Colors.ToArray();

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

        mesh.SurfaceSetMaterial(0, new StandardMaterial3D()
        {
            VertexColorUseAsAlbedo = true,
        });

        return mesh;
    }

    public ArrayMeshData() {}
}