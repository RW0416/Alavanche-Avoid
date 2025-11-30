using UnityEngine;
using UnityEngine.InputSystem;

public class SnowboarderTricks : MonoBehaviour
{
    [Header("References")]
    public SnowboarderController controller;

    [Header("Visual Orientation Fix")]
    [Tooltip("Rotate the character mesh 90 or -90 degrees so the board faces downhill.")]
    public float modelRotationOffset = -90f;

    [Header("Visual Turn / Lean (No Extra Yaw)")]
    [Tooltip("How much the rider leans when turning left/right.")]
    public float maxTurnYaw = 25f;
    public float leanLerp = 15f;

    [Header("Trick Speeds")]
    public float flipSpeed = 360f;   // degrees per second, front/back
    public float spinSpeed = 360f;   // degrees per second, side spin

    [Header("Landing Upright")]
    public float uprightRecoverySpeed = 720f;
    [Tooltip("How fast to blend back to a flat orientation on the slope after landing.")]
    public float uprightOnSlopeLerp = 10f;

    [Header("Brake Pose")]
    [Tooltip("Degrees to yaw the board sideways when braking.")]
    public float brakeYawAngle = 90f;
    [Tooltip("How fast to blend in/out the brake yaw.")]
    public float brakePoseLerp = 10f;
    [Tooltip("Extra lean angle (deg) when braking, independent of turn input.")]
    public float brakeBackLeanAngle = 20f;

    [Header("Scoring")]
    [Range(0.1f, 1f)]
    [Tooltip("Fraction of a full 360° rotation required before awarding points (e.g. 0.7 = 70% of a rotation).")]
    public float rotationScoreThreshold = 0.7f;
    public int frontFlipScore = 15;
    public int backFlipScore = 10;
    public int spinScore = 5;

    Transform visualRoot;

    // input
    Vector2 moveInput;
    bool jumpHeld;          // only true when you press space in the air (for tricks)

    // state
    bool inJump;
    bool prevGrounded;

    // braking state for stable pose
    bool prevBraking;
    float brakeSide = 1f;   // +1 = fixed brake orientation

    // trick angles (local)
    float flipAngle;        // front/back flip around local X
    float spinAngle;        // side spin around local Y

    // scoring accumulators (degrees)
    float frontFlipAccum;
    float backFlipAccum;
    float spinAccum;

    float lastFlipAngle;
    float lastSpinAngle;

    float currentBrakeYaw;

    void Awake()
    {
        if (!controller)
            controller = GetComponent<SnowboarderController>();

        visualRoot = controller != null ? controller.visualRoot : null;
        if (!visualRoot)
        {
            Debug.LogError("SnowboarderTricks: visualRoot not set. Assign the mesh child in the controller.");
            enabled = false;
        }
    }

