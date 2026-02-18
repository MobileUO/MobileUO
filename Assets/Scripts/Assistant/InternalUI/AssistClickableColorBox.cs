#region License
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
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistClickableColorBox : Control
    {
        private const int CELL = 12;
        private readonly ColorBox _colorBox;

        public AssistClickableColorBox
        (
            int x,
            int y,
            int w,
            int h,
            ushort hue
            //uint color
        )
        {
            X = x;
            Y = y;
            WantUpdateSize = false;

            GumpPic background = new GumpPic(0, 0, 0x00D4, 0);
            Add(background);
            _colorBox = new ColorBox(w, h, hue);
            _colorBox.X = 3;
            _colorBox.Y = 3;
            Add(_colorBox);

            Width = background.Width;
            Height = background.Height;
        }

        public event EventHandler ValueChanged;

        public ushort Hue
        {
            get => _colorBox.Hue;
            set => _colorBox.Hue = value;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                UIManager.GetGump<ColorPickerGump>()?.Dispose();

                ColorPickerGump pickerGump = new ColorPickerGump
                (
                    Client.Game.UO.World, 0, 0, 100, 100, s =>
                    {
                        _colorBox.Hue = s;
                        ValueChanged?.Invoke(this, null);
                    }
                );

                UIManager.Add(pickerGump);
            }
        }
    }
}
