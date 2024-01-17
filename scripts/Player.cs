using Godot;
using Godot.Collections;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class Player : CharacterBody3D
{
    [Export]    
    public bool canJump;     
    [Export]    
    public float Mass = 25;    
    [Export]
    public float jumpForce = 10;
    [Export]
    public float moveForce = 0.5f;        
    [Export]
    public float sensitivityHorizontal = 0.5f;
    [Export]
    public float sensitivityVertical = 0.5f;
    [Export]
    public float airControl = 0.1f;    
    
    private const float walkSpeed = 1.5f;
    private const float runSpeed = 4.5f;
    private const float lerpSpeed = 0.15f;
    private float currentSpeed;
    private bool isRunning = false;
    private bool isLocked = false;    
    private Node3D cameraThirdPerson;
    private Node3D Character;
    private Node3D CharacterLookNode;
    private AnimationTree AnimationTree;
    private AudioStreamPlayer3D soundPlayer;
    private string[] punchAnimations = {"punch1", "punch2","punch3"};
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;//Hide and lock the mouse
		cameraThirdPerson = GetNode<Node3D>("CameraMountThirdPerson");  
		Character = GetNode<Node3D>("character_default");
		CharacterLookNode = GetNode<Node3D>("character_default/default_rig/Skeleton3D/BoneAttachment3D/CharacterLookNode");
		soundPlayer = GetNode<AudioStreamPlayer3D>("SoundPlayer3D");
		AnimationTree = GetNode<AnimationTree>("character_default/AnimationTree");
        AnimationTree.Set("parameters/MainStates/conditions/isGrounded",true);
        AnimationTree.Set("parameters/MainStates/conditions/isJumping",false);
        AnimationTree.Set("parameters/MainStates/conditions/isFalling",false);        
        canJump = true;
        currentSpeed = 0;
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * sensitivityHorizontal));
            cameraThirdPerson.RotateX(Mathf.DegToRad(-mouseMotionEvent.Relative.Y * sensitivityVertical));
            cameraThirdPerson.RotationDegrees = new Vector3(Math.Clamp(cameraThirdPerson.RotationDegrees.X,-100,100),-180,0);            
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape) GetTree().Quit();
    }


    public void jumpFunction()
    {
        Vector3 velocity = Velocity;
        velocity.Y = Mathf.Lerp(velocity.Y,jumpForce,0.8f);
        Velocity = velocity;
        MoveAndSlide();
    }

    public void playFootsteps()
    {
        if (IsOnFloor()) 
        {
            //if (isRunning) 
            soundPlayer.Play();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        //AnimationNodeStateMachinePlayback stateMachine = (AnimationNodeStateMachinePlayback)AnimationTree.Get("parameters/MainStates/playback");
        Vector3 velocity = Velocity;        

        if (Input.IsActionJustPressed("jump") && IsOnFloor() && canJump) 
        {
            AnimationTree.Set("parameters/MainStates/conditions/isJumping",true);
            AnimationTree.Set("parameters/MainStates/conditions/isFalling",false);
            AnimationTree.Set("parameters/MainStates/conditions/isGrounded",false);
        }       

        //Lerp the speed slowly to accomodate run acceleration, and set isRunning to true
        switch (Input.IsActionPressed("sprint"))        
        {
            case true:  currentSpeed = Mathf.Lerp(currentSpeed,runSpeed,lerpSpeed*0.15f);
                        isRunning = true;
                        break;
            case false: currentSpeed = Mathf.Lerp(currentSpeed,walkSpeed,lerpSpeed*0.75f);
                        isRunning = false;
                        break;
        }   

        Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            if(-Transform.Basis.Z.Dot(Velocity) < 0)  direction.Z *= currentSpeed*0.75f;
            else direction.Z *= currentSpeed;
            
            direction.X *= currentSpeed;
        }
        else
        {
            direction.X = 0;
            direction.Z = 0;
        }    

        velocity.X = Mathf.Lerp(velocity.X,direction.X,lerpSpeed * moveForce);
        velocity.Z = Mathf.Lerp(velocity.Z,direction.Z,lerpSpeed * moveForce);
        
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta*(Mass/gravity);

        float LongSpeed = -Transform.Basis.Z.Dot(Velocity);
        float LatSpeed = Transform.Basis.X.Dot(Velocity);
        float LatLongSpeed = (float)Math.Sqrt(Math.Pow(LongSpeed, 2) + Math.Pow(LatSpeed, 2));
        float LocomotionBlendX = AnimationTree.Get("parameters/MainStates/Locomotion/blend_position").AsVector2().X;
        float LocmotionBlendY = AnimationTree.Get("parameters/MainStates/Locomotion/blend_position").AsVector2().Y;
        float LocomotionSpeedScale = (float)AnimationTree.Get("parameters/TimeScale/scale");
        AnimationTree.Set("parameters/MainStates/Locomotion/blend_position", new Vector2(Math.Clamp(Mathf.Lerp(LocomotionBlendX,LatSpeed / runSpeed,0.25f),-1,1),Math.Clamp(Mathf.Lerp(LocmotionBlendY,LongSpeed / runSpeed,0.25f),-1,1)));


        AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocomotionSpeedScale,Math.Clamp(LatLongSpeed / currentSpeed, 1, 1000),lerpSpeed));
        //if (IsOnFloor()) 
        ////else AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocomotionSpeedScale,1,1.25f));        

        Velocity = velocity;
        MoveAndSlide();

        if (IsOnFloor()) 
        {
            AnimationTree.Set("parameters/MainStates/conditions/isGrounded",true);
            AnimationTree.Set("parameters/MainStates/conditions/isFalling",false); 
        }
        else
        {            
            AnimationTree.Set("parameters/MainStates/conditions/isGrounded",false);                        
            if (velocity.Y <= 0) AnimationTree.Set("parameters/MainStates/conditions/isFalling",true);            
        }
    }
}