    // from PlayerInput (Move)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // from PlayerInput (Jump) – ONLY for tricks now
    // First space press (on ground) is handled by SnowboarderController.
    // Pressing space again in the air arms tricks.
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (inJump && !controller.IsGrounded)
            {
                // only start tricks if we're already in the air
                jumpHeld = true;
            }
        }
        else if (context.canceled)
        {
            jumpHeld = false;
        }
    }

    void Update()
    {
        if (!controller || !visualRoot)
            return;

        float dt = Time.deltaTime;
        bool grounded = controller.IsGrounded;
        bool justLeftGround = !grounded && prevGrounded;
        bool justLanded = grounded && !prevGrounded;
        bool braking = grounded && controller.IsBraking;

        // --- brake start / end: lock a stable side (always same) ---
        if (braking && !prevBraking)
        {
            // Always use the same orientation for brake pose.
            // This makes S, S+D, and A then S all end up in the SAME pose.
            brakeSide = 1f;
        }
        prevBraking = braking;

        // --- jump transitions ---
        if (justLeftGround)
        {
            inJump = true;

            // tricks only start on another space press in air
            jumpHeld = false;

            // reset trick angles and scoring each jump
            flipAngle = 0f;
            spinAngle = 0f;

            frontFlipAccum = 0f;
            backFlipAccum = 0f;
            spinAccum = 0f;

            lastFlipAngle = 0f;
            lastSpinAngle = 0f;
        }

        if (justLanded)
        {
            inJump = false;
            jumpHeld = false;
        }

        // --- trick control in air ---
        if (inJump && !grounded && jumpHeld)
        {
            float h = moveInput.x;
            float v = moveInput.y;

            bool wantSpin = Mathf.Abs(h) > 0.25f;
            bool wantFlip = Mathf.Abs(v) > 0.25f;

            // prefer spin if stronger sideways input
            if (wantSpin && Mathf.Abs(h) >= Mathf.Abs(v))
            {
                // side spins (around local Y)
                spinAngle += spinSpeed * h * dt;
            }
            else if (wantFlip)
            {
                // front/back flips (around local X)
                // v > 0 = frontflip, v < 0 = backflip
                flipAngle += flipSpeed * v * dt;
            }
        }

        // --- scoring while in air ---
        if (inJump && !grounded)
        {
            float deltaFlip = flipAngle - lastFlipAngle;
            float deltaSpin = spinAngle - lastSpinAngle;

            if (deltaFlip > 0f)
                frontFlipAccum += deltaFlip;
            else
                backFlipAccum += -deltaFlip;

            spinAccum += Mathf.Abs(deltaSpin);

            float threshold = 360f * rotationScoreThreshold;

            if (frontFlipAccum >= threshold)
            {
                int count = Mathf.FloorToInt(frontFlipAccum / threshold);
                frontFlipAccum -= threshold * count;
                AwardScore(frontFlipScore * count);
            }

            if (backFlipAccum >= threshold)
            {
                int count = Mathf.FloorToInt(backFlipAccum / threshold);
                backFlipAccum -= threshold * count;
                AwardScore(backFlipScore * count);
            }

            if (spinAccum >= threshold)
            {
                int count = Mathf.FloorToInt(spinAccum / threshold);
                spinAccum -= threshold * count;
                AwardScore(spinScore * count);
            }

            lastFlipAngle = flipAngle;
            lastSpinAngle = spinAngle;
        }

        // --- recover upright when on ground ---
        if (grounded && !inJump)
        {
            // bring trick angles back towards neutral
            flipAngle = Mathf.MoveTowardsAngle(flipAngle, 0f, uprightRecoverySpeed * dt);
            spinAngle = Mathf.MoveTowardsAngle(spinAngle, 0f, uprightRecoverySpeed * dt);

            lastFlipAngle = flipAngle;
            lastSpinAngle = spinAngle;
        }

        prevGrounded = grounded;

        UpdateVisualRotation(dt, grounded, braking);
    }

    void UpdateVisualRotation(float dt, bool grounded, bool braking)
    {
        Quaternion baseRot = controller.transform.rotation;

        // 1) turn lean: only when NOT braking
        float targetLean = 0f;
        if (grounded && !inJump && !braking)
        {
            // regular carve lean (A/D)
            targetLean = -moveInput.x * maxTurnYaw;
        }
        else if (grounded && braking)
        {
            // separate brake lean – lean back instead of forward
            targetLean = -brakeBackLeanAngle;
        }
        Quaternion leanRot = Quaternion.AngleAxis(targetLean, Vector3.forward);

        // 2) brake pose yaw – fixed side while braking, independent of A/D
        float targetBrakeYaw = 0f;
        if (grounded && braking)
        {
            targetBrakeYaw = brakeYawAngle * brakeSide;
        }

        currentBrakeYaw = Mathf.Lerp(currentBrakeYaw, targetBrakeYaw, brakePoseLerp * dt);
        Quaternion brakeRot = Quaternion.AngleAxis(currentBrakeYaw, Vector3.up);

        // 3) trick rotations (local)
        Quaternion flipRot = Quaternion.AngleAxis(flipAngle, Vector3.right);
        Quaternion spinRot = Quaternion.AngleAxis(spinAngle, Vector3.up);
        Quaternion trickRot = spinRot * flipRot;

        // 4) mesh orientation fix
        Quaternion offsetRot = Quaternion.Euler(0f, modelRotationOffset, 0f);

        // base final rotation: physics → brake → lean → tricks → mesh offset
        Quaternion targetRot = baseRot * brakeRot * leanRot * trickRot * offsetRot;

        // 5) extra upright correction on slope when grounded (keeps you horizontal, not nose-diving)
        if (grounded && !inJump)
        {
            Vector3 slopeUp = controller.GroundNormal;
            Vector3 currentForward = targetRot * Vector3.forward;

            // flatten forward onto slope plane so you end up lying "horizontally" on the slope
            Vector3 flatForward = Vector3.ProjectOnPlane(currentForward, slopeUp);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                // fallback: use right axis if forward is nearly vertical
                flatForward = Vector3.ProjectOnPlane(targetRot * Vector3.right, slopeUp);
            }

            if (flatForward.sqrMagnitude > 0.0001f)
            {
                Quaternion uprightOnSlope = Quaternion.LookRotation(flatForward.normalized, slopeUp);
                targetRot = Quaternion.Slerp(targetRot, uprightOnSlope, uprightOnSlopeLerp * dt);
            }
        }

        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            targetRot,
            leanLerp * dt
        );
    }

    void AwardScore(int amount)
    {
        if (ScoreManager.Instance != null && controller != null)
            ScoreManager.Instance.AddScore(amount, controller.transform);
    }
}
