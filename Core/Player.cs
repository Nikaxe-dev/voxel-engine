using Godot;
using System;

namespace VoxelEngine.Core;

public partial class Player : CharacterBody3D
{
    [Export] public float SPEED = 5;
    [Export] public float FLY_SPEED = 20;
    [Export] public float JUMP_VELOCITY = 6.5f;

    [Export] public float FLY_VERTICAL_SPEED = 20;

    [Export] public Camera3D PlayerCamera;

    [Export] public float CAMERA_TURN_SENS = 0.009f;

    [Export] public bool IsFlying = false;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!IsOnFloor() && !IsFlying)
        {
            Velocity += GetGravity() * (float)delta;
        }

        if (Input.IsActionJustPressed("move_up") && IsOnFloor() && !IsFlying)
        {
            Velocity = new Vector3(1,0,1) * Velocity + Vector3.Up * JUMP_VELOCITY;
        }

        if (IsFlying)
        {
            float Joystick = Input.GetAxis("move_down", "move_up");
            Velocity = new Vector3(1,0,1) * Velocity + Vector3.Up * Joystick * FLY_VERTICAL_SPEED;
        }

        Vector2 InputDir = Input.GetVector("move_left","move_right","move_forward","move_back");

		Vector3 forward = PlayerCamera.GlobalBasis.Z;
		forward *= new Vector3(1,0,1);
		forward = forward.Normalized();

		Vector3 right = PlayerCamera.GlobalBasis.X;
		right *= new Vector3(1,0,1);
		right = right.Normalized();

		Vector3 Direction = (right * InputDir.X + forward * InputDir.Y).Normalized();

        float Speed = IsFlying ? FLY_SPEED : SPEED;

        if (InputDir != Vector2.Zero)
        {
            Velocity = new Vector3(Direction.X*Speed, Velocity.Y, Velocity.Z);
            Velocity = new Vector3(Velocity.X, Velocity.Y, Direction.Z*Speed);
        } else
        {
            Velocity = new Vector3(Mathf.MoveToward(Velocity.X, 0, Speed),Velocity.Y,Velocity.Z);
            Velocity = new Vector3(Velocity.X,Velocity.Y,Mathf.MoveToward(Velocity.Z, 0, Speed));
        }

        MoveAndSlide();
    }

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector3 Rotation = Vector3.Down * mouseMotion.Relative.X * CAMERA_TURN_SENS + Vector3.Left * mouseMotion.Relative.Y * CAMERA_TURN_SENS;
            PlayerCamera.Rotation += Rotation;
            PlayerCamera.Rotation = new Vector3((float)Math.Clamp(PlayerCamera.Rotation.X, Mathf.DegToRad(-80), Mathf.DegToRad(80)), PlayerCamera.Rotation.Y, PlayerCamera.Rotation.Z);
        }

        if (@event.IsActionPressed("move_fly"))
        {
            IsFlying = !IsFlying;
        }
    }
}
