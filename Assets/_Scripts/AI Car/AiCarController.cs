using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class AiCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 8f;
    public float deceleration = 6f;
    public float baseMaxSpeed = 33f;

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

    [Header("Waypoint Settings")]
    public float waypointThreshold = 2f; // distance to consider waypoint reached

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private ColliderResizer cr;

    private float targetSpeed = 0f;
    private Vector2 currentTargetPoint;

    public bool canMove = false;
    [HideInInspector] public int CurrentLap = 0;
    [HideInInspector] public float CurrentForwardSpeed => Vector2.Dot(rb.linearVelocity, transform.up);

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
        foreach (var wp in waypoints)
        {
            if (wp == null)
            {
                Debug.LogError("Null waypoint!");
                continue;
            }

            // Search children, including inactive ones
            BoxCollider2D box = wp.GetComponentInChildren<BoxCollider2D>(true);
            Debug.Log($"Waypoint {wp.name} has BoxCollider2D? {box != null}");
        }
        SetRandomTargetPoint();
    }

    private void FixedUpdate()
    {
        if (!canMove || waypoints == null || waypoints.Length < 2) return;

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
        if (Vector2.Distance(transform.position, currentTargetPoint) < waypointThreshold)
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
        if (box == null)
            box = wp.GetComponentInChildren<BoxCollider2D>();

        if (box != null)
            Debug.Log($"Found BoxCollider2D on {box.gameObject.name}");
        else
            Debug.LogError($"No BoxCollider2D found for waypoint {wp.name}");
        if (box != null)
        {
            Vector2 worldPos = wp.position;
            Vector2 halfSize = box.size * 0.5f;

            float x = worldPos.x + Random.Range(-halfSize.x, halfSize.x);
            float y = worldPos.y + Random.Range(-halfSize.y, halfSize.y);

            currentTargetPoint = new Vector2(x, y);
            Debug.Log("Random point set");
        }
        else
        {
            currentTargetPoint = wp.position;
            Debug.Log("Center set");
        }
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

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.green;
        int lookAheadCount = 5;
        Vector3 previousPoint = transform.position;
        int tempIndex = currentIndex;

        for (int i = 0; i < lookAheadCount; i++)
        {
            Vector3 nextPoint = (i == 0) ? currentTargetPoint : waypoints[tempIndex].position;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
            tempIndex = (tempIndex + 1) % waypoints.Length;
        }

        // Draw spheres
        Gizmos.color = Color.yellow;
        tempIndex = currentIndex;
        for (int i = 0; i < lookAheadCount; i++)
        {
            Vector3 spherePos = (i == 0) ? (Vector3)currentTargetPoint : waypoints[tempIndex].position;
            Gizmos.DrawSphere(spherePos, 2f);
            tempIndex = (tempIndex + 1) % waypoints.Length;
        }

        // Draw avoidance ray
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * avoidanceRayDistance);
    }
}
