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

using ClassicUO.Assets;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using SDL2;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistNiceButton : HitBox
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;

        public AssistNiceButton(int x, int y, int w, int h, ButtonAction action, string text, int groupnumber = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_CENTER, ushort hue = 0xFFFF, bool copytext = false) : base(x, y, w, h)
        {
            _action = action;
            Add(TextLabel = new Label(text, true, hue, w, 0xFF, FontStyle.BlackBorder | FontStyle.Cropped, align));
            TextLabel.Y = (h - TextLabel.Height) >> 1;
            _groupnumber = groupnumber;
            CopyTextLabel = copytext;
        }

        public bool CopyTextLabel { get; }

        internal Label TextLabel { get; }

        public int ButtonParameter { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected && IsSelectable;
            set
            {
                if (!IsSelectable)
                {
                    return;
                }

                _isSelected = value;

                if (value)
                {
                    Control p = Parent;

                    if (p == null)
                    {
                        return;
                    }

                    IEnumerable<AssistNiceButton> list = p.FindControls<AssistNiceButton>();
                    foreach (AssistNiceButton b in list)
                    {
                        if (b != this && b._groupnumber == _groupnumber)
                        {
                            b.IsSelected = false;
                        }
                    }
                }
            }
        }

        internal static AssistNiceButton GetSelected(Control asa, int group)
        {
            IEnumerable<AssistNiceButton> list = asa.FindControls<AssistNiceButton>();
            foreach (AssistNiceButton b in list)
            {
                if (b._groupnumber == group && b.IsSelected)
                {
                    return b;
                }
            }
            return null;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsSelected = true;

                if (_action == ButtonAction.SwitchPage)
                {
                    ChangePage(ButtonParameter);
                }
                else
                {
                    OnButtonClick(ButtonParameter);
                    LeftClicked?.Invoke(null, this);
                }
            }
        }

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsSelected)
            {
                //float layerDepth = layerDepthRef;
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                /*renderLists.AddGumpNoAtlas(
                    batcher =>
                    {*/
                        batcher.Draw
                        (
                            _texture,
                            new Vector2(x, y),
                            new Rectangle(0, 0, Width, Height),
                            hueVector//,
                            //layerDepth
                        );
                        /*return true;
                    }
                );*/
            }

            return base.Draw(batcher, x, y);//base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            return base.OnMouseDoubleClick(x, y, button);
        }

        internal event EventHandler<AssistNiceButton> LeftClicked;

        public override bool AcceptKeyboardInput { get => CopyTextLabel; set { } }
        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (TextLabel != null && !string.IsNullOrEmpty(TextLabel.Text) && IsSelected)
            {
                switch (key)
                {
                    case SDL.SDL_Keycode.SDLK_x when Keyboard.Ctrl:
                    case SDL.SDL_Keycode.SDLK_c when Keyboard.Ctrl:
                        SDL.SDL_SetClipboardText(TextLabel.Text);

                        break;
                }
            }
        }
    }
}
