using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SnowboarderController : MonoBehaviour
{
    private enum TrickType { None, BarrelRoll, FrontFlip, BackFlip }

    [Header("References")]
    public Transform visualRoot;
    public LayerMask groundMask = ~0;

    [Header("Ground Movement")]
    public float pushAcceleration = 12f;
    public float maxSpeed = 30f;

    public float baseGroundFriction = 0.6f;
    public float carveExtraFriction = 0.4f;
    public float brakeExtraFriction = 2f;
    public float brakeStrength = 18f;

    [Header("Turning / Carving")]
    public float turnSpeed = 90f;          // degrees / second
    public float carveAcceleration = 6f;   // small lateral force
    public float bodyAlignLerp = 10f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public float groundCheckDistance = 0.4f;

    [Header("Visual Lean")]
    public float maxLeanAngle = 25f;
    public float leanLerp = 12f;

    [Header("Tricks")]
    public float trickDuration = 0.8f;     // seconds for a full 360

    Rigidbody rb;

    Vector2 moveInput;
    float horizontalInput;
    float verticalInput;
    bool jumpQueued;

    bool isGrounded;
    Vector3 groundNormal = Vector3.up;

    // trick state
    TrickType currentTrick = TrickType.None;
    float trickTimer;
    Vector3 trickAxisLocal = Vector3.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // allow yaw but stop tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // ---------- INPUT SYSTEM CALLBACKS ----------

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        if (isGrounded)
        {
            // normal jump from ground
            jumpQueued = true;
        }
        else
        {
            // in air: space triggers a sideways flip
            StartTrick(TrickType.BarrelRoll);
        }
    }

    // ---------------- UPDATE LOOPS ----------------

    void Update()
    {
        horizontalInput = moveInput.x;
        verticalInput   = moveInput.y;

        // in the air: W = front flip, S = back flip
        if (!isGrounded && currentTrick == TrickType.None)
        {
            if (verticalInput > 0.5f)
            {
                StartTrick(TrickType.FrontFlip);
            }
            else if (verticalInput < -0.5f)
            {
                StartTrick(TrickType.BackFlip);
            }
        }

        UpdateVisualRotation();
    }

    void FixedUpdate()
    {
        UpdateGroundInfo();

        if (isGrounded)
        {
            ApplyGroundMovement();   // ONLY move when on ground
        }

        HandleJump();
        ClampMaxSpeed();
    }

    // ---------------- GROUND CHECK ----------------

    void UpdateGroundInfo()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit,
                            groundCheckDistance + 0.2f,
                            groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }

    // ---------------- MOVEMENT (GROUND ONLY) ----------------

    void ApplyGroundMovement()
    {
        Vector3 vel = rb.linearVelocity;

        float vNormal = Vector3.Dot(vel, groundNormal);
        Vector3 normalVel = groundNormal * vNormal;
        Vector3 horizontalVel = vel - normalVel;

        // face movement direction if sliding
        if (horizontalVel.sqrMagnitude > 0.01f)
        {
            Vector3 moveDir = horizontalVel.normalized;
            Quaternion targetRot = Quaternion.LookRotation(moveDir, groundNormal);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                bodyAlignLerp * Time.fixedDeltaTime
            );
        }

        // downhill from global gravity, just for orientation / push
        Vector3 downhill = Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
        if (downhill.sqrMagnitude > 0.001f)
            downhill.Normalize();

        // base friction
        float friction = baseGroundFriction;

        if (Mathf.Abs(horizontalInput) > 0.01f)
            friction += carveExtraFriction;    // carving slows a bit

        if (verticalInput < 0f)
            friction += brakeExtraFriction;    // holding S slows harder

        // friction only affects horizontal sliding
        horizontalVel -= horizontalVel * (friction * Time.fixedDeltaTime);

        // strong brake so S can actually stop you
        if (verticalInput < 0f && horizontalVel.sqrMagnitude > 0.0001f)
        {
            float speed = horizontalVel.magnitude;
            float newSpeed = Mathf.Max(0f, speed - brakeStrength * Time.fixedDeltaTime);
            horizontalVel = horizontalVel.normalized * newSpeed;
        }

        // small manual push with W in direction of board forward
        if (verticalInput > 0f)
        {
            Vector3 pushDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            rb.AddForce(pushDir * pushAcceleration, ForceMode.Acceleration);
        }

        // carving left/right
        if (Mathf.Abs(horizontalInput) > 0.01f && horizontalVel.sqrMagnitude > 0.01f)
        {
            float turnAmount = horizontalInput * turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(Vector3.up, turnAmount, Space.World);

            Vector3 sideDir = Vector3.Cross(groundNormal, transform.forward).normalized;
            rb.AddForce(sideDir * (horizontalInput * carveAcceleration), ForceMode.Acceleration);
        }

        rb.linearVelocity = horizontalVel + normalVel;
    }

    // ---------------- JUMP ----------------

    void HandleJump()
    {
        if (!jumpQueued) return;
        jumpQueued = false;
        if (!isGrounded) return;

        Vector3 vel = rb.linearVelocity;
        float vNormal = Vector3.Dot(vel, groundNormal);
        if (vNormal < 0f)
        {
            vel -= groundNormal * vNormal;
            rb.linearVelocity = vel;
        }

        rb.AddForce(groundNormal * jumpForce, ForceMode.VelocityChange);
    }

    // ---------------- SPEED LIMIT ----------------

    void ClampMaxSpeed()
    {
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed > maxSpeed)
        {
            rb.linearVelocity = v * (maxSpeed / speed);
        }
    }

    // ---------------- TRICKS ----------------

    void StartTrick(TrickType type)
    {
        if (currentTrick != TrickType.None)
            return; // no stacking tricks

        currentTrick = type;
        trickTimer = 0f;

        // local axes: X = side, Y = up, Z = forward
        switch (type)
        {
            case TrickType.BarrelRoll:   // space in air
                trickAxisLocal = Vector3.forward;   // roll around forward axis
                break;
            case TrickType.FrontFlip:    // W in air
                trickAxisLocal = Vector3.right;     // forward flip
                break;
            case TrickType.BackFlip:     // S in air
                trickAxisLocal = -Vector3.right;    // backward flip
                break;
        }
    }

    void UpdateVisualRotation()
    {
        if (!visualRoot) return;

        // base orientation: board forward projected onto ground
        Vector3 lookDir = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (lookDir.sqrMagnitude < 0.001f)
            lookDir = transform.forward;

        Quaternion baseRot = Quaternion.LookRotation(lookDir.normalized, groundNormal);

        // no lean during tricks
        float targetLean = (isGrounded && currentTrick == TrickType.None)
            ? -horizontalInput * maxLeanAngle
            : 0f;

        Quaternion leanRot = Quaternion.AngleAxis(targetLean, Vector3.forward);

        // trick rotation
        Quaternion trickRot = Quaternion.identity;
        if (currentTrick != TrickType.None)
        {
            trickTimer += Time.deltaTime;
            float t = Mathf.Clamp01(trickTimer / trickDuration);
            float angle = 360f * t;

            // axis in world space based on board orientation
            Vector3 axisWorld = baseRot * trickAxisLocal;
            trickRot = Quaternion.AngleAxis(angle, axisWorld);

            if (t >= 1f)
            {
                currentTrick = TrickType.None;
            }
        }

        Quaternion targetRot = baseRot * leanRot * trickRot;

        // smooth a bit
        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            targetRot,
            leanLerp * Time.deltaTime
        );
    }
}
