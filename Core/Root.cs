using Godot;
using System;

namespace VoxelEngine.Core;

public partial class Root : Node
{
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event.IsActionPressed("ui_cancel"))
        {
            GetTree().Quit();
        }

        if (@event.IsActionPressed("debug_toggle_wireframe"))
        {
            GetViewport().DebugDraw = GetViewport().DebugDraw == Viewport.DebugDrawEnum.Wireframe ? Viewport.DebugDrawEnum.Disabled : Viewport.DebugDrawEnum.Wireframe;
        }
    }
}
