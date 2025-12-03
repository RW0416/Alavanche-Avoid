using UnityEngine;

public class CutsceneNPCMover : MonoBehaviour
{
    [Header("path")]
    public Transform[] waypoints;
    public float moveSpeed = 3f;
    public float rotateSpeed = 8f;
    public float reachDistance = 0.1f;
    public bool playOnStart = true;
    public bool loop = false;

    [Header("disable while cutscene plays (optional)")]
    public MonoBehaviour[] disableThese;   // e.g. PlayerInput, ThirdPersonController, etc.

    [Header("animation (Starter Assets friendly)")]
    public Animator animator;
    public string speedParamName = "Speed";
    public string motionSpeedParamName = "MotionSpeed";
    public string groundedParamName = "Grounded";
    [Range(0.1f, 6f)] public float animationSpeed = 1.0f;   // <-- add this


    int currentIndex = 0;
    bool isPlaying = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (playOnStart)
            StartCutscene();
    }

    public void StartCutscene()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("[CutsceneNPCMover] no waypoints set.");
            return;
        }

        ToggleExternalControllers(false);

        currentIndex = 0;
        isPlaying = true;

        // tell animator we're moving
        SetLocomotion(1f);
    }

    public void StopCutscene()
    {
        isPlaying = false;
        SetLocomotion(0f);
        ToggleExternalControllers(true);
    }

    void Update()
    {
        if (!isPlaying) return;

        if (waypoints == null || waypoints.Length == 0)
        {
            StopCutscene();
            return;
        }

        Transform target = waypoints[currentIndex];
        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;

        // reached this waypoint
        if (dist <= reachDistance)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
            {
                if (loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    StopCutscene();
                    return;
                }
            }

            target = waypoints[currentIndex];
            toTarget = target.position - transform.position;
            dist = toTarget.magnitude;
        }

        Vector3 dir = toTarget.normalized;

        // rotate towards movement direction
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }

        // move
        float step = moveSpeed * Time.deltaTime;
        if (step > dist) step = dist;
        transform.position += dir * step;

        // keep telling animator we're walking
        SetLocomotion(moveSpeed > 0.01f ? 1f : 0f);
    }

    void ToggleExternalControllers(bool value)
    {
        if (disableThese == null) return;

        for (int i = 0; i < disableThese.Length; i++)
        {
            if (disableThese[i] != null)
                disableThese[i].enabled = value;
        }
    }

    void SetLocomotion(float normalizedSpeed)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(speedParamName))
            animator.SetFloat(speedParamName, normalizedSpeed);

        if (!string.IsNullOrEmpty(motionSpeedParamName))
            animator.SetFloat(motionSpeedParamName, animationSpeed);   // <-- use slider

        if (!string.IsNullOrEmpty(groundedParamName))
            animator.SetBool(groundedParamName, true);
    }

}
