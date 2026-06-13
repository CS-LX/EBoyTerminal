using Engine;
using Engine.Graphics;
using Engine.Media;
using Game;
using GameEntitySystem;
using SCIENEW;
using SCIENEW.Screens;
using SCIENEW.Utils;
using TemplatesDatabase;
using Screen = SCIENEW.Screens.Screen;

namespace EBoyTerminal {
    /// <summary>将月之终端 Lua 输出绘制到已链接的 CRT 等 <see cref="IScreenContainerComponent"/>。</summary>
    public class ComponentMoonTerminalScreenProvider : Component, IScreenProviderComponent {
        const float TextScale = 0.003f;

        ComponentBlockEntity m_blockEntity = null!;
        ComponentMoonTerminal m_terminal = null!;
        BitmapFont m_font = null!;
        PrimitivesRenderer3D m_primitivesRenderer3D = new();
        FontBatch3D m_fontBatch3D = null!;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
            m_terminal = Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);
            m_font = IndustrialModLoader.PixelFont;
            m_fontBatch3D = m_primitivesRenderer3D.FontBatch(m_font, 0);
        }

        public void Draw(Screen screen, Project project, Camera camera, int drawOrder) {
            if (!m_terminal.IsPowered) {
                return;
            }
            int visibleLines = CalculateVisibleLineCount(screen);
            float screenWidth = (screen.WorldPos2 - screen.WorldPos1).Length();
            float horizontalScale = TextScale * screen.Edge1.Length();
            string text = TruncateTextToScreenWidth(
                m_terminal.GetScreenText(visibleLines),
                screenWidth,
                horizontalScale);
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            Vector3 right = TextScale * -screen.Edge1;
            Vector3 down = TextScale * -screen.Edge2;
            // CRT 四角：1 左下、2 右下、3 右上、4 左上。配合 -Edge1/-Edge2 时，从 WorldPos3 起笔才是视觉上的左上→右下排版，勿改成 WorldPos4。
            m_fontBatch3D.QueueText(text, screen.WorldPos3, right, down, Color.Green, default);
            m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
        }

        int CalculateVisibleLineCount(Screen screen) {
            float screenHeight = (screen.WorldPos4 - screen.WorldPos1).Length();
            float lineHeight = (m_font.GlyphHeight + m_font.Spacing.Y) * m_font.Scale * TextScale * screen.Edge2.Length();
            if (lineHeight <= 0f) {
                return 1;
            }
            return Math.Max(1, (int)Math.Floor(screenHeight / lineHeight));
        }

        /// <summary>按 <see cref="FontBatch3D"/> 同款度量截断每行，避免用字高代替字宽导致过早截断。</summary>
        string TruncateTextToScreenWidth(string text, float screenWidth, float horizontalScale) {
            if (string.IsNullOrEmpty(text)) {
                return text;
            }
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                if (line.Length == 0) {
                    continue;
                }
                int fitCount = m_font.FitText(screenWidth, line, horizontalScale, 0f);
                if (fitCount < line.Length) {
                    lines[i] = line[..fitCount];
                }
            }
            return string.Join("\n", lines);
        }

        public Entity GetEntity() => Entity;

        public Vector3 GetPosition() => new Vector3(m_blockEntity.Coordinates) + Vector3.One * 0.5f;

        public BoundingBox GetBoundingBox() => new(new Vector3(m_blockEntity.Coordinates), new Vector3(m_blockEntity.Coordinates) + Vector3.One);

        public string GetDescription() => LanguageUtils.GetText(this, "ProviderDescription");
    }
}
