using System.Xml.Linq;
using Engine;
using Game;
using SCIENEW;
using SCIENEW.Utils;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    public class MoonTerminalScriptDialog : Dialog {
        readonly ComponentMoonTerminal m_component;
        readonly ComponentPlayer? m_player;

        string m_savedText;

        string m_baseTitle;

        bool m_dismissing;

        bool m_outputExpanded;

        string m_lastSyncedOutput = string.Empty;

        LuaMachineState m_previousLuaState = LuaMachineState.Stopped;

        CodeBoxWidget m_scriptText;

        LabelWidget m_titleWidget;

        LabelWidget m_outputToggleHint;

        LabelWidget m_outputText;

        Widget m_outputBody;

        ScrollPanelWidget m_outputScroll;

        ClickableWidget m_saveButton;

        ClickableWidget m_runButton;

        ClickableWidget m_pauseButton;

        ClickableWidget m_stopButton;

        ClickableWidget m_closeButton;

        ClickableWidget m_outputToggleButton;

        public MoonTerminalScriptDialog(ComponentMoonTerminal component, ComponentPlayer? player) {
            m_component = component;
            m_player = player;
            m_savedText = component.ScriptText ?? string.Empty;
            XElement node = ContentManager.Get<XElement>("Dialogs/MoonTerminalScriptDialog");
            LoadContents(this, node);
            m_scriptText = Children.Find<CodeBoxWidget>("ScriptText");
            m_titleWidget = Children.Find<LabelWidget>("Title");
            m_outputToggleHint = Children.Find<LabelWidget>("OutputToggleHint");
            m_outputText = Children.Find<LabelWidget>("OutputText");
            m_outputBody = Children.Find<Widget>("OutputBody");
            m_outputScroll = Children.Find<ScrollPanelWidget>("OutputScroll");
            m_saveButton = Children.Find<ClickableWidget>("SaveButton");
            m_runButton = Children.Find<ClickableWidget>("RunButton");
            m_pauseButton = Children.Find<ClickableWidget>("PauseButton");
            m_stopButton = Children.Find<ClickableWidget>("StopButton");
            m_closeButton = Children.Find<ClickableWidget>("CloseButton");
            m_outputToggleButton = Children.Find<ClickableWidget>("OutputToggleButton");
            m_baseTitle = m_titleWidget.Text;
            m_scriptText.Font = IndustrialModLoader.PixelFont;
            m_scriptText.TextureLinearFilter = false;
            m_scriptText.FontScale = 1f;
            m_scriptText.AutoSize = false;
            m_scriptText.EnterAsNewLine = true;
            m_scriptText.MaximumLinesCount = 512;
            m_scriptText.SwitchTextBoxWhenTabbed = false;
            m_scriptText.IndentAsSpace = true;
            m_scriptText.Text = m_savedText;
            m_scriptText.ClearUndoHistory();
            m_scriptText.TextChanged += OnScriptTextChanged;
            m_scriptText.HasFocus = true;
            m_outputText.Font = IndustrialModLoader.PixelFont;
            m_outputText.FontScale = 1f;
            m_outputText.TextureLinearFilter = false;
            m_outputText.Text = string.Empty;
            SetOutputExpanded(expanded: false);
            m_component.SetOpenDialog(this);
            m_previousLuaState = m_component.LuaState;
            SyncOutputPanel(forceScroll: true);
            UpdateTitle();
        }

        void OnScriptTextChanged(TextBoxWidget _) => UpdateTitle();

        void UpdateTitle() {
            bool dirty = m_scriptText.Text != m_savedText;
            string status = m_component.LuaState.ToString();
            m_titleWidget.Text = dirty ? $"{m_baseTitle} [{status}] *" : $"{m_baseTitle} [{status}]";
        }

        public override void Update() {
            if (!m_component.IsPowered) {
                CloseDueToPowerLoss();
                return;
            }
            if (Input.Cancel || m_closeButton.IsClicked) {
                Dismiss();
            }
            else if (m_outputToggleButton.IsClicked) {
                SetOutputExpanded(!m_outputExpanded);
            }
            else if (m_saveButton.IsClicked) {
                Save();
            }
            else if (m_runButton.IsClicked) {
                Save();
                SetOutputExpanded(expanded: true);
                if (!m_component.StartScript()) {
                    m_component.EnsureLastErrorVisible();
                }
            }
            else if (m_pauseButton.IsClicked) {
                m_component.PauseScript();
            }
            else if (m_stopButton.IsClicked) {
                m_component.StopScript();
            }

            HandleLuaStateTransitions();
            SyncOutputPanel(forceScroll: false);
            UpdateTitle();
        }

        void HandleLuaStateTransitions() {
            LuaMachineState state = m_component.LuaState;
            if (state != m_previousLuaState && state is LuaMachineState.Running or LuaMachineState.Error) {
                SetOutputExpanded(expanded: true);
            }
            m_previousLuaState = state;
        }

        void SetOutputExpanded(bool expanded) {
            m_outputExpanded = expanded;
            m_outputBody.IsVisible = expanded;
            m_outputToggleHint.Text = LanguageUtils.GetText(
                this,
                expanded ? "OutputCollapse" : "OutputExpand");
        }

        void SyncOutputPanel(bool forceScroll) {
            string output = m_component.GetScreenText(ComponentMoonTerminal.DialogOutputLineCount);
            if (output != m_lastSyncedOutput) {
                m_lastSyncedOutput = output;
                m_outputText.Text = string.IsNullOrEmpty(output) ? string.Empty : output;
                forceScroll = true;
            }
            if (!m_outputExpanded) {
                return;
            }
            if (forceScroll) {
                ScrollOutputToBottom();
            }
        }

        void ScrollOutputToBottom() {
            float scrollAreaLength = m_outputScroll.CalculateScrollAreaLength();
            float viewHeight = m_outputScroll.ActualSize.Y;
            m_outputScroll.ScrollPosition = Math.Max(0f, scrollAreaLength - viewHeight);
        }

        public void CloseDueToPowerLoss() {
            if (m_dismissing) {
                return;
            }
            m_dismissing = true;
            m_component.StopScript();
            Save();
            m_player?.ComponentGui.DisplaySmallMessage(
                LanguageUtils.GetText(m_component, "PowerLostClose"),
                Color.White,
                blinking: true,
                playNotificationSound: true);
            Dismiss();
        }

        void Save() {
            m_savedText = m_scriptText.Text;
            m_component.ScriptText = m_savedText;
            SyncOutputPanel(forceScroll: true);
        }

        void Dismiss() {
            m_component.SetOpenDialog(null);
            DialogsManager.HideDialog(this);
        }
    }
}
