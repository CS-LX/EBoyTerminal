using System.Xml.Linq;
using Engine;
using Game;
using SCIENEW;
using SCIENEW.Utils;

namespace EBoyTerminal {
    public class MoonTerminalScriptDialog : Dialog {
        readonly ComponentMoonTerminal m_component;
        readonly ComponentPlayer? m_player;

        string m_savedText;

        string m_baseTitle;

        bool m_dismissing;

        CodeBoxWidget m_scriptText;

        LabelWidget m_titleWidget;

        ClickableWidget m_saveButton;

        ClickableWidget m_runButton;

        ClickableWidget m_pauseButton;

        ClickableWidget m_stopButton;

        ClickableWidget m_closeButton;

        public MoonTerminalScriptDialog(ComponentMoonTerminal component, ComponentPlayer? player) {
            m_component = component;
            m_player = player;
            m_savedText = component.ScriptText ?? string.Empty;
            XElement node = ContentManager.Get<XElement>("Dialogs/MoonTerminalScriptDialog");
            LoadContents(this, node);
            m_scriptText = Children.Find<CodeBoxWidget>("ScriptText");
            m_titleWidget = Children.Find<LabelWidget>("Title");
            m_saveButton = Children.Find<ClickableWidget>("SaveButton");
            m_runButton = Children.Find<ClickableWidget>("RunButton");
            m_pauseButton = Children.Find<ClickableWidget>("PauseButton");
            m_stopButton = Children.Find<ClickableWidget>("StopButton");
            m_closeButton = Children.Find<ClickableWidget>("CloseButton");
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
            m_component.SetOpenDialog(this);
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
            else if (m_saveButton.IsClicked) {
                Save();
            }
            else if (m_runButton.IsClicked) {
                if (!m_component.IsPowered) {
                    ShowNoPowerMessage();
                    return;
                }
                Save();
                m_component.StartScript();
                UpdateTitle();
            }
            else if (m_pauseButton.IsClicked) {
                m_component.PauseScript();
                UpdateTitle();
            }
            else if (m_stopButton.IsClicked) {
                m_component.StopScript();
                UpdateTitle();
            }
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

        void ShowNoPowerMessage() {
            m_player?.ComponentGui.DisplaySmallMessage(
                LanguageUtils.GetText(m_component, "NoPowerInteract"),
                Color.White,
                blinking: true,
                playNotificationSound: true);
        }

        void Save() {
            m_savedText = m_scriptText.Text;
            m_component.ScriptText = m_savedText;
            UpdateTitle();
        }

        void Dismiss() {
            m_component.SetOpenDialog(null);
            DialogsManager.HideDialog(this);
        }
    }
}
