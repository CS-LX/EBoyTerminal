using System.Text;
using Game;
using GameEntitySystem;
using TemplatesDatabase;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    public class ComponentMoonTerminal : Component {
        public const string ScriptTextKey = "ScriptText";
        const int MaxOutputLines = 32;

        readonly List<string> m_outputLines = new();

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

        public IReadOnlyList<string> OutputLines => m_outputLines;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_luaScriptHost = Entity.FindComponent<ComponentLuaScriptHost>(throwOnError: true);
            m_scriptText = valuesDictionary.GetValue(ScriptTextKey, string.Empty);
            PushScriptToHost();
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(ScriptTextKey, m_scriptText);
        }

        public string GetScreenText() {
            if (m_outputLines.Count == 0) {
                return string.Empty;
            }
            return string.Join("\n", m_outputLines);
        }

        public void ClearOutput() => m_outputLines.Clear();

        public void AppendOutput(string line) {
            m_outputLines.Add(line ?? string.Empty);
            while (m_outputLines.Count > MaxOutputLines) {
                m_outputLines.RemoveAt(0);
            }
        }

        void PushScriptToHost() {
            if (m_luaScriptHost == null) {
                return;
            }
            m_luaScriptHost.Host.OnOutput = AppendOutput;
            m_luaScriptHost.ReloadFromSource(m_scriptText);
        }

        public bool StartScript() {
            if (m_luaScriptHost?.State is LuaMachineState.Ready or LuaMachineState.Stopped) {
                ClearOutput();
            }
            return m_luaScriptHost?.Start() == true;
        }

        public void PauseScript() => m_luaScriptHost?.Pause();

        public void StopScript() => m_luaScriptHost?.Stop();
    }
}
