using UnityEngine;
using UnityEngine.InputSystem;

public class SnowboarderTricks : MonoBehaviour
{
    [Header("References")]
    public SnowboarderController controller;

    [Header("Visual Orientation Fix")]
    [Tooltip("Rotate the character mesh 90 or -90 degrees so the board faces downhill.")]
    public float modelRotationOffset = -90f; 

    [Header("Visual Turn / Lean")]
    public float maxTurnYaw = 35f;     
    public float leanLerp = 15f;       // Smoother visual lerp

    [Header("Trick Speeds")]
    public float flipSpeed = 360f;     
    public float spinSpeed = 360f;     

    [Header("Landing Upright")]
    public float uprightRecoverySpeed = 720f;  // Fast recovery on landing

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

    void Awake()
    {
        if (!controller) controller = GetComponent<SnowboarderController>();
        
        visualRoot = controller.visualRoot;
        if (!visualRoot)
        {
            Debug.LogError("SnowboarderTricks: visualRoot is null. Assign the child object containing the mesh in the inspector.");
            enabled = false;
            return;
        }
        
        jumpBaseRot = controller.transform.rotation;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpHeld = true;
            if (inJump && !controller.IsGrounded)
            {
                trickArmed = true;
            }
        }
        else if (context.canceled)
        {
            jumpHeld = false;
            trickArmed = false;
        }
    }

    void Update()
    {
        if (!controller || !visualRoot) return;

        float dt = Time.deltaTime;
        bool grounded = controller.IsGrounded;
        bool justLanded = grounded && !prevGrounded;
        bool justLeftGround = !grounded && prevGrounded;

        // --- TRANSITIONS ---

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

            // Capture rotation at takeoff for trick axes
            jumpBaseRot = controller.transform.rotation;
        }

        if (justLanded)
        {
            inJump = false;
            trickArmed = false;
            
            // CRITICAL FIX: Prevent freak-out on landing.
            // Instantly snap angles closer to 0 or multiple of 360 so the visual lerp doesn't spin wildly.
            flipAngle %= 360f;
            spinAngle %= 360f;
            
            if (flipAngle > 180) flipAngle -= 360;
            if (flipAngle < -180) flipAngle += 360;
            if (spinAngle > 180) spinAngle -= 360;
            if (spinAngle < -180) spinAngle += 360;
        }

        // --- AIR TRICKS ---

        if (!grounded && inJump && trickArmed && jumpHeld)
        {
            float h = moveInput.x;
            float v = moveInput.y;
            bool wantFlip = Mathf.Abs(v) > 0.1f;
            bool wantSpin = Mathf.Abs(h) > 0.1f;

            // Prioritize inputs to avoid confusing diagonal rotation
            if (wantSpin)
            {
                spinAngle += spinSpeed * h * dt;
            }
            else if (wantFlip)
            {
                // Invert V input if needed for "pull back to backflip" feel
                if (v > 0.1f) flipAngle += flipSpeed * dt;
                else flipAngle -= flipSpeed * dt;
            }
        }

        // --- SCORING ---
        if (!grounded && inJump)
        {
            float deltaFlip = flipAngle - lastFlipAngle;
            float deltaSpin = spinAngle - lastSpinAngle;

            if (deltaFlip > 0) frontFlipAccum += deltaFlip;
            else backFlipAccum += -deltaFlip;
            
            spinAccum += Mathf.Abs(deltaSpin);

            while (frontFlipAccum >= 360f) { frontFlipAccum -= 360f; AwardScore(15); }
            while (backFlipAccum >= 360f) { backFlipAccum -= 360f; AwardScore(10); }
            while (spinAccum >= 360f) { spinAccum -= 360f; AwardScore(5); }
            
            lastFlipAngle = flipAngle;
            lastSpinAngle = spinAngle;
        }

        // --- LANDING RECOVERY ---
        if (grounded)
        {
            // Smoothly unwind tricks to 0
            flipAngle = Mathf.MoveTowardsAngle(flipAngle, 0f, uprightRecoverySpeed * dt);
            spinAngle = Mathf.MoveTowardsAngle(spinAngle, 0f, uprightRecoverySpeed * dt);
        }

        prevGrounded = grounded;
        UpdateVisualRotation(dt);
    }

    void UpdateVisualRotation(float dt)
    {
        // 1. Get Base Rotation (Physics facing)
        Quaternion baseRot = controller.transform.rotation;
        
        // 2. Determine Trick Axes
        // If in air, use the takeoff rotation so tricks don't wobble if physics body twists slightly
        Quaternion trickRefRot = inJump ? jumpBaseRot : baseRot;
        
        Vector3 trickRight = trickRefRot * Vector3.right;
        Vector3 trickUp    = trickRefRot * Vector3.up;

        // 3. Ground Turn Lean (only when not doing tricks)
        float turnYaw = (controller.IsGrounded && !inJump) ? moveInput.x * maxTurnYaw : 0f;
        Quaternion turnRot = Quaternion.AngleAxis(turnYaw, baseRot * Vector3.up);

        // 4. Trick Rotations
        Quaternion flipRot = Quaternion.AngleAxis(flipAngle, trickRight);
        Quaternion spinRot = Quaternion.AngleAxis(spinAngle, trickUp);

        // 5. Apply Model Offset (The Orientation Fix)
        // This permanently rotates the mesh (e.g., -90 degrees Y) so "Forward" becomes "Sideways"
        Quaternion offsetRot = Quaternion.Euler(0, modelRotationOffset, 0);

        // Combine: Base -> Offset -> Turn -> Spin -> Flip
        // Note: We apply offset to the base, then apply tricks on top
        Quaternion finalRot = baseRot * turnRot * spinRot * flipRot * offsetRot;

        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, finalRot, leanLerp * dt);
    }

    void AwardScore(int amount)
    {
        if (ScoreManager.Instance != null && controller != null)
            ScoreManager.Instance.AddScore(amount, controller.transform);
    }
}