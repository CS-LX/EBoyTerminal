using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;
using Game;

namespace EBoyTerminal {
    /// <summary>
    /// 自顶对齐的多行代码编辑框，提供垂直滚动与行号栏。
    /// </summary>
    public class CodeBoxWidget : TextBoxWidget {
        const float GutterPadding = 8f;
        const float ScrollBarThickness = 4f;
        const float ScrollBarMinThumbLength = 16f;
        static readonly Color ScrollBarTrackColor = new(0, 0, 0, 80);
        static readonly Color ScrollBarThumbColor = new(220, 220, 220, 160);

        public float ScrollY { get; set; }

        public bool ShowLineNumbers { get; set; } = true;

        public Color LineNumberColor { get; set; } = new(160, 160, 160, 255);

        public float LineNumberGutterWidth => ShowLineNumbers ? CalculateLineNumberGutterWidth() : 0f;

        public CodeBoxWidget() {
            TextChanged += _ => {
                LimitScroll();
                if (HasFocus) {
                    EnsureCaretVisible();
                }
            };
        }

        public override void Update() {
            foreach (UpdateTask task in TasksQueue) {
                task.Run(this);
            }
            TasksQueue.Clear();
            LimitScroll();

            if (Input.Click.HasValue) {
                if (HitTestGlobal(Input.Click.Value.Start) == this
                    && HitTestGlobal(Input.Click.Value.End) == this) {
                    FocusStartTime = Time.RealTime;
#if WINDOWS
                    ShowInputMethod();
#elif ANDROID || BROWSER
                    Keyboard.ShowKeyboard(
                        Title ?? "",
                        Description ?? "",
                        Text,
                        PasswordMode,
                        text => {
                            Text = text;
                            HasFocus = false;
                        },
                        () => HasFocus = false
                    );
#endif
                    HasFocus = true;
                    Caret = CalculateClickedCharacterIndex(ScreenToWidget(Input.Click.Value.Start));
                }
                else if (FocusedTextBox == this) {
                    CloseInputMethod();
                    HasFocus = false;
                }
                SelectionLength = 0;
            }
#if !ANDROID
            if (Input.Scroll.HasValue
                && Input.MousePosition.HasValue
                && HitTestGlobal(Input.MousePosition.Value) == this) {
                ScrollY -= 40f * Input.Scroll.Value.Z;
                LimitScroll();
            }
            if (Input.Drag.HasValue) {
                if (DragStartTime < 0) {
                    DragStartTime = Time.RealTime;
                    SelectionLength = 0;
                    if (HitTestGlobal(Input.Drag.Value) == this) {
                        HasFocus = true;
                        SelectionStarted = true;
                        ShowInputMethod();
                        Caret = CalculateClickedCharacterIndex(ScreenToWidget(Input.Drag.Value));
                        DragStartedInsideTextBox = true;
                    }
                    else {
                        SelectionStarted = false;
                        DragStartedInsideTextBox = false;
                        HasFocus = false;
                    }
                }
                else if (Time.RealTime - DragStartTime > 0 && DragStartedInsideTextBox) {
                    int caret2 = CalculateClickedCharacterIndex(ScreenToWidget(Input.Drag.Value));
                    if (SelectionStarted) {
                        SelectionLength = caret2 - Caret;
                    }
                    if (Math.Abs(caret2 - Caret) > 1) {
                        ScrollStarted = true;
                    }
                }
                LastDragPosition = Input.Drag.Value;
            }
            else if (DragStartTime >= 0
                && !Input.Drag.HasValue) {
                DragStartTime = -1;
                SelectionStarted = false;
                ScrollStarted = false;
            }

            if (HasFocus && string.IsNullOrEmpty(CompositionText)) {
                if (Keyboard.IsKeyDownRepeat(Key.LeftArrow)) {
                    Caret = Math.Max(0, Caret - 1);
                    SelectionLength = 0;
                    SelectionStarted = false;
                    FocusStartTime = Time.RealTime;
                }
                if (Keyboard.IsKeyDownRepeat(Key.RightArrow)) {
                    Caret = Math.Min(Text.Length, Caret + 1);
                    SelectionLength = 0;
                    SelectionStarted = false;
                    FocusStartTime = Time.RealTime;
                }
                if (Keyboard.IsKeyDownOnce(Key.Home)
                    || Keyboard.IsKeyDownOnce(Key.UpArrow)) {
                    Caret = 0;
                    SelectionLength = 0;
                    SelectionStarted = false;
                    FocusStartTime = Time.RealTime;
                }
                if (Keyboard.IsKeyDownOnce(Key.End)
                    || Keyboard.IsKeyDownOnce(Key.DownArrow)) {
                    Caret = Text.Length;
                    SelectionLength = 0;
                    SelectionStarted = false;
                    FocusStartTime = Time.RealTime;
                }
            }
            if (HasFocus && Keyboard.IsKeyDownOnce(Key.Escape)) {
                HasFocus = false;
                CloseInputMethod();
            }

            if (HasFocus
                && Caret != Text.Length
                && Keyboard.IsKeyDownRepeat(Key.Delete)) {
                if (Keyboard.IsKeyDown(Key.Control)) {
                    Delete(count: -1, character: Text[Caret]);
                }
                else {
                    Delete();
                }
            }
            if (HasFocus && Keyboard.IsKeyDownOnce(Key.Enter)) {
                if (EnterAsNewLine) {
                    EnterCharacter('\n');
                }
                else {
                    HasFocus = false;
                    CloseInputMethod();
                }
            }

            if (!InputMethodEnabled && HasFocus) {
                if (Caret != 0
                    && (Keyboard.IsKeyDownRepeat(Key.Delete) || Keyboard.IsKeyDownRepeat(Key.BackSpace))) {
                    if (Keyboard.IsKeyDown(Key.Control)) {
                        BackSpace(count: -1, character: Text[Caret - 1]);
                    }
                    else {
                        BackSpace();
                    }
                    FocusStartTime = Time.RealTime;
                }

                if (!SwitchTextBoxWhenTabbed
                    && Keyboard.IsKeyDownRepeat(Key.Tab)) {
                    EnterCharacter('\t');
                }

                char? lastChar = Keyboard.LastChar;
                if (lastChar != null
                    && lastChar != '\n') {
                    EnterCharacter(lastChar.Value);
                }
            }

            if (HasFocus
                && SwitchTextBoxWhenTabbed
                && Keyboard.IsKeyDownRepeat(Key.Tab)) {
                if (RootWidget is not ContainerWidget rootWidget) {
                    return;
                }
                List<TextBoxWidget> textBoxes = FindTextBoxWidgets(rootWidget);
                int thisIndex = textBoxes.IndexOf(this);
                FocusedTextBox = textBoxes[(thisIndex + 1) % textBoxes.Count];
            }
            if (HasFocus
                && SelectionLength != 0
                && !InputMethodEnabled) {
                if (Keyboard.IsKeyDown(Key.Control)
                    && Keyboard.IsKeyDownOnce(Key.C)) {
                    ClipboardManager.ClipboardString = SelectionString;
                }
                if (Keyboard.IsKeyDown(Key.Control)
                    && Keyboard.IsKeyDownOnce(Key.X)) {
                    ClipboardManager.ClipboardString = SelectionString;
                    DeleteSelection();
                }
                if (Keyboard.IsKeyDown(Key.Control)
                    && Keyboard.IsKeyDownOnce(Key.V)) {
                    string text = ClipboardManager.ClipboardString;
                    if (text != null) {
                        EnterText(ClipboardManager.ClipboardString);
                    }
                }
            }
#endif
            if (HasFocus) {
                EnsureCaretVisible();
            }
        }

