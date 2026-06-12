using Engine;
using Engine.Graphics;
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
        PrimitivesRenderer3D m_primitivesRenderer3D = new();
        FontBatch3D m_fontBatch3D = null!;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
            m_terminal = Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);
            m_fontBatch3D = m_primitivesRenderer3D.FontBatch(IndustrialModLoader.PixelFont, 0);
        }

        public void Draw(Screen screen, Project project, Camera camera, int drawOrder) {
            string text = m_terminal.GetScreenText();
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            Vector3 right = TextScale * -screen.Edge1;
            Vector3 down = TextScale * -screen.Edge2;
            m_fontBatch3D.QueueText(text, screen.WorldPos4, right, down, Color.Green, default);
            m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
        }

        public Entity GetEntity() => Entity;

        public Vector3 GetPosition() => new Vector3(m_blockEntity.Coordinates) + Vector3.One * 0.5f;

        public BoundingBox GetBoundingBox() => new(new Vector3(m_blockEntity.Coordinates), new Vector3(m_blockEntity.Coordinates) + Vector3.One);

        public string GetDescription() => LanguageUtils.GetText(this, "ProviderDescription");
    }
}
