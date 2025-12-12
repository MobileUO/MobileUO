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
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistCheckbox : Control
    {
        private readonly RenderedText _text;
        private bool _isChecked;
        private ushort _inactive,
            _active;

        public AssistCheckbox(ushort inactive, ushort active, string text = "", byte font = 0, ushort color = 0, bool isunicode = true, int maxWidth = 0)
        {
            _inactive = inactive;
            _active = active;

            ref readonly var gumpInfoInactive = ref Client.Game.UO.Gumps.GetGump(inactive);
            ref readonly var gumpInfoActive = ref Client.Game.UO.Gumps.GetGump(active);

            if (gumpInfoInactive.Texture == null || gumpInfoActive.Texture == null)
            {
                Dispose();

                return;
            }
            Width = gumpInfoInactive.UV.Width;

            _text = RenderedText.Create(text, color, font, isunicode, maxWidth: maxWidth);

            Width += _text.Width;

            Height = Math.Max(gumpInfoInactive.UV.Width, _text.Height);
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
            {
                return false;
            }

            var ok = base.Draw(batcher, x, y);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                IsChecked ? _active : _inactive
            );

            batcher.Draw(
                gumpInfo.Texture,
                new Vector2(x, y),
                gumpInfo.UV,
                ShaderHueTranslator.GetHueVector(0)
            );

            _text.Draw(batcher, x + gumpInfo.UV.Width + 2, y);

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