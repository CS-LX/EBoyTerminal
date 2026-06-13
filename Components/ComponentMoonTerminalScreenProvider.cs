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
            m_fontBatch3D = m_primitivesRenderer3D.FontBatch(m_font, 0);
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            ComponentMoonTerminal? terminal = Entity.FindComponent<ComponentMoonTerminal>(throwOnError: false);
            if (terminal == null) {
                return;
            }
            TerminalScreenResolver resolver = new(terminal, context.Project);
            context.AddSubTable("screen", new Dictionary<string, DynValue> {
                ["isLinked"] = context.Callback((_, _) => DynValue.NewBoolean(resolver.GetSnapshot().IsLinked)),
                ["rows"] = context.Callback((_, _) => DynValue.NewNumber(GetMetric(resolver, static snapshot => snapshot.Rows))),
                ["columns"] = context.Callback((_, _) => DynValue.NewNumber(GetMetric(resolver, static snapshot => snapshot.Columns))),
                ["position"] = context.Callback((executionContext, _) =>
                    ToVectorTable(executionContext, resolver.GetSnapshot().Position)),
            });
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
            Vector3 right = TerminalScreenLayout.TextScale * -screen.Edge1;
            Vector3 down = TerminalScreenLayout.TextScale * -screen.Edge2;
            // CRT 四角：1 左下、2 右下、3 右上、4 左上。配合 -Edge1/-Edge2 时，从 WorldPos3 起笔才是视觉上的左上→右下排版，勿改成 WorldPos4。
            m_fontBatch3D.QueueText(text, screen.WorldPos3, right, down, Color.Green, default);
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

        static DynValue ToVectorTable(ScriptExecutionContext executionContext, Vector3 vector) {
            Table table = new(executionContext.OwnerScript);
            table.Set("x", DynValue.NewNumber(vector.X));
            table.Set("y", DynValue.NewNumber(vector.Y));
            table.Set("z", DynValue.NewNumber(vector.Z));
            return DynValue.NewTable(table);
        }
    }
}
