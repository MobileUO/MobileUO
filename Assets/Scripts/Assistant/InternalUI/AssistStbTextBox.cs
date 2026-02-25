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
// See <https://www.gnu.org/licenses/agpl-3.0.html> for details.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using SDL2;
using StbTextEditSharp;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistStbTextBox : StbTextBox
    {
        public AssistStbTextBox
        (
            byte font,
            int max_char_count = -1,
            int maxWidth = 0,
            bool isunicode = true,
            FontStyle style = FontStyle.None,
            ushort hue = 0,
            TEXT_ALIGN_TYPE align = 0
        ) : base(font, max_char_count, maxWidth, isunicode, style, hue, align)
        {
        }
    }
}
