using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SnowboarderController : MonoBehaviour
{
    [Header("references")]
    public Transform visualRoot;
    public LayerMask groundMask = ~0;

    [Header("speed mod")]
    float baseMaxSpeed;
    float basePushAcceleration;
    float baseBoostAcceleration;

    [Header("local gravity")]
    public float slopeGravity = 25f;
    public float stickToGroundGravity = 40f;
    public float airGravity = 30f;

    [Header("ground movement")]
    public bool alwaysAccelerate = true;
    public float pushAcceleration = 12f;
    public float maxSpeed = 35f;
    public float baseGroundFriction = 5f;
    public float carveExtraFriction = 8f;
    public float brakeExtraFriction = 16f;

    [Header("brake / boost")]
    public float brakeStrength = 18f;
    public float boostAcceleration = 20f;
    public float boostMaxSpeedMultiplier = 1.2f;

    [Header("turning / carving")]
    public float turnSpeed = 90f;
    public float bodyAlignLerp = 8f;
    
    [Header("visual turning")]
    [Tooltip("Extra visual yaw when carving left/right (degrees).")]
    public float visualTurnYawDegrees = 15f;

    [Header("air control")]
    public float airTurnSpeed = 2f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.4f;

    [Header("collision / hit")]
    [Tooltip("speed * factor after hit")]
    public float hitSlowFactor = 0.4f;
    public float hitMinSpeedAfter = 5f;
    public GameObject hitEffectPrefab;
    public float hitEffectLifetime = 3f;
    public float hitCooldown = 0.3f;

    float lastHitTime = -999f;


    Rigidbody rb;

    public ThirdPersonCamera tpCamera;

    // input
    Vector2 moveInput;
    float horizontalInput;
    float verticalInput;
    bool boostHeld;

    // ground state
    bool isGrounded;
    bool wasGrounded;
    Vector3 groundNormal = Vector3.up;
    Vector3 smoothGroundNormal = Vector3.up;

    // jump
    bool jumpRequested;
    bool hasJumpedSinceGrounded;

    // board direction (for carving)
    Vector3 rideDirection = Vector3.forward;

    // public for other scripts
    public bool IsGrounded => isGrounded;
    public bool WasGrounded => wasGrounded;
    public Vector3 GroundNormal => groundNormal;
    public Vector2 MoveInput => moveInput;
    public float HorizontalInput => horizontalInput;
    public float VerticalInput => verticalInput;
    public Rigidbody Body => rb;
    public bool IsBraking { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        rb.linearDamping = 0.0f;
        rb.angularDamping = 0.05f;

        rideDirection = transform.forward;

        // === 记录基础值 ===
        baseMaxSpeed = maxSpeed;
        basePushAcceleration = pushAcceleration;
        baseBoostAcceleration = boostAcceleration;

        // === 应用升级（如果有） ===
        ApplySpeedUpgrades();
    }

    public void ApplySpeedUpgrades()
    {
        if (GameProgress.Instance == null)
            return;

        float mul = GameProgress.Instance.GetSpeedMultiplier();

        maxSpeed = baseMaxSpeed * mul;
        pushAcceleration = basePushAcceleration * mul;
        boostAcceleration = baseBoostAcceleration * mul;
    }


    // called from PlayerInput (Move action)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // called from PlayerInput (Jump action)
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
            jumpRequested = true;
    }

    // called from PlayerInput (Boost action, e.g. left shift)
    public void OnBoost(InputAction.CallbackContext context)
    {
        if (context.performed)
            boostHeld = true;
        else if (context.canceled)
            boostHeld = false;
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
        float castDistance = col ? col.bounds.extents.y + groundCheckDistance : 1.5f;

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
        Vector3 slopeNormal = smoothGroundNormal;

        Vector3 velOnPlane = Vector3.ProjectOnPlane(vel, slopeNormal);
        float planeSpeed = velOnPlane.magnitude;

        // keep a stable board direction
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

        bool braking = verticalInput < -0.1f && planeSpeed > 0.1f;
        IsBraking = braking;

        // turn around slope normal (no steer while braking)
        if (!braking && Mathf.Abs(horizontalInput) > 0.01f && planeSpeed > 0.1f)
        {
            float turnAngle = horizontalInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRot = Quaternion.AngleAxis(turnAngle, slopeNormal);
            rideDirection = (turnRot * rideDirection).normalized;
            velOnPlane = turnRot * velOnPlane;
        }

        Vector3 forwardTangent = rideDirection;
        Vector3 rightTangent = Vector3.Cross(slopeNormal, forwardTangent).normalized;

        float forwardSpeed = Vector3.Dot(velOnPlane, forwardTangent);
        float sideSpeed = Vector3.Dot(velOnPlane, rightTangent);
        float normalSpeed = Vector3.Dot(vel, slopeNormal);

        // sideways friction
        float lateralFriction = baseGroundFriction;
        if (Mathf.Abs(horizontalInput) > 0.1f)
            lateralFriction += carveExtraFriction;
        if (braking)
            lateralFriction += brakeExtraFriction;

        float sideSign = Mathf.Sign(sideSpeed);
        float sideMag = Mathf.Abs(sideSpeed);
        float maxSideLoss = lateralFriction * Time.fixedDeltaTime;

        if (sideMag <= maxSideLoss)
            sideSpeed = 0f;
        else
            sideSpeed -= sideSign * maxSideLoss;

        // small forward drag
        float forwardDrag = 0.5f * baseGroundFriction * Time.fixedDeltaTime;
        if (forwardSpeed > 0f)
            forwardSpeed = Mathf.Max(0f, forwardSpeed - forwardDrag);
        else if (forwardSpeed < 0f)
            forwardSpeed = Mathf.Min(0f, forwardSpeed + forwardDrag);

        // auto push down the hill
        bool wantsAccelerate = alwaysAccelerate ? !braking : verticalInput > 0.1f;
        if (wantsAccelerate)
            forwardSpeed += pushAcceleration * Time.fixedDeltaTime;

        // boost (shift)
        if (boostHeld && !braking)
            forwardSpeed += boostAcceleration * Time.fixedDeltaTime;

        // active brake (s)
        if (braking)
        {
            float brakeAmount = brakeStrength * Time.fixedDeltaTime;
            if (Mathf.Abs(forwardSpeed) <= brakeAmount)
                forwardSpeed = 0f;
            else
                forwardSpeed -= Mathf.Sign(forwardSpeed) * brakeAmount;
        }

        Vector3 newVelOnPlane = forwardTangent * forwardSpeed + rightTangent * sideSpeed;
        Vector3 newVel = newVelOnPlane + slopeNormal * normalSpeed;
        rb.linearVelocity = newVel;

        // rotate body to board + slope, with a bit of extra yaw when carving
        Vector3 displayForward = forwardTangent;

        if (!braking && Mathf.Abs(horizontalInput) > 0.01f && planeSpeed > 0.1f && visualTurnYawDegrees > 0f)
        {
            float yawAngle = horizontalInput * visualTurnYawDegrees;
            Quaternion yawRot = Quaternion.AngleAxis(yawAngle, slopeNormal);
            displayForward = (yawRot * forwardTangent).normalized;
        }

        Quaternion targetRot = Quaternion.LookRotation(displayForward, slopeNormal);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            bodyAlignLerp * Time.fixedDeltaTime
        );

    }

    void ApplyAirMovement()
    {
    }

    void HandleJump()
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (!isGrounded || hasJumpedSinceGrounded)
            return;

        hasJumpedSinceGrounded = true;

        // stop any rotation from carrying into the air
        rb.angularVelocity = Vector3.zero;

        // keep only the velocity along the slope when we jump
        Vector3 vel = rb.linearVelocity;
        vel = Vector3.ProjectOnPlane(vel, groundNormal);
        rb.linearVelocity = vel;

        Vector3 jumpDir = (Vector3.up + groundNormal).normalized;
        rb.AddForce(jumpDir * jumpForce, ForceMode.VelocityChange);
    }


    void ClampMaxSpeed()
    {
        float currentMax = maxSpeed * (boostHeld ? boostMaxSpeedMultiplier : 1f);
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        if (speed > currentMax)
            rb.linearVelocity = vel * (currentMax / speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle"))
            return;

        if (Time.time < lastHitTime + hitCooldown)
            return;

        lastHitTime = Time.time;

        HandleObstacleHit(other);
    }

    void HandleObstacleHit(Collider obstacle)
    {
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        if (speed > 0.1f)
        {
            float newSpeed = Mathf.Max(speed * hitSlowFactor, hitMinSpeedAfter);
            Vector3 newVel = vel.normalized * newSpeed;
            rb.linearVelocity = newVel;
        }

        Vector3 hitPos = obstacle.ClosestPoint(transform.position);

        if (hitEffectPrefab != null)
        {
            var fx = Instantiate(
                hitEffectPrefab,
                hitPos,
                Quaternion.identity
            );
            if (hitEffectLifetime > 0f)
            {
                Destroy(fx, hitEffectLifetime);
            }
        }

        Destroy(obstacle.gameObject);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (tpCamera != null)
            tpCamera.OnLook(context);
    }

}
