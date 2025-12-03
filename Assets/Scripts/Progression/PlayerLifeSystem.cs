using UnityEngine;

/// <summary>
/// Two death flows:
/// A) Has extra life  -> freeze game, no ragdoll, show panel with "Use Extra Life".
/// B) No extra life   -> ragdoll launch, normal game over.
/// </summary>
public class PlayerLifeSystem : MonoBehaviour
{
    [Header("references")]
    public SnowboarderController controller;
    public SnowboarderRagdoll ragdoll;
    public Rigidbody playerBody;
    public AvalancheController avalanche;
    public SpeedScoreGenerator speedScore;
    public SurvivalTimerUI survivalTimer;
    public GameOverUI gameOverUI;

    [Header("extra life")]
    [Tooltip("How many extra lives you start this run with.")]
    public int baseExtraLives = 0;

    [Tooltip("How far behind the player the avalanche is pushed when reviving with an extra life (local Z units).")]
    public float avalanchePushbackDistance = 80f;

    [Header("fatal ragdoll launch")]
    public float fatalForwardImpulse = 25f;
    public float fatalUpwardImpulse = 12f;
    public float extraBodyImpulseMultiplier = 0.5f;

    int extraLives;
    bool isGameOver;
    float savedTimeScale = 1f;

    public int ExtraLives => extraLives;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<SnowboarderController>();

        if (playerBody == null)
            playerBody = GetComponent<Rigidbody>();

        if (ragdoll == null)
            ragdoll = GetComponentInChildren<SnowboarderRagdoll>();

        if (avalanche == null)
            avalanche = FindObjectOfType<AvalancheController>();

        if (speedScore == null)
            speedScore = FindObjectOfType<SpeedScoreGenerator>();

        if (survivalTimer == null)
            survivalTimer = FindObjectOfType<SurvivalTimerUI>();

        if (gameOverUI == null)
            gameOverUI = FindObjectOfType<GameOverUI>();

        extraLives = Mathf.Max(0, baseExtraLives);
        isGameOver = false;
        savedTimeScale = Time.timeScale;

        Debug.Log($"[PlayerLifeSystem] start extraLives = {extraLives}");
    }

    // optional override from Garage
    public void SetExtraLives(int value)
    {
        extraLives = Mathf.Max(0, value);
        Debug.Log($"[PlayerLifeSystem] SetExtraLives -> {extraLives}");
    }

    /// <summary>Called by AvalancheController when it hits the player.</summary>
    public void OnAvalancheHit(AvalancheController source)
    {
        if (isGameOver)
            return;

        avalanche = source;

        Debug.Log($"[PlayerLifeSystem] OnAvalancheHit, extraLives = {extraLives}");

        if (extraLives > 0)
            HandleExtraLifeDeath();
        else
            HandleFinalDeath();
    }

    // ====== Scenario A: has extra life, FULL FREEZE, no ragdoll ======
    void HandleExtraLifeDeath()
    {
        isGameOver = true;

        // freeze game time
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // freeze player movement
        if (controller != null)
            controller.enabled = false;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        // make sure ragdoll is off
        if (ragdoll != null)
            ragdoll.DisableRagdoll();

        // stop score/timer/avalanche
        if (speedScore != null)
            speedScore.SetGameOver(true);

        if (survivalTimer != null)
            survivalTimer.SetGameOver(true);

        if (avalanche != null)
            avalanche.PauseChase();

        // unlock mouse so UI is clickable
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // show panel with extra-life button
        if (gameOverUI != null)
            gameOverUI.ShowGameOver(true, extraLives);
    }

    // ====== Scenario B: no extra life, ragdoll launch (no time freeze) ======
    void HandleFinalDeath()
    {
        isGameOver = true;

        if (controller != null)
            controller.enabled = false;

        if (speedScore != null)
            speedScore.SetGameOver(true);

        if (survivalTimer != null)
            survivalTimer.SetGameOver(true);

        if (avalanche != null)
            avalanche.PauseChase();

        // unlock cursor for the game-over UI here too
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ragdoll + cannonball
        if (ragdoll != null)
        {
            ragdoll.EnableRagdoll();

            Vector3 dir = Vector3.forward;
            if (avalanche != null)
            {
                dir = transform.position - avalanche.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = -avalanche.transform.forward;
            }
            dir.Normalize();

            Vector3 impulse =
                dir * fatalForwardImpulse +
                Vector3.up * fatalUpwardImpulse;

            if (ragdoll.mainBody != null)
            {
                var rb = ragdoll.mainBody;
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(impulse, ForceMode.VelocityChange);
            }

            var bodies = GetComponentsInChildren<Rigidbody>();
            foreach (var body in bodies)
            {
                if (body == null || body.isKinematic) continue;
                body.AddForce(impulse * extraBodyImpulseMultiplier, ForceMode.VelocityChange);
            }
        }

        if (gameOverUI != null)
            gameOverUI.ShowGameOver(false, extraLives);
    }

    /// <summary>Called by GameOverUI when "Use Extra Life" is pressed.</summary>
    public void UseExtraLife()
    {
        if (!isGameOver)
            return;
        if (extraLives <= 0)
            return;

        extraLives--;
        Debug.Log($"[PlayerLifeSystem] UseExtraLife -> remaining = {extraLives}");

        // restore time
        Time.timeScale = (savedTimeScale <= 0f) ? 1f : savedTimeScale;

        // relock mouse for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (ragdoll != null)
            ragdoll.DisableRagdoll();

        if (controller != null)
            controller.enabled = true;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        if (speedScore != null)
            speedScore.SetGameOver(false);

        if (survivalTimer != null)
            survivalTimer.SetGameOver(false);

        if (avalanche != null)
            avalanche.ResetBehindPlayer(avalanchePushbackDistance);

        isGameOver = false;
    }
}
