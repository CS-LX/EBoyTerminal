using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using SCIENEW.VoltNet;
using TemplatesDatabase;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    public class ComponentMoonTerminal : Component, IUpdateable {
        public const string ScriptTextKey = "ScriptText";
        public const string MaxOutputLinesKey = "MaxOutputLines";
        public const int DefaultMaxOutputLines = 512;

        readonly List<string> m_outputLines = new();

        string m_scriptText = string.Empty;

        ComponentBlockEntity m_blockEntity = null!;
        ComponentLuaScriptHost m_luaScriptHost = null!;
        SubsystemVoltNet m_subsystemVoltNet = null!;

        MoonTerminalScriptDialog? m_openDialog;
        WorkState m_workState = WorkState.Underpowered;
        bool m_wasPowered;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

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

        public bool IsPowered => m_workState == WorkState.Active;

        /// <summary>输出缓冲最多保留的行数；屏上实际可见行数由 Screen 尺寸动态决定。</summary>
        public int MaxOutputLines { get; private set; } = DefaultMaxOutputLines;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
            m_luaScriptHost = Entity.FindComponent<ComponentLuaScriptHost>(throwOnError: true);
            m_subsystemVoltNet = Project.FindSubsystem<SubsystemVoltNet>(throwOnError: true);
            m_scriptText = valuesDictionary.GetValue(ScriptTextKey, string.Empty);
            MaxOutputLines = Math.Max(1, valuesDictionary.GetValue(MaxOutputLinesKey, DefaultMaxOutputLines));
            PushScriptToHost();
            RefreshWorkState();
            m_wasPowered = IsPowered;
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(ScriptTextKey, m_scriptText);
            valuesDictionary.SetValue(MaxOutputLinesKey, MaxOutputLines);
        }

        public void SetOpenDialog(MoonTerminalScriptDialog? dialog) => m_openDialog = dialog;

        public void Update(float dt) {
            RefreshWorkState();
            if (m_wasPowered && !IsPowered) {
                HandlePowerLost();
            }
            m_wasPowered = IsPowered;
        }

        void RefreshWorkState() {
            VoltElement? voltElement = m_subsystemVoltNet.GetVoltElement(m_blockEntity.Coordinates);
            m_workState = voltElement?.CurrentWorkState ?? WorkState.Broken;
        }

        void HandlePowerLost() {
            StopScript();
            m_openDialog?.CloseDueToPowerLoss();
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

        public void AppendOutput(string text) {
            text = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (text.Length == 0) {
                AddOutputLine(string.Empty);
                return;
            }
            int lineStart = 0;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] != '\n') {
                    continue;
                }
                AddOutputLine(text.Substring(lineStart, i - lineStart));
                lineStart = i + 1;
            }
            if (lineStart < text.Length || text[^1] == '\n') {
                AddOutputLine(text.Substring(lineStart));
            }
        }

        void AddOutputLine(string line) {
            m_outputLines.Add(line);
            while (m_outputLines.Count > MaxOutputLines) {
                m_outputLines.RemoveAt(0);
            }
        }

        public void InitializeLuaHost() {
            if (m_luaScriptHost == null) {
                return;
            }
            m_luaScriptHost.Host.OnOutput = AppendOutput;
            m_luaScriptHost.Host.RegisterApiTable("terminal", new Dictionary<string, DynValue> {
                ["write"] = DynValue.NewCallback(TerminalWrite),
                ["print"] = DynValue.NewCallback(TerminalPrint),
                ["clear"] = DynValue.NewCallback(TerminalClear),
                ["lines"] = DynValue.NewCallback(TerminalLines)
            });
        }

        void PushScriptToHost() {
            if (m_luaScriptHost == null) {
                return;
            }
            InitializeLuaHost();
            m_luaScriptHost.ReloadFromSource(m_scriptText);
        }

        DynValue TerminalWrite(ScriptExecutionContext context, CallbackArguments args) {
            AppendOutput(args.Count > 0 ? args[0].ToPrintString() : string.Empty);
            return DynValue.Nil;
        }

        DynValue TerminalPrint(ScriptExecutionContext context, CallbackArguments args) {
            string text = args.Count == 0
                ? string.Empty
                : string.Join("\t", args.GetArray().Select(static value => value.ToPrintString()));
            AppendOutput(text);
            return DynValue.Nil;
        }

        DynValue TerminalClear(ScriptExecutionContext context, CallbackArguments args) {
            ClearOutput();
            return DynValue.Nil;
        }

        DynValue TerminalLines(ScriptExecutionContext context, CallbackArguments args) {
            return DynValue.NewNumber(m_outputLines.Count);
        }

        public bool StartScript() {
            if (!IsPowered) {
                return false;
            }
            if (m_luaScriptHost?.State is LuaMachineState.Ready or LuaMachineState.Stopped) {
                ClearOutput();
            }
            return m_luaScriptHost?.Start() == true;
        }

        public void PauseScript() => m_luaScriptHost?.Pause();

        public void StopScript() => m_luaScriptHost?.Stop();
    }
}