        public override void Draw_(DrawContext dc) {
            string textToDraw = Text.Replace("\t", new string(' ', IndentWidth));
            int caretIndex = Text[..Caret].Sum(c => c == '\t' ? IndentWidth : 1);
            int selectionLength = SelectionLength == 0
                ? 0
                : SelectionString.Sum(c => c == '\t' ? IndentWidth : 1) * (SelectionLength / Math.Abs(SelectionLength));
            int selectionStart = caretIndex;
            if (SelectionLength < 0) {
                selectionStart += selectionLength;
                selectionLength = -selectionLength;
            }
            if (PasswordMode) {
                textToDraw = new string('*', textToDraw.Length);
            }

            LimitScroll();
            float gutterWidth = LineNumberGutterWidth;
            float lineHeight = CalculateLineHeight();
            FlatBatch2D flatBatch = dc.PrimitivesRenderer2D.FlatBatch(blendState: BlendState.NonPremultiplied);
            FlatBatch2D scrollBarBatch = dc.PrimitivesRenderer2D.FlatBatch(2, blendState: BlendState.NonPremultiplied);
            FontBatch2D fontBatch = dc.PrimitivesRenderer2D.FontBatch(
                Font,
                samplerState: TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp
            );
            FontBatch2D lineNumberBatch = dc.PrimitivesRenderer2D.FontBatch(
                Font,
                1,
                samplerState: TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp
            );
            FlatBatch2D underlineFlatBatch = dc.PrimitivesRenderer2D.FlatBatch(1);
            List<TextDrawItem> drawItems = new(3);
            Vector2 currentDrawPosition = new(gutterWidth, lineHeight / 2f);

            string[] lines = textToDraw.Split('\n');
            int charIndex = 0;
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                float y = lineHeight / 2f + i * lineHeight;
                if (ShowLineNumbers) {
                    lineNumberBatch.QueueText(
                        (i + 1).ToString(),
                        new Vector2(gutterWidth - GutterPadding, y),
                        0,
                        LineNumberColor,
                        TextAnchor.Right | TextAnchor.VerticalCenter,
                        new Vector2(FontScale),
                        FontSpacing
                    );
                }

                if (selectionLength != 0
                    && selectionStart < charIndex + line.Length
                    && selectionStart + selectionLength > charIndex) {
                    int lineStart = charIndex;
                    int selectionStartInLine = Math.Max(selectionStart - lineStart, 0);
                    int selectionEndInLine = Math.Min(selectionStart + selectionLength - lineStart, line.Length);
                    int actualSelectionLength = selectionEndInLine - selectionStartInLine;
                    drawItems.Add(
                        new SelectionDrawItem(
                            flatBatch,
                            line,
                            selectionStartInLine,
                            actualSelectionLength,
                            Font,
                            FontSpacing,
                            new Color(64, 64, 255, 128),
                            Font.GlyphHeight * FontScale * Font.Scale,
                            new Vector2(FontScale)
                        )
                    );
                }
                if (charIndex <= caretIndex
                    && charIndex + line.Length >= caretIndex) {
                    string[] split = SplitStringAt(line, caretIndex - charIndex);
                    drawItems.Add(new NormalDrawItem(line, 0, split[0].Length, fontBatch, FontScale, FontSpacing, Color));
                    if (SelectionLength == 0
                        && FocusedTextBox == this
                        && ((Time.RealTime - FocusStartTime - 0.4) % 1.0 <= 0.3f || (Time.RealTime - FocusStartTime - 0.4) % 1.0 >= 0.8f)) {
                        drawItems.Add(
                            new CaretDrawItem(
                                flatBatch,
                                1,
                                Font.GlyphHeight * FontScale * Font.Scale,
                                CompositionText,
                                CompositionTextCaret,
                                Font,
                                FontSpacing,
                                Color.White,
                                new Vector2(FontScale)
                            )
                        );
                    }
                    drawItems.Add(new CompositionTextDrawItem(CompositionText ?? "", fontBatch, underlineFlatBatch, FontScale, FontSpacing, Color));
                    if (split.Length > 1) {
                        drawItems.Add(new NormalDrawItem(line, split[0].Length, split[1].Length, fontBatch, FontScale, FontSpacing, Color));
                    }
                }
                else {
                    drawItems.Add(new NormalDrawItem(textToDraw, charIndex, line.Length, fontBatch, FontScale, FontSpacing, Color));
                }
                drawItems.Add(new EndOfLineDrawItem(LineNumberGutterWidth, Font, FontSpacing, FontScale));
                charIndex += line.Length + 1;
            }

            foreach (TextDrawItem drawItem in drawItems) {
                drawItem.Draw(ref currentDrawPosition);
            }

            Matrix textTransform = Matrix.CreateTranslation(new Vector3(-Scroll, -ScrollY, 0));
            Matrix lineNumberTransform = Matrix.CreateTranslation(new Vector3(0, -ScrollY, 0));
            flatBatch.TransformTriangles(textTransform);
            fontBatch.TransformTriangles(textTransform);
            underlineFlatBatch.TransformLines(textTransform);
            lineNumberBatch.TransformTriangles(lineNumberTransform);
            DrawScrollBars(scrollBarBatch);
            fontBatch.TransformTriangles(GlobalTransform);
            lineNumberBatch.TransformTriangles(GlobalTransform);
            flatBatch.TransformTriangles(GlobalTransform);
            flatBatch.TransformLines(GlobalTransform);
            underlineFlatBatch.TransformLines(GlobalTransform);
            scrollBarBatch.TransformTriangles(GlobalTransform);
        }

        float CalculateLineHeight() {
            return Font.GlyphHeight * FontScale * Font.Scale + FontSpacing.Y;
        }

        float CalculateLineNumberGutterWidth() {
            int lineCount = Text.Length == 0 ? 1 : Text.Count(c => c == '\n') + 1;
            int digits = Math.Max(2, lineCount.ToString().Length);
            string sample = new('9', digits);
            return Font.MeasureText(sample, new Vector2(FontScale), FontSpacing).X + 2f * GutterPadding;
        }

