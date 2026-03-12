#region license
// Copyright (C) 2022-2025 Sascha Puligheddu
// 
// This project is a complete reproduction of AssistUO for MobileUO and ClassicUO.
// Developed as a lightweight, native assistant.
// 
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// 
// SPECIAL PERMISSION: Integration with projects under BSD 2-Clause (like ClassicUO)
// is permitted, provided that the integrated result remains publicly accessible 
// and the AGPL-3.0 terms are respected for this specific module.
//
// This program is distributed WITHOUT ANY WARRANTY. 
// See <https://www.gnu.org> for details.
#endregion
using System;
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistCheckbox : Control
    {
        private const int INACTIVE = 0;
        private const int ACTIVE = 1;
        private readonly RenderedText _text;
        private readonly UOTexture32[] _textures = new UOTexture32[2];
        private bool _isChecked;

        public AssistCheckbox(ushort inactive, ushort active, string text = "", byte font = 0, ushort color = 0, bool isunicode = true, int maxWidth = 0)
        {
            _textures[INACTIVE] = GumpsLoader.Instance.GetTexture(inactive);
            _textures[ACTIVE] = GumpsLoader.Instance.GetTexture(active);

            if (_textures[0] == null || _textures[1] == null)
            {
                Dispose();

                return;
            }

            UOTexture32 t = _textures[INACTIVE];
            Width = t.Width;

            _text = RenderedText.Create(text, color, font, isunicode, maxWidth: maxWidth);
            Width += _text.Width;

            Height = Math.Max(t.Width, _text.Height);
            CanMove = false;
            AcceptMouseInput = true;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged();
                }
            }
        }

        public string Text
        {
            get => _text.Text;
            set
            {
                if (!string.IsNullOrEmpty(value) && _text.Text != value)
                {
                    _text.Text = value;
                    _text.CreateTexture();
                }
            }
        }

        public ushort Hue
        {
            get => _text.Hue;
            set
            {
                if (_text.Hue != value)
                {
                    _text.Hue = value;
                    _text.CreateTexture();
                }
            }
        }

        public event EventHandler ValueChanged;

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _textures.Length; i++)
            {
                UOTexture32 t = _textures[i];

                if (t != null)
                    t.Ticks = (long) totalMS;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            ResetHueVector();

            bool ok = base.Draw(batcher, x, y);
            batcher.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], x, y, ref _hueVector);
            _text.Draw(batcher, x + _textures[ACTIVE].Width + 2, y);

            return ok;
        }

        protected virtual void OnCheckedChanged()
        {
            ValueChanged.Raise(this);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && MouseIsOver)
                IsChecked = !IsChecked;
        }

        public override void Dispose()
        {
            base.Dispose();
            _text?.Destroy();
        }
    }
}