using Godot;
using Godot.Collections;
using System;

public partial class Player : CharacterBody3D
{
    [Export]
    public float moveMultiplier = 1;
    [Export]    
    public bool canJump;     
    [Export]    
    public float Mass = 25;    
    [Export]
    public const float jumpForce = 10;
    [Export]
    public float sensitivityHorizontal = 0.5f;
    [Export]
    public float sensitivityVertical = 0.5f;

    public const float accelerationSpeed = 0.4f;    
    public const float airControl = 0.1f;
    public const float walkSpeed = 2;
    public const float runSpeed = 4;    
    public const float lerpSpeed = 0.15f;
    public float currentSpeed;
    public bool isRunning = false;
    public bool isLocked = false;    
    public Node3D cameraThirdPerson;
    public Node3D Character;
    public Node3D CharacterLookNode;
    public AnimationTree AnimationTree;
    public AudioStreamPlayer3D soundPlayer;
    public bool superRun = false;    
    public string[] punchAnimations = {"punch1", "punch2","punch3"};
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;//Hide and lock the mouse
		cameraThirdPerson = GetNode<Node3D>("CameraMountThirdPerson");  
		Character = GetNode<Node3D>("character_default");
		CharacterLookNode = GetNode<Node3D>("character_default/default_rig/Skeleton3D/BoneAttachment3D/CharacterLookNode");
		soundPlayer = GetNode<AudioStreamPlayer3D>("SoundPlayer3D");
		AnimationTree = GetNode<AnimationTree>("character_default/AnimationTree");
        AnimationTree.Set("parameters/Main States/conditions/isGrounded",true);
        AnimationTree.Set("parameters/Main States/conditions/isJumping",false);
        AnimationTree.Set("parameters/Main States/conditions/isFalling",false);
        canJump = true;
        currentSpeed = 0;
    }
    public override void _Input(InputEvent @event)
    {
        
        if (Input.IsActionJustPressed("melee"))
        {
            Random rand = new Random();            
            AnimationNodeStateMachinePlayback stateMachine = (AnimationNodeStateMachinePlayback)AnimationTree.Get("parameters/Upper Anims States/playback");
            string randomPunchAnimation = punchAnimations[rand.Next(punchAnimations.Length)];
            stateMachine.Travel(randomPunchAnimation);
        }    

        if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * sensitivityHorizontal));
            cameraThirdPerson.RotateX(Mathf.DegToRad(-mouseMotionEvent.Relative.Y * sensitivityVertical));
            cameraThirdPerson.RotationDegrees = new Vector3(Math.Clamp(cameraThirdPerson.RotationDegrees.X,-100,100),-180,0);

            float currentRotationX = cameraThirdPerson.RotationDegrees.X;
            float remappedValue = -1 + (currentRotationX - 100) * (1 - -1) / (-100 - 100);          
            AnimationTree.Set("parameters/LookAnims/blend_position", new Vector2(0, remappedValue));
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
        AnimationNodeStateMachinePlayback stateMachine = (AnimationNodeStateMachinePlayback)AnimationTree.Get("parameters/Main States/playback");
        Vector3 velocity = Velocity;

        if (Input.IsActionJustPressed("jump") && IsOnFloor() && canJump) 
        {
            AnimationTree.Set("parameters/Main States/conditions/isJumping",true);
            AnimationTree.Set("parameters/Main States/conditions/isFalling",false);
            AnimationTree.Set("parameters/Main States/conditions/isGrounded",false);
        }        

        if(Input.IsActionJustPressed("interact"))
        {
            switch(superRun)
            {
                case true: superRun = false;
                            moveMultiplier = 1;
                            break;                
                case false: superRun = true;
                            moveMultiplier = 2.5f;
                            break;
            }            
        }           

        //Lerp the speed slowly to accomodate run acceleration, and set isRunning to true
        switch (Input.IsActionPressed("sprint"))        
        {
            case true:  currentSpeed = Mathf.Lerp(currentSpeed,runSpeed*moveMultiplier,lerpSpeed*0.15f);
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

        velocity.X = Mathf.Lerp(velocity.X,direction.X,lerpSpeed * accelerationSpeed);
        velocity.Z = Mathf.Lerp(velocity.Z,direction.Z,lerpSpeed * accelerationSpeed);
        
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta*(Mass/gravity);

        float LongSpeed = -Transform.Basis.Z.Dot(Velocity);
        float LatSpeed = Transform.Basis.X.Dot(Velocity);
        float LatLongSpeed = (float)Math.Sqrt(Math.Pow(LongSpeed, 2) + Math.Pow(LatSpeed, 2));

        float LocmotionBlendX = AnimationTree.Get("parameters/Main States/Locomotion/blend_position").AsVector2().X;
        float LocmotionBlendY = AnimationTree.Get("parameters/Main States/Locomotion/blend_position").AsVector2().Y;
        bool LocmotionIsJumping = (bool)AnimationTree.Get("parameters/Main States/conditions/isJumping");
        float LocmotionSpeedScale = (float)AnimationTree.Get("parameters/TimeScale/scale");
        AnimationTree.Set("parameters/Main States/Locomotion/blend_position", new Vector2(Math.Clamp(Mathf.Lerp(LocmotionBlendX,LatSpeed / runSpeed,0.25f),-1,1),Math.Clamp(Mathf.Lerp(LocmotionBlendY,LongSpeed / runSpeed,0.25f),-1,1)));

        //Vector3 normalizedLook = (GlobalPosition - CharacterLookNode.GlobalPosition).Normalized();
        //float lookDotProduct = -Transform.Basis.Z.Dot(normalizedLook);
        //AnimationTree.Set("parameters/LookAnims/blend_position", new Vector2(Mathf.Lerp(AnimationTree.Get("parameters/LookAnims/blend_position").AsVector2().Y,lookDotProduct,2), AnimationTree.Get("parameters/LookAnims/blend_position").AsVector2().Y));        

        if (IsOnFloor()) AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocmotionSpeedScale,Math.Clamp(LatLongSpeed / runSpeed, 1, 1000),lerpSpeed));
        else if (LocmotionIsJumping) AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocmotionSpeedScale,1,1.25f));

        Velocity = velocity;
        MoveAndSlide();

        if (IsOnFloor()) 
        {
            AnimationTree.Set("parameters/Main States/conditions/isGrounded",true);
            AnimationTree.Set("parameters/Main States/conditions/isFalling",false); 
        }
        else
        {            
            AnimationTree.Set("parameters/Main States/conditions/isGrounded",false);                        
            if (velocity.Y <= 0) AnimationTree.Set("parameters/Main States/conditions/isFalling",true);            
        }
    }
}