        Vector2 CalculateTextAreaSize() {
            return new Vector2(
                Math.Max(0f, ActualSize.X - LineNumberGutterWidth - ScrollBarThickness),
                Math.Max(0f, ActualSize.Y - ScrollBarThickness)
            );
        }

        float CalculateTextHeight() {
            return (Text.Count(c => c == '\n') + 1) * CalculateLineHeight();
        }

        float CalculateTextWidth() {
            string text = PasswordMode ? new string('*', Text.Length) : Text;
            string[] lines = text.Replace("\t", new string(' ', IndentWidth)).ReplaceLineEndings("\n").Split('\n');
            float maxWidth = 0f;
            foreach (string line in lines) {
                maxWidth = Math.Max(maxWidth, Font.MeasureText(line, new Vector2(FontScale), FontSpacing).X);
            }
            return maxWidth;
        }

        void LimitScroll() {
            Vector2 textAreaSize = CalculateTextAreaSize();
            float scrollRangeX = Math.Max(0f, CalculateTextWidth() - textAreaSize.X);
            float scrollRangeY = Math.Max(0f, CalculateTextHeight() - textAreaSize.Y);
            m_scroll = Math.Clamp(m_scroll, 0f, scrollRangeX);
            ScrollY = Math.Clamp(ScrollY, 0f, scrollRangeY);
        }

        void EnsureCaretVisible() {
            Vector2 caretPosition = CalculateCaretPosition();
            Vector2 textAreaSize = CalculateTextAreaSize();
            float lineHeight = CalculateLineHeight();
            float marginX = Math.Min(24f, Math.Max(0f, textAreaSize.X / 4f));

            if (caretPosition.X - m_scroll < marginX) {
                m_scroll = caretPosition.X - marginX;
            }
            else if (caretPosition.X - m_scroll > textAreaSize.X - marginX) {
                m_scroll = caretPosition.X - textAreaSize.X + marginX;
            }
            if (caretPosition.Y - ScrollY < 0f) {
                ScrollY = caretPosition.Y;
            }
            else if (caretPosition.Y + lineHeight - ScrollY > textAreaSize.Y) {
                ScrollY = caretPosition.Y + lineHeight - textAreaSize.Y;
            }
            LimitScroll();
        }

        Vector2 CalculateCaretPosition() {
            string textBeforeCaret = Text[..Caret].Replace("\t", new string(' ', IndentWidth)).ReplaceLineEndings("\n");
            string[] lines = textBeforeCaret.Split('\n');
            int lineIndex = Math.Max(0, lines.Length - 1);
            string lineBeforeCaret = lines.Length == 0 ? string.Empty : lines[^1];
            float x = Font.MeasureText(lineBeforeCaret, new Vector2(FontScale), FontSpacing).X;
            if (!string.IsNullOrEmpty(CompositionText)) {
                x += Font.MeasureText(CompositionText, 0, CompositionTextCaret, new Vector2(FontScale), FontSpacing).X;
            }
            return new Vector2(x, lineIndex * CalculateLineHeight());
        }

