using Engine;
using Engine.Graphics;
using Engine.Media;
using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using SCIENEW;
using SCIENEW.Screens;
using SCIENEW.Utils;
using TemplatesDatabase;
using EBoyTerminal.Runtime;
using EBoyTerminal.Terminal;
using EBoyTerminal.Utils;
using Screen = SCIENEW.Screens.Screen;

namespace EBoyTerminal {
    /// <summary>将月之终端 Lua 输出绘制到已链接的 CRT 等 <see cref="IScreenContainerComponent"/>。</summary>
    public class ComponentMoonTerminalScreenProvider : Component, IScreenProviderComponent, ILuaScriptApiProvider {
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
            m_fontBatch3D = m_primitivesRenderer3D.FontBatch(m_font, 0, null, Screen.FontRasterizerState);
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            ComponentMoonTerminal? terminal = m_terminal ?? Entity.FindComponent<ComponentMoonTerminal>(throwOnError: false);
            if (terminal == null) {
                return;
            }
            TerminalScreenResolver resolver = new(terminal, context.Project);
            context.AddSubTable("screen", new Dictionary<string, DynValue> {
                ["isLinked"] = context.Callback((_, _) => DynValue.NewBoolean(resolver.GetSnapshot().IsLinked)),
                ["countRows"] = context.Callback((_, _) => DynValue.NewNumber(GetMetric(resolver, static snapshot => snapshot.Rows))),
                ["countColumns"] = context.Callback((_, _) => DynValue.NewNumber(GetMetric(resolver, static snapshot => snapshot.Columns))),
                ["readPosition"] = context.Callback((executionContext, _) => LuaUtils.NewVector3(executionContext.OwnerScript, resolver.GetSnapshot().Position)) });
        }

        public void Draw(Screen screen, Project project, Camera camera, int drawOrder) {
            if (!m_terminal.IsPowered) {
                return;
            }
            int visibleLines = TerminalScreenLayout.CalculateRows(screen, m_font);
            string text = TerminalScreenLayout.TruncateTextToScreenWidth(
                m_terminal.GetScreenText(visibleLines),
                screen,
                m_font);
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            screen.GetTextAxes(TerminalScreenLayout.TextScale, out Vector3 textRight, out Vector3 textDown);
            m_fontBatch3D.QueueText(text, screen.WorldPos4, textRight, textDown, Color.Green, default);
            m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
        }

        public Entity GetEntity() => Entity;

        public Vector3 GetPosition() => new Vector3(m_blockEntity.Coordinates) + Vector3.One * 0.5f;

        public BoundingBox GetBoundingBox() => new(new Vector3(m_blockEntity.Coordinates), new Vector3(m_blockEntity.Coordinates) + Vector3.One);

        public string GetDescription() => LanguageUtils.GetText(this, "ProviderDescription");

        static double GetMetric(TerminalScreenResolver resolver, Func<TerminalScreenSnapshot, int> selector) {
            TerminalScreenSnapshot snapshot = resolver.GetSnapshot();
            return snapshot.IsLinked ? selector(snapshot) : 0d;
        }
    }
}
