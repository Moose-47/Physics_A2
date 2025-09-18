using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class AiCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 8f;
    public float deceleration = 6f;
    public float baseMaxSpeed = 33f;
    public float maxTurnAngle = 90f;
    public float turnSpeed = 180f;
    public float speedDependentTurnFactor = 0.5f;

    [Header("Track Settings")]
    public RacingLine racingLine;
    public Transform[] waypoints;
    public int currentIndex = 0;

    [Header("Off-Track Settings")]
    public LayerMask offTrackLayer;
    public float offTrackSlowFactor = 0.9f;

    [Header("Waypoint Settings")]
    public float waypointThreshold = 4f;
    public int lookaheadCount = 5;
    public float baseLookaheadDistance = 5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private ColliderResizer cr;

    private Queue<Vector2> targetQueue = new Queue<Vector2>();
    private Vector2 currentTargetPoint;
    private float targetSpeed = 0f;
    private float lapStartTime = 0f;

    public bool canMove = false;

    public int CurrentLap = -1;
    [HideInInspector] public int CurrentForwardSpeed => (int)Vector2.Dot(rb.linearVelocity, transform.up);

    [HideInInspector] public List<float> lapTimes = new List<float>(); // store completed lap times

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        cr = GetComponent<ColliderResizer>();
        baseMaxSpeed += Random.Range(-5f, 5f);
    }

    public void Initialize(Sprite chosenSprite)
    {
        if (chosenSprite == null) return;
        sr.sprite = chosenSprite;
        cr?.ResetCollider();
    }

    private void Start()
    {
        racingLine = FindAnyObjectByType<RacingLine>();
        if (racingLine != null)
            waypoints = racingLine.waypoints;

        FillTargetQueue();
    }

    private void FixedUpdate()
    {
        if (!canMove || waypoints == null || waypoints.Length < 2) return;

        //Calculate dynamic lookahead point 
        float dynamicLookahead = Mathf.Max(baseLookaheadDistance, CurrentForwardSpeed * 0.5f);
        currentTargetPoint = GetLookaheadPoint(dynamicLookahead);

        //Calculate angle to target 
        Vector2 toTarget = (currentTargetPoint - (Vector2)transform.position).normalized;
        float angleToTarget = Vector2.SignedAngle(transform.up, toTarget);

        //Calculate maximum speed to safely turn
        float maxTurnSpeed = baseMaxSpeed * (maxTurnAngle / Mathf.Max(Mathf.Abs(angleToTarget), 1f));
        targetSpeed = Mathf.Min(baseMaxSpeed, maxTurnSpeed);

        //Apply speed smoothly
        float currentSpeed = Vector2.Dot(rb.linearVelocity, transform.up);
        float accel = (currentSpeed < targetSpeed) ? acceleration : deceleration;
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = transform.up * newSpeed;

        //Apply player-style turning
        float steerInput = Mathf.Clamp(angleToTarget / maxTurnAngle, -1f, 1f);
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / baseMaxSpeed);
        float rotationAmount = steerInput * maxTurnAngle * (1f - speedDependentTurnFactor * speedFactor) * Time.fixedDeltaTime * turnSpeed / 90f;
        rb.MoveRotation(rb.rotation + rotationAmount);

        //Off-track slowdown
        if (IsOffTrack())
            rb.linearVelocity *= offTrackSlowFactor;

        //Waypoint progression
        if (Vector2.Distance(transform.position, targetQueue.Peek()) < waypointThreshold)
        {
            AdvanceToNextTarget();
        }
    }

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

    private void AdvanceToNextTarget()
    {
        if (targetQueue.Count > 0)
            targetQueue.Dequeue();

        currentIndex = (currentIndex + 1) % waypoints.Length;
        int nextIdx = (currentIndex + lookaheadCount - 1) % waypoints.Length;
        targetQueue.Enqueue(GetPointFromWaypoint(waypoints[nextIdx]));
    }

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
        return wp.position;
    }

    private Vector2 GetLookaheadPoint(float lookaheadDistance)
    {
        Vector2 prev = (Vector2)transform.position;
        foreach (var pt in targetQueue)
        {
            float segDist = Vector2.Distance(prev, pt);
            if (lookaheadDistance <= segDist)
                return Vector2.Lerp(prev, pt, lookaheadDistance / segDist);
            lookaheadDistance -= segDist;
            prev = pt;
        }
        return prev;
    }

    private bool IsOffTrack()
    {
        return Physics2D.OverlapPoint(transform.position, offTrackLayer);
    }

    // Call this at the start of the race
    public void StartLap(float currentTime)
    {
        lapStartTime = currentTime;
    }

    // Call this when completing a lap
    public void EndLap(float currentTime)
    {
        float lapTime = currentTime - lapStartTime;
        lapTimes.Add(lapTime);
        lapStartTime = currentTime; // start new lap
    }

    // Compute average lap time
    public float AverageLapTime()
    {
        if (lapTimes.Count == 0) return 0f;
        float sum = 0f;
        foreach (var t in lapTimes) sum += t;
        return sum / lapTimes.Count;
    }

    private void OnDrawGizmosSelected()
    {
        if (targetQueue == null || targetQueue.Count == 0) return;

        // Draw waypoints
        Gizmos.color = Color.yellow;
        Vector3 prev = transform.position;
        foreach (var pt in targetQueue)
        {
            Gizmos.DrawLine(prev, pt);
            Gizmos.DrawSphere(pt, 4f);
            prev = pt;
        }

        // Draw lookahead point
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(currentTargetPoint, 5f);
    }
}
