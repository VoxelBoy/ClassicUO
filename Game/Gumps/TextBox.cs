using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.Game.Gumps
{
    public class TextBox : GumpControl
    {
        const float CARAT_BLINK_TIME = 500f;

        private bool _caratBlink;
        private readonly RenderedText _text, _carat;
        private Point _caretPosition;
        private int _caretIndex;
        private int _offset;
        private string _plainText;


        public TextBox() : base()
        {
            _text = new RenderedText()
            {
                IsUnicode = true,
                Font = 1,
            };
            _carat = new RenderedText("_")
            {
                IsUnicode = true,
                Font = 1,
            };

            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
        }

        public TextBox(string[] parts, string[] lines) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            Hue = Hue.Parse(parts[5]);
            Graphic = Graphic.Parse(parts[6]);
            Text = lines[int.Parse(parts[7])];
            MaxCharCount = 0;
            if (parts[0] == "textentrylimited")
                MaxCharCount = int.Parse(parts[8]);
        }


        public Hue Hue { get; set; }
        public Graphic Graphic { get; set; }
        public int MaxCharCount { get; set; }
        public bool IsPassword { get; set; }
        public bool NumericOnly { get; set; }
        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }
        public string Text
        {
            get => IsPassword ? _plainText : _text.Text;
            set
            {
                _plainText = value;
                if (MultiLine)
                    _text.MaxWidth = Parent.Width;

                _text.Text = IsPassword ? new string('*', value.Length) : value;

                UpdateCaretPosition(_text.Text);
            }
        }

        public bool MultiLine { get; set; }
        public bool AllowTAB { get; set; }

        public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;
        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;



        public override void Update(double totalMS, double frameMS)
        {
            if (GumpManager.KeyboardFocusControl == this)
            {
                if (!IsFocused)
                {
                    SetFocused();
                    _caratBlink = true;
                }
                _caratBlink = true;
            }
            else
            {
                RemoveFocus();
                _caratBlink = false;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            _text.Draw(spriteBatch, new Vector3(position.X + _offset, position.Y, 0));

            if (IsEditable)
            {
                if (_caratBlink)
                    _carat.Draw(spriteBatch, new Vector3(position.X + _offset + _caretPosition.X, position.Y + _caretPosition.Y, 0));
            }

            return base.Draw(spriteBatch, position);
        }

        protected override void OnTextInput(char c)
        {
            if (MaxCharCount != 0 && Text.Length >= MaxCharCount)
                return;

            if (NumericOnly && !char.IsNumber(c))
                return;

            if (ReplaceDefaultTextOnFirstKeyPress)
            {
                Text = string.Empty;
                ReplaceDefaultTextOnFirstKeyPress = false;
            }

            //Text += c;
            Insert(c.ToString());
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_PASTE:

                    break;
                case SDL.SDL_Keycode.SDLK_TAB:
                    if (AllowTAB)
                        Insert("    ");
                    break;
                case SDL.SDL_Keycode.SDLK_RETURN:
                    if (MultiLine)
                        Insert("\n");
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    if (ReplaceDefaultTextOnFirstKeyPress)
                    {
                        Text = string.Empty;
                        ReplaceDefaultTextOnFirstKeyPress = false;
                    }
                    else
                    {
                        RemoveChar(true);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_LEFT:
                    AddCaretPosition(-1);
                    break;
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    AddCaretPosition(1);
                    break;
                case SDL.SDL_Keycode.SDLK_DELETE:
                    RemoveChar();
                    break;
                case SDL.SDL_Keycode.SDLK_HOME:
                    SetCaretPosition(0);
                    break;
                case SDL.SDL_Keycode.SDLK_END:
                    SetCaretPosition(Text.Length - 1);
                    break;
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            int oldPos = _caretIndex;

            if (_text.IsUnicode)
                _caretIndex = IO.Resources.Fonts.CalculateCaretPosUnicode(_text.Font, Text, x, y, _text.MaxWidth, _text.Align, (ushort)_text.FontStyle);
            else
                _caretIndex = IO.Resources.Fonts.CalculateCaretPosASCII(_text.Font, Text, x, y, _text.MaxWidth, _text.Align, (ushort)_text.FontStyle);


            if (oldPos != _caretIndex)
                UpdateCaretPosition(Text);
        }

        private void AddCaretPosition(int value)
        {
            _caretIndex += value;

            if (_caretIndex < 0)
                _caretIndex = 0;

            if (_caretIndex > Text.Length)
                _caretIndex = Text.Length;

            UpdateCaretPosition(Text);
        }

        private void SetCaretPosition(int value)
        {
            _caretIndex = value;

            if (_caretIndex < 0)
                _caretIndex = 0;

            if (_caretIndex > Text.Length)
                _caretIndex = Text.Length;

            UpdateCaretPosition(Text);
        }

        private void Insert(string c)
        {
            if (_caretIndex < 0)
                _caretIndex = 0;

            if (_caretIndex > Text.Length)
                _caretIndex = Text.Length;

            if (MaxCharCount > 0)
            {
                if (NumericOnly)
                {
                    string s = Text;
                    s = s.Insert(_caretIndex, c);

                    if (int.Parse(s) > MaxCharCount)
                        return;
                }
                else if (Text.Length >= MaxCharCount)
                    return;
            }

            string text = Text.Insert(_caretIndex, c);
            _caretIndex += c.Length;

            Text = text;
        }

        private void RemoveChar(bool fromleft = false)
        {
            if (fromleft)
            {
                if (_caretIndex < 1)
                    return;
                _caretIndex--;
            }
            else
            {
                if (_caretIndex >= Text.Length)
                    return;
            }

            if (_caretIndex < Text.Length)
                Text = Text.Remove(_caretIndex, 1);
            else
                Text = Text.Remove(Text.Length);
        }


        private void UpdateCaretPosition(string text)
        {
            int x, y;

            if (_text.IsUnicode)
                (x, y) = IO.Resources.Fonts.GetCaretPosUnicode(_carat.Font, text, _caretIndex, _text.MaxWidth, _carat.Align, (ushort)_carat.FontStyle);
            else
                (x, y) = IO.Resources.Fonts.GetCaretPosASCII(_carat.Font, text, _caretIndex, _text.MaxWidth, _carat.Align, (ushort)_carat.FontStyle);

            _caretPosition = new Point(x, y);

            if (_offset > 0)
            {
                if (_caretPosition.X + _offset < 0)
                    _offset = -_caretPosition.X;
                else if (Width + -_offset < _caretPosition.X)
                    _offset = Width - _caretPosition.X - _carat.Width;
            }
            else if (Width + _offset < _caretPosition.X + _carat.Width)
                _offset = Width - _caretPosition.X - _carat.Width;
            else
                _offset = 0;
        }

    }
}
