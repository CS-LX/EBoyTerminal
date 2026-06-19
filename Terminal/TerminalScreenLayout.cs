using Engine;
using Engine.Media;
using SCIENEW.Screens;

namespace EBoyTerminal.Terminal;

/// <summary>与 CRT 绘制共用的屏幕字符格度量（行/列）。</summary>
public static class TerminalScreenLayout {
    public const float TextScale = 0.003f;

    const int ColumnProbeLength = 2048;

    public static int CalculateRows(Screen screen, BitmapFont font) {
        float screenHeight = (screen.WorldPos4 - screen.WorldPos1).Length();
        float lineHeight = (font.GlyphHeight + font.Spacing.Y) * font.Scale * TextScale * (screen.Up.LengthSquared() > 0f ? 1f : 0f);
        if (lineHeight <= 0f) {
            return 1;
        }
        return Math.Max(1, (int)Math.Floor(screenHeight / lineHeight));
    }

    public static int CalculateColumns(Screen screen, BitmapFont font) {
        float screenWidth = (screen.WorldPos2 - screen.WorldPos1).Length();
        float horizontalScale = TextScale * (screen.Right.LengthSquared() > 0f ? 1f : 0f);
        if (screenWidth <= 0f || horizontalScale <= 0f) {
            return 1;
        }
        string probe = new('W', ColumnProbeLength);
        return Math.Max(1, font.FitText(screenWidth, probe, horizontalScale, 0f));
    }

    public static string TruncateTextToScreenWidth(string text, Screen screen, BitmapFont font) {
        if (string.IsNullOrEmpty(text)) {
            return text;
        }
        float screenWidth = (screen.WorldPos2 - screen.WorldPos1).Length();
        float horizontalScale = TextScale * (screen.Right.LengthSquared() > 0f ? 1f : 0f);
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            string line = lines[i];
            if (line.Length == 0) {
                continue;
            }
            int fitCount = font.FitText(screenWidth, line, horizontalScale, 0f);
            if (fitCount < line.Length) {
                lines[i] = line[..fitCount];
            }
        }
        return string.Join("\n", lines);
    }
}
