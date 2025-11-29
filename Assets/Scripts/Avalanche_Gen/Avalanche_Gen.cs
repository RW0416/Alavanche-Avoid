using System.Collections.Generic;
using UnityEngine;

public class AvalancheController : MonoBehaviour
{
    [Header("References")]
    public Transform player;      // Reference to the player object
    public Transform trackRoot;   // Transform that defines the local track coordinate system

    [Header("Snow Speed")]
    public float baseSpeed = 15f;              // Initial avalanche speed
    public float speedIncreasePerSecond = 0.7f; // Speed increase over time
    public float maxSpeed = 55f;               // Maximum avalanche speed limit

    [Header("Chase Settings")]
    public float startOffsetZ = 100f;      // Initial distance behind the player in local Z space
    public float catchDistance = 5f;       // Game over if avalanche is closer than this distance

    [Header("Catch-up Tuning")]
    public float chaseDistanceThreshold = 200f; // If distance to the player exceeds this, enter catch-up mode
    public float chaseSpeed = 50f;              // Fixed high speed when in catch-up mode

    [Header("Path Sampling")]
    public float sampleDistance = 50f;      // Record a new waypoint every time the player moves this distance
    public float waypointReachEpsilon = 2f; // Consider waypoint "reached" when within this distance

    private float elapsedTime = 0f;
    private bool caught = false;

    // List of recorded player positions (stored in TrackRoot local space)
    private readonly List<Vector3> waypoints = new List<Vector3>();
    private Vector3 lastRecordedPlayerLocal;  // The last recorded sample position of the player

    void Start()
    {
        // Auto-assign trackRoot if avalanche object is already parented under TrackRoot
        if (trackRoot == null)
        {
            trackRoot = transform.parent;
        }

        if (player == null || trackRoot == null)
        {
            Debug.LogError("AvalancheController: Player and TrackRoot references must be assigned.");
            enabled = false;
            return;
        }

        // Initialize first waypoint using player's current local position
        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        lastRecordedPlayerLocal = playerLocal;
        waypoints.Clear();
        waypoints.Add(playerLocal);

        // Position avalanche behind the player at the starting offset distance
        Vector3 myLocal = playerLocal;
        myLocal.z += startOffsetZ;   // Since downhill movement is along negative Z, behind = +Z
        transform.localPosition = myLocal;

        elapsedTime = 0f;
        caught = false;
    }

    void Update()
    {
        if (caught) return;
        if (!player || !trackRoot) return;

        elapsedTime += Time.deltaTime;

        // Convert current positions into track-local coordinate space
        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        Vector3 myLocal = transform.localPosition;

        // Record a new waypoint when the player has moved far enough
        float movedSinceLast = Vector3.Distance(playerLocal, lastRecordedPlayerLocal);
        if (movedSinceLast >= sampleDistance)
        {
            waypoints.Add(playerLocal);
            lastRecordedPlayerLocal = playerLocal;
        }

        // Compute distance to player to determine whether speed boost is needed
        float distToPlayer = Vector3.Distance(myLocal, playerLocal);

        float speed;
        if (distToPlayer > chaseDistanceThreshold)
        {
            // Too far behind → enter catch-up mode
            speed = chaseSpeed;
        }
        else
        {
            // Normal acceleration curve: base speed + time-based ramp, clamped to a max
            speed = Mathf.Min(baseSpeed + speedIncreasePerSecond * elapsedTime, maxSpeed);
        }

        // Follow stored player path (waypoints)
        if (waypoints.Count > 0)
        {
            // Move toward the oldest recorded waypoint
            Vector3 target = waypoints[0];

            Vector3 toTarget = target - myLocal;
            float distanceToTarget = toTarget.magnitude;

            if (distanceToTarget <= waypointReachEpsilon)
            {
                // Reached this waypoint → remove it and move toward the next one
                waypoints.RemoveAt(0);
            }
            else
            {
                // Move toward measured player path direction
                Vector3 dir = toTarget.normalized;
                float moveDist = speed * Time.deltaTime;

                if (moveDist >= distanceToTarget)
                {
                    // Snap directly to waypoint if close enough
                    myLocal = target;
                }
                else
                {
                    // Move partially toward target
                    myLocal += dir * moveDist;
                }

                transform.localPosition = myLocal;
            }
        }
        else
        {
            // No remaining waypoints → avalanche continues down the slope
            //
            // NOTE: TrackRoot already defines slope orientation, so modifying local Z
            // will naturally move avalanche downhill in world space (Y+Z movement).
            myLocal.z -= speed * Time.deltaTime;
            transform.localPosition = myLocal;
        }

        // After movement, check if avalanche has caught the player
        myLocal = transform.localPosition;
        distToPlayer = Vector3.Distance(myLocal, playerLocal);

        if (distToPlayer <= catchDistance)
        {
            OnCaughtPlayer();
        }
    }

    void OnCaughtPlayer()
    {
        if (caught) return;
        caught = true;

        Debug.Log("❄ Avalanche caught the player!");

        // Pause time as a basic Game Over reaction (replace with UI logic later)
        Time.timeScale = 0f;

    }
}
