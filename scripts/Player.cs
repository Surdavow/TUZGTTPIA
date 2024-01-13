using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;

public partial class Player : CharacterBody3D
{
    public const float Speed = 3f;
    public const float JumpVelocity = 4.5f;
    [Export]
    public float sensitivity_horizontal = 0.5f;
    [Export]
    public float sensitivity_vertical = 0.5f;
    public Node3D camera_mount_thirdperson;
    public AnimationPlayer animation_player;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
		camera_mount_thirdperson = GetNode<Node3D>("camera_mount_thirdperson");        
		animation_player = GetNode<AnimationPlayer>("Visuals/alpha/AnimationPlayer");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * sensitivity_horizontal));
            camera_mount_thirdperson.RotateX(Mathf.DegToRad(-mouseMotionEvent.Relative.Y * sensitivity_vertical));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            if (animation_player.CurrentAnimation != "walking")
            {
                animation_player.Play("walk");
            }

            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            if (animation_player.CurrentAnimation != "idle")
            {
                animation_player.Play("idle");
            }

            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
