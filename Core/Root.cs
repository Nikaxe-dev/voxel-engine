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
    }
}
