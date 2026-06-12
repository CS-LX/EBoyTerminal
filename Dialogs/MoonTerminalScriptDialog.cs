using System.Xml.Linq;
using Engine;
using Game;
using SCIENEW;

namespace EBoyTerminal {
    public class MoonTerminalScriptDialog : Dialog {
        readonly ComponentMoonTerminal m_component;

        readonly string m_originalText;

        TextBoxWidget m_scriptText;

        ButtonWidget m_okButton;

        ButtonWidget m_cancelButton;

        public MoonTerminalScriptDialog(ComponentMoonTerminal component) {
            m_component = component;
            m_originalText = component.ScriptText ?? string.Empty;
            XElement node = ContentManager.Get<XElement>("Dialogs/MoonTerminalScriptDialog");
            LoadContents(this, node);
            m_scriptText = Children.Find<TextBoxWidget>("ScriptText");
            m_okButton = Children.Find<ButtonWidget>("OkButton");
            m_cancelButton = Children.Find<ButtonWidget>("CancelButton");
            m_scriptText.Font = IndustrialModLoader.PixelFont;
            m_scriptText.TextureLinearFilter = false;
            m_scriptText.FontScale = 1f;
            m_scriptText.AutoSize = false;
            m_scriptText.EnterAsNewLine = true;
            m_scriptText.MaximumLinesCount = 512;
            m_scriptText.SwitchTextBoxWhenTabbed = false;
            m_scriptText.IndentAsSpace = true;
            m_scriptText.Text = m_originalText;
            m_scriptText.HasFocus = true;
        }

        public override void Update() {
            if (Input.Cancel) {
                Dismiss(false);
            }
            else if (Input.Ok) {
                Dismiss(true);
            }
            else if (m_okButton.IsClicked) {
                Dismiss(true);
            }
            else if (m_cancelButton.IsClicked) {
                Dismiss(false);
            }
        }

        void Dismiss(bool save) {
            if (save) {
                m_component.ScriptText = m_scriptText.Text;
            }
            DialogsManager.HideDialog(this);
        }
    }
}
