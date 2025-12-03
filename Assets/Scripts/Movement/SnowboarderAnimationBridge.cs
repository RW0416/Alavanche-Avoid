using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class SnowboarderAnimationBridge : MonoBehaviour
{
    public SnowboarderController controller;
    public SnowboarderTricks tricks;

    [Header("Bomb / Grab Settings")]
    [Tooltip("Relative speed (0–1) at which we switch to the Bomb pose when boosting.")]
    [Range(0f, 1f)] public float bombSpeedThreshold = 0.4f;

    Animator anim;

    enum Pose
    {
        Ride        = 1,
        Bomb        = 2,
        Shaky       = 3,
        JumpRegular = 6,
        JumpGoofy   = 7,
        Air         = 8,
        Land        = 9,
        Grab01      = 10,
        Grab02      = 11,
        LandHard    = 12,
        LandSoft    = 13
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (!controller)
            controller = GetComponentInParent<SnowboarderController>();
        if (!tricks)
            tricks = GetComponentInParent<SnowboarderTricks>();
    }

    void Update()
    {
        if (!controller || !anim) return;

        var vel = controller.Body.linearVelocity;
        float speed = vel.magnitude;

        // 1) speed parameter for blend tree
        float relSpeed = Mathf.InverseLerp(0f, controller.maxSpeed, speed);
        anim.SetFloat("rel_speed", relSpeed);

        bool grounded = controller.IsGrounded;
        bool braking  = controller.IsBraking;

        bool inAir      = !grounded;
        bool doingTrick = tricks != null && tricks.IsDoingTrick;
        bool flatSpin   = tricks != null && tricks.IsFlatSpinTrick;

        // read boost directly from keyboard (Left Shift)
        bool boostHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        int pose;

        if (inAir)
        {
            // --- AIR STATES ---
            if (doingTrick)
            {
                // SPIN = Grab01, FLIP = Grab02
                pose = flatSpin ? (int)Pose.Grab01 : (int)Pose.Grab02;
            }
            else
            {
                // normal jump vs generic air
                float vertical = 0f;
                if (vel.sqrMagnitude > 0.0001f)
                    vertical = Vector3.Dot(vel.normalized, controller.GroundNormal);

                if (vertical > 0.2f)
                    pose = (int)Pose.JumpRegular;
                else
                    pose = (int)Pose.Air;
            }
        }
        else
        {
            // --- GROUNDED STATES ---
            if (speed < 0.5f)
            {
                pose = (int)Pose.LandSoft;
            }
            else
            {
                // bombing ONLY when actively boosting with Shift AND moving fast enough
                bool isBomb = boostHeld && !braking && relSpeed >= bombSpeedThreshold;

                if (isBomb)
                    pose = (int)Pose.Bomb;
                else if (braking)
                    pose = (int)Pose.Shaky;
                else
                    pose = (int)Pose.Ride;
            }
        }

        anim.SetInteger("pose", pose);
    }
}
