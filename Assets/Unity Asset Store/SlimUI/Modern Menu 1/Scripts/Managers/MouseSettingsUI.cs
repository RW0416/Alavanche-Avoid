using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles mouse sensitivity settings for X, Y, and Smoothing.
/// - Reads saved values from PlayerPrefs
/// - Updates values when sliders change
/// - Stores updated values back to PlayerPrefs
/// </summary>
public class MouseSettingsManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider sensitivityXSlider;
    public TMP_Text sensitivityXLabel;

    public Slider sensitivityYSlider;
    public TMP_Text sensitivityYLabel;

    public Slider smoothingSlider;
    public TMP_Text smoothingLabel;

    // PlayerPrefs keys
    const string PREF_X_SENS = "XSensitivity";
    const string PREF_Y_SENS = "YSensitivity";
    const string PREF_SMOOTH = "MouseSmoothing";

    // Cached slider values
    float valueX;
    float valueY;
    float valueSmooth;

    void Start()
    {
        LoadSettings();
        ApplyValuesToUI();
        UpdateLabels();
        AddListeners();
    }

    /// <summary>
    /// Load values from PlayerPrefs, set defaults if missing
    /// </summary>
    void LoadSettings()
    {
        valueX = PlayerPrefs.GetFloat(PREF_X_SENS, 1f);
        valueY = PlayerPrefs.GetFloat(PREF_Y_SENS, 1f);
        valueSmooth = PlayerPrefs.GetFloat(PREF_SMOOTH, 0.0f);
    }

    /// <summary>
    /// Set slider initial values
    /// </summary>
    void ApplyValuesToUI()
    {
        if (sensitivityXSlider != null)
            sensitivityXSlider.value = valueX;

        if (sensitivityYSlider != null)
            sensitivityYSlider.value = valueY;

        if (smoothingSlider != null)
            smoothingSlider.value = valueSmooth;
    }

    /// <summary>
    /// Set UI text labels if available
    /// </summary>
    void UpdateLabels()
    {
        if (sensitivityXLabel != null)
            sensitivityXLabel.text = $"X: {valueX:0.00}";

        if (sensitivityYLabel != null)
            sensitivityYLabel.text = $"Y: {valueY:0.00}";

        if (smoothingLabel != null)
            smoothingLabel.text = $"Smoothing: {valueSmooth:0.00}";
    }

    /// <summary>
    /// Subscribes slider events
    /// </summary>
    void AddListeners()
    {
        if (sensitivityXSlider != null)
            sensitivityXSlider.onValueChanged.AddListener(OnXChanged);

        if (sensitivityYSlider != null)
            sensitivityYSlider.onValueChanged.AddListener(OnYChanged);

        if (smoothingSlider != null)
            smoothingSlider.onValueChanged.AddListener(OnSmoothChanged);
    }

    /// <summary>
    /// Called when X slider changes
    /// </summary>
    void OnXChanged(float value)
    {
        valueX = value;
        PlayerPrefs.SetFloat(PREF_X_SENS, value);
        PlayerPrefs.Save();
        UpdateLabels();
    }

    /// <summary>
    /// Called when Y slider changes
    /// </summary>
    void OnYChanged(float value)
    {
        valueY = value;
        PlayerPrefs.SetFloat(PREF_Y_SENS, value);
        PlayerPrefs.Save();
        UpdateLabels();
    }

    /// <summary>
    /// Called when smoothing slider changes
    /// </summary>
    void OnSmoothChanged(float value)
    {
        valueSmooth = value;
        PlayerPrefs.SetFloat(PREF_SMOOTH, value);
        PlayerPrefs.Save();
        UpdateLabels();
    }
}
