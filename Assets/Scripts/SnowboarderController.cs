using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SnowboarderController : MonoBehaviour
{
    [Header("references")]
    public Transform visualRoot;
    public LayerMask groundMask = ~0;

    [Header("local gravity")]
    public float slopeGravity = 25f;
    public float stickToGroundGravity = 40f;
    public float airGravity = 30f;

    [Header("ground movement (feel)")]
    public bool alwaysAccelerate = true;
    public float pushAcceleration = 12f;
    public float maxSpeed = 35f;
    [Tooltip("base sideways friction, smaller = more slide")]
    public float baseGroundFriction = 5f;
    [Tooltip("extra friction while turning")]
    public float carveExtraFriction = 8f;
    [Tooltip("extra friction while braking")]
    public float brakeExtraFriction = 16f;
    public float brakeStrength = 18f;

    [Header("turn / carve")]
    public float turnSpeed = 90f;
    public float bodyAlignLerp = 8f;

    [Header("air control")]
    public float airTurnSpeed = 2f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.4f;

    Rigidbody rb;

    Vector2 moveInput;
    float horizontalInput;
    float verticalInput;

    bool isGrounded;
    bool wasGrounded;
    Vector3 groundNormal = Vector3.up;
    Vector3 smoothGroundNormal = Vector3.up;

    bool jumpRequested;
    bool hasJumpedSinceGrounded;

    // like old BordDirection
    Vector3 rideDirection = Vector3.forward;

    // public api for tricks etc
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
        rb.useGravity = false; // we handle gravity
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // let our own code control slide, keep this low
        rb.linearDamping = 0.0f;
        rb.angularDamping = 0.05f;

        rideDirection = transform.forward;
    }

    // input from new input system
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

    void Update()
    {
        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;
    }

    void FixedUpdate()
    {
        UpdateGroundInfo();
        ApplyLocalGravity();

        if (isGrounded)
            ApplyGroundMovement();
        else
            ApplyAirMovement();

        HandleJump();
        ClampMaxSpeed();
    }

    void UpdateGroundInfo()
    {
        wasGrounded = isGrounded;
        isGrounded = false;

        if (groundNormal.sqrMagnitude < 0.1f)
            groundNormal = Vector3.up;

        Collider col = GetComponent<Collider>();
        float castDistance = col ? col.bounds.extents.y + 0.5f : 1.5f;

        Vector3 origin = transform.position + Vector3.up * 0.2f;
        RaycastHit hit;

        if (Physics.SphereCast(origin, 0.2f, Vector3.down,
            out hit, castDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform != transform &&
                !hit.collider.transform.IsChildOf(transform))
            {
                isGrounded = true;
                groundNormal = hit.normal;
            }
        }

        smoothGroundNormal = Vector3.Slerp(
            smoothGroundNormal,
            groundNormal,
            15f * Time.fixedDeltaTime
        );

        if (isGrounded && !wasGrounded)
            hasJumpedSinceGrounded = false;
    }

    void ApplyLocalGravity()
    {
        if (isGrounded)
        {
            Vector3 gravityDir = -smoothGroundNormal;
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, smoothGroundNormal).normalized;

            Vector3 slopeForce = downhill * slopeGravity;
            Vector3 stickForce = gravityDir * stickToGroundGravity;

            rb.AddForce(slopeForce + stickForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * airGravity, ForceMode.Acceleration);
        }
    }

    void ApplyGroundMovement()
    {
        Vector3 vel = rb.linearVelocity;

        // velocity on slope plane
        Vector3 slopeNormal = smoothGroundNormal;
        Vector3 velOnPlane = Vector3.ProjectOnPlane(vel, slopeNormal);
        float planeSpeed = velOnPlane.magnitude;

        // keep a board direction similar to old BordDirection
        if (planeSpeed > 0.5f)
        {
            rideDirection = velOnPlane.normalized;
        }
        else
        {
            rideDirection = Vector3.ProjectOnPlane(rideDirection, slopeNormal).normalized;
            if (rideDirection.sqrMagnitude < 0.1f)
                rideDirection = Vector3.ProjectOnPlane(transform.forward, slopeNormal).normalized;
        }

        bool isBraking = verticalInput < -0.1f;

        // turn by rotating around slope normal (can’t steer while braking)
        if (!isBraking && Mathf.Abs(horizontalInput) > 0.01f && planeSpeed > 0.1f)
        {
            float turnAngle = horizontalInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRot = Quaternion.AngleAxis(turnAngle, slopeNormal);
            rideDirection = (turnRot * rideDirection).normalized;
            velOnPlane = turnRot * velOnPlane;
        }

        // basis along board and sideways
        Vector3 forwardTangent = rideDirection;
        Vector3 rightTangent = Vector3.Cross(slopeNormal, forwardTangent).normalized;

        float forwardSpeed = Vector3.Dot(velOnPlane, forwardTangent);
        float sideSpeed = Vector3.Dot(velOnPlane, rightTangent);
        float normalSpeed = Vector3.Dot(vel, slopeNormal);

        // sideways friction (slide vs carve)
        float lateralFriction = baseGroundFriction;
        if (Mathf.Abs(horizontalInput) > 0.1f)
            lateralFriction += carveExtraFriction;
        if (isBraking)
            lateralFriction += brakeExtraFriction;

        float sideSign = Mathf.Sign(sideSpeed);
        float sideMag = Mathf.Abs(sideSpeed);
        float maxSideLoss = lateralFriction * Time.fixedDeltaTime;

        if (sideMag <= maxSideLoss)
            sideSpeed = 0f;
        else
            sideSpeed -= sideSign * maxSideLoss;

        // small forward drag so you don’t accelerate forever
        float forwardDrag = 0.5f * baseGroundFriction * Time.fixedDeltaTime;
        if (forwardSpeed > 0)
            forwardSpeed = Mathf.Max(0f, forwardSpeed - forwardDrag);
        else if (forwardSpeed < 0)
            forwardSpeed = Mathf.Min(0f, forwardSpeed + forwardDrag);

        // acceleration along board dir
        bool wantsAccelerate = alwaysAccelerate ? !isBraking : verticalInput > 0.1f;
        if (wantsAccelerate)
            forwardSpeed += pushAcceleration * Time.fixedDeltaTime;

        // active brake
        if (isBraking)
        {
            float brakeAmount = brakeStrength * Time.fixedDeltaTime;
            if (Mathf.Abs(forwardSpeed) <= brakeAmount)
                forwardSpeed = 0f;
            else
                forwardSpeed -= Mathf.Sign(forwardSpeed) * brakeAmount;
        }

        // rebuild velocity
        Vector3 newVelOnPlane = forwardTangent * forwardSpeed + rightTangent * sideSpeed;
        Vector3 newVel = newVelOnPlane + slopeNormal * normalSpeed;
        rb.linearVelocity = newVel;

        // orient body to board direction + slope
        Quaternion targetRot = Quaternion.LookRotation(forwardTangent, slopeNormal);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            bodyAlignLerp * Time.fixedDeltaTime
        );
    }

    void ApplyAirMovement()
    {
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            rb.AddTorque(Vector3.up * horizontalInput * airTurnSpeed, ForceMode.Acceleration);
        }
    }

    void HandleJump()
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (!isGrounded || hasJumpedSinceGrounded)
            return;

        hasJumpedSinceGrounded = true;

        // flatten vertical so jump height is consistent
        Vector3 vel = rb.linearVelocity;
        vel.y = 0;
        rb.linearVelocity = vel;

        Vector3 jumpDir = (Vector3.up + groundNormal).normalized;
        rb.AddForce(jumpDir * jumpForce, ForceMode.VelocityChange);
    }

    void ClampMaxSpeed()
    {
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;
        if (speed > maxSpeed)
        {
            rb.linearVelocity = vel * (maxSpeed / speed);
        }
    }
}
