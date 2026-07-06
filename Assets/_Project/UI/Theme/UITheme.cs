using UnityEngine;

[CreateAssetMenu(menuName = "SOLITUDE/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Surfaces")]
    public Color background;
    public Color panel;
    public Color panelAlt;
    public Color slot;
    public Color slotHover;

    [Header("Borders")]
    public Color border;
    public Color accent;

    [Header("Text")]
    public Color text;
    public Color textMuted;

    [Header("Typography")]
    public Font font;
}