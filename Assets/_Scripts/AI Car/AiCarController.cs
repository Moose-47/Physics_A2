using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class AiCarController : MonoBehaviour
{
    // ----- MOVEMENT SETTINGS -----
    [Header("Movement Settings")]
    public float acceleration = 8f;               //How fast the car speeds up when it's below its target speed
    public float deceleration = 6f;               //How fast the car slows down when it's above its target speed
    public float baseMaxSpeed = 33f;              //The "normal" top speed of the car
    public float maxTurnAngle = 90f;              //The maximum angle the car can turn at once
    public float turnSpeed = 180f;                //How quickly the car rotates toward the target
    public float speedDependentTurnFactor = 0.5f; //How much the car's turning is limited when going fast

    // ----- TRACK SETTINGS -----
    [Header("Track Settings")]
    public RacingLine racingLine;                 //The object in the scene that contains waypoints for the car to follow
    public Transform[] waypoints;                 //The actual points the car will aim for
    public int currentIndex = 0;                  //The index of the current waypoint the car is aiming for

    // ----- OFF-TRACK SETTINGS -----
    [Header("Off-Track Settings")]
    public LayerMask offTrackLayer;              //Defines what layers count as "off the track"
    public float offTrackSlowFactor = 0.9f;      //How much the car slows down if it's off track

    // ----- WAYPOINT SETTINGS -----
    [Header("Waypoint Settings")]
    public float waypointThreshold = 4f;         //How close the car has to get to a waypoint before moving to the next one
    public int lookaheadCount = 5;               //How many waypoints the car looks ahead to plan its path
    public float baseLookaheadDistance = 5f;     //Minimum distance ahead the car looks for its target

    // ----- INTERNAL COMPONENTS -----
    private Rigidbody2D rb;                       //Physics component that moves the car
    private SpriteRenderer sr;                    //Component that draws the car sprite
    private ColliderResizer cr;                   //Optional helper for resizing collider to match the sprite

    // ----- INTERNAL AI VARIABLES -----
    private Queue<Vector2> targetQueue = new Queue<Vector2>(); //Stores the next few target points the car will aim for
    private Vector2 currentTargetPoint;                            //The exact point the car is currently steering toward
    private float targetSpeed = 0f;                                //How fast the car wants to go right now
    private float lapStartTime = 0f;                               //Records when the current lap started

    public bool canMove = false;  //Flag to turn the AI on/off (used to start the race)

    public int CurrentLap = -1;   //Tracks which lap the car is on
    [HideInInspector] public int CurrentForwardSpeed => (int)Vector2.Dot(rb.linearVelocity, transform.up);
    //Projects the velocity along the car's forward direction to get "speed in the direction it’s facing"

    [HideInInspector] public List<float> lapTimes = new List<float>();
    //Stores all completed lap times so we can calculate averages or compare laps

    // ----- INITIAL SETUP -----
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();   //Grab the Rigidbody2D component to control physics
        sr = GetComponent<SpriteRenderer>(); //Grab the SpriteRenderer so we can change the car sprite
        cr = GetComponent<ColliderResizer>(); //Optional, if we want the collider to match sprite size
        baseMaxSpeed += Random.Range(-5f, 5f); //Add a small random variation to each AI car so they're not all identical
    }

    //Setup the sprite and reset collider if needed
    public void Initialize(Sprite chosenSprite)
    {
        if (chosenSprite == null) return;
        sr.sprite = chosenSprite;  //Assign the sprite visually
        cr?.ResetCollider();       //Resize the collider to match the new sprite
    }

    private void Start()
    {
        //Find the racing line automatically if not assigned
        racingLine = FindAnyObjectByType<RacingLine>();
        if (racingLine != null)
            waypoints = racingLine.waypoints;

        //Fill the target queue with the first few points to start moving
        FillTargetQueue();
    }

    // ----- AI MOVEMENT -----
    private void FixedUpdate()
    {
        //Only move if allowed and there are enough waypoints
        if (!canMove || waypoints == null || waypoints.Length < 2) return;

        // --- LOOKAHEAD POINT ---
        //Determine how far ahead the car should look
        float dynamicLookahead = Mathf.Max(baseLookaheadDistance, CurrentForwardSpeed * 0.5f);
        currentTargetPoint = GetLookaheadPoint(dynamicLookahead); // Calculate exact target point

        // --- STEERING ---
        //Vector pointing from car to target
        Vector2 toTarget = (currentTargetPoint - (Vector2)transform.position).normalized;

        //Angle difference between car's current forward and the target
        float angleToTarget = Vector2.SignedAngle(transform.up, toTarget);

        //Maximum safe speed to make this turn
        float maxTurnSpeed = baseMaxSpeed * (maxTurnAngle / Mathf.Max(Mathf.Abs(angleToTarget), 1f));
        targetSpeed = Mathf.Min(baseMaxSpeed, maxTurnSpeed); //Don't go faster than base max

        // --- SPEED CONTROL ---
        float currentSpeed = Vector2.Dot(rb.linearVelocity, transform.up); //How fast car is moving forward
        float accel = (currentSpeed < targetSpeed) ? acceleration : deceleration; //Decide whether to speed up or slow down
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime); // Smooth change
        rb.linearVelocity = transform.up * newSpeed; //Apply forward speed

        // --- TURNING ---
        float steerInput = Mathf.Clamp(angleToTarget / maxTurnAngle, -1f, 1f); //Convert angle to a -1..1 input
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / baseMaxSpeed); //Factor for slower turning at higher speeds
        float rotationAmount = steerInput * maxTurnAngle * (1f - speedDependentTurnFactor * speedFactor)
                               * Time.fixedDeltaTime * turnSpeed / 90f; //Calculate rotation for this frame
        rb.MoveRotation(rb.rotation + rotationAmount); //Rotate the car

        // --- OFF-TRACK SLOWDOWN ---
        if (IsOffTrack())
            rb.linearVelocity *= offTrackSlowFactor; //Reduce speed if off track

        // --- WAYPOINT PROGRESSION ---
        if (Vector2.Distance(transform.position, targetQueue.Peek()) < waypointThreshold)
        {
            AdvanceToNextTarget(); //Go to the next point
        }
    }

    //Fill the queue with the first lookaheadCount points
    private void FillTargetQueue()
    {
        targetQueue.Clear();
        for (int i = 0; i < lookaheadCount; i++)
        {
            int idx = (currentIndex + i) % waypoints.Length;
            targetQueue.Enqueue(GetPointFromWaypoint(waypoints[idx]));
        }
        currentTargetPoint = targetQueue.Peek();
    }

    //Move the queue forward when reaching a waypoint
    private void AdvanceToNextTarget()
    {
        if (targetQueue.Count > 0)
            targetQueue.Dequeue();

        currentIndex = (currentIndex + 1) % waypoints.Length;
        int nextIdx = (currentIndex + lookaheadCount - 1) % waypoints.Length;
        targetQueue.Enqueue(GetPointFromWaypoint(waypoints[nextIdx]));
    }

    //Get a random point inside the waypoint box
    private Vector2 GetPointFromWaypoint(Transform wp)
    {
        BoxCollider2D box = wp.GetComponentInChildren<BoxCollider2D>(true);
        if (box != null)
        {
            Vector2 worldPos = box.bounds.center;
            Vector2 halfSize = box.bounds.extents;
            return new Vector2(
                Random.Range(worldPos.x - halfSize.x, worldPos.x + halfSize.x),
                Random.Range(worldPos.y - halfSize.y, worldPos.y + halfSize.y)
            );
        }
        return wp.position; //Fallback: just use the transform
    }

    //Calculate the lookahead point along the queued waypoints
    private Vector2 GetLookaheadPoint(float lookaheadDistance)
    {
        Vector2 prev = (Vector2)transform.position;
        foreach (var pt in targetQueue)
        {
            float segDist = Vector2.Distance(prev, pt); //Distance to next point
            if (lookaheadDistance <= segDist)
                return Vector2.Lerp(prev, pt, lookaheadDistance / segDist); //Interpolate if within this segment
            lookaheadDistance -= segDist;
            prev = pt;
        }
        return prev; //Return last point if distance exceeds total
    }

    //Check if the car is off-track using Physics2D
    private bool IsOffTrack()
    {
        return Physics2D.OverlapPoint(transform.position, offTrackLayer);
    }

    //----- LAP TIMING -----
    public void StartLap(float currentTime)
    {
        lapStartTime = currentTime; //Save start time
    }

    public void EndLap(float currentTime)
    {
        float lapTime = currentTime - lapStartTime;
        lapTimes.Add(lapTime); //Save completed lap time
        lapStartTime = currentTime; //Start new lap immediately
    }

    public float AverageLapTime()
    {
        if (lapTimes.Count == 0) return 0f;
        float sum = 0f;
        foreach (var t in lapTimes) sum += t;
        return sum / lapTimes.Count;
    }

    // ----- DEBUG DRAWING -----
    private void OnDrawGizmosSelected()
    {
        if (targetQueue == null || targetQueue.Count == 0) return;

        //Draw lines between queued waypoints in yellow
        Gizmos.color = Color.yellow;
        Vector3 prev = transform.position;
        foreach (var pt in targetQueue)
        {
            Gizmos.DrawLine(prev, pt);
            Gizmos.DrawSphere(pt, 4f); //Draw small spheres at each point
            prev = pt;
        }

        //Draw the current lookahead point in magenta
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(currentTargetPoint, 5f);
    }
}
