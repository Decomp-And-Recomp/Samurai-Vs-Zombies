using UnityEngine;
using UnityEngine.Serialization;

// Draws the recovered bitmap font atlas directly; it has no TrueType font face.
public sealed class SUITextRenderer : MonoBehaviour
{
    [FormerlySerializedAs("m_Text")] public string text = "";
    [FormerlySerializedAs("m_Font")] public Font font;
    [FormerlySerializedAs("m_Anchor")] public TextAnchor anchor = TextAnchor.UpperLeft;
    [FormerlySerializedAs("m_Alignment")] public TextAlignment alignment = TextAlignment.Left;
    [FormerlySerializedAs("m_PixelOffset")] public Vector2 pixelOffset;
    [FormerlySerializedAs("m_FontSize")] public int fontSize;
    [FormerlySerializedAs("m_FontStyle")] public FontStyle fontStyle;
    public Color color = Color.white;

    private bool Glyph(char c, out CharacterInfo info)
    {
        return font.GetCharacterInfo(c, out info, fontSize, fontStyle)
            || font.GetCharacterInfo('?', out info, fontSize, fontStyle);
    }

    private float LineWidth(string line)
    {
        float width = 0f;
        foreach (char c in line)
        {
            CharacterInfo glyph;
            if (Glyph(c, out glyph)) width += glyph.advance;
        }
        return width;
    }

    public Rect GetScreenRect()
    {
        if (font == null) return new Rect();
        if (font.dynamic) font.RequestCharactersInTexture(text ?? "", fontSize, fontStyle);
        string[] lines = (text ?? "").Split('\n');
        float width = 0f;
        foreach (string line in lines) width = Mathf.Max(width, LineWidth(line));
        float scale = transform.localScale.y;
        Vector2 size = new Vector2(width, font.lineHeight * lines.Length) * scale;
        Vector3 p = transform.position;
        float x = p.x * Screen.width + pixelOffset.x;
        float y = p.y * Screen.height + pixelOffset.y - font.ascent * scale;
        x -= size.x * ((int)anchor % 3) * 0.5f;
        y -= size.y * (1f - ((int)anchor / 3) * 0.5f);
        return new Rect(x, y, size.x, size.y);
    }

    private void OnGUI()
    {
        // Unity retains GUI depth per behaviour; restoring it here loses the layer order.
        GUI.depth = -Mathf.RoundToInt(transform.position.z * 10000f);
        if (Event.current.type != EventType.Repaint || font == null || string.IsNullOrEmpty(text)) return;
        float scale = transform.localScale.y;
        if (scale <= 0f || font.material == null || font.material.mainTexture == null) return;
        Rect bounds = GetScreenRect();
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        GUI.matrix = Matrix4x4.TRS(new Vector3(bounds.x, Screen.height - bounds.yMax, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        GUI.color = color;
        float y = 0f;
        foreach (string line in text.Split('\n'))
        {
            float spare = bounds.width / scale - LineWidth(line);
            float x = alignment == TextAlignment.Center ? spare * 0.5f : alignment == TextAlignment.Right ? spare : 0f;
            foreach (char c in line)
            {
                CharacterInfo glyph;
                if (!Glyph(c, out glyph)) continue;
                if (glyph.glyphWidth > 0 && glyph.glyphHeight > 0)
                {
                    Rect rect = new Rect(x + glyph.minX, y - glyph.maxY, glyph.glyphWidth, glyph.glyphHeight);
                    Rect uv = Rect.MinMaxRect(glyph.uvBottomLeft.x, glyph.uvBottomLeft.y, glyph.uvTopRight.x, glyph.uvTopRight.y);
                    GUI.DrawTextureWithTexCoords(rect, font.material.mainTexture, uv, true);
                }
                x += glyph.advance;
            }
            y += font.lineHeight;
        }
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}
