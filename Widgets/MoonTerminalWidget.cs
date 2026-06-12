using System.Xml.Linq;
using Engine;
using Game;
using SCIENEW;

namespace EBoyTerminal {
    public class MoonTerminalWidget : CanvasWidget {
        public ComponentMoonTerminal m_component;

        public TextBoxWidget m_scriptText;

        public MoonTerminalWidget(ComponentMoonTerminal component) {
            m_component = component;
            XElement node = ContentManager.Get<XElement>("Widgets/MoonTerminalWidget");
            LoadContents(this, node);
            m_scriptText = Children.Find<TextBoxWidget>("ScriptText");
            m_scriptText.Font = IndustrialModLoader.PixelFont;
            m_scriptText.TextureLinearFilter = false;
            m_scriptText.Text = m_component.ScriptText;
        }

        public override void Update() {
            if (!m_component.IsAddedToProject) {
                ParentWidget.Children.Remove(this);
                return;
            }
            if (m_scriptText.Text != m_component.ScriptText) {
                m_component.ScriptText = m_scriptText.Text;
            }
        }
    }
}
