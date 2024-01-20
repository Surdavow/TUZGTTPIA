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
    public float runForce = 0.25f;        
    [Export]
    public float mouseSensitivity = 1;
    [Export]
    public float airControl = 0.8f;    
    private const float walkSpeed = 2;
    private const float runSpeed = 4;
    private const float lerpSpeed = 0.15f;
    private float currentSpeed;
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private bool isRunning = false;
    private bool isLocked = false;
    private bool isGrounded = true;
    private Vector3 previousVelocity = new Vector3();
    private Node3D cameraThirdPerson;
    private Node3D Character;
    private AnimationTree AnimationTree;
    private Skeleton3D CharacterSkeleton;
    private AudioStreamPlayer3D soundPlayerFootsteps;
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
    private AudioStream[] jumpSounds = new AudioStream[]
    {
        (AudioStream)ResourceLoader.Load("res://assets/audio/foley/jump1.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/foley/jump2.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/foley/jump3.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/foley/jump4.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/foley/jump5.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/foley/jump6.wav"),
    };
    private AudioStream[] footSteplandSounds = new AudioStream[]
    {
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/land_rubber_01.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/land_rubber_02.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/land_rubber_03.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/land_rubber_04.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/land_rubber_05.wav"),
        (AudioStream)ResourceLoader.Load("res://assets/audio/footsteps/land_rubber_06.wav"),
    };    
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;//Hide and lock the mouse
		cameraThirdPerson = GetNode<Node3D>("CameraMountThirdPerson");  
		Character = GetNode<Node3D>("character_default");
		soundPlayerFootsteps = GetNode<AudioStreamPlayer3D>("SoundPlayerFootsteps3D");
		soundPlayer = GetNode<AudioStreamPlayer3D>("SoundPlayer3D");
		AnimationTree = GetNode<AnimationTree>("character_default/AnimationTree");
		CharacterSkeleton = GetNode<Skeleton3D>("character_default/default_rig/Skeleton3D");
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

    private void jumpFunction()
    {
        if(!IsOnFloor()) return;

        int randomjumpsoundIndex = (int)GD.RandRange(0, jumpSounds.Length-1);
        soundPlayer.Stream = jumpSounds[randomjumpsoundIndex];            
        soundPlayer.PitchScale = (float)GD.RandRange(1,2);
        soundPlayer.UnitSize = 0.75f;
        soundPlayer.Play();
        Vector3 velocity = Velocity;
        velocity.Y = Mathf.Lerp(velocity.Y,jumpForce,0.8f);
        Velocity = velocity;
        MoveAndSlide();
    }

    private void playFootsteps(string type)
    {
        switch(type)
        {
            case "footstep":    float currentSpeed = Math.Abs(-Transform.Basis.Z.Dot(Velocity));
                                if (IsOnFloor() && currentSpeed >= (walkSpeed / 4)) 
                                {
                                    int randomfootstepIndex = (int)GD.RandRange(0, footStepSounds.Length-1);
                                    soundPlayerFootsteps.Stream = footStepSounds[randomfootstepIndex];            
                                    soundPlayerFootsteps.PitchScale = (float)GD.RandRange(0.6f*currentSpeed,1f*currentSpeed);
                                    soundPlayerFootsteps.UnitSize = 0.75f + currentSpeed/4;                            
                                    soundPlayerFootsteps.Play();
                                }
                                break;
            case "land":    int randomlandIndex = (int)GD.RandRange(0, footSteplandSounds.Length-1);
                            soundPlayerFootsteps.Stream = footSteplandSounds[randomlandIndex];            
                            soundPlayerFootsteps.PitchScale = (float)GD.RandRange(0.75f,1.5f);
                            soundPlayerFootsteps.UnitSize = 1.5f;
                            soundPlayerFootsteps.Play();
                            break;

        }            
    }

    private void animatePlayerStates()
    {        
        //Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        //Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        //float targetAngle = 0;
        //if(isRunning) targetAngle = (float)Math.Atan2(direction.X, direction.Z);
        //float clampedTargetAngle = Mathf.Clamp(targetAngle, -0.25f, 0.25f);
        //Character.Rotation = new Vector3(Character.Rotation.X, Mathf.LerpAngle(Character.Rotation.Y, clampedTargetAngle, 0.05f), Character.Rotation.Z);

        float LongSpeed = -Transform.Basis.Z.Dot(Velocity);
        float LatSpeed = Transform.Basis.X.Dot(Velocity);
        float LatLongSpeed = (float)Math.Sqrt(Math.Pow(LongSpeed, 2) + Math.Pow(LatSpeed, 2));
        bool isGrounded = IsOnFloor();
        Quaternion headBoneQuaternion = CharacterSkeleton.GetBonePoseRotation(5);
        Quaternion neckBoneQuaternion = CharacterSkeleton.GetBonePoseRotation(4);
        Quaternion spine2BoneQuaternion = CharacterSkeleton.GetBonePoseRotation(3);
        CharacterSkeleton.SetBonePoseRotation(5, new Quaternion(-cameraThirdPerson.Rotation.X*0.25f, headBoneQuaternion.Y, headBoneQuaternion.Z, headBoneQuaternion.W));        
        CharacterSkeleton.SetBonePoseRotation(4, new Quaternion(-cameraThirdPerson.Rotation.X*0.1f, neckBoneQuaternion.Y, neckBoneQuaternion.Z, neckBoneQuaternion.W));
        //CharacterSkeleton.SetBonePoseRotation(3, new Quaternion(-cameraThirdPerson.Rotation.X*0.1f, neckBoneQuaternion.Y, neckBoneQuaternion.Z, neckBoneQuaternion.W));

        //Locomotion blending
        Vector2 LocomotionBlend = AnimationTree.Get("parameters/MainStates/Locomotion/blend_position").AsVector2();
        AnimationTree.Set("parameters/MainStates/Locomotion/blend_position", new Vector2(Math.Clamp(Mathf.Lerp(LocomotionBlend.X,LatSpeed / runSpeed,0.75f),-1,1),Math.Clamp(Mathf.Lerp(LocomotionBlend.Y,LongSpeed / runSpeed, 0.75f),-1,1)));        
        //Locomotion timescale
        float LocomotionSpeedScale = (float)AnimationTree.Get("parameters/TimeScale/scale");
        if (isGrounded) AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocomotionSpeedScale,Math.Clamp(LatLongSpeed / runSpeed, 1, 1000),lerpSpeed * runForce));
        else AnimationTree.Set("parameters/TimeScale/scale",Mathf.Lerp(LocomotionSpeedScale,1,1.25f));        

        AnimationNodeStateMachinePlayback stateMachine = (AnimationNodeStateMachinePlayback)AnimationTree.Get("parameters/MainStates/playback");
        if(!isGrounded) stateMachine.Travel("fall_idle1",false);
    }

    private void OnImpact(Vector3 previousVelocity)
    {        
        if (previousVelocity == Vector3.Zero) return;
    }    

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        bool isGrounded = IsOnFloor();
        previousVelocity = velocity;
        Vector2 inputDir = Input.GetVector("move left", "move right", "move forward", "move backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        AnimationTree.Set("parameters/MainStates/conditions/isGrounded",isGrounded);   

        if (Input.IsActionJustPressed("jump") && isGrounded && canJump)
        AnimationTree.Set("parameters/MainStates/conditions/isJumping",true);

        switch (Input.IsActionPressed("sprint") && Input.IsActionPressed("move forward") && isGrounded)        
        {
            case true:  currentSpeed = runSpeed;                        
                        isRunning = true;                                                
                        break;
            case false: currentSpeed = walkSpeed;
                        isRunning = false;                        
                        break;
        }

        if (direction != Vector3.Zero)
        {
            direction.Z *= currentSpeed;
            direction.X *= currentSpeed;

            if(!isGrounded)
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

        if (!isGrounded) velocity.Y -= gravity * (float)delta*(Mass/gravity);
        velocity.X = Mathf.Lerp(velocity.X,direction.X,lerpSpeed * runForce);
        velocity.Z = Mathf.Lerp(velocity.Z,direction.Z,lerpSpeed * runForce);

        Velocity = velocity;        
        MoveAndSlide();
        animatePlayerStates();        

        if (!isGrounded && IsOnFloor()) OnImpact(previousVelocity);
    }

}