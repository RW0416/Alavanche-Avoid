using UnityEngine;
using UnityEngine.InputSystem;

public class SnowboarderTricks : MonoBehaviour
{
    [Header("References")]
    public SnowboarderController controller;   // root with rigidbody + movement

    [Header("Visual Turn / Lean")]
    public float maxTurnYaw = 25f;     // visual twist into the turn (deg)
    public float leanLerp = 10f;       // smoothing for all visual rotations

    [Header("Trick Speeds")]
    public float flipSpeed = 360f;     // deg/sec for W/S flips
    public float spinSpeed = 360f;     // deg/sec for A/D spins

    [Header("Landing Upright")]
    public float uprightRecoverySpeed = 540f;  // deg/sec to recover to upright while grounded

    Transform visualRoot;

    // input
    Vector2 moveInput;
    float horizontalInput;
    float verticalInput;
    bool jumpHeld;        // is Space currently held?

    // trick state
    bool trickArmed;      // we pressed Space while already in the air (this jump)
    bool inJump;          // currently airborne since last time we were grounded

    float flipAngle;      // rotation around board right axis (front/back flip)
    float spinAngle;      // rotation around board up axis   (flat spin)

    // scoring accumulators for this jump
    float frontFlipAccum;   // positive flip degrees
    float backFlipAccum;    // negative flip degrees (stored positive)
    float spinAccum;        // absolute spin degrees

    float lastFlipAngle;
    float lastSpinAngle;

    bool prevGrounded;

    // orientation captured at jump start – used for flip/spin axes
    Quaternion jumpBaseRot;

    void Awake()
    {
        if (!controller)
            controller = GetComponent<SnowboarderController>();

        if (!controller)
        {
            Debug.LogError("SnowboarderTricks: no SnowboarderController assigned.");
            enabled = false;
            return;
        }

        visualRoot = controller.visualRoot;
        if (!visualRoot)
        {
            Debug.LogError("SnowboarderTricks: controller.visualRoot is not set.");
            enabled = false;
        }

        jumpBaseRot = controller.transform.rotation;
    }

    // same Move input as controller
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // SPACE:
    // - first press on ground: jump (controller handles it); we just record jumpHeld
    // - press that STARTS while in the air: arms tricks for THIS jump
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpHeld = true;

            // only arm tricks if this press begins while in the air during a jump
            if (inJump && !controller.IsGrounded)
            {
                trickArmed = true;
            }
        }
        else if (context.canceled)
        {
            jumpHeld = false;
            // releasing Space cancels trick prep; need a new mid-air press next time
            trickArmed = false;
        }
    }

    void Update()
    {
        if (!controller || !visualRoot) return;

        horizontalInput = moveInput.x;
        verticalInput   = moveInput.y;

        bool grounded        = controller.IsGrounded;
        bool justLanded      = grounded && !prevGrounded;
        bool justLeftGround  = !grounded && prevGrounded;
        float dt             = Time.deltaTime;

        // ---------- JUMP STATE TRANSITIONS ----------

        if (justLeftGround)
        {
            // New jump starts here: reset EVERYTHING so this jump is fresh.
            inJump = true;
            trickArmed = false;   // must press Space again in air to arm

            flipAngle = 0f;
            spinAngle = 0f;

            frontFlipAccum = 0f;
            backFlipAccum  = 0f;
            spinAccum      = 0f;

            lastFlipAngle = 0f;
            lastSpinAngle = 0f;

            // capture facing at takeoff – tricks stay aligned to this
            jumpBaseRot = controller.transform.rotation;

            // make sure visuals start aligned with the board for this jump
            visualRoot.rotation = jumpBaseRot;
        }

        if (justLanded)
        {
            // This jump is over. Any new tricks require a new mid-air press next time.
            inJump = false;
            trickArmed = false;
        }

        // ---------- IN AIR: manual flips & spins (two-step: must be armed) ----------

        float prevFlip = flipAngle;
        float prevSpin = spinAngle;

        if (!grounded && inJump && trickArmed && jumpHeld)
        {
            bool wantFlip = Mathf.Abs(verticalInput)   > 0.1f;
            bool wantSpin = Mathf.Abs(horizontalInput) > 0.1f;

            if (wantFlip && !wantSpin)
            {
                // PURE FLIP: lock spin, only front/back (relative to facing at jump)
                spinAngle = 0f;

                if (verticalInput > 0.1f)
                {
                    flipAngle += flipSpeed * dt;      // front flip
                }
                else if (verticalInput < -0.1f)
                {
                    flipAngle -= flipSpeed * dt;      // back flip
                }
            }
            else if (wantSpin && !wantFlip)
            {
                // PURE FLAT SPIN: lock flip, only yaw around facing at jump
                flipAngle = 0f;
                spinAngle += spinSpeed * horizontalInput * dt;
            }
            else if (wantSpin && wantFlip)
            {
                // both pressed: treat as SPIN priority, keep it flat
                flipAngle = 0f;
                spinAngle += spinSpeed * horizontalInput * dt;
            }
            else
            {
                // armed + Space held but no directional input:
                // DO NOTHING, keep current angles exactly as they are
            }
        }
        // if not inJump, not armed, or not holding Space → we do not touch angles in air

        // ---------- SCORING (per jump) ----------

        if (!grounded && inJump && trickArmed && jumpHeld)
        {
            float deltaFlip = flipAngle - lastFlipAngle;
            float deltaSpin = spinAngle - lastSpinAngle;

            if (deltaFlip > 0f)
            {
                frontFlipAccum += deltaFlip;
            }
            else if (deltaFlip < 0f)
            {
                backFlipAccum += -deltaFlip;
            }

            spinAccum += Mathf.Abs(deltaSpin);

            // award per full 360
            while (frontFlipAccum >= 360f)
            {
                frontFlipAccum -= 360f;
                AwardScore(15);   // front flip
            }

            while (backFlipAccum >= 360f)
            {
                backFlipAccum -= 360f;
                AwardScore(10);   // back flip
            }

            while (spinAccum >= 360f)
            {
                spinAccum -= 360f;
                AwardScore(5);    // flat spin
            }
        }

        lastFlipAngle = flipAngle;
        lastSpinAngle = spinAngle;

        // ---------- GROUND: smooth back to upright only while grounded ----------

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
        if (!controller || !visualRoot) return;

        // base orientation from physics object (for where the board actually is)
        Quaternion baseRot = controller.transform.rotation;

        // axes for tricks are locked to facing at jump start, not travel direction
        Vector3 trickRight = (inJump ? (jumpBaseRot * Vector3.right) : (baseRot * Vector3.right));
        Vector3 trickUp    = (inJump ? (jumpBaseRot * Vector3.up)    : (baseRot * Vector3.up));

        // small yaw twist into the turn on the ground only (no weird tilting)
        float turnYaw = controller.IsGrounded ? horizontalInput * maxTurnYaw : 0f;
        Quaternion turnRot = Quaternion.AngleAxis(turnYaw, baseRot * Vector3.up);

        // tricks
        Quaternion flipRot = Quaternion.AngleAxis(flipAngle, trickRight);
        Quaternion spinRot = Quaternion.AngleAxis(spinAngle, trickUp);

        // order: base → turn → spin → flip
        Quaternion targetRot = baseRot * turnRot * spinRot * flipRot;

        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            targetRot,
            leanLerp * dt
        );
    }

    void AwardScore(int amount)
    {
        if (ScoreManager.Instance == null) return;
        if (controller == null) return;

        ScoreManager.Instance.AddScore(amount, controller.transform);
    }
}
