using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AvalancheController : MonoBehaviour
{
    [Header("references")]
    public Transform player;        // Character_Snowboarder_01 root
    public Transform trackRoot;     // Track_Root

    [Header("snow speed")]
    public float baseSpeed = 20f;
    public float speedIncreasePerSecond = 0.2f;
    public float maxSpeed = 50f;

    [Header("chase settings")]
    public float startOffsetZ = 100f;
    public float catchDistance = 5f;

    [Header("catch-up tuning")]
    public float chaseDistanceThreshold = 200f;
    public float chaseSpeed = 50f;

    [Header("path sampling")]
    public float sampleDistance = 50f;
    public float waypointReachEpsilon = 2f;

    Rigidbody rb;
    readonly List<Vector3> waypoints = new List<Vector3>();
    Vector3 lastRecordedPlayerLocal;
    float elapsedTime;
    bool isPaused;
    bool hasHitPlayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;   // moved by script, but still uses trigger collider
    }

    void Start()
    {
        if (!player || !trackRoot)
        {
            Debug.LogError("AvalancheController: missing player or trackRoot.");
            enabled = false;
            return;
        }

        // first waypoint at player pos
        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        lastRecordedPlayerLocal = playerLocal;
        waypoints.Clear();
        waypoints.Add(playerLocal);

        // place avalanche behind player
        Vector3 myLocal = playerLocal;
        myLocal.z += startOffsetZ;
        transform.localPosition = myLocal;

        elapsedTime = 0f;
        isPaused = false;
        hasHitPlayer = false;
    }

    void Update()
    {
        if (isPaused) return;
        if (!player || !trackRoot) return;

        elapsedTime += Time.deltaTime;

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        Vector3 myLocal = transform.localPosition;

        // record path as player moves
        float movedSinceLast = Vector3.Distance(playerLocal, lastRecordedPlayerLocal);
        if (movedSinceLast >= sampleDistance)
        {
            waypoints.Add(playerLocal);
            lastRecordedPlayerLocal = playerLocal;
        }

        // dynamic speed
        float distToPlayer = Vector3.Distance(myLocal, playerLocal);
        float speed = distToPlayer > chaseDistanceThreshold
            ? chaseSpeed
            : Mathf.Min(baseSpeed + speedIncreasePerSecond * elapsedTime, maxSpeed);

        // follow the path
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
            // no waypoints, just roll downhill (local -Z)
            myLocal.z -= speed * Time.deltaTime;
            transform.localPosition = myLocal;
        }

        // backup distance check
        myLocal = transform.localPosition;
        distToPlayer = Vector3.Distance(myLocal, playerLocal);
        if (!hasHitPlayer && distToPlayer <= catchDistance)
        {
            NotifyHit();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHitPlayer) return;
        if (player == null) return;

        // any collider inside the player rig
        if (!other.transform.IsChildOf(player) && other.transform != player)
            return;

        NotifyHit();
    }

    void NotifyHit()
    {
        if (hasHitPlayer) return;
        hasHitPlayer = true;

        var life = player.GetComponent<PlayerLifeSystem>();
        if (life != null)
        {
            life.OnAvalancheHit(this);
        }
        else
        {
            Debug.LogWarning("AvalancheController: PlayerLifeSystem not found on player.");
        }
    }

    // ===== public api used by PlayerLifeSystem =====

    public void PauseChase()
    {
        isPaused = true;
    }

    public void ResumeChase()
    {
        isPaused = false;
        hasHitPlayer = false;
    }

    public void ResetBehindPlayer(float pushbackDistance)
    {
        if (!player || !trackRoot) return;

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        lastRecordedPlayerLocal = playerLocal;
        waypoints.Clear();
        waypoints.Add(playerLocal);

        float offset = pushbackDistance > 0f ? pushbackDistance : startOffsetZ;

        Vector3 myLocal = playerLocal;
        myLocal.z += offset;
        transform.localPosition = myLocal;

        elapsedTime = 0f;
        isPaused = false;
        hasHitPlayer = false;
    }
}
