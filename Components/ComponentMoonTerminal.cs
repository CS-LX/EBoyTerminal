using Game;
using GameEntitySystem;
using TemplatesDatabase;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    public class ComponentMoonTerminal : Component {
        public const string ScriptTextKey = "ScriptText";

        string m_scriptText = string.Empty;

        ComponentLuaScriptHost m_luaScriptHost = null!;

        public string ScriptText {
            get => m_scriptText;
            set {
                m_scriptText = value ?? string.Empty;
                PushScriptToHost();
            }
        }

        public ComponentLuaScriptHost LuaScriptHost => m_luaScriptHost;

        public LuaMachineState LuaState => m_luaScriptHost?.State ?? LuaMachineState.Stopped;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_luaScriptHost = Entity.FindComponent<ComponentLuaScriptHost>(throwOnError: true);
            m_scriptText = valuesDictionary.GetValue(ScriptTextKey, string.Empty);
            PushScriptToHost();
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(ScriptTextKey, m_scriptText);
        }

        void PushScriptToHost() {
            m_luaScriptHost?.ReloadFromSource(m_scriptText);
        }

        public bool StartScript() => m_luaScriptHost?.Start() == true;

        public void StopScript() => m_luaScriptHost?.Stop();
    }
}
