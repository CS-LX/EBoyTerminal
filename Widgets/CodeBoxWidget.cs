using System.Reflection;
using Engine;
using Engine.Graphics;
using Game;

namespace EBoyTerminal {
    /// <summary>
    /// 自顶对齐的多行代码编辑框；后续可扩展行号与语法高亮。
    /// </summary>
    public class CodeBoxWidget : TextBoxWidget {
        public override void Draw(DrawContext dc) {
            Vector2 actual = ActualSize;
            float lineHeight = Font.GlyphHeight * FontScale * Font.Scale + FontSpacing.Y;
            m_actualSize = new Vector2(actual.X, lineHeight);
            try {
                base.Draw(dc);
            }
            finally {
                m_actualSize = actual;
            }
        }
    }
}
