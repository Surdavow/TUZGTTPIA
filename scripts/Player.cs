using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection.Metadata;

public partial class Player : CharacterBody3D
{
    public float Speed = 3f;
    
    [Export]
    public float airControl = 0.1f;
    [Export]
    public float runSpeed = 5f;
    [Export]
    public float walkSpeed = 3f;
    public float jumpVelocity = 3.5f;
    [Export]
    public float sensitivityHorizontal = 0.5f;
    [Export]
    public float sensitivityVertical = 0.5f;
    public bool isRunning = false;
    public bool isLocked = false;    
    public Node3D cameraThirdPerson;
    public Node3D Visuals;
    public AnimationPlayer Animator;
    public AudioStreamPlayer3D soundPlayer;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;//Hide and lock the mouse
		cameraThirdPerson = GetNode<Node3D>("cameraMountThirdPerson");  
		//Visuals = GetNode<Node3D>("Visuals");
		soundPlayer = GetNode<AudioStreamPlayer3D>("soundPlayer");
		//Animator = GetNode<AnimationPlayer>("Visuals/alpha/AnimationPlayer");
    }

    public void playFootsteps()
    {
        if (IsOnFloor()) 
        {
            //if (isRunning) 
            soundPlayer.Play();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * sensitivityHorizontal));
            cameraThirdPerson.RotateX(Mathf.DegToRad(-mouseMotionEvent.Relative.Y * sensitivityVertical));            
        }
    }

    public override void _PhysicsProcess(double delta)
    {

        Vector3 velocity = Velocity;        
        // Add the gravity.
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta;

        switch (Input.IsActionPressed("sprint"))        
        {
            case true:  Speed = runSpeed;
                        isRunning = true;
                        break;
            case false: Speed = walkSpeed;
                        isRunning = false;
                        break;
        }

        // Get the input direction and handle the movement/deceleration.
        Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized(); 
        
        if (direction != Vector3.Zero)
        {
                velocity.X = direction.X * Speed;
                velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);           
        }

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor()) velocity.Y = jumpVelocity;

        Velocity = velocity;            
        if (!isLocked) MoveAndSlide();
    }
}
