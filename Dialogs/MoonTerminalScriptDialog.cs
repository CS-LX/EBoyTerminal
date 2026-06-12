using System.Xml.Linq;
using Engine;
using Game;
using SCIENEW;

namespace EBoyTerminal {
    public class MoonTerminalScriptDialog : Dialog {
        readonly ComponentMoonTerminal m_component;

        string m_savedText;

        string m_baseTitle = string.Empty;

        CodeBoxWidget m_scriptText;

        LabelWidget m_titleWidget;

        ClickableWidget m_saveButton;

        ClickableWidget m_runButton;

        ClickableWidget m_stopButton;

        ClickableWidget m_closeButton;

        public MoonTerminalScriptDialog(ComponentMoonTerminal component) {
            m_component = component;
            m_savedText = component.ScriptText ?? string.Empty;
            XElement node = ContentManager.Get<XElement>("Dialogs/MoonTerminalScriptDialog");
            LoadContents(this, node);
            m_scriptText = Children.Find<CodeBoxWidget>("ScriptText");
            m_titleWidget = Children.Find<LabelWidget>("Title");
            m_saveButton = Children.Find<ClickableWidget>("SaveButton");
            m_runButton = Children.Find<ClickableWidget>("RunButton");
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
            m_scriptText.TextChanged += OnScriptTextChanged;
            m_scriptText.HasFocus = true;
            UpdateTitle();
        }

        void OnScriptTextChanged(TextBoxWidget _) => UpdateTitle();

        void UpdateTitle() {
            bool dirty = m_scriptText.Text != m_savedText;
            string status = m_component.LuaState.ToString();
            m_titleWidget.Text = dirty ? $"{m_baseTitle} [{status}] *" : $"{m_baseTitle} [{status}]";
        }

        public override void Update() {
            if (Input.Cancel || m_closeButton.IsClicked) {
                Dismiss();
            }
            else if (m_saveButton.IsClicked) {
                Save();
            }
            else if (m_runButton.IsClicked) {
                Save();
                m_component.StartScript();
                UpdateTitle();
            }
            else if (m_stopButton.IsClicked) {
                m_component.StopScript();
                UpdateTitle();
            }
        }

        void Save() {
            m_savedText = m_scriptText.Text;
            m_component.ScriptText = m_savedText;
            UpdateTitle();
        }

        void Dismiss() {
            DialogsManager.HideDialog(this);
        }
    }
}
