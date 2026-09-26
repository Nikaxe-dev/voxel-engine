using Godot;
using System;

namespace VoxelEngine.Core;

public partial class Root : Node
{
    [Export] public Camera3D DebugCamera;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Player.Instance.PlayerCamera.MakeCurrent();
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

        if (@event.IsActionPressed("debug_toggle_camera"))
        {
            if (GetViewport().GetCamera3D() == Player.Instance.PlayerCamera)
            {
                DebugCamera.GlobalTransform = Player.Instance.PlayerCamera.GlobalTransform;
                DebugCamera.MakeCurrent();
                Player.Instance.MovementEnabled = false;
            } else
            {
                Player.Instance.PlayerCamera.MakeCurrent();
                Player.Instance.MovementEnabled = true;
            }
        }
    }
}
