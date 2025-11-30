using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SnowboarderAnimationBridge : MonoBehaviour
{
    public SnowboarderController controller;

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
    }

    void Update()
    {
        if (!controller || !anim) return;

        // 1) speed parameter for blend tree
        var vel = controller.Body.linearVelocity;
        float speed = vel.magnitude;
        float relSpeed = Mathf.InverseLerp(0f, controller.maxSpeed, speed);
        anim.SetFloat("rel_speed", relSpeed);

        // 2) decide pose from controller state
        bool grounded = controller.IsGrounded;
        bool braking  = controller.IsBraking;

        int pose = (int)Pose.Ride;

        if (!grounded)
        {
            // simple: going up = jump, else generic air
            float vertical = Vector3.Dot(vel.normalized, controller.GroundNormal);
            if (vertical > 0.2f)
                pose = (int)Pose.JumpRegular;
            else
                pose = (int)Pose.Air;
        }
        else
        {
            if (speed < 0.5f)
            {
                // just landed / almost stopped
                pose = (int)Pose.LandSoft;
            }
            else if (braking)
            {
                // braking crouch / shaky stance
                pose = (int)Pose.Shaky;
            }
            else
            {
                pose = (int)Pose.Ride;
            }
        }

        anim.SetInteger("pose", pose);
    }
}
