using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection.Metadata;

public partial class Player : CharacterBody3D
{
    public float Speed = 3f;
    public bool isRunning = false;
    public const float runSpeed = 5f;
    public const float walkSpeed = 3f;
    public const float jumpVelocity = 3.5f;
    [Export]
    public float sensitivityHorizontal = 0.5f;
    [Export]
    public float sensitivityVertical = 0.5f;
    public Node3D cameraThirdPerson;
    public Node3D Visuals;
    public AnimationPlayer Animator;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
		cameraThirdPerson = GetNode<Node3D>("camera_mount_thirdperson");  
		Visuals = GetNode<Node3D>("visuals");  
		Animator = GetNode<AnimationPlayer>("visuals/alpha/AnimationPlayer");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * sensitivityHorizontal));
            cameraThirdPerson.RotateX(Mathf.DegToRad(-mouseMotionEvent.Relative.Y * sensitivityVertical));
            Visuals.RotateY(Mathf.DegToRad(mouseMotionEvent.Relative.X * sensitivityHorizontal));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        switch (Input.IsActionPressed("sprint"))        
        {
            case true:  Speed = runSpeed;
                        isRunning = true;
                        break;
            case false: Speed = walkSpeed;
                        isRunning = false;
                        break;
        }    

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = jumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            switch(isRunning)
            {
                case true:  if (Animator.CurrentAnimation != "sprint") Animator.Play("sprint");                            
                            break;
                case false: if (Animator.CurrentAnimation != "walk") Animator.Play("walk");
                            break;
            }

            Visuals.LookAt(GlobalPosition + direction);
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);

            if (Animator.CurrentAnimation != "idle") Animator.Play("idle");                
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
