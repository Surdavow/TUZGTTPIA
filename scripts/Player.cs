using Godot;
using Godot.Collections;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class Player : CharacterBody3D
{
    [Export]    
    public bool canJump;     
    [Export]    
    public float Mass = 15;    
    [Export]
    public float jumpForce = 10;
    [Export]
    public float moveForce = 0.375f;        
    [Export]
    public float mouseSensitivity = 1;
    [Export]
    public float airControl = 0.8f;    
    private const float walkSpeed = 2;
    private const float runSpeed = 4;
    private const float lerpSpeed = 0.25f;
    private float currentSpeed;
    private bool isRunning = false;
    private bool isLocked = false;    
    private Node3D cameraThirdPerson;
    private Node3D Character;
    private AnimationTree AnimationTree;
    private Skeleton3D CharacterSkeleton;
    private AudioStreamPlayer3D soundPlayer;
    private string[] punchAnimations = {"punch1", "punch2","punch3"};
    private AudioStream[] footStepSounds = new AudioStream[]
    {
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_03.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_04.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_05.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_06.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_07.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_08.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_11.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/rubber_12.wav")
    };
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;//Hide and lock the mouse
		cameraThirdPerson = GetNode<Node3D>("CameraMountThirdPerson");  
		Character = GetNode<Node3D>("character_default");
		soundPlayer = GetNode<AudioStreamPlayer3D>("SoundPlayerFootsteps3D");
		AnimationTree = GetNode<AnimationTree>("character_default/AnimationTree");
		CharacterSkeleton = GetNode<Skeleton3D>("character_default/default_rig/Skeleton3D");
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
            RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * (0.25f*mouseSensitivity)));
            cameraThirdPerson.RotationDegrees = new Vector3(Math.Clamp(cameraThirdPerson.RotationDegrees.X - mouseMotionEvent.Relative.Y * (0.5f*mouseSensitivity),-90,100),0,0);            
        }

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape) GetTree().Quit();
    }

    public void jumpFunction()
    {
        if(!IsOnFloor()) return;

        Vector3 velocity = Velocity;
        velocity.Y = Mathf.Lerp(velocity.Y,jumpForce,0.8f);
        Velocity = velocity;
        MoveAndSlide();
    }

    public void playFootsteps()
    {
        float currentSpeed = -Transform.Basis.Z.Dot(Velocity);
        if (IsOnFloor() && currentSpeed >= (walkSpeed / 4)) 
        {
            int randomIndex = (int)GD.RandRange(0, footStepSounds.Length-1);
            soundPlayer.Stream = footStepSounds[randomIndex];            
            soundPlayer.PitchScale = (float)GD.RandRange(0.4f*currentSpeed,0.8f*currentSpeed);
                
            soundPlayer.Play();
        }
    }

    public override void _PhysicsProcess(double delta)
    {        
        Vector3 velocity = Velocity;

        if (Input.IsActionJustPressed("jump") && IsOnFloor() && canJump) 
        {
            AnimationTree.Set("parameters/MainStates/conditions/isJumping",true);
        }

        Quaternion headBoneQuaternion = CharacterSkeleton.GetBonePoseRotation(5);
        Quaternion neckBoneQuaternion = CharacterSkeleton.GetBonePoseRotation(4);
        Quaternion spine2BoneQuaternion = CharacterSkeleton.GetBonePoseRotation(3);
        CharacterSkeleton.SetBonePoseRotation(5, new Quaternion(-cameraThirdPerson.Rotation.X*0.25f, headBoneQuaternion.Y, headBoneQuaternion.Z, headBoneQuaternion.W));        
        CharacterSkeleton.SetBonePoseRotation(4, new Quaternion(-cameraThirdPerson.Rotation.X*0.1f, neckBoneQuaternion.Y, neckBoneQuaternion.Z, neckBoneQuaternion.W));
        CharacterSkeleton.SetBonePoseRotation(3, new Quaternion(-cameraThirdPerson.Rotation.X*0.1f, neckBoneQuaternion.Y, neckBoneQuaternion.Z, neckBoneQuaternion.W));

        //Lerp the speed slowly to accomodate run acceleration, and set isRunning to true
        switch (Input.IsActionPressed("sprint"))        
        {
            case true:  if(IsOnFloor()) currentSpeed = Mathf.Lerp(currentSpeed,runSpeed,lerpSpeed);
                        else currentSpeed = Mathf.Lerp(currentSpeed,walkSpeed,lerpSpeed);
                        isRunning = true;
                        break;
            case false: currentSpeed = Mathf.Lerp(currentSpeed,walkSpeed,lerpSpeed);
                        isRunning = false;
                        break;
        }   

        Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta*(Mass/gravity);        

        if (direction != Vector3.Zero)
        {
            //if(-Transform.Basis.Z.Dot(Velocity) < 0 && IsOnFloor())  direction.Z *= currentSpeed*0.5f;
            //else direction.Z *= currentSpeed;            
            direction.Z *= currentSpeed;
            direction.X *= currentSpeed;
            
            if(!IsOnFloor())
            {
                direction.Z *= airControl;
                direction.X *= airControl;
            }        
        }
        else
        {
            direction.X = 0;
            direction.Z = 0;            
        }

        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta*(Mass/gravity);
        velocity.X = Mathf.Lerp(velocity.X,direction.X,lerpSpeed * moveForce);
        velocity.Z = Mathf.Lerp(velocity.Z,direction.Z,lerpSpeed * moveForce);
        
        float LongSpeed = -Transform.Basis.Z.Dot(Velocity);    
        float LatSpeed = Transform.Basis.X.Dot(Velocity);
        float LatLongSpeed = (float)Math.Sqrt(Math.Pow(LongSpeed, 2) + Math.Pow(LatSpeed, 2));
        Vector2 LocomotionBlend = AnimationTree.Get("parameters/MainStates/Locomotion/blend_position").AsVector2();      
        AnimationTree.Set("parameters/MainStates/Locomotion/blend_position", new Vector2(Math.Clamp(Mathf.Lerp(LocomotionBlend.X,LatSpeed / runSpeed,0.375f),-1,1),Math.Clamp(Mathf.Lerp(LocomotionBlend.Y,LongSpeed / runSpeed,0.375f),-1,1)));
        
        float LocomotionSpeedScale = (float)AnimationTree.Get("parameters/TimeScale/scale");
        if (IsOnFloor()) AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocomotionSpeedScale,Math.Clamp(Math.Ceiling(LatLongSpeed) / currentSpeed, 1, 1000),lerpSpeed));
        else AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocomotionSpeedScale,1,1.25f));

        Velocity = velocity;
        MoveAndSlide();

        AnimationNodeStateMachinePlayback stateMachine = (AnimationNodeStateMachinePlayback)AnimationTree.Get("parameters/MainStates/playback");
        if (IsOnFloor()) 
        {
            AnimationTree.Set("parameters/MainStates/conditions/isGrounded",true);
            AnimationTree.Set("parameters/MainStates/conditions/isFalling",false);
            
            //if((bool)AnimationTree.Get("parameters/MainStates/conditions/isFalling") == true)
            //{
            //    AnimationTree.Set("parameters/MainStates/conditions/isFalling",false);
            //    stateMachine.Travel("fall_land",false);
            //}
        }
        else
        {            
            AnimationTree.Set("parameters/MainStates/conditions/isFalling",true);
            AnimationTree.Set("parameters/MainStates/conditions/isGrounded",false);
            //if((bool)AnimationTree.Get("parameters/MainStates/conditions/isJumping") == false) stateMachine.Travel("fall_idle1",false);
        }
    }
}
