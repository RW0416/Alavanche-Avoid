using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Avalanche_Gen : MonoBehaviour
{
    [Header("references")]
    public Transform player;                 // Character_Snowboarder_01 root
    public Transform trackRoot;              // Track_Root
    public SnowboarderController playerController;
    public SnowboarderRagdoll playerRagdoll;

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

    [Header("impact – cannonball launch")]
    public float forwardImpulse = 30f;          // along ground away from avalanche
    public float upwardImpulse = 15f;           // upwards
    public float extraBodyImpulseMultiplier = 0.5f;

    Rigidbody rb;
    readonly List<Vector3> waypoints = new List<Vector3>();
    Vector3 lastRecordedPlayerLocal;
    float elapsedTime;
    bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;   // moved by script, but needed for triggers
    }

    void Start()
    {
        if (!player || !trackRoot)
        {
            Debug.LogError("Avalanche_Gen: missing player or trackRoot.");
            enabled = false;
            return;
        }

        if (!playerController)
            playerController = player.GetComponent<SnowboarderController>();
        if (!playerRagdoll)
            playerRagdoll = player.GetComponentInChildren<SnowboarderRagdoll>();

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        lastRecordedPlayerLocal = playerLocal;
        waypoints.Clear();
        waypoints.Add(playerLocal);

        Vector3 myLocal = playerLocal;
        myLocal.z += startOffsetZ;        // start behind player
        transform.localPosition = myLocal;

        elapsedTime = 0f;
        hasHit = false;
    }

    void Update()
    {
        if (hasHit) return;
        if (!player || !trackRoot) return;

        elapsedTime += Time.deltaTime;

        Vector3 playerLocal = trackRoot.InverseTransformPoint(player.position);
        Vector3 myLocal = transform.localPosition;

        // record the path the player took
        float movedSinceLast = Vector3.Distance(playerLocal, lastRecordedPlayerLocal);
        if (movedSinceLast >= sampleDistance)
        {
            waypoints.Add(playerLocal);
            lastRecordedPlayerLocal = playerLocal;
        }

        // choose speed
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
            // once we're out of waypoints, just roll downhill
            myLocal.z -= speed * Time.deltaTime;
            transform.localPosition = myLocal;
        }

        // backup distance check in case trigger somehow misses
        myLocal = transform.localPosition;
        distToPlayer = Vector3.Distance(myLocal, playerLocal);
        if (distToPlayer <= catchDistance)
        {
            HitPlayer();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit || player == null) return;

        // root collider OR any child collider of the player
        if (other.transform == player || other.transform.IsChildOf(player))
        {
            HitPlayer();
        }
    }

    void HitPlayer()
    {
        if (hasHit) return;
        hasHit = true;

        Debug.Log("❄ avalanche hit player – cannonball ragdoll");

        // direction: from avalanche → player, flattened on XZ
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            // fallback: push downhill
            if (trackRoot != null)
                dir = -trackRoot.forward;
            else
                dir = Vector3.forward;
        }
        dir.Normalize();

        Vector3 impulse = dir * forwardImpulse + Vector3.up * upwardImpulse;

        // 1) shove the main rigidbody BEFORE ragdoll so velocity is copied
        Rigidbody mainBody = null;
        if (playerRagdoll != null && playerRagdoll.mainBody != null)
            mainBody = playerRagdoll.mainBody;
        else
            mainBody = player.GetComponent<Rigidbody>();

        if (mainBody != null)
        {
            mainBody.AddForce(impulse, ForceMode.VelocityChange);
        }

        // 2) stop the controller fighting physics
        if (playerController != null)
            playerController.enabled = false;

        // 3) enable ragdoll
        if (playerRagdoll != null)
            playerRagdoll.EnableRagdoll();

        // 4) next physics step, kick all ragdoll bodies too
        StartCoroutine(ApplyImpulseNextFixed(impulse));
    }

    IEnumerator ApplyImpulseNextFixed(Vector3 impulse)
    {
        // wait until after the ragdoll has fully enabled its rigidbodies
        yield return new WaitForFixedUpdate();

        if (player == null) yield break;

        var bodies = player.GetComponentsInChildren<Rigidbody>();
        foreach (var body in bodies)
        {
            if (body != null && !body.isKinematic)
                body.AddForce(impulse * extraBodyImpulseMultiplier, ForceMode.VelocityChange);
        }
    }
}
