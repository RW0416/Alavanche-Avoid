using UnityEngine;
using TMPro;

[DefaultExecutionOrder(10)]
public class WorldText : MonoBehaviour
{
    public enum BillboardMode
    {
        LookAtCamera,       // Rotates to face the camera position exactly
        AlignWithCamera,    // Matches camera rotation (Flat to screen - Best for 3rd Person)
        VerticalOnly        // Rotates on Y axis only (Classic RPG NPC style)
    }

    [Header("Content")]
    [TextArea] public string textContent = "Press F";
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);

    [Header("Visual Style")]
    public Color textColor = Color.white;
    public float fontSize = 2.2f;
    public FontStyles fontStyle = FontStyles.Normal;
    public TMP_FontAsset customFont; // Optional: Leave empty for default
    
    [Header("Camera Behavior")]
    public BillboardMode billboardMode = BillboardMode.AlignWithCamera;
    public bool hideWhenBehindCamera = true;

    [Header("Target Logic")]
    public float showRadius = 3f;
    [Tooltip("Tags to search for a target at runtime. First found wins.")]
    public string[] tagsToSearch = new[] { "Player" };
    public float fadeSpeed = 12f;

    // Internal references
    Camera cam;
    Canvas worldCanvas;
    RectTransform rt;
    TextMeshProUGUI tmp;

    Transform target;
    float targetAlpha = 0f;
    float currentAlpha = 0f;
    float retryTimer = 0f;

    void Awake()
    {
        InitializeCanvas();
    }

    void InitializeCanvas()
    {
        cam = Camera.main;

        // 1. Create world-space canvas
        var canvasGO = new GameObject("__WorldTextCanvas");
        canvasGO.layer = gameObject.layer;
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = worldOffset;

        worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 400; // Sort above most world items

        rt = worldCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(3f, 1f); // Slightly wider default

        // 2. Create Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        
        tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        
        // Apply initial styles
        tmp.fontSize = fontSize;
        tmp.text = textContent;
        tmp.color = new Color(textColor.r, textColor.g, textColor.b, 0); // Start invisible
        tmp.fontStyle = fontStyle;
        if (customFont != null) tmp.font = customFont;

        // Center the text rect
        var textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        HideImmediate();
    }

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!worldCanvas) return;

        HandleTargeting();
        HandleVisibility();
        HandleRotation();
        UpdateStyleRuntime(); // Keeps text updated if you change inspector values
    }

    void HandleTargeting()
    {
        // Try to find player if we don't have one yet
        if (!target)
        {
            retryTimer -= Time.unscaledDeltaTime;
            if (retryTimer <= 0f)
            {
                ResolveTarget();
                retryTimer = 0.5f;
            }
        }
    }

    void HandleRotation()
    {
        if (!cam) return;

        // Update position to stay with the object + offset
        worldCanvas.transform.position = transform.position + worldOffset;

        switch (billboardMode)
        {
            case BillboardMode.AlignWithCamera:
                // Makes the text plane parallel to the camera plane.
                // Best for 3rd person to prevent text tilting backward when looking down.
                worldCanvas.transform.rotation = cam.transform.rotation;
                break;

            case BillboardMode.LookAtCamera:
                // Looks directly at the camera lens
                worldCanvas.transform.rotation = Quaternion.LookRotation(worldCanvas.transform.position - cam.transform.position);
                break;

            case BillboardMode.VerticalOnly:
                // Looks at camera but stays upright (Y-axis only)
                Vector3 direction = worldCanvas.transform.position - cam.transform.position;
                direction.y = 0; // flatten height
                if (direction != Vector3.zero)
                    worldCanvas.transform.rotation = Quaternion.LookRotation(direction);
                break;
        }
    }

    void HandleVisibility()
    {
        bool shouldShow = false;

        if (target)
        {
            float dist = Vector3.Distance(target.position, transform.position);
            shouldShow = dist <= showRadius;
        }

        // Hide if behind camera (optional safety check)
        if (shouldShow && hideWhenBehindCamera && cam)
        {
            Vector3 toObj = (worldCanvas.transform.position - cam.transform.position).normalized;
            if (Vector3.Dot(cam.transform.forward, toObj) <= 0f)
                shouldShow = false;
        }

        targetAlpha = shouldShow ? 1f : 0f;

        // Smooth fade
        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );

        // Apply Alpha
        if (tmp)
        {
            // We use the base color chosen in inspector, but override the alpha
            Color c = textColor;
            c.a = currentAlpha;
            tmp.color = c;
        }

        // Disable canvas entirely if fully invisible to save performance
        bool isVisible = currentAlpha > 0.001f;
        if (worldCanvas.enabled != isVisible)
            worldCanvas.enabled = isVisible;
    }

    void UpdateStyleRuntime()
    {
        // Syncs data if you change it in Inspector while playing
        if (tmp)
        {
            if (tmp.text != textContent) tmp.text = textContent;
            if (tmp.fontSize != fontSize) tmp.fontSize = fontSize;
            if (tmp.fontStyle != fontStyle) tmp.fontStyle = fontStyle;
        }
    }

    void ResolveTarget()
    {
        if (tagsToSearch == null) return;

        foreach (string tag in tagsToSearch)
        {
            if (string.IsNullOrEmpty(tag)) continue;
            
            // Note: FindGameObjectWithTag is okay to call occasionally, 
            // but heavy if called every frame. That's why we use a timer.
            GameObject go = null;
            try { go = GameObject.FindGameObjectWithTag(tag); } catch {} // Catch invalid tag errors

            if (go)
            {
                target = go.transform;
                return;
            }
        }
    }

    void OnDisable() => HideImmediate();

    void HideImmediate()
    {
        currentAlpha = 0f;
        if (tmp) tmp.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        if (worldCanvas) worldCanvas.enabled = false;
    }
}