using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "UITheme", menuName = "UI/UI Theme")]
public class UITheme : ScriptableObject
{
    public TMP_FontAsset font;
    public Color fontColor = Color.white;
    public float fontSize = 24f;

    [Header("Effects")]
    public Color outlineColor = Color.black;
    public float outlineWidth = 0.1f;
    public bool useShadow = true;
    public Color shadowColor = new Color(0,0,0,0.5f);
    public Vector2 shadowDistance = new Vector2(1f, -1f);
}
