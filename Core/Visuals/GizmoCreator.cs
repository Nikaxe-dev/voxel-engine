using System.Collections.Generic;
using Godot;

namespace VoxelEngine.Core.Visuals;

public static class GizmoCreator
{
    private static readonly Vector3[] CUBE_VERTICES = [
        // front 4 vertices
        new Vector3(0,0,1),
        new Vector3(1,0,1),
        new Vector3(1,1,1),
        new Vector3(0,1,1),

        // back 4 vertices
        new Vector3(0,0,0),
        new Vector3(1,0,0),
        new Vector3(1,1,0),
        new Vector3(0,1,0),
    ];

    private static void CreateLineLoop(ImmediateMesh mesh, Vector3[] vertices)
    {
        List<Vector3> LoopedVertices = [.. vertices];
        LoopedVertices.Add(vertices[0]);

        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        foreach (var vertex in LoopedVertices)
        {
            mesh.SurfaceAddVertex(vertex);
        }
        mesh.SurfaceEnd();
    }

    private static void CreateLine(ImmediateMesh mesh, Vector3 start, Vector3 end)
    {
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        mesh.SurfaceAddVertex(start);
        mesh.SurfaceAddVertex(end);
        mesh.SurfaceEnd();
    }

    public static MeshInstance3D CreateOutlineBox(Vector3 position, Basis basis, Vector3 size, Color color, bool visibleThroughWalls)
    {
        MeshInstance3D box = new();

        ImmediateMesh mesh = new();

        OrmMaterial3D material = new()
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = color,
            NoDepthTest = visibleThroughWalls,
        };

        CreateLineLoop(mesh, [CUBE_VERTICES[0],CUBE_VERTICES[1],CUBE_VERTICES[2],CUBE_VERTICES[3]]);
        CreateLineLoop(mesh, [CUBE_VERTICES[4],CUBE_VERTICES[5],CUBE_VERTICES[6],CUBE_VERTICES[7]]);
        for (int i = 0; i<4; i++)
        {
            CreateLine(mesh, CUBE_VERTICES[i], CUBE_VERTICES[i+4]);
        }

        box.MaterialOverride = material;
        box.Mesh = mesh;

        box.Transform = new(basis, position);
        box.Scale = size;

        box.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        return box;
    }
}