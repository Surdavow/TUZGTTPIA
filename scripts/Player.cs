using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection.Metadata;

public partial class Player : CharacterBody3D
{
    [Export]
    public float lerpSpeed = 0.15f;
    public float Speed = 3f;
    [Export]
    public float airControl = 0.1f;
    [Export]
    public float runSpeed = 5f;
    [Export]
    public float walkSpeed = 2.5f;
    public float jumpVelocity = 3.5f;
    [Export]
    public float sensitivityHorizontal = 0.5f;
    [Export]
    public float sensitivityVertical = 0.5f;
    public bool isRunning = false;
    public bool isLocked = false;    
    public Node3D cameraThirdPerson;
    public Node3D Visuals;
    public AnimationTree AnimationTree;
    public AudioStreamPlayer3D soundPlayer;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;//Hide and lock the mouse
		cameraThirdPerson = GetNode<Node3D>("CameraMountThirdPerson");  
		Visuals = GetNode<Node3D>("Visuals");
		soundPlayer = GetNode<AudioStreamPlayer3D>("SoundPlayer3D");
		AnimationTree = GetNode<AnimationTree>("Visuals/character_default/AnimationTree");
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
            cameraThirdPerson.RotationDegrees = new Vector3(Math.Clamp(cameraThirdPerson.RotationDegrees.X,-90,100),-180,0);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape)
        GetTree().Quit();
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
                velocity.X = Mathf.Lerp(velocity.X,direction.X * Speed,lerpSpeed);
                velocity.Z = Mathf.Lerp(velocity.Z,direction.Z * Speed,lerpSpeed);
        }
        else
        {
            velocity.X = Mathf.Lerp(velocity.X,0,lerpSpeed);
            velocity.Z = Mathf.Lerp(velocity.Z,0,lerpSpeed);
        }

        float ForwardVectorSpeed = -Transform.Basis.Z.Dot(velocity) / Speed;
        float SideVectorSpeed = Transform.Basis.X.Dot(velocity) / Speed;

        AnimationTree.Set("parameters/Locomotion/blend_position", new Vector2(SideVectorSpeed, ForwardVectorSpeed));

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor()) velocity.Y = jumpVelocity;

        Velocity = velocity;            
        MoveAndSlide();
    }

}
