using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SnowboarderController : MonoBehaviour
{
    [Header("References")]
    public Transform visualRoot;
    public LayerMask groundMask = ~0;

    [Header("Ground Movement")]
    [Tooltip("Extra push when holding W (on top of gravity).")]
    public float pushAcceleration = 12f;
    public float maxSpeed = 30f;

    [Tooltip("Base friction while sliding (per second).")]
    public float baseGroundFriction = 0.6f;
    [Tooltip("Extra friction while carving with A/D.")]
    public float carveExtraFriction = 0.4f;
    [Tooltip("Extra friction while holding S.")]
    public float brakeExtraFriction = 2f;
    [Tooltip("How strongly S kills your speed, in m/s per second.")]
    public float brakeStrength = 18f;

    [Header("Turning / Carving")]
    public float turnSpeed = 90f;          // degrees per second
    public float carveAcceleration = 6f;   // small lateral force
    public float bodyAlignLerp = 10f;      // how fast body lines up with movement

    [Header("Jump")]
    public float jumpForce = 6f;
    public float groundCheckDistance = 0.4f;

    [Header("Visual Lean")]
    public float maxLeanAngle = 25f;
    public float leanLerp = 12f;

    Rigidbody rb;

    Vector2 moveInput;
    float horizontalInput;
    float verticalInput;
    bool jumpQueued;

    bool isGrounded;
    Vector3 groundNormal = Vector3.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // allow yaw, but prevent tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // ---------- INPUT SYSTEM CALLBACKS ----------

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
            jumpQueued = true;
    }

    // --------------- UPDATE LOOPS ----------------

    void Update()
    {
        horizontalInput = moveInput.x;
        verticalInput   = moveInput.y;

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

    // --------------- GROUND CHECK ----------------

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

    // --------------- MOVEMENT (GROUND ONLY) ----------------

    void ApplyGroundMovement()
    {
        Vector3 vel = rb.linearVelocity;

        // split velocity into along-ground and into-ground
        float vNormal = Vector3.Dot(vel, groundNormal);
        Vector3 normalVel = groundNormal * vNormal;
        Vector3 horizontalVel = vel - normalVel;

        // if we're actually sliding, face the movement direction
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

        // downhill from GLOBAL gravity, only for tuning / push direction
        Vector3 downhill = Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
        if (downhill.sqrMagnitude > 0.001f)
            downhill.Normalize();

        // --- friction ---

        float friction = baseGroundFriction;

        if (Mathf.Abs(horizontalInput) > 0.01f)
            friction += carveExtraFriction;    // carving slows you a bit

        if (verticalInput < 0f)
            friction += brakeExtraFriction;    // pressing S slows harder

        // apply friction only to horizontal component
        horizontalVel -= horizontalVel * (friction * Time.fixedDeltaTime);

        // extra brutal braking so S can bring you to a stop
        if (verticalInput < 0f && horizontalVel.sqrMagnitude > 0.0001f)
        {
            float speed = horizontalVel.magnitude;
            float newSpeed = Mathf.Max(0f, speed - brakeStrength * Time.fixedDeltaTime);
            horizontalVel = horizontalVel.normalized * newSpeed;
        }

        // --- pushing with W (small boost) ---
        if (verticalInput > 0f && downhill.sqrMagnitude > 0f)
        {
            // push in board's forward direction projected on the ground
            Vector3 pushDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            rb.AddForce(pushDir * pushAcceleration, ForceMode.Acceleration);
        }

        // --- carving left/right ---

        if (Mathf.Abs(horizontalInput) > 0.01f && horizontalVel.sqrMagnitude > 0.01f)
        {
            // rotate around world up (track is sloped but still basically vertical Y)
            float turnAmount = horizontalInput * turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(Vector3.up, turnAmount, Space.World);

            // slight lateral force so you can drift across the slope
            Vector3 sideDir = Vector3.Cross(groundNormal, transform.forward).normalized;
            rb.AddForce(sideDir * (horizontalInput * carveAcceleration), ForceMode.Acceleration);
        }

        // reapply normal component so we don't mess with gravity
        rb.linearVelocity = horizontalVel + normalVel;
    }

    // --------------- JUMP ----------------

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

    // --------------- LIMIT SPEED ----------------

    void ClampMaxSpeed()
    {
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed > maxSpeed)
        {
            rb.linearVelocity = v * (maxSpeed / speed);
        }
    }

    // --------------- VISUAL LEAN ----------------

    void UpdateVisualRotation()
    {
        if (!visualRoot) return;

        // no lean in air
        float targetLean = isGrounded ? -horizontalInput * maxLeanAngle : 0f;
        Quaternion leanRot = Quaternion.AngleAxis(targetLean, Vector3.forward);

        // look along the board's forward projected onto the ground
        Vector3 lookDir = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (lookDir.sqrMagnitude < 0.001f)
            lookDir = transform.forward;

        Quaternion baseRot = Quaternion.LookRotation(lookDir.normalized, groundNormal);

        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            baseRot * leanRot,
            leanLerp * Time.deltaTime
        );
    }
}
