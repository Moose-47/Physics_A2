using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class AiCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 8f;
    public float deceleration = 6f;
    public float baseMaxSpeed = 12f;

    [Header("Track Settings")]
    public RacingLine racingLine;
    public Transform[] waypoints;
    public int currentIndex = 0;

    [Header("Off-Track Settings")]
    public LayerMask offTrackLayer;
    public float offTrackSlowFactor = 0.5f;

    [Header("Avoidance Settings")]
    public float avoidanceRayDistance = 2f;
    public float avoidanceStrength = 1.5f;
    public LayerMask playerLayer;

    [Header("Recovery Settings")]
    public float wrongWayAngle = 100f;
    public float stuckSpeedThreshold = 1f;
    public float stuckTimeLimit = 2f;
    public float recoveryTurnSpeed = 150f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private ColliderResizer cr;

    private float targetSpeed = 0f;
    private bool recovering = false;
    private float stuckTimer = 0f;
    private Vector2 currentTargetPoint;

    private float laneOffset; // stable per car

    public bool canMove = false;
    [HideInInspector] public int CurrentLap = 0;
    [HideInInspector] public float CurrentForwardSpeed => Vector2.Dot(rb.linearVelocity, transform.up);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        cr = GetComponent<ColliderResizer>();
        racingLine = FindAnyObjectByType<RacingLine>();

        if (racingLine != null)
            waypoints = racingLine.waypoints;

        // Assign stable lane offset per car
        laneOffset = Random.Range(-0.5f, 0.5f);

        // Initialize the first target point
        SetRandomTargetPoint();
    }

    public void Initialize(Sprite chosenSprite)
    {
        if (chosenSprite == null) return;
        sr.sprite = chosenSprite;
        cr?.ResetCollider();
    }

    private void FixedUpdate()
    {
        if (!canMove || waypoints == null || waypoints.Length < 2) return;

        // Recovery overrides normal driving
        if (CheckRecovery()) return;

        Vector2 toTarget = (currentTargetPoint - (Vector2)transform.position).normalized;

        // --- Steering ---
        float angleToTarget = Vector2.SignedAngle(transform.up, toTarget);
        float steerInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);

        // Collision avoidance
        steerInput += CheckAvoidance();

        rb.MoveRotation(rb.rotation + steerInput * 200f * Time.fixedDeltaTime);

        // --- Speed control ---
        AdjustSpeedForCorners(currentTargetPoint);
        float currentSpeed = Vector2.Dot(rb.linearVelocity, transform.up);
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = transform.up * Mathf.Max(newSpeed, 0f);

        if (IsOffTrack())
            rb.linearVelocity *= offTrackSlowFactor;

        // --- Waypoint progression ---
        if (Vector2.Distance(transform.position, currentTargetPoint) < 0.5f) // threshold
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            SetRandomTargetPoint();
        }
    }

    private void SetRandomTargetPoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform wp = waypoints[currentIndex];
        BoxCollider2D box = wp.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Vector2 size = box.size;
            Vector2 center = (Vector2)wp.position + box.offset;

            float x = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
            float y = Random.Range(center.y - size.y / 2f, center.y + size.y / 2f);
            currentTargetPoint = new Vector2(x, y);
        }
        else
        {
            currentTargetPoint = wp.position;
        }

        // Apply lane offset
        int prevIndex = (currentIndex - 1 + waypoints.Length) % waypoints.Length;
        Vector2 segment = (Vector2)(wp.position - waypoints[prevIndex].position);
        Vector2 sideDir = new Vector2(-segment.y, segment.x).normalized;
        currentTargetPoint += sideDir * laneOffset;
    }

    private void AdjustSpeedForCorners(Vector2 target)
    {
        int nextIndex = (currentIndex + 1) % waypoints.Length;
        Vector2 nextTarget = waypoints[nextIndex].position;
        Vector2 toCurrent = (target - (Vector2)transform.position).normalized;
        Vector2 toNext = (nextTarget - target).normalized;

        float angle = Vector2.Angle(toCurrent, toNext);
        float maxSpeed = baseMaxSpeed;

        if (angle > 45f) maxSpeed *= 0.6f;
        else if (angle > 20f) maxSpeed *= 0.8f;

        targetSpeed = maxSpeed;
    }

    private bool IsOffTrack()
    {
        return Physics2D.OverlapPoint(transform.position, offTrackLayer);
    }

    private float CheckAvoidance()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, avoidanceRayDistance, playerLayer);
        if (hit.collider != null)
            return (Random.value > 0.5f ? 1 : -1) * avoidanceStrength;
        return 0f;
    }

    private bool CheckRecovery()
    {
        Vector2 toTarget = (currentTargetPoint - (Vector2)transform.position).normalized;
        float angle = Vector2.Angle(transform.up, toTarget);

        if (angle > wrongWayAngle) recovering = true;

        if (rb.linearVelocity.magnitude < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckTimeLimit) recovering = true;
        }
        else stuckTimer = 0f;

        if (recovering)
        {
            float steerInput = Mathf.Sign(Vector2.SignedAngle(transform.up, toTarget));
            rb.MoveRotation(rb.rotation - steerInput * recoveryTurnSpeed * Time.fixedDeltaTime);
            rb.linearVelocity = transform.up * (baseMaxSpeed * 0.5f);

            if (angle < 30f)
            {
                recovering = false;
                stuckTimer = 0f;
            }
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.green;
        int lookAheadCount = 5;
        int lookaheadIndex = currentIndex;
        Vector3 previousPoint = transform.position;

        for (int i = 0; i < lookAheadCount; i++)
        {
            lookaheadIndex = (lookaheadIndex + 1) % waypoints.Length;
            Vector3 nextPoint = waypoints[lookaheadIndex].position;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        lookaheadIndex = currentIndex;
        for (int i = 0; i < lookAheadCount; i++)
        {
            lookaheadIndex = (lookaheadIndex + 1) % waypoints.Length;
            Gizmos.DrawSphere(waypoints[lookaheadIndex].position, 0.2f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * avoidanceRayDistance);
    }
}
