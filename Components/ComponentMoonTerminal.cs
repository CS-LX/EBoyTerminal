using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using SCIENEW.VoltNet;
using TemplatesDatabase;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    public class ComponentMoonTerminal : Component, IUpdateable, ILuaScriptApiProvider {
        public const string ScriptTextKey = "ScriptText";
        public const string MaxOutputLinesKey = "MaxOutputLines";
        public const int DefaultMaxOutputLines = 512;
        public const int DialogOutputLineCount = 32;

        readonly List<string> m_outputLines = new();

        string m_scriptText = string.Empty;
        string? m_lastReportedError;

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

        public string? LastScriptError => m_luaScriptHost?.LastError;

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
            Entity.FindComponent<ComponentMoonTerminalElectric>(throwOnError: false)?.OnPowerLost();
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

        /// <summary>脚本对话框输出区：保留末尾 <see cref="DialogOutputLineCount"/> 行。</summary>
        public string GetDialogOutputText() => GetScreenText(DialogOutputLineCount);

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

        void AppendError(string message) {
            if (string.IsNullOrEmpty(message) || message == m_lastReportedError) {
                return;
            }
            m_lastReportedError = message;
            AppendOutput($"[error] {message}");
        }

        void AddOutputLine(string line) {
            m_outputLines.Add(line);
            while (m_outputLines.Count > MaxOutputLines) {
                m_outputLines.RemoveAt(0);
            }
        }

        public void WireScriptHost() {
            if (m_luaScriptHost == null) {
                return;
            }
            m_luaScriptHost.Host.OnOutput = AppendOutput;
            m_luaScriptHost.Host.OnError = AppendError;
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            context.AddMember("write", context.Callback((_, args) => {
                AppendOutput(args.Count > 0 ? args[0].ToPrintString() : string.Empty);
                return DynValue.Nil;
            }));
            context.AddMember("print", context.Callback((_, args) => {
                string text = args.Count == 0
                    ? string.Empty
                    : string.Join("\t", args.GetArray().Select(static value => value.ToPrintString()));
                AppendOutput(text);
                return DynValue.Nil;
            }));
            context.AddMember("clear", context.Callback((_, _) => {
                ClearOutput();
                return DynValue.Nil;
            }));
            context.AddMember("countLines", context.Callback((_, _) => DynValue.NewNumber(OutputLines.Count)));
            context.AddMember("direction", DynValue.NewNumber(MoonTerminalBlock.GetFacing(m_subsystemVoltNet.m_subsystemTerrain.Terrain.GetCellValue(m_blockEntity.Coordinates))));
        }

        void PushScriptToHost() {
            if (m_luaScriptHost == null) {
                return;
            }
            m_lastReportedError = null;
            WireScriptHost();
            m_luaScriptHost.ReloadFromSource(m_scriptText);
        }

        public void EnsureLastErrorVisible() {
            string? error = LastScriptError;
            if (!string.IsNullOrEmpty(error)) {
                AppendError(error);
            }
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
