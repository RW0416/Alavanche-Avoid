using UnityEngine;
using UnityEngine.InputSystem;

public class SnowboarderTricks : MonoBehaviour
{
    [Header("references")]
    public SnowboarderController controller;

    [Header("visual orientation fix")]
    [Tooltip("rotate the character mesh 90 or -90 degrees so the board faces downhill")]
    public float modelRotationOffset = -90f;

    [Header("visual turn / lean")]
    public float maxTurnYaw = 35f;
    public float leanLerp = 15f;

    [Header("trick speeds")]
    public float flipSpeed = 360f;
    public float spinSpeed = 360f;

    [Header("landing upright")]
    public float uprightRecoverySpeed = 720f;

    [Header("brake pose")]
    [Tooltip("degrees to yaw the board sideways when braking")]
    public float brakeYawAngle = 90f;
    public float brakePoseLerp = 10f;

    Transform visualRoot;

    // input
    Vector2 moveInput;
    bool jumpHeld;

    // trick state
    bool trickArmed;
    bool inJump;

    float flipAngle;
    float spinAngle;

    // scoring accumulators
    float frontFlipAccum;
    float backFlipAccum;
    float spinAccum;

    float lastFlipAngle;
    float lastSpinAngle;

    bool prevGrounded;
    Quaternion jumpBaseRot;

    float currentBrakeYaw;

    void Awake()
    {
        if (!controller)
            controller = GetComponent<SnowboarderController>();

        visualRoot = controller != null ? controller.visualRoot : null;
        if (!visualRoot)
        {
            Debug.LogError("SnowboarderTricks: visualRoot not set. assign the mesh child in the controller.");
            enabled = false;
        }
    }

    // from PlayerInput (Move)
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // from PlayerInput (Jump)
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpHeld = true;

            if (inJump && !controller.IsGrounded)
                trickArmed = true;
        }
        else if (context.canceled)
        {
            jumpHeld = false;
            trickArmed = false;
        }
    }

    void Update()
    {
        if (!controller || !visualRoot)
            return;

        float dt = Time.deltaTime;
        bool grounded = controller.IsGrounded;
        bool justLanded = grounded && !prevGrounded;
        bool justLeftGround = !grounded && prevGrounded;

        // transitions
        if (justLeftGround)
        {
            inJump = true;
            trickArmed = false;

            flipAngle = 0f;
            spinAngle = 0f;
            frontFlipAccum = 0f;
            backFlipAccum = 0f;
            spinAccum = 0f;
            lastFlipAngle = 0f;
            lastSpinAngle = 0f;

            // store base rotation for trick axes
            jumpBaseRot = controller.transform.rotation;
        }

        if (justLanded)
        {
            inJump = false;
            trickArmed = false;

            // clamp crazy values to avoid snap spins
            flipAngle %= 360f;
            spinAngle %= 360f;

            if (flipAngle > 180f) flipAngle -= 360f;
            if (flipAngle < -180f) flipAngle += 360f;
            if (spinAngle > 180f) spinAngle -= 360f;
            if (spinAngle < -180f) spinAngle += 360f;
        }

        // air tricks
        if (!grounded && inJump && trickArmed && jumpHeld)
        {
            float h = moveInput.x;
            float v = moveInput.y;
            bool wantFlip = Mathf.Abs(v) > 0.1f;
            bool wantSpin = Mathf.Abs(h) > 0.1f;

            // prefer spin if both pressed
            if (wantSpin)
            {
                spinAngle += spinSpeed * h * dt;
            }
            else if (wantFlip)
            {
                // pull back = backflip, push forward = frontflip
                if (v > 0.1f) flipAngle += flipSpeed * dt;
                else flipAngle -= flipSpeed * dt;
            }
        }

        // scoring
        if (!grounded && inJump)
        {
            float deltaFlip = flipAngle - lastFlipAngle;
            float deltaSpin = spinAngle - lastSpinAngle;

            if (deltaFlip > 0f) frontFlipAccum += deltaFlip;
            else backFlipAccum += -deltaFlip;

            spinAccum += Mathf.Abs(deltaSpin);

            while (frontFlipAccum >= 360f) { frontFlipAccum -= 360f; AwardScore(15); }
            while (backFlipAccum  >= 360f) { backFlipAccum  -= 360f; AwardScore(10); }
            while (spinAccum      >= 360f) { spinAccum      -= 360f; AwardScore(5);  }

            lastFlipAngle = flipAngle;
            lastSpinAngle = spinAngle;
        }

        // landing recovery
        if (grounded)
        {
            flipAngle = Mathf.MoveTowardsAngle(flipAngle, 0f, uprightRecoverySpeed * dt);
            spinAngle = Mathf.MoveTowardsAngle(spinAngle, 0f, uprightRecoverySpeed * dt);
        }

        prevGrounded = grounded;
        UpdateVisualRotation(dt);
    }

    void UpdateVisualRotation(float dt)
    {
        // base physics rotation
        Quaternion baseRot = controller.transform.rotation;

        // trick axes
        Quaternion trickRefRot = inJump ? jumpBaseRot : baseRot;
        Vector3 trickRight = trickRefRot * Vector3.right;
        Vector3 trickUp = trickRefRot * Vector3.up;

        // turn lean from input (only when not tricking)
        float turnYaw = (controller.IsGrounded && !inJump) ? moveInput.x * maxTurnYaw : 0f;
        Quaternion turnRot = Quaternion.AngleAxis(turnYaw, baseRot * Vector3.up);

        // brake pose – yaw board sideways when braking
        float targetBrakeYaw = 0f;
        if (controller.IsGrounded && controller.IsBraking)
        {
            // decide which way to point; use input if there is any, else default to left
            float side = Mathf.Abs(moveInput.x) > 0.1f ? Mathf.Sign(moveInput.x) : 1f;
            targetBrakeYaw = brakeYawAngle * side;
        }

        currentBrakeYaw = Mathf.Lerp(currentBrakeYaw, targetBrakeYaw, brakePoseLerp * dt);
        Vector3 upAxis = baseRot * Vector3.up;
        Quaternion brakeRot = Quaternion.AngleAxis(currentBrakeYaw, upAxis);

        // trick rotations
        Quaternion flipRot = Quaternion.AngleAxis(flipAngle, trickRight);
        Quaternion spinRot = Quaternion.AngleAxis(spinAngle, trickUp);

        // mesh orientation offset
        Quaternion offsetRot = Quaternion.Euler(0f, modelRotationOffset, 0f);

        // final
        Quaternion finalRot = baseRot * brakeRot * turnRot * spinRot * flipRot * offsetRot;
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, finalRot, leanLerp * dt);
    }

    void AwardScore(int amount)
    {
        if (ScoreManager.Instance != null && controller != null)
            ScoreManager.Instance.AddScore(amount, controller.transform);
    }
}
