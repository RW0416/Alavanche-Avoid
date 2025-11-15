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

    // trick state (continuous)
    bool trickArmed;      // we pressed Space while already in the air
    float flipAngle;      // rotation around board right axis (front/back flip)
    float spinAngle;      // rotation around board up axis   (flat spin)

    bool prevGrounded;

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
    }

    // same Move input as controller
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // SPACE:
    // - first press on ground: jump (controller handles it); we just record jumpHeld
    // - press that STARTS while in the air: arms tricks
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpHeld = true;

            // only arm tricks if this press begins while not grounded
            if (!controller.IsGrounded)
            {
                trickArmed = true;
            }
        }
        else if (context.canceled)
        {
            jumpHeld = false;
            // releasing Space cancels trick prep; need a new mid-air press
            trickArmed = false;
        }
    }

    void Update()
    {
        if (!controller || !visualRoot) return;

        horizontalInput = moveInput.x;
        verticalInput   = moveInput.y;

        bool grounded      = controller.IsGrounded;
        bool justLanded    = grounded && !prevGrounded;
        bool justLeftGround = !grounded && prevGrounded;
        float dt           = Time.deltaTime;

        // ---------- JUST LEFT GROUND: clean neutral if no tricks ----------
        if (justLeftGround)
        {
            // start each jump from a clean state unless you actively do a trick
            flipAngle = 0f;
            spinAngle = 0f;
        }

        // ---------- IN AIR: manual flips & spins (two-step: must be armed) ----------
        if (!grounded && trickArmed && jumpHeld)
        {
            bool wantFlip = Mathf.Abs(verticalInput)   > 0.1f;
            bool wantSpin = Mathf.Abs(horizontalInput) > 0.1f;

            if (wantFlip && !wantSpin)
            {
                // PURE FLIP: lock spin, only front/back
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
                // PURE FLAT SPIN: lock flip, only yaw
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
                // no directional input while armed: DO NOTHING
                // keep current flipAngle / spinAngle exactly as they are
            }
        }
        // if not armed or not holding space, we don't touch angles in air at all

        // ---------- LANDING: smooth recovery to upright (shortest path) ----------
        if (justLanded)
        {
            // this jump is over; next trick needs a new air press
            trickArmed = false;
        }

        if (grounded)
        {
            // smoothly recover to upright along shortest path, only while on ground
            flipAngle = Mathf.MoveTowardsAngle(flipAngle, 0f, uprightRecoverySpeed * dt);
            spinAngle = Mathf.MoveTowardsAngle(spinAngle, 0f, uprightRecoverySpeed * dt);
        }

        prevGrounded = grounded;

        UpdateVisualRotation(dt);
    }

    void UpdateVisualRotation(float dt)
    {
        if (!controller || !visualRoot) return;

        // base orientation from physics object
        Quaternion baseRot = controller.transform.rotation;

        // small yaw twist into the turn on the ground only (no weird tilting)
        float turnYaw = controller.IsGrounded ? horizontalInput * maxTurnYaw : 0f;

        // board local axes in world space
        Vector3 axisRight = baseRot * Vector3.right;   // flip axis
        Vector3 axisUp    = baseRot * Vector3.up;      // yaw/spin axis

        // visual "turn into carve" – yaw only
        Quaternion turnRot = Quaternion.AngleAxis(turnYaw, axisUp);

        // tricks
        Quaternion flipRot = Quaternion.AngleAxis(flipAngle, axisRight);
        Quaternion spinRot = Quaternion.AngleAxis(spinAngle, axisUp);

        // order: base → turn → spin → flip
        Quaternion targetRot = baseRot * turnRot * spinRot * flipRot;

        visualRoot.rotation = Quaternion.Slerp(
            visualRoot.rotation,
            targetRot,
            leanLerp * dt
        );
    }
}
