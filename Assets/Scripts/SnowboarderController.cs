using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SnowboarderController : MonoBehaviour
{
    [Header("References")]
    public Transform visualRoot;
    public LayerMask groundMask = ~0;

    [Header("Local Gravity")]
    public float slopeGravity = 25f;
    public float stickToGroundGravity = 40f;

    [Header("Ground Movement")]
    public float pushAcceleration = 12f;
    public float maxSpeed = 30f;
    public float baseGroundFriction = 0.6f;
    public float carveExtraFriction = 0.4f;
    public float brakeExtraFriction = 2f;
    public float brakeStrength = 18f;

    [Header("Turning / Carving")]
    public float turnSpeed = 90f;          // mostly for side force
    public float carveAcceleration = 6f;
    public float bodyAlignLerp = 10f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public float groundCheckDistance = 0.4f;

    Rigidbody rb;

    Vector2 moveInput;
    float horizontalInput;
    float verticalInput;

    bool isGrounded;
    bool wasGrounded;
    Vector3 groundNormal = Vector3.up;

    bool jumpRequested;
    bool hasJumpedSinceGrounded;

    // ---- public for tricks script ----
    public bool IsGrounded => isGrounded;
    public bool WasGrounded => wasGrounded;
    public Vector3 GroundNormal => groundNormal;
    public Vector2 MoveInput => moveInput;
    public float HorizontalInput => horizontalInput;
    public float VerticalInput => verticalInput;
    public Rigidbody Body => rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // we handle gravity manually
        rb.useGravity = false;

        // allow yaw, block tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // ------------ INPUT ------------

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpRequested = true;
        }
    }

    // ------------ UPDATE LOOPS ------------

    void Update()
    {
        horizontalInput = moveInput.x;
        verticalInput   = moveInput.y;
    }

    void FixedUpdate()
    {
        UpdateGroundInfo();
        ApplyLocalGravity();

        if (isGrounded)
        {
            ApplyGroundMovement();
        }

        HandleJump();
        ClampMaxSpeed();
    }

    // ------------ GROUND CHECK (ignores own collider) ------------

    void UpdateGroundInfo()
    {
        wasGrounded = isGrounded;
        isGrounded = false;
        groundNormal = Vector3.up;

        // ray length based on collider size, not some random big number
        Collider col = GetComponent<Collider>();
        float castDistance = col ? col.bounds.extents.y + 0.5f : 2f;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(origin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        float closest = float.MaxValue;
        RaycastHit bestHit = new RaycastHit();
        bool found = false;

        foreach (var h in hits)
        {
            if (!h.collider) continue;

            // ignore our own colliders
            if (h.rigidbody == rb) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;

            if (h.distance < closest)
            {
                closest = h.distance;
                bestHit = h;
                found = true;
            }
        }

        if (found)
        {
            isGrounded = true;
            groundNormal = bestHit.normal;
        }

        if (isGrounded && !wasGrounded)
        {
            hasJumpedSinceGrounded = false;
        }
    }


    // ------------ LOCAL GRAVITY (world-down) ------------

    void ApplyLocalGravity()
    {
        if (isGrounded)
        {
            // stick to slope
            Vector3 intoSlope = -groundNormal * stickToGroundGravity;

            // downhill = world down projected onto the surface
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
            if (downhill.sqrMagnitude > 0.0001f)
                downhill.Normalize();
            else
                downhill = Vector3.zero;

            rb.AddForce(intoSlope + downhill * slopeGravity, ForceMode.Acceleration);
        }
        else
        {
            // in air, just fall
            rb.AddForce(Vector3.down * stickToGroundGravity, ForceMode.Acceleration);
        }
    }

    // ------------ MOVEMENT (GROUND ONLY) ------------

    void ApplyGroundMovement()
    {
        Vector3 vel = rb.linearVelocity;

        float vNormal = Vector3.Dot(vel, groundNormal);
        Vector3 normalVel = groundNormal * vNormal;
        Vector3 horizontalVel = vel - normalVel;

        // face actual sliding direction (or keep current if almost stopped)
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

        // friction
        float friction = baseGroundFriction;
        if (Mathf.Abs(horizontalInput) > 0.01f)
            friction += carveExtraFriction;
        if (verticalInput < 0f)
            friction += brakeExtraFriction;

        horizontalVel -= horizontalVel * (friction * Time.fixedDeltaTime);

        // strong brake with S
        if (verticalInput < 0f && horizontalVel.sqrMagnitude > 0.0001f)
        {
            float speed = horizontalVel.magnitude;
            float newSpeed = Mathf.Max(0f, speed - brakeStrength * Time.fixedDeltaTime);
            horizontalVel = horizontalVel.normalized * newSpeed;
        }

        // small push with W
        if (verticalInput > 0f)
        {
            Vector3 pushDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            rb.AddForce(pushDir * pushAcceleration, ForceMode.Acceleration);
        }

        // carving = side force only, rotation handled by velocity alignment
        if (Mathf.Abs(horizontalInput) > 0.01f && horizontalVel.sqrMagnitude > 0.01f)
        {
            Vector3 sideDir = Vector3.Cross(groundNormal, transform.forward).normalized;
            rb.AddForce(sideDir * (horizontalInput * carveAcceleration), ForceMode.Acceleration);
        }

        rb.linearVelocity = horizontalVel + normalVel;
    }

    // ------------ JUMP (once per landing) ------------

    void HandleJump()
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (!isGrounded)
            return;

        if (hasJumpedSinceGrounded)
            return;

        hasJumpedSinceGrounded = true;

        Vector3 vel = rb.linearVelocity;
        float vNormal = Vector3.Dot(vel, groundNormal);
        if (vNormal < 0f)
        {
            vel -= groundNormal * vNormal;
            rb.linearVelocity = vel;
        }

        rb.AddForce(groundNormal * jumpForce, ForceMode.VelocityChange);
    }

    // ------------ SPEED LIMIT ------------

    void ClampMaxSpeed()
    {
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed > maxSpeed)
        {
            rb.linearVelocity = v * (maxSpeed / speed);
        }
    }
}
