using UnityEngine;
using TMPro;

[ExecuteAlways]
public class UIThemeApplier : MonoBehaviour
{
    public UITheme theme;

    private TMP_Text text;

    void OnEnable()
    {
        text = GetComponent<TMP_Text>();
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        if (theme == null || text == null) return;

        text.font = theme.font;
        text.color = theme.fontColor;
        text.fontSize = theme.fontSize;

        // Outline
        text.outlineWidth = theme.outlineWidth;
        text.outlineColor = theme.outlineColor;

        // Shadow
        if (theme.useShadow)
        {
            text.enableWordWrapping = true;
            text.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
            text.fontSharedMaterial.SetColor("_UnderlayColor", theme.shadowColor);
            text.fontSharedMaterial.SetFloat("_UnderlayOffsetX", theme.shadowDistance.x);
            text.fontSharedMaterial.SetFloat("_UnderlayOffsetY", theme.shadowDistance.y);
        }
        else
        {
            text.fontSharedMaterial.DisableKeyword("UNDERLAY_ON");
        }
    }
}
