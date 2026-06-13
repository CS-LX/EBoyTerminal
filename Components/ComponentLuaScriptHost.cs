using EBoyTerminal.Runtime;
using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;

namespace EBoyTerminal {
    /// <summary>
    /// 实体级 Lua 运行时；扫描同实体 <see cref="ILuaScriptApiProvider"/> 并注入 API。
    /// </summary>
    public class ComponentLuaScriptHost : Component, IUpdateable {
        readonly LuaScriptHost m_host = new();

        string m_sourceText = string.Empty;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public string SourceText => m_sourceText;

        public string? LastError => m_host.LastError;

        public LuaScriptHost Host => m_host;

        public LuaMachineState State => m_host.State;

        /// <summary>重新编译脚本并保持停止/待运行状态；不会在 Load 或保存时同步执行玩家代码。</summary>
        public void ReloadFromSource(string source) {
            m_sourceText = source ?? string.Empty;
            RefreshTerminalApi();
            m_host.TryLoad(m_sourceText, GetChunkName());
        }

        public bool Start() => m_host.Start();

        public void Pause() => m_host.Pause();

        public void Stop() => m_host.Stop();

        public void Update(float dt) {
            m_host.Tick(dt);
        }

        void RefreshTerminalApi() {
            LuaScriptApiBuildContext context = new() {
                ScriptHost = this,
                Project = Project
            };
            List<Component>? components = Entity.Components;
            if (components != null) {
                foreach (Component component in components) {
                    if (component is ILuaScriptApiProvider provider) {
                        provider.ContributeLuaApi(context);
                    }
                }
            }
            m_host.RegisterApiTable("terminal", context.Root, context.SubTables);
        }

        string GetChunkName() {
            ComponentBlockEntity? blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
            return blockEntity != null ? $"terminal@{blockEntity.Coordinates}" : "lua";
        }
    }
}
