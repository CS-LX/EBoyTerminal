using Game;
using GameEntitySystem;
using TemplatesDatabase;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    public class ComponentMoonTerminal : Component {
        public const string ScriptTextKey = "ScriptText";
        public const string MaxOutputLinesKey = "MaxOutputLines";
        public const int DefaultMaxOutputLines = 512;

        readonly List<string> m_outputLines = new();

        string m_scriptText = string.Empty;

        ComponentLuaScriptHost m_luaScriptHost = null!;

        public string ScriptText {
            get => m_scriptText;
            set {
                string text = value ?? string.Empty;
                if (m_scriptText == text) {
                    return;
                }
                m_scriptText = text;
                PushScriptToHost();
            }
        }

        public ComponentLuaScriptHost LuaScriptHost => m_luaScriptHost;

        public LuaMachineState LuaState => m_luaScriptHost?.State ?? LuaMachineState.Stopped;

        public IReadOnlyList<string> OutputLines => m_outputLines;

        /// <summary>输出缓冲最多保留的行数；屏上实际可见行数由 Screen 尺寸动态决定。</summary>
        public int MaxOutputLines { get; private set; } = DefaultMaxOutputLines;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_luaScriptHost = Entity.FindComponent<ComponentLuaScriptHost>(throwOnError: true);
            m_scriptText = valuesDictionary.GetValue(ScriptTextKey, string.Empty);
            MaxOutputLines = Math.Max(1, valuesDictionary.GetValue(MaxOutputLinesKey, DefaultMaxOutputLines));
            PushScriptToHost();
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(ScriptTextKey, m_scriptText);
            valuesDictionary.SetValue(MaxOutputLinesKey, MaxOutputLines);
        }

        /// <summary>取缓冲末尾最多 <paramref name="maxDisplayLines"/> 行，类似终端滚动区。</summary>
        public string GetScreenText(int maxDisplayLines) {
            if (m_outputLines.Count == 0) {
                return string.Empty;
            }
            int lineCount = Math.Max(1, Math.Min(MaxOutputLines, maxDisplayLines));
            int start = Math.Max(0, m_outputLines.Count - lineCount);
            return string.Join("\n", m_outputLines.Skip(start));
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
