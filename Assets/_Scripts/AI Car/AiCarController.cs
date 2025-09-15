using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class AiCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 8f;            //How quickly the AI car speeds up per second
    public float deceleration = 6f;            //How quickly the AI slows down naturally
    public float baseMaxSpeed = 12f;           //Maximum speed the AI can reach
    public float lookAheadDistance = 5f;       //Distance along the racing line AI looks ahead to steer
    public float laneOffset = 0f;              //Sideways variation for realism

    [Header("Track Settings")]
    public RacingLine racingLine;              //Reference to racing line object containing all waypoints
    public Transform[] waypoints;              //Array of points forming the track path
    public int currentIndex = 0;               //Current waypoint AI is heading toward

    [Header("Off-Track Settings")]
    public LayerMask offTrackLayer;            //Layer representing areas off the track
    public float offTrackSlowFactor = 0.5f;    //Slows AI when off-track

    [Header("Avoidance Settings")]
    public float avoidanceRayDistance = 2f;    //Distance AI looks forward to detect other cars
    public float avoidanceStrength = 1.5f;     //Amount AI steers to avoid collision
    public LayerMask playerLayer;              //Layer representing player cars

    [Header("Recovery Settings")]
    public float wrongWayAngle = 100f;         //Angle beyond which AI is considered going the wrong way
    public float stuckSpeedThreshold = 1f;     //Below this speed, AI is considered stuck
    public float stuckTimeLimit = 2f;          //Time AI must remain slow to trigger recovery
    public float recoveryTurnSpeed = 150f;     //Rotation speed during recovery
    public float recoveryTeleportDistance = 10f; //Distance beyond which AI teleports back to track

    private Rigidbody2D rb;            //Reference to Rigidbody2D component
    private SpriteRenderer sr;         //Reference to SpriteRenderer for setting selected sprite
    private ColliderResizer cr;        //Reference to the ColliderResizer.cs for resizing collider on spawn

    private float targetSpeed = 0f;    //Speed AI wants to reach
    private bool recovering = false;   //Whether AI is currently recovering
    private float stuckTimer = 0f;     //Timer to track how long AI has been stuck

    [HideInInspector] public int CurrentLap = 0;

    [HideInInspector] public float CurrentForwardSpeed => Vector2.Dot(rb.linearVelocity, transform.up);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        cr = GetComponent<ColliderResizer>();
        racingLine = FindAnyObjectByType<RacingLine>();

        //If a RacingLine exists, copy its waypoints
        if (racingLine != null)
            waypoints = racingLine.waypoints;

        //Randomize small differences per AI so not all cars behave identically
        laneOffset += Random.Range(-0.5f, 0.5f);     //Shift left/right slightly
        lookAheadDistance += Random.Range(-1f, 1f);  //Slightly closer/further look-ahead
        baseMaxSpeed += Random.Range(-2f, 2f);       //Slightly faster/slower car
    }

    public void Initialize(Sprite chosenSprite)
    {
        if (chosenSprite == null) return;

        sr.sprite = chosenSprite;

        if (cr != null)
            cr.ResetCollider();
    }

    private void FixedUpdate()
    {
        //Ensure we have enough waypoints to drive
        if (waypoints == null || waypoints.Length < 2) return;

        //--- Recovery system ---
        //If AI is stuck or facing wrong way, fix it first and skip normal driving
        if (CheckRecovery()) return;

        //--- Determine target point to aim at ---
        Vector2 target = GetLookAheadTarget();

        //--- STEERING ---
        //Direction vector from AI's current position to target
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        //normalized = vector with length 1 pointing in same direction

        //Determine how fast AI is moving in its forward direction
        //Vector2.Dot(rb.velocity, transform.up) projects velocity onto the car's facing direction
        float currentForwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

        //Only steer if moving to avoid unrealistic pivoting in place
        if (Mathf.Abs(currentForwardSpeed) > 0.01f)
        {
            //Calculate signed angle between car's forward direction (transform.up) and the direction to target
            //Vector2.SignedAngle returns angle in degrees (-180 to 180)
            //Negative = target is to the left, Positive = target is to the right
            float angleToTarget = Vector2.SignedAngle(transform.up, dir);

            //Convert angle to a -1 to 1 range for steering input
            float steerInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

            //Adjust steering slightly if a player is detected ahead to avoid collisions
            steerInput += CheckAvoidance();

            //Reduce rotation sharpness at higher speed for realistic turning
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentForwardSpeed) / baseMaxSpeed);
            //Mathf.Clamp01 ensures value stays between 0 and 1
            float rotationAmount = steerInput * 200f * (1f - 0.5f * speedFactor) * Time.fixedDeltaTime;

            //Rigidbody2D.MoveRotation rotates the physics object smoothly
            //Subtract rotationAmount because Unity rotates clockwise with negative angles
            rb.MoveRotation(rb.rotation - rotationAmount);
        }

        //--- SPEED CONTROL ---
        AdjustSpeedForCorners(target); //Adjust target speed based on corner sharpness

        //Smoothly accelerate or decelerate toward target speed
        float currentSpeed = Vector2.Dot(rb.linearVelocity, transform.up); //forward velocity
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        //Mathf.MoveTowards gradually changes currentSpeed toward targetSpeed by max change per frame

        rb.linearVelocity = transform.up * newSpeed; //Apply velocity in forward direction

        //If off-track, slow down
        if (IsOffTrack())
            rb.linearVelocity *= offTrackSlowFactor;
    }

    //--- Returns a point ahead along the racing line for AI to aim at ---
    private Vector2 GetLookAheadTarget()
    {
        float dist = 0f;
        int i = currentIndex;

        //Step along waypoints until reaching look-ahead distance
        while (dist < lookAheadDistance)
        {
            int next = (i + 1) % waypoints.Length; //wrap around
            dist += Vector2.Distance(waypoints[i].position, waypoints[next].position);
            //Vector2.Distance calculates straight-line distance between two points
            i = next;
        }

        currentIndex = i;

        Vector2 target = waypoints[i].position;

        //--- Apply lane offset ---
        int prev = (i - 1 + waypoints.Length) % waypoints.Length;
        Vector2 forward = (waypoints[i].position - waypoints[prev].position).normalized;
        //Sideways direction = perpendicular to forward
        Vector2 side = new Vector2(-forward.y, forward.x);
        target += side * laneOffset; //offset target sideways for variation

        return target;
    }

    //--- Adjust speed depending on upcoming corner angle ---
    private void AdjustSpeedForCorners(Vector2 target)
    {
        int nextIndex = (currentIndex + 1) % waypoints.Length;
        Vector2 nextTarget = waypoints[nextIndex].position;

        Vector2 toCurrent = (target - (Vector2)transform.position).normalized;
        Vector2 toNext = (nextTarget - target).normalized;

        //Angle between current direction and next direction
        float angle = Vector2.Angle(toCurrent, toNext);
        //Vector2.Angle returns unsigned angle in degrees (0 to 180)

        float maxSpeed = baseMaxSpeed;

        if (angle > 45f)
            maxSpeed *= 0.6f; //slow down sharply in tight corners
        else if (angle > 20f)
            maxSpeed *= 0.8f; //moderate slowdown for medium corners

        targetSpeed = maxSpeed; //AI will smoothly accelerate/decelerate toward this
    }

    //--- Returns true if AI is off the track ---
    private bool IsOffTrack()
    {
        //Physics2D.OverlapPoint checks if a point overlaps any colliders on the given layer
        return Physics2D.OverlapPoint(transform.position, offTrackLayer);
    }

    //--- Returns steering adjustment if a player car is ahead ---
    private float CheckAvoidance()
    {
        //Cast a ray forward to detect players
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, avoidanceRayDistance, playerLayer);
        if (hit.collider != null)
        {
            // Randomly choose left or right to avoid
            bool goRight = Random.value > 0.5f;
            return goRight ? avoidanceStrength : -avoidanceStrength;
        }

        return 0f;
    }

    //--- Recovery system: correct wrong way or stuck AI ---
    private bool CheckRecovery()
    {
        Vector2 toTarget = (GetLookAheadTarget() - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(transform.up, toTarget); //How far AI is from correct direction

        if (angle > wrongWayAngle)
            recovering = true;

        if (rb.linearVelocity.magnitude < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckTimeLimit)
                recovering = true;
        }
        else
            stuckTimer = 0f;

        if (recovering)
        {
            //Rotate toward target direction
            float steerInput = Mathf.Sign(Vector2.SignedAngle(transform.up, toTarget));
            //Mathf.Sign returns -1 for negative, +1 for positive, 0 for zero
            rb.MoveRotation(rb.rotation - steerInput * recoveryTurnSpeed * Time.fixedDeltaTime);

            //Push forward slightly to help unstuck
            rb.linearVelocity = transform.up * (baseMaxSpeed * 0.5f);

            if (angle < 30f)
            {
                recovering = false;
                stuckTimer = 0f;
            }

            if (Vector2.Distance(transform.position, waypoints[currentIndex].position) > recoveryTeleportDistance)
            {
                //Teleport back to track if extremely off
                transform.position = waypoints[currentIndex].position;
                rb.linearVelocity = Vector2.zero;
                recovering = false;
                stuckTimer = 0f;
            }

            return true; //Skip normal driving this frame
        }

        return false;
    }

    //--- Draw debug line in editor to visualize avoidance ray ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * avoidanceRayDistance);
    }
}