        void DrawScrollBars(FlatBatch2D flatBatch) {
            Vector2 textAreaSize = CalculateTextAreaSize();
            float contentWidth = CalculateTextWidth();
            float contentHeight = CalculateTextHeight();
            float verticalRange = Math.Max(0f, contentHeight - textAreaSize.Y);
            float horizontalRange = Math.Max(0f, contentWidth - textAreaSize.X);

            if (verticalRange > 0f && ActualSize.Y > ScrollBarThickness) {
                Vector2 trackStart = new(ActualSize.X - ScrollBarThickness, 0f);
                Vector2 trackEnd = new(ActualSize.X, textAreaSize.Y);
                flatBatch.QueueQuad(trackStart, trackEnd, 0f, ScrollBarTrackColor);

                float thumbLength = Math.Max(ScrollBarMinThumbLength, textAreaSize.Y * textAreaSize.Y / contentHeight);
                float thumbTravel = Math.Max(0f, textAreaSize.Y - thumbLength);
                float thumbY = verticalRange > 0f ? thumbTravel * ScrollY / verticalRange : 0f;
                flatBatch.QueueQuad(
                    new Vector2(ActualSize.X - ScrollBarThickness, thumbY),
                    new Vector2(ActualSize.X, thumbY + thumbLength),
                    0f,
                    ScrollBarThumbColor
                );
            }

            if (horizontalRange > 0f && ActualSize.X > LineNumberGutterWidth + ScrollBarThickness) {
                Vector2 trackStart = new(LineNumberGutterWidth, ActualSize.Y - ScrollBarThickness);
                Vector2 trackEnd = new(ActualSize.X - ScrollBarThickness, ActualSize.Y);
                flatBatch.QueueQuad(trackStart, trackEnd, 0f, ScrollBarTrackColor);

                float trackLength = Math.Max(0f, ActualSize.X - LineNumberGutterWidth - ScrollBarThickness);
                float thumbLength = Math.Max(ScrollBarMinThumbLength, trackLength * trackLength / contentWidth);
                float thumbTravel = Math.Max(0f, trackLength - thumbLength);
                float thumbX = horizontalRange > 0f ? thumbTravel * m_scroll / horizontalRange : 0f;
                flatBatch.QueueQuad(
                    new Vector2(LineNumberGutterWidth + thumbX, ActualSize.Y - ScrollBarThickness),
                    new Vector2(LineNumberGutterWidth + thumbX + thumbLength, ActualSize.Y),
                    0f,
                    ScrollBarThumbColor
                );
            }
        }

        int CalculateClickedCharacterIndex(Vector2 widgetPosition) {
            string text = PasswordMode ? new string('*', Text.Length) : Text;
            string[] lines = text.ReplaceLineEndings("\n").Split('\n');
            if (lines.Length == 0) {
                return 0;
            }

            float lineHeight = CalculateLineHeight();
            int lineIndex = Math.Clamp((int)MathF.Floor((widgetPosition.Y + ScrollY) / lineHeight), 0, lines.Length - 1);
            int textIndex = 0;
            for (int i = 0; i < lineIndex; i++) {
                textIndex += lines[i].Length + 1;
            }

            string line = lines[lineIndex];
            float x = widgetPosition.X - LineNumberGutterWidth + Scroll;
            if (x <= 0f) {
                return textIndex;
            }

            float currentPosition = 0f;
            for (int i = 0; i < line.Length; i++) {
                char letter = line[i];
                string drawText = letter == '\t' ? new string(' ', IndentWidth) : letter.ToString();
                float width = Font.MeasureText(drawText, new Vector2(FontScale), FontSpacing).X;
                if (currentPosition + width / 2f > x) {
                    return textIndex + i;
                }
                currentPosition += width;
            }
            return textIndex + line.Length;
        }

        static List<TextBoxWidget> FindTextBoxWidgets(ContainerWidget widget) {
            List<TextBoxWidget> textBoxes = new(16);
            foreach (Widget child in widget.Children) {
                if (child is TextBoxWidget textBoxWidget) {
                    textBoxes.Add(textBoxWidget);
                }
                if (child is not ContainerWidget containerWidget) {
                    continue;
                }
                List<TextBoxWidget> result = FindTextBoxWidgets(containerWidget);
                if (result.Count is not 0) {
                    textBoxes.AddRange(result);
                }
            }
            return textBoxes;
        }

        public new class EndOfLineDrawItem(float x, BitmapFont font, Vector2 fontSpacing, float fontScale) : TextDrawItem {
            public override void Draw(ref Vector2 position) {
                position.X = x;
                position.Y += font.GlyphHeight * font.Scale * fontScale + fontSpacing.Y;
            }
        }
    }
}
