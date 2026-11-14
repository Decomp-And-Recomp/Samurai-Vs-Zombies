using UnityEngine;

// The SUI layout uses normalized screen positions plus a pixel inset.
public sealed class SUITextureRenderer : MonoBehaviour
{
    public Texture texture;
    public Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Rect pixelInset;

    public Rect GetScreenRect()
    {
        Vector3 p = transform.position;
        Vector3 s = transform.localScale;
        return new Rect(p.x * Screen.width + pixelInset.x,
            p.y * Screen.height + pixelInset.y,
            s.x * Screen.width + pixelInset.width,
            s.y * Screen.height + pixelInset.height);
    }

    private void OnGUI()
    {
        GUI.depth = -Mathf.RoundToInt(transform.position.z * 10000f);
        if (Event.current.type != EventType.Repaint || texture == null)
            return;
        Rect r = GetScreenRect();
        r.y = Screen.height - r.yMax;
        Color previousColor = GUI.color;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        // GUITexture multiplied its tint by two, including alpha.
        GUI.color = new Color(color.r * 2f, color.g * 2f, color.b * 2f, color.a * 2f);
        GUI.depth = -Mathf.RoundToInt(transform.position.z * 10000f);
        GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, true);
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }
}
