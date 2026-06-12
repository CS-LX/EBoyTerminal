using EBoyTerminal.Runtime;
using Game;
using GameEntitySystem;

namespace EBoyTerminal {
    /// <summary>
    /// 实体级 Lua 运行时；与具体终端/机器解耦，供月之终端等实体组合使用。
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
            m_host.TryLoad(m_sourceText, GetChunkName());
        }

        public bool Start() => m_host.Start();

        public void Stop() => m_host.Stop();

        public void Update(float dt) {
            m_host.Tick(dt);
        }

        string GetChunkName() {
            ComponentBlockEntity? blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
            return blockEntity != null ? $"terminal@{blockEntity.Coordinates}" : "lua";
        }
    }
}
