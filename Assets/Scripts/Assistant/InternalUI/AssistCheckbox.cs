#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistCheckbox : Control
    {
        private const int INACTIVE = 0;
        private const int ACTIVE = 1;
        private readonly RenderedText _text;
        private readonly Texture2D[] _textures = new Texture2D[2];
        private bool _isChecked;

        public AssistCheckbox(ushort inactive, ushort active, string text = "", byte font = 0, ushort color = 0, bool isunicode = true, int maxWidth = 0)
        {
            _textures[INACTIVE] = Client.Game.Gumps.GetGump(inactive).Texture;
            _textures[ACTIVE] =  Client.Game.Gumps.GetGump(active).Texture;

            if (_textures[0] == null || _textures[1] == null)
            {
                Dispose();

                return;
            }

            Texture2D t = _textures[INACTIVE];
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

        public override void Update()
        {
            //for (int i = 0; i < _textures.Length; i++)
            //{
            //    Texture2D t = _textures[i];

            // MobileUO: CUO 0.1.11.0 removed totalMS from Update method
            //    if (t != null)
            //        t.Ticks = (long) totalMS;
            //}

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            // MobileUO: CUO 0.1.11.0 drops ResetHueVector()
            var hueVector = ShaderHueTranslator.GetHueVector(0);
            //ResetHueVector();

            bool ok = base.Draw(batcher, x, y);
            batcher.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], x, y, ref hueVector);
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