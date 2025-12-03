using System.Collections;
using UnityEngine;

public class PlayerLifeSystem : MonoBehaviour
{
    [Header("refs")]
    public SnowboarderController controller;
    public SnowboarderRagdoll ragdoll;
    public Rigidbody playerBody;
    public AvalancheController avalanche;
    public SpeedScoreGenerator speedScore;
    public SurvivalTimerUI survivalTimer;

    [Header("extra life settings")]
    // how many extra lives this run starts with (can be overridden by upgrades)
    public int startExtraLives = 0;

    // garage can write PlayerPrefs.SetInt(extraLifePrefKey, boughtLives)
    public string extraLifePrefKey = "ExtraLives";

    // how long the screen freezes when extra life is used (seconds, realtime)
    public float freezeDuration = 1.0f;

    // how far behind the player to push the avalanche after revive (local z distance)
    public float revivePushbackDistance = 100f;

    [Header("fatal ragdoll launch")]
    public float fatalForwardImpulse = 25f;
    public float fatalUpwardImpulse = 12f;
    public float extraBodyImpulseMultiplier = 0.5f;

    int extraLives;
    bool isDead;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<SnowboarderController>();

        if (ragdoll == null)
            ragdoll = GetComponentInChildren<SnowboarderRagdoll>();

        if (playerBody == null)
            playerBody = GetComponent<Rigidbody>();

        // how many extra lives we got from the garage for this run
        int fromPrefs = PlayerPrefs.GetInt(extraLifePrefKey, startExtraLives);
        extraLives = Mathf.Max(0, fromPrefs);
    }

    public int ExtraLives => extraLives;

    // optional: let your garage script set this directly instead of PlayerPrefs
    public void SetExtraLivesFromOutside(int value)
    {
        extraLives = Mathf.Max(0, value);
    }

    // avalanche calls this when it touches the player
    public void OnAvalancheHit(AvalancheController source)
    {
        if (isDead) return;

        avalanche = source;

        if (extraLives > 0)
        {
            StartCoroutine(UseExtraLifeRoutine());
        }
        else
        {
            FatalDeathRoutine();
        }
    }

    // ====== SCENARIO A: has extra life ======
    IEnumerator UseExtraLifeRoutine()
    {
        extraLives--;
        PlayerPrefs.SetInt(extraLifePrefKey, extraLives);

        // freeze player movement but NO ragdoll
        if (controller != null)
        {
            controller.enabled = false;

            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }
        }

        if (ragdoll != null)
            ragdoll.DisableRagdoll();

        if (speedScore != null)
            speedScore.SetGameOver(true);
        if (survivalTimer != null)
            survivalTimer.SetGameOver(true);
        if (avalanche != null)
            avalanche.PauseChase();

        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0f; // hard freeze

        yield return new WaitForSecondsRealtime(freezeDuration);

        Time.timeScale = oldTimeScale;

        // push avalanche back and continue run
        if (avalanche != null)
        {
            avalanche.ResetBehindPlayer(revivePushbackDistance);
            avalanche.ResumeChase();
        }

        if (speedScore != null)
            speedScore.SetGameOver(false);
        if (survivalTimer != null)
            survivalTimer.SetGameOver(false);

        if (controller != null)
            controller.enabled = true;
    }

    // ====== SCENARIO B: no extra life, real death ======
    void FatalDeathRoutine()
    {
        isDead = true;

        if (controller != null)
            controller.enabled = false;

        if (speedScore != null)
            speedScore.SetGameOver(true);
        if (survivalTimer != null)
            survivalTimer.SetGameOver(true);
        if (avalanche != null)
            avalanche.PauseChase();

        // ragdoll launch
        if (ragdoll != null)
        {
            // direction from avalanche → player on ground plane
            Vector3 dir = Vector3.forward;
            if (avalanche != null)
            {
                dir = transform.position - avalanche.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f)
                    dir = -avalanche.transform.forward;
            }
            dir.Normalize();

            Vector3 impulse = dir * fatalForwardImpulse + Vector3.up * fatalUpwardImpulse;

            // kick the main rigidbody
            if (playerBody != null)
            {
                playerBody.isKinematic = false;
                playerBody.useGravity = true;
                playerBody.constraints = RigidbodyConstraints.None;
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
                playerBody.AddForce(impulse, ForceMode.VelocityChange);
            }

            // enable ragdoll and push all ragdoll bodies a bit
            ragdoll.EnableRagdoll();

            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
            foreach (var body in bodies)
            {
                if (body == null || body.isKinematic) continue;
                body.AddForce(impulse * extraBodyImpulseMultiplier, ForceMode.VelocityChange);
            }
        }

        // no timescale change here so you still see the full ragdoll flight
        // hook your restart / game over UI somewhere else
    }
}
