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
using System.Text;

using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Assistant;

using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistHotkeyBox : Control
    {
        private readonly AssistNiceButton _buttonClear;
        private readonly Checkbox _PassToUO;
        private readonly HoveredLabel _label;
        private readonly GumpPicTiled _pic;

        private bool _actived;

        public AssistHotkeyBox(int x, int y, int width, int height, byte font, ushort hue)
        {
            CanMove = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;


            Width = width;
            Height = height;

            Add(_pic = new GumpPicTiled(1, 0, width, 20, 0xBBC) { AcceptKeyboardInput = true, Hue = 666 });
            Add(_label = new HoveredLabel(string.Empty, true, 0x0025, 0x0025, 0x0025, width - 4, font, FontStyle.Cropped, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 1,
                Y = 1
            });
            _pic.MouseExit += AssistHotkeyBox_FocusLost;
            _label.MouseExit += AssistHotkeyBox_FocusLost;
            _pic.MouseEnter += AssistHotkeyBox_FocusEnter;
            _label.MouseEnter += AssistHotkeyBox_FocusEnter;
            Add(_PassToUO = new Checkbox(210, 211, "Pass to CUO", font, hue, true) { X = 0, Y = _label.Height + 12 });

            Add(_buttonClear = new AssistNiceButton(10, _PassToUO.Y + _PassToUO.Height + 10, (width >> 2) + (width >> 3), 20, ButtonAction.Activate, "Clear")
            {
                IsSelectable = false,
                ButtonParameter = (int)ButtonState.Clear
            });

            //NOTE: Added Add Button
            Add(new AssistNiceButton(10, _buttonClear.Y + _buttonClear.Height + 10, width - 20, 20, ButtonAction.Activate, "Add Button")
            {
                IsSelectable = false,
                ButtonParameter = (int)ButtonState.AddMacroButton
            });

            Height += 20;

            X = x;
            Y = y;
            WantUpdateSize = false;
            IsActive = false;
        }

        private void AssistHotkeyBox_FocusLost(object sender, EventArgs e)
        {
            IsActive = false;
        }

        private void AssistHotkeyBox_FocusEnter(object sender, EventArgs e)
        {
            IsActive = true;
        }

        public SDL.SDL_Keycode Key { get; private set; }
        public SDL.SDL_Keymod Mod { get; private set; }
        public MacroAction PassToCUO => (_PassToUO?.IsChecked ?? false) ? MacroAction.PassToUO : MacroAction.None;

        public bool IsActive
        {
            get => _actived;
            set
            {
                if (value != _actived)
                {
                    _actived = value;
                    if (_actived)
                    {
                        _pic.Hue = 0;
                        SetKeyboardFocus();
                    }
                    else
                    {
                        _pic.Hue = 666;
                    }
                }
            }
        }

        public event EventHandler HotkeyChanged, HotkeyCleared, AddButton;


        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (IsActive)
            {
                _KeyMod = mod;
                if (key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    SetKey(key, mod);
                }
            }
        }

        protected override void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            _KeyMod = SDL.SDL_Keymod.KMOD_NONE;
        }

        private SDL.SDL_Keymod _KeyMod { get; set; }
        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            int but = (int)button;
            if (IsActive && but >= 2 && but <= 6 && but != 3)
            {
                if (but > 1)
                    --but;
                SetKey((SDL.SDL_Keycode)but, _KeyMod);
            }
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);
            if (IsActive && (delta == MouseEventType.WheelScrollUp || delta == MouseEventType.WheelScrollDown))
            {
                SetKey((SDL.SDL_Keycode)((int)delta + 0xFD), _KeyMod);
            }
        }

        public void SetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            mod = (SDL.SDL_Keymod)((ushort)mod & 0x3C3);
            if (key == SDL.SDL_Keycode.SDLK_UNKNOWN && mod == SDL.SDL_Keymod.KMOD_NONE)
            {
                Key = key;
                Mod = mod;
                _label.Text = string.Empty;
            }
            else if (key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                retry:
                string newvalue = TryGetKey(key, mod);

                if(string.IsNullOrEmpty(newvalue))
                {
                    uint nkey = (uint)key + 0x100;
                    if (nkey >= 0x110 && nkey <= 0x1FF)
                    {
                        newvalue = Char.ToString((char)key);
                        XmlFileParser.SDLkeyToVK[key] = (nkey, newvalue);
                        XmlFileParser.vkToSDLkey[nkey] = key;
                        goto retry;
                    }
                    else
                        return;
                }
                Key = key;
                Mod = mod;
                _label.Text = newvalue;
                HotkeyChanged.Raise(this);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonState)buttonID)
            {
                case ButtonState.Clear:
                    _label.Text = string.Empty;

                    HotkeyCleared.Raise(this);

                    Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    Mod = SDL.SDL_Keymod.KMOD_NONE;

                    break;

                case ButtonState.AddMacroButton:

                    AddButton.Raise(this);

                    break;
            }

            IsActive = false;
        }

        private enum ButtonState
        {
            Clear=222,
            AddMacroButton = 333
        }

        public static string TryGetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE)
        {
            if (XmlFileParser.SDLkeyToVK.TryGetValue(key, out (uint vkey, string name) value))
            {
                StringBuilder sb = new StringBuilder();

                GetModType(mod, out bool isshift, out bool isctrl, out bool isalt);

                if (isshift)
                    sb.Append("Shift ");

                if (isctrl)
                    sb.Append("Ctrl ");

                if (isalt)
                    sb.Append("Alt ");


                sb.Append(value.name);

                return sb.ToString();
            }

            return string.Empty;
        }

        internal static void GetModType(SDL.SDL_Keymod mod, out bool isshift, out bool isctrl, out bool isalt)
        {
            isshift = (mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            isctrl = (mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE && ((mod & SDL.SDL_Keymod.KMOD_RALT) == 0 || (mod & SDL.SDL_Keymod.KMOD_LCTRL) == 0);//for ALTGR
            isalt = (mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
        }
    }
}
