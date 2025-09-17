using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 15f;          //How quickly the car speeds up when accelerating
    public float deceleration = 8f;           //How quickly the car naturally slows down when not accelerating/braking
    public float brakeDeceleration = 20f;     //How quickly the car slows down when the brake is held
    public float maxForwardSpeed = 12f;       //Maximum speed moving forward
    public float maxReverseSpeed = 6f;        //Maximum speed moving backward (slower than forward)
    public float maxTurnAngle = 30f;          //Maximum angle the front wheels can turn
    public float turnSpeed = 180f;            //Base rotation speed (degrees per second)
    public float speedDependentTurnFactor = 0.5f; //Reduces turn sharpness at high speeds

    [Header("Off-Track Settings")]
    public LayerMask offTrackLayer;            //Layer representing areas off the track
    public float offTrackSlowFactor = 0.5f;    //Slows AI when off-track

    [Header("Input Actions")]
    public InputActionAsset inputActions;     //The Input Actions asset from Unity's new Input System

    [Header("Sound Effects")]
    public AudioClip accel;
    public AudioClip decel;
    public AudioClip idle;

    // --- Input actions ---
    private InputAction moveAction;           //Controls steering: x-axis for left/right
    private InputAction accelerateAction;     //Button or trigger for accelerating forward
    private InputAction brakeAction;          //Button or trigger for braking/reversing

    // --- Runtime input variables ---
    private float steerInput;                 //Value between -1 (full left) and +1 (full right)
    private bool accelerating;                //True if the player is holding the accelerate input
    private bool braking;                     //True if the player is holding the brake input

    private Rigidbody2D rb;                   //Reference to the Rigidbody2D for physics
    private SpriteRenderer sr;                //Reference to SpriteRenderer for setting selected sprite
    private ColliderResizer cr;               //Reference to the ColliderResizer.cs for resizing collider on spawn
    private AudioSource audioSource;

    public bool canMove = false;             //Prevent player from moving while the countdown is happening
    private bool playingAudio = false;
    private void Awake()
    {
        //Cache the Rigidbody2D for physics calculations
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        cr = GetComponent<ColliderResizer>();
        audioSource = GetComponent<AudioSource>();
        
        //Find the Player action map in the Input Actions asset
        var playerMap = inputActions.FindActionMap("Player");

        //Assign actions from the map
        moveAction = playerMap.FindAction("Move");           //Steering input
        accelerateAction = playerMap.FindAction("Accelerate"); //Accelerate input
        brakeAction = playerMap.FindAction("Brake");        //Brake input
    }
    public void Initialize(Sprite chosenSprite)
    {
        if (chosenSprite == null) return;

        sr.sprite = chosenSprite;

        if (cr != null)
            cr.ResetCollider();
    }
    private void OnEnable()
    {
        //Enable all actions so they start listening to input
        moveAction.Enable();
        accelerateAction.Enable();
        brakeAction.Enable();

        //Subscribe to the move action performed event
        moveAction.performed += ctx => steerInput = ctx.ReadValue<Vector2>().x;
        //When move is released, reset steer input to 0
        moveAction.canceled += ctx => steerInput = 0f;

        //Subscribe to acceleration input
        accelerateAction.performed += ctx => accelerating = true;
        accelerateAction.canceled += ctx =>
        {
            accelerating = false;
            playingAudio = false;
            audioSource.Stop();
        };

        //Subscribe to brake input
        brakeAction.performed += ctx => braking = true;
        brakeAction.canceled += ctx =>
        {
            braking = false;
            playingAudio = false;
            audioSource.Stop();
            audioSource.clip = idle;
            audioSource.Play();
        };
    }

    private void OnDisable()
    {
        //Unsubscribe from input events to avoid memory leaks
        moveAction.performed -= ctx => steerInput = ctx.ReadValue<Vector2>().x;
        moveAction.canceled -= ctx => steerInput = 0f;

        accelerateAction.performed -= ctx => accelerating = true;
        accelerateAction.canceled -= ctx => 
        { 
            accelerating = false; 
            playingAudio = false; 
            audioSource.Stop();
        };

        brakeAction.performed -= ctx => braking = true;
        brakeAction.canceled -= ctx => 
        { 
            braking = false; 
            playingAudio = false; 
            audioSource.Stop();
            audioSource.clip = idle;
            audioSource.Play();
        };

        //Disable all actions
        moveAction.Disable();
        accelerateAction.Disable();
        brakeAction.Disable();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

        // --- Determine current forward speed ---
        // Vector2.Dot(rb.velocity, transform.up) calculates how much of the velocity is in the direction the car is facing
        // transform.up is the local "forward" of the car
        float currentSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

        //Start with the current speed; will adjust based on input
        float targetSpeed = currentSpeed;

        // --- Handle acceleration/braking/reverse ---
        if (accelerating)
        {
            if (!playingAudio)
            {
                playingAudio = true;
                audioSource.clip = accel;
                audioSource.Play();
            }
            //If accelerating, increase speed by acceleration * deltaTime
            targetSpeed += acceleration * Time.fixedDeltaTime;

            //Clamp the speed so it doesn't exceed max forward speed
            targetSpeed = Mathf.Min(targetSpeed, maxForwardSpeed);
        }
        else if (braking)
        {
            if (!playingAudio)
            {
                playingAudio = true;
                audioSource.clip = decel;
                audioSource.Play();
            }
            if (currentSpeed < 0f && audioSource.clip == decel)
            {
                audioSource.Stop();
                audioSource.clip = accel;
                audioSource.Play();
            }
            //If braking, reduce speed by brakeDeceleration * deltaTime
            targetSpeed -= brakeDeceleration * Time.fixedDeltaTime;

            //Clamp speed so it doesn't go past max reverse speed
            targetSpeed = Mathf.Max(targetSpeed, -maxReverseSpeed);
        }
        else
        {
            //Natural deceleration: slow down gradually if neither accelerating nor braking
            if (currentSpeed > 0f)
            {
                if (audioSource.clip != idle)
                {
                    audioSource.clip = idle;
                    audioSource.Play();
                }
                //Moving forward: subtract deceleration to slow down
                targetSpeed -= deceleration * Time.fixedDeltaTime;

                //Clamp to prevent going below 0
                targetSpeed = Mathf.Max(targetSpeed, 0f);
            }
            else if (currentSpeed < 0f)
            {
                //Moving backward: add deceleration (pushes towards 0)
                targetSpeed += deceleration * Time.fixedDeltaTime;

                //Clamp to prevent exceeding 0
                targetSpeed = Mathf.Min(targetSpeed, 0f);
            }
        }

        //--- Apply velocity ---
        //rb.velocity = direction the car is facing (transform.up) multiplied by target speed
        rb.linearVelocity = transform.up * targetSpeed;

        //---Off-Track slowdown---
        if (IsOffTrack())
            rb.linearVelocity *= offTrackSlowFactor;

        //--- Steering ---
        //Only allow turning if the car is moving (speed is not zero)
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            //Calculate speed factor: higher speed means wider turns
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxForwardSpeed);

            //Determine how much to rotate the car this frame
            //steerInput (-1 to +1) * maxTurnAngle gives desired rotation
            //Multiply by (1 - speedDependentTurnFactor * speedFactor) to reduce turning at high speed
            //Multiply by deltaTime for frame independence
            float rotationAmount = steerInput * maxTurnAngle * (1f - speedDependentTurnFactor * speedFactor) * Time.fixedDeltaTime;

            //Rotate the car
            rb.MoveRotation(rb.rotation - rotationAmount);
        }
    }

    private bool IsOffTrack()
    {
        return Physics2D.OverlapPoint(transform.position, offTrackLayer);
    }
}
