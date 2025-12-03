using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the avalanche and notifies PlayerLifeSystem when it hits the player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AvalancheController : MonoBehaviour
{
    [Header("References")]
    public Transform player;        // Character_Snowboarder_01
    public Transform trackRoot;     // Track_Root
    public PlayerLifeSystem playerLife;

    [Header("Snow Speed")]
    public float baseSpeed = 20f;
    public float speedIncreasePerSecond = 0.2f;
    public float maxSpeed = 50f;

    [Header("Chase Settings")]
    public float startOffsetZ = 100f;
    public float catchDistance = 5f;

    [Header("Catch-up Tuning")]
    public float chaseDistanceThreshold = 200f;
    public float chaseSpeed = 50f;

    [Header("Path Sampling")]
    public float sampleDistance = 50f;
    public float waypointReachEpsilon = 2f;

    readonly List<Vector3> waypoints = new List<Vector3>();
    Vector3 lastRecordedPlayerLocal;
    float elapsedTime;
    bool caught;
    bool isPaused;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void Start()
    {
        if (trackRoot == null && transform.parent != null)
            trackRoot = transform.parent;

        if (player == null || trackRoot == null)
        {
            Debug.LogError("[AvalancheController] player or trackRoot not set.");
            enabled = false;
            return;
        }

        if (playerLife == null)
            playerLife = player.GetComponent<PlayerLifeSystem>();

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        lastRecordedPlayerLocal = playerLocal;
        waypoints.Clear();
        waypoints.Add(playerLocal);

        Vector3 myLocal = playerLocal;
        myLocal.z += startOffsetZ;
        transform.localPosition = myLocal;

        elapsedTime = 0f;
        caught = false;
        isPaused = false;
    }

    void Update()
    {
        if (caught || isPaused)
            return;
        if (player == null || trackRoot == null)
            return;

        elapsedTime += Time.deltaTime;

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        Vector3 myLocal = transform.localPosition;

        // record player path
        float movedSinceLast = Vector3.Distance(playerLocal, lastRecordedPlayerLocal);
        if (movedSinceLast >= sampleDistance)
        {
            waypoints.Add(playerLocal);
            lastRecordedPlayerLocal = playerLocal;
        }

        float distToPlayer = Vector3.Distance(myLocal, playerLocal);
        float speed = (distToPlayer > chaseDistanceThreshold)
            ? chaseSpeed
            : Mathf.Min(baseSpeed + speedIncreasePerSecond * elapsedTime, maxSpeed);

        // follow waypoints
        if (waypoints.Count > 0)
        {
            Vector3 target = waypoints[0];
            Vector3 toTarget = target - myLocal;
            float distanceToTarget = toTarget.magnitude;

            if (distanceToTarget <= waypointReachEpsilon)
            {
                waypoints.RemoveAt(0);
            }
            else
            {
                Vector3 dir = toTarget.normalized;
                float moveDist = speed * Time.deltaTime;
                myLocal += dir * Mathf.Min(distanceToTarget, moveDist);
                transform.localPosition = myLocal;
            }
        }
        else
        {
            // roll downhill in local -Z
            myLocal.z -= speed * Time.deltaTime;
            transform.localPosition = myLocal;
        }

        // backup distance catch
        myLocal = transform.localPosition;
        distToPlayer = Vector3.Distance(myLocal, playerLocal);
        if (distToPlayer <= catchDistance)
            OnCaughtPlayer();
    }

    void OnTriggerEnter(Collider other)
    {
        if (caught || player == null)
            return;

        if (other.transform == player || other.transform.IsChildOf(player))
            OnCaughtPlayer();
    }

    void OnCaughtPlayer()
    {
        if (caught)
            return;

        caught = true;
        Debug.Log("[AvalancheController] caught player, notifying PlayerLifeSystem.");

        if (playerLife != null)
            playerLife.OnAvalancheHit(this);
    }

    // ===== API used by PlayerLifeSystem =====

    public void PauseChase()
    {
        isPaused = true;
    }

    public void ResetBehindPlayer(float pushbackDistance)
    {
        if (player == null || trackRoot == null)
            return;

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        lastRecordedPlayerLocal = playerLocal;
        waypoints.Clear();
        waypoints.Add(playerLocal);

        float offset = pushbackDistance > 0f ? pushbackDistance : startOffsetZ;

        Vector3 myLocal = playerLocal;
        myLocal.z += offset;
        transform.localPosition = myLocal;

        elapsedTime = 0f;
        caught = false;
        isPaused = false;
    }
}
