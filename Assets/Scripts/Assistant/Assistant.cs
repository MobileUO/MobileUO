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

using Assistant;
using Assistant.Scripts;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AssistantGump : Gump
    {
        public override GumpType GumpType => GumpType.Assistant;

        internal class TimersGump : Gump
        {
            private Dictionary<string, uint> _Timers { get; } = new Dictionary<string, uint>();
            private AssistScrollArea _Area;
            internal TimersGump() : base(ClassicUO.Client.Game.UO.World, 0, 0)
            {
                AssistantGump gump = UOSObjects.Gump;
                if (gump == null || gump.IsDisposed)
                {
                    Dispose();
                    return;
                }
                
                AcceptMouseInput = true;
                CanMove = true;
                CanCloseWithRightClick = true;
                CanCloseWithEsc = false;
                UOScript.Interpreter.GetTimersInDict(_Timers);
                X = gump.X + (gump.Width >> 1);
                Y = gump.Y + (gump.Height >> 2);
                int w = gump.Width >> 1;
                int h = gump.Height;
                Width = w;
                Height = h;
                Add(new AlphaBlendControl(gump.Alpha) { X = 1, Y = 1, Width = w - 2, Height = h - 2 });
                CreateRectangleArea(this, 4, 4, w - 8, h - 8, 0, Color.Gray.PackedValue, 2, "Active Timers");
                _Area = new AssistScrollArea(15, 20, w - 30, h - 40);
                foreach(var kvp in _Timers)
                {
                    _Buttons.Add(CreateSingleTextSelection(_Area, 1, TEXT_ALIGN_TYPE.TS_LEFT, w - 40, $"{kvp.Key}", $"{kvp.Value}", ScriptTextBox.GRAY_HUE));
                }
                Add(_Area);
                _NextUpdate = Time.Ticks + 100;
            }

            private uint _NextUpdate;
            private List<AssistDoubleNiceLabelButton> _Buttons = new List<AssistDoubleNiceLabelButton>();
            public override void Update()
            {
                base.Update();
                if(_NextUpdate < Time.Ticks)
                {
                    UOScript.Interpreter.GetTimersInDict(_Timers);
                    List<AssistDoubleNiceLabelButton> toRemove = _Buttons.FindAll(b => !_Timers.ContainsKey(b.TextLabel.Text));
                    foreach(var but in toRemove)
                    {
                        _Buttons.Remove(but);
                        _Area.Remove(but);
                    }
                    List<string> toAdd = _Timers.Keys.Where(s => _Buttons.All(b => b.TextLabel.Text != s)).ToList();
                    foreach (var s in toAdd)
                    {
                        _Buttons.Add(CreateSingleTextSelection(_Area, 1, TEXT_ALIGN_TYPE.TS_LEFT, Width - 40, $"{s}", $"{_Timers[s]}", ScriptTextBox.GRAY_HUE));
                    }
                    foreach(var but in _Buttons)
                    {
                        but.SecondLabel.Text = $"{_Timers[but.TextLabel.Text]}";
                    }
                    _NextUpdate = Time.Ticks + 100;
                }
            }

            private AssistDoubleNiceLabelButton CreateSingleTextSelection(AssistScrollArea area, int y, TEXT_ALIGN_TYPE align, int width, string text, string extra, ushort hue)
            {
                AssistDoubleNiceLabelButton but = new AssistDoubleNiceLabelButton(0, y, width, _buttonHeight - (_buttonHeight >> 2), ButtonAction.Activate, text, extra, (int)ButtonType.SetLinkedProfile, align, hue, true);
                area.Add(but);
                return but;
            }
        }

        internal class MessageBoxGump : Gump
        {
            public override GumpType GumpType => GumpType.Assistant;

            internal MessageBoxGump(string title, string body) : base(ClassicUO.Client.Game.UO.World, 0, 0)
            {
                AssistantGump gump = UOSObjects.Gump;
                if(gump == null || gump.IsDisposed || string.IsNullOrEmpty(body))
                {
                    Dispose();
                    return;
                }
                AcceptMouseInput = true;
                CanMove = true;
                CanCloseWithRightClick = true;
                CanCloseWithEsc = false;
                X = gump.X + (gump.Width >> 2);
                Y = gump.Y + (gump.Height >> 2);
                gump.IsEnabled = false;
                int w = Math.Min(300, Client.Game.UO.FileManager.Fonts.GetWidthUnicode(FONT, body) >> 1);
                int h = Client.Game.UO.FileManager.Fonts.GetHeightUnicode(FONT, body, w, TEXT_ALIGN_TYPE.TS_CENTER, 0x0);
                w = Math.Max(160, w + 20);
                h = Math.Max(80, h + 60);
                Width = w;
                Height = h;
                Add(new AlphaBlendControl(gump.Alpha) { X = 1, Y = 1, Width = w - 2, Height = h - 2 });
                CreateRectangleArea(this, 10, 10, w - 20, h - 20, 0, Color.Gray.PackedValue, 2, $"{title}");
                Add(new Label($"{body}", true, ScriptTextBox.GRAY_HUE, w - 40, FONT, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER) { X = 20, Y = 20, Height = h - 40 });
                Add(new AssistNiceButton((w >> 1) - 29, h - 32, 60, 20, ButtonAction.Activate, "OKAY") { ButtonParameter = 222, IsSelectable = false });
            }

            public override void Dispose()
            {
                Commands.HasMessageGump = false;
                if(UOSObjects.Gump != null)
                    UOSObjects.Gump.IsEnabled = true;
                base.Dispose();
            }

            public override void OnButtonClick(int buttonID)
            {
                Dispose();
            }
        }

        #region Object Inspector
        internal class ObjectInspectorGump : Gump
        {
            public override GumpType GumpType => GumpType.Assistant;

            private enum NotorietyFlag : byte
            {
                any = 0x00,
                innocent = 0x01,
                friend = 0x02,
                gray = 0x03,
                criminal = 0x04,
                enemy = 0x05,
                murderer = 0x06,
                invulnerable = 0x07,
            }

            internal ObjectInspectorGump(UOEntity inspected) : base(ClassicUO.Client.Game.UO.World, 0, 0)
            {
                AssistantGump gump = UOSObjects.Gump;
                if (gump == null || gump.IsDisposed || inspected == null)
                {
                    Dispose();
                    return;
                }
                AcceptMouseInput = true;
                CanMove = true;
                CanCloseWithRightClick = true;
                CanCloseWithEsc = false;
                X = gump.X + (gump.Width >> 2);
                Y = gump.Y + (gump.Height >> 2);
                int w = 310;
                int h = inspected is UOMobile ? 400 : 300;
                Width = w;
                Height = h;
                Add(new AlphaBlendControl(gump.Alpha) { X = 1, Y = 1, Width = w - 2, Height = h - 2 });
                CreateRectangleArea(this, 10, 10, w - 20, h - 20, 0, Color.Gray.PackedValue, 2, "Object Inspector");
                int x = 20, y = 20, mw = 110, mh;
                Label l;
                Add(l = new Label("Serial:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                mh = l.Height;
                Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"0x{inspected.Serial:X8}" });
                y += mh + 2;
                Add(new Label("Graphic:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"0x{inspected.Graphic:X4}" });
                y += mh + 2;
                Add(new Label("Color:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{inspected.Hue}" });
                if (inspected.Hue > 0 && inspected.Hue <= Client.Game.UO.FileManager.Hues.HuesCount)
                {
                    Add(new AssistIntegralColorBox(96, 14, inspected.Hue) { X = 193, Y = y + 3 });
                }
                y += mh + 2;
                Add(new Label("Position (X Y Z):", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{inspected.Position.X} {inspected.Position.Y} {inspected.Position.Z}" });
                y += mh + 5;
                if (inspected is UOMobile mob)
                {
                    Add(new Label("Mobile", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    y += mh;
                    Add(new Label("Name:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{mob.Name}" });
                    y += mh + 2;
                    Add(new Label("Sex:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Female ? "Female" : "Male")}" });
                    y += mh + 2;
                    Add(new Label("Hits:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{mob.Hits}{(mob.InParty ? "%" : "")}" });
                    y += mh + 2;
                    Add(new Label("Max Hits:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{mob.HitsMax}{(mob.InParty ? "%" : "")}" });
                    y += mh + 2;
                    Add(new Label("Notoriety:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{mob.Notoriety} - {(NotorietyFlag)mob.Notoriety}" });
                    y += mh + 2;
                    Add(new Label("Direction:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(int)(mob.Direction & AssistDirection.Up)} - {(mob.Direction & AssistDirection.Up).ToString().ToLower(XmlFileParser.Culture)}" });
                    y += mh + 5;
                    Add(new Label("Flags", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"0x{mob.GetPacketFlags():X2}" });
                    y += mh;
                    Add(new Label("Paralyzed:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Paralyzed ? "Yes" : "No")}" });
                    y += mh + 2;
                    Add(new Label("Poisoned:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Poisoned ? "Yes" : "No")}" });
                    y += mh + 2;
                    Add(new Label("Invulnerable:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Blessed ? "Yes" : "No")}" });
                    y += mh + 2;
                    Add(new Label("War Mode:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Warmode ? "Yes" : "No")}" });
                    y += mh + 2;
                    Add(new Label("Hidden:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Visible ? "No" : "Yes")}" });
                    y += mh + 2;
                    Add(new Label("Flying:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(mob.Flying ? "Yes" : "No")}" });
                }
                else if (inspected is UOItem it)
                {
                    Add(new Label("Item", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    y += mh;
                    Add(new Label("Name:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{it.Name}" });
                    y += mh + 2;
                    Add(new Label("Container:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    uint cnt = it.GetContainerSerial();
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(!SerialHelper.IsValid(cnt) ? "ground" : $"0x{cnt:X8}")}" });
                    y += mh + 2;
                    Add(new Label("Root Container:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    cnt = it.GetRootContainerSerial();
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(!SerialHelper.IsValid(cnt) ? "ground" : $"0x{cnt:X8}")}" });
                    y += mh + 2;
                    Add(new Label("Amount:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{it.Amount}" });
                    y += mh + 2;
                    Add(new Label("Layer:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(byte)it.Layer}" });
                    y += mh + 2;
                    Add(new Label("Owned:", true, ScriptTextBox.GRAY_HUE, mw) { X = x, Y = y });
                    Add(new SelectableReadOnlyBox(FONT, -1, 0, true, FontStyle.None, ScriptTextBox.GREEN_HUE) { X = x + mw, Y = y, Width = w - (40 + mw), Text = $"{(UOSObjects.Player.Serial == cnt ? "Yes" : "No")}" });
                }
            }
        }
        #endregion


        #region newprofile
        internal class NewProfileGump : Gump
        {
            public override GumpType GumpType => GumpType.Assistant;

            private AssistStbTextBox _box;
            private Label _warning;
            private Line[] _lines;
            private AlphaBlendControl _alpha;
            internal NewProfileGump() : base(Client.Game.UO.World, 0, 0)
            {
                AcceptMouseInput = true;
                CanMove = false;
                CanCloseWithRightClick = true;
                CanCloseWithEsc = false;
                X = UOSObjects.Gump.X + (UOSObjects.Gump.Width >> 2);
                Y = UOSObjects.Gump.Y + (UOSObjects.Gump.Height >> 2);
                Width = 300;
                Height = 100;
                Add(_alpha = new AlphaBlendControl(UOSObjects.Gump.Alpha) { X = 1, Y = 1, Width = 298, Height = 98});
                _lines = CreateRectangleArea(this, 10, 10, 280, 80, 0, Color.Gray.PackedValue, 2, "Enter the New Profile Name");
                //Add(new Label("Enter the New Profile Name", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = 60, Y = 10 });
                Add(new GumpPicTiled(15, 30, 270, 23, 0x52));
                Add(new GumpPicTiled(16, 31, 269, 21, 0xBBC));
                Add(_box = new AssistStbTextBox(FONT, 30, 260, true) { X = 20, Y = 30, Width = 260, Height = 21 });
                Add(new AssistNiceButton(_box.X + 15, _box.Y + _box.Height + 5, 80, 30, ButtonAction.Activate, "OKAY") {ButtonParameter = 123, IsSelectable = false});
                Add(new AssistNiceButton(_box.X + 170, _box.Y + _box.Height + 5, 80, 30, ButtonAction.Activate, "CANCEL") { ButtonParameter = 321, IsSelectable = false });
                Add(_warning = new Label("", true, 37, 265, FONT, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER) { X = 15, Y = 90, Height = 40, IsVisible = false });
                //ControlInfo.IsModal = true;
                UOSObjects.Gump.IsEnabled = false;
                _box.SetKeyboardFocus();
            }

            private void Resize()
            {
                if (!_warning.IsVisible)
                {
                    _warning.IsVisible = true;
                    Height += _warning.Height;
                    _alpha.Height += _warning.Height;
                    for (int i = 0; i < _lines.Length; i++)
                    {
                        if (i + 1 < _lines.Length)
                            _lines[i].Height += _warning.Height;
                        else
                            _lines[i].Y += _warning.Height;
                    }
                    WantUpdateSize = true;
                }
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                if (key == SDL.SDL_Keycode.SDLK_RETURN)
                    OnButtonClick(123);
            }

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case 123:
                    {
                        if (!Engine.Validate(_box.Text, 3, 24, true, true, _box.Text.Length >> 1))
                        {
                            _warning.Text = "Invalid name (only alphanumeric or '-_ ', between 3 and 24 chars length)";
                            Resize();
                            return;
                        }
                        else
                        {
                            FileInfo info = new FileInfo(Path.Combine(Engine.ProfilePath, $"{_box.Text}.xml"));
                            bool success = false;
                            if (info.Exists)
                            {
                                _warning.Text = $"The profile name '{_box.Text}' already exist!";
                                Resize();
                                return;
                            }
                            else if (UOSObjects.Gump._SetLinkedProfile)
                            {
                                if (!XmlFileParser.SaveProfile(_box.Text))
                                {
                                    UOSObjects.Player.SendMessage(MsgLevel.Error, "Cannot save profile!");
                                }
                                else
                                {
                                    success = true;
                                }
                                UOSObjects.Gump.SetLinkedClicked(this, UOSObjects.Gump._linkSelectedProfile);
                            }
                            else
                            {
                                try
                                {
                                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                                    {
                                        w.Write(XmlFileParser.DEFPROF);
                                        w.Flush();
                                    }
                                    success = true;
                                }
                                catch
                                {
                                    UOSObjects.Player.SendMessage(MsgLevel.Error, "Cannot save profile!");
                                }
                            }
                            if (success)
                            {
                                UOSObjects.Gump.OnRecordMacroButton(true);
                                XmlFileParser.SaveProfile();
                                UOSObjects.Gump.LoadProfiles(_box.Text);
                                UOSObjects.GumpIsLoading = true;
                                XmlFileParser.LoadProfile(UOSObjects.Gump, _box.Text);
                                UOSObjects.GumpIsLoading = false;
                            }
                        }
                        break;
                    }
                }
                UOSObjects.Gump.IsEnabled = true;
                Dispose();
                UOSObjects.Gump.BringOnTop();
            }
        }
        #endregion

        #region newmacro
        internal class NewMacroGump : Gump
        {
            public override GumpType GumpType => GumpType.Assistant;

            private AssistStbTextBox _box;
            private Label _warning;
            private Line[] _lines;
            private AlphaBlendControl _alpha;
            internal NewMacroGump() : base(Client.Game.UO.World, 0, 0)
            {
                AcceptMouseInput = true;
                CanMove = false;
                CanCloseWithRightClick = true;
                CanCloseWithEsc = false;
                X = UOSObjects.Gump.X + (UOSObjects.Gump.Width >> 2);
                Y = UOSObjects.Gump.Y + (UOSObjects.Gump.Height >> 2);
                Width = 300;
                Height = 100;
                Add(_alpha = new AlphaBlendControl(UOSObjects.Gump.Alpha) { X = 1, Y = 1, Width = 298, Height = 98 });
                _lines = CreateRectangleArea(this, 10, 10, 280, 80, 0, Color.Gray.PackedValue, 2, "Enter the New Macro Name");
                Add(new GumpPicTiled(15, 30, 270, 23, 0x52));
                Add(new GumpPicTiled(16, 31, 269, 21, 0xBBC));
                Add(_box = new AssistStbTextBox(FONT, 30, 260, true) { X = 20, Y = 30, Width = 260, Height = 21 });
                Add(new AssistNiceButton(_box.X + 15, _box.Y + _box.Height + 5, 80, 30, ButtonAction.Activate, "OKAY") { ButtonParameter = 123, IsSelectable = false });
                Add(new AssistNiceButton(_box.X + 170, _box.Y + _box.Height + 5, 80, 30, ButtonAction.Activate, "CANCEL") { ButtonParameter = 321, IsSelectable = false });
                Add(_warning = new Label("", true, 37, 265, FONT, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER) { X = 15, Y = 90, Height = 40, IsVisible = false });
                //ControlInfo.IsModal = true;
                UOSObjects.Gump.IsEnabled = false;
                _box.SetKeyboardFocus();
            }

            private void Resize()
            {
                if (!_warning.IsVisible)
                {
                    _warning.IsVisible = true;
                    Height += _warning.Height;
                    _alpha.Height += _warning.Height;
                    for (int i = 0; i < _lines.Length; i++)
                    {
                        if (i + 1 < _lines.Length)
                            _lines[i].Height += _warning.Height;
                        else
                            _lines[i].Y += _warning.Height;
                    }
                    WantUpdateSize = true;
                }
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                if (key == SDL.SDL_Keycode.SDLK_RETURN)
                    OnButtonClick(123);
            }

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case 123:
                    {
                        if (!Engine.Validate(_box.Text, 3, 24, true, true, _box.Text.Length >> 1, true, new char[]{' '}))
                        {
                            _warning.Text = "Invalid name, only alphanumeric chars allowed (minimum 3 and maximum 24 char length)";
                            Resize();
                            return;
                        }
                        else
                        {
                            if(ScriptManager.MacroDictionary.ContainsKey(_box.Text))
                            {
                                _warning.Text = $"The macro name '{_box.Text}' already exists!";
                                Resize();
                                return;
                            }
                            else
                            {
                                ScriptManager.MacroDictionary.Add(_box.Text, new HotKeyOpts(MacroAction.None, "macro.play", _box.Text));
                                UOSObjects.Gump.UpdateMacroListGump(_box.Text);
                                XmlFileParser.SaveData();
                            }
                        }
                        break;
                    }
                }
                UOSObjects.Gump.IsEnabled = true;
                Dispose();
                UOSObjects.Gump.BringOnTop();
            }
        }
        #endregion

        #region hotkey overwrite
        internal class OverWriteHKGump : Gump
        {
            public override GumpType GumpType => GumpType.Assistant;

            private uint _num;
            private HotKeyOpts _hotkeyOpts;
            private AssistHotkeyBox _hkbox;
            private string _selHK;
            internal OverWriteHKGump(uint num, HotKeyOpts hkopts, AssistHotkeyBox box, ref string selHK, string oldHKname) : base(Client.Game.UO.World, 0, 0)
            {
                if (!string.IsNullOrEmpty(selHK) && hkopts != null && !string.IsNullOrEmpty(hkopts.Action))
                {
                    string[] split = oldHKname.Split('.');
                    _selHK = selHK;
                    _hkbox = box;
                    _num = num;
                    _hotkeyOpts = hkopts;
                    AcceptMouseInput = true;
                    CanMove = false;
                    CanCloseWithRightClick = true;
                    CanCloseWithEsc = false;
                    X = UOSObjects.Gump.X + (UOSObjects.Gump.Width >> 2);
                    Y = UOSObjects.Gump.Y + (UOSObjects.Gump.Height >> 2);
                    Width = 320;
                    Height = 120;
                    Add(new AlphaBlendControl(UOSObjects.Gump.Alpha) { X = 1, Y = 1, Width = 318, Height = 118 });
                    CreateRectangleArea(this, 10, 10, 300, 100, 0, Color.Gray.PackedValue, 2, "Warning!");
                    Add(new Label($"Existant HotKey ({split[split.Length - 1]})!", true, ScriptTextBox.GRAY_HUE, 280, FONT, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER){ X = 20, Y = 30 });
                    Add(new Label("Click on OKAY to overwrite it!", true, ScriptTextBox.GRAY_HUE, 280, FONT, FontStyle.None, TEXT_ALIGN_TYPE.TS_CENTER) { X = 20, Y = 50 });
                    Add(new AssistNiceButton(40, 80, 80, 30, ButtonAction.Activate, "OKAY") { ButtonParameter = 123, IsSelectable = false });
                    Add(new AssistNiceButton(180, 80, 80, 30, ButtonAction.Activate, "CANCEL") { ButtonParameter = 321, IsSelectable = false });
                    IsModal = true;
                }
                else
                    Dispose();
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                if (key == SDL.SDL_Keycode.SDLK_RETURN)
                    OnButtonClick(123);
            }

            public override void OnButtonClick(int buttonID)
            {
                switch (buttonID)
                {
                    case 123:
                    {
                        Dispose();
                        HotKeys.AddHotkey(_num, _hotkeyOpts, _hkbox, ref _selHK, UOSObjects.Gump, true);
                        UOSObjects.Gump.OnFocusEnter();
                        break;
                    }
                    case 321:
                    {
                        Dispose();
                        if(!string.IsNullOrEmpty(_selHK))
                        {
                            HotKeys.GetSDLKeyCodes(_selHK, out int key, out int mod);
                            _hkbox.SetKey((SDL.SDL_Keycode)key, (SDL.SDL_Keymod)mod);
                        }
                        UOSObjects.Gump.OnFocusEnter();
                        break;
                    }
                }
            }
        }
        #endregion

        internal static StringBuilder _InstanceSB = new StringBuilder();
        private const int RISECONST = 64;//63 buttons max for every page or tab...modulus _RiseConst to get the button, RightShift six (>> 6) to get the correct section
        internal enum PageType
        {
            Invalid = 0,
            General = 1,
            Options = 2,
            Hotkeys,
            Macros,
            //Skills,
            //Snapshots,
            Agents,
            LastMain,
            Generic,
            Combat,
            Friends,
            LastOptions,
            Autoloot,
            //Counters,
            Dress,
            Organizer,
            Scavenger,
            Vendors,
            LastAgents,
            Minimized = LastMain * RISECONST
        }

        private enum ActionExclusion
        {
            None = 0,
            NotTargeting = 1,
            NotHiding = 2,
            Both = 3
        }

        private enum ButtonType
        {
            //Main section that comprises every button above the window
            Invalid = 0,
            // * General *
            AddNewProfile = 1 * RISECONST,
            //SaveCurrentProfile,
            SetLinkedProfile,
            ApplySizeChange,
            // * Options *
            // General Tab
            ExemptionsContSearch = 2 * RISECONST,
            BoneCutSetBlade,
            // Combat Tab
            RemountSetMount = 3 * RISECONST,
            // Friends Tab
            RemoveFriend = 4 * RISECONST,
            InsertFriend,
            FriendsList,
            // SpellGrid Tab
            RemoveGridProfile = 5 * RISECONST,
            AddNewGridProfile,
            // * Hotkeys *
            ClearHotkey = 6 * RISECONST,
            SaveHotkey,
            HotKeyList,
            // * Macros *
            PlayMacro = 7 * RISECONST,
            RecordMacro,
            RemoveMacro,
            NewMacro,
            SaveMacro,
            ObjectInspector,
            CommandsHelper,
            ActiveObjectsHelper,
            MacroList,
            // * Skills *
            ResetSkillModCount = 8 * RISECONST,
            CopySingleSkillVal,
            CopyAllSkillVal,
            SetAllSkillLocks,
            // * Agents *
            // AutoLoot Tab
            SetAutolootContainer = 9 * RISECONST,
            RemoveAutolootItem,
            InsertAutolootItem,
            AutolootList,
            RemovePropertySearch,
            InsertPropertySearch,
            CopyPropertySearch,
            // Dress Tab
            DressList = 10 * RISECONST,
            RemoveDressList,
            CreateDressList,
            SetUndressContainer,
            DressSelectedList,
            UndressSelectedList,
            ImportCurrentlyDressed,
            AddItemToDressList,
            RemoveItemFromDressList,
            ClearSelectedDressList,
            DressListItem,
            DressTypeOrSerial,
            // Organizer Tab
            OrganizerList = 11 * RISECONST,
            PlaySelectedOrganizer,
            RemoveOrganizerList,
            CreateOrganizerList,
            SetOrganizerContainers,
            RemoveItemFromOrganizer,
            InsertItemIntoOrganizer,
            OrganizerListItem,
            // Scavenger Tab
            InsertScavengeItem = 12 * RISECONST,
            RemoveScavengeItem,
            ClearScavengeItems,
            ScavengeDestinationCont,
            ScavengerListItem,
            // Vendors Tab
            BuySellList = 13 * RISECONST,
            RemoveBuySellList,
            NewBuySellList,
            RemoveBuySellItem,
            InsertBuySellItem,
            BuySellItemList,
            RenameBuySellList,
            // * Minimized Gump Page * 
            MinimizeGump = RISECONST * RISECONST
        }

        internal static int InsertFriendButton => (int)ButtonType.InsertFriend;
        internal static readonly byte FONT = (byte)(ClassicUO.Client.Game.UO.FileManager.Version >= ClassicUO.Utility.ClientVersion.CV_305D ? 1 : 0);
        //WARNING, MINIMUM WIDTH IS 500, HEIGHT IS 325, if you go lower than this, the items won't fit inside the gump! YOU HAVE BEEN WARNED!
        private static readonly int minwidth = (ClassicUO.Client.Game.UO.FileManager.Version > ClassicUO.Utility.ClientVersion.CV_200 ? 500 : 600);
        private static readonly int minheight = (ClassicUO.Client.Game.UO.FileManager.Version > ClassicUO.Utility.ClientVersion.CV_200 ? 325 : 380);

        private static int _width = minwidth;
        private static int _height = minheight;
        private static int WIDTH
        {
            get { return _width; }
            set
            {
                if(value >= minwidth && value <= 800)
                {
                    _width = value;
                    _buttonWidth = _width / 20;
                }
            }
        }
        private static int HEIGHT
        {
            get { return _height; }
            set
            {
                if (value >= minheight && value <= 600)
                {
                    _height = value;
                    _buttonHeight = _height / 13;
                }
            }
        }

        private AlphaBlendControl _alphaBlend;
        private AlphaBlendControl _alphaMinimizedBlend;
        public float Alpha => _alphaBlend?.Alpha ?? 1.0f;

        private readonly Button _prevbutton = new Button(0, 5537, 5539, 5538)
        {
            ButtonAction = ButtonAction.SwitchPage,
            ToPage = (int)PageType.General,
            X = 2,
            Y = 5
        };
        private readonly List<Control> _controls = new List<Control>();
        public override void ChangePage(int pageIndex)
        {
            int prevpage = ActivePage;
            base.ChangePage(pageIndex);
            if (ActivePage != 0)
            {
                if (prevpage == (int)PageType.Minimized)
                {
                    foreach (Control c in _controls)
                    {
                        c.Page = 0;
                    }
                    WantUpdateSize = true;
                }
                else if (ActivePage == (int)PageType.Minimized)
                {
                    _prevbutton.ToPage = prevpage;
                    foreach (Control c in _controls)
                    {
                        c.Page = (int)PageType.LastMain;
                    }
                    WantUpdateSize = true;
                }
            }
        }

        private static int _buttonHeight = HEIGHT / 13;
        private static int _buttonWidth = WIDTH / 20;
        internal const float TRANSPARENCY = 0.25f;

        //PRIMARY GUMP, EVERYTHING STARTS HERE
        public AssistantGump() : base(Client.Game.UO.World, 0, 0)
        {
            Add(_alphaBlend = new AlphaBlendControl(1.0f)
            {
                X = 1,
                Y = 1,
                Width = WIDTH - 2,
                Height = HEIGHT - 2
            });
            _controls.Add(_alphaBlend);
            AcceptMouseInput = true;
            CanMove = true;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            int diffy = ((_buttonHeight - (_buttonHeight >> 3)) >> 2);
            int width = (WIDTH >> 3) + (WIDTH >> 4);
            Control c;
            _controls.Add(c = new Line(0, 0, WIDTH, 2, Color.Gray.PackedValue));
            Add(c);
            _controls.Add(c = new Line(0, 0, 2, HEIGHT, Color.Gray.PackedValue));
            Add(c);
            _controls.Add(c = new Line(WIDTH - 1, 0, 2, HEIGHT, Color.Gray.PackedValue));
            Add(c);
            _controls.Add(c = new Line(0, HEIGHT - 1, WIDTH, 2, Color.Gray.PackedValue));
            Add(c);

            for (int x = (_buttonWidth - (_buttonWidth >> 3)) + 1, y = _buttonHeight >> 3, page = 1; page < (int)PageType.LastMain; x += width + (width >> 5), page++)
            {
                _controls.Add(c = new AssistNiceButton(x, y, width, _buttonHeight, ButtonAction.SwitchPage, ((PageType)page).ToString(), (int)PageType.LastMain) { IsSelected = page == 1, ButtonParameter = page });
                Add(c);
            }
            _controls.Add(c = new Button(0, 5540, 5542, 5541)
            {
                ButtonAction = ButtonAction.SwitchPage,
                ToPage = (int)PageType.Minimized,
                X = _buttonWidth >> 3,
                Y = diffy
            });
            Add(c);
            _controls.Add(c = new Line(1, _buttonHeight + diffy, WIDTH - 2, 2, Color.Gray.PackedValue));
            Add(c);
            //Minimized status
            Add(_alphaMinimizedBlend = new AlphaBlendControl(1.0f)
            {
                X = 1,
                Y = 1,
                Width = 85,
                Height = 30
            }, (int)PageType.Minimized);
            Add(_prevbutton, (int)PageType.Minimized);
            Add(new Label("Assistant", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = 26, Y = 5 }, (int)PageType.Minimized);
            //End of Minimized status
            
            //SECTIONS OF RELEVANCE
            BuildGeneral((int)PageType.General);
            BuildOptions();
            BuildHotkeys((int)PageType.Hotkeys);
            BuildMacros((int)PageType.Macros);
            //BuildSkills((int)PageType.Skills);//is this page really needed?
            BuildAgents();

#if UNITY_2017_1_OR_NEWER
            var isMinimized = UserPreferences.AssistantMinimized.CurrentValue == (int)PreferenceEnums.AssistantMinimized.On;
            //NOTE: It seems to be necessary to switch to 1st page first before setting the page to Minimized
            ChangePage(1);

            if (isMinimized)
            {
                ChangePage((int)PageType.Minimized);
            }
#else
            ChangePage(1);
#endif
        }

        public override void OnPageChanged()
        {
            base.OnPageChanged();
#if UNITY_2017_1_OR_NEWER
            var minimized = false;
#endif
            switch ((PageType)ActivePage)
            {
                case PageType.Options:
                {
                    if (_selectedSubOptionsPage < 0)
                    {
                        _selectedSubOptionsPage = (int)PageType.Generic;
                    }
                    ActivePage = _selectedSubOptionsPage;
                    break;
                }
                case PageType.Generic:
                case PageType.Combat:
                case PageType.Friends:
                {
                    _selectedSubOptionsPage = ActivePage;
                    _subOptionsPageNiceButtons[_selectedSubOptionsPage - (int)PageType.Generic].IsSelected = true;
                    break;
                }
                case PageType.Agents:
                {
                    if (_selectedSubAgentsPage < 0)
                    {
                        _selectedSubAgentsPage = (int)PageType.Autoloot;
                    }
                    ActivePage = _selectedSubAgentsPage;
                    break;
                }
                case PageType.Autoloot:
                case PageType.Dress:
                case PageType.Organizer:
                case PageType.Scavenger:
                case PageType.Vendors:
                {
                    _selectedSubAgentsPage = ActivePage;
                    _subAgentsPageNiceButtons[_selectedSubAgentsPage - (int)PageType.Autoloot].IsSelected = true;
                    break;
                }
#if UNITY_2017_1_OR_NEWER
                case PageType.Minimized:
                    minimized = true;
                    break;
#endif
            }
#if UNITY_2017_1_OR_NEWER
            UserPreferences.AssistantMinimized.CurrentValue = (int)(minimized ? PreferenceEnums.AssistantMinimized.On : PreferenceEnums.AssistantMinimized.Off);
#endif
        }

#region GENERAL_TAB
        // general
        private HSliderBar _opacity;//_lightLevel, 
        private AssistCheckbox _loadLinkedProfile;//, _negotiateFeatures;
        private AssistCombobox _profileSelected;//_lootSystem, 
        private AssistNiceButton _linkSelectedProfile;
        internal AssistCombobox ProfileCombobox => _profileSelected;
        internal string ProfileSelected => _profileSelected.GetItem(_profileSelected.SelectedIndex);
        private AssistArrowNumbersTextBox _assistSizeX, _assistSizeY;

        private void OnOpacityChanged(object sender, EventArgs e)
        {
            float val = (99 - _opacity.Value) * 0.01f;
            _alphaBlend.Alpha = 1.0f - val;
            _alphaMinimizedBlend.Alpha = 1.0f - val;
            WantUpdateSize = true;
        }

        //private static readonly string[] _commandprefixes = new[] { "  -", "  .", "  ,", "  ;", "  !", "  ?", "  #", "  [", "  =" };

        private string[] _profiles = null;
        private int _currentProfileIndex = 0;
        private void OnBeforeProfileList(object sender, EventArgs e)
        {
            LoadProfiles();
            _currentProfileIndex = _profileSelected.SelectedIndex;
        }

        private void OnProfileSelected(object sender, int e)
        {
            if (_profileSelected != null)
            {
                if (_currentProfileIndex != _profileSelected.SelectedIndex && !UOSObjects.GumpIsLoading)
                {
                    UOSObjects.Gump.OnRecordMacroButton(true);
                    string oldprofile = _profileSelected.GetItem(_currentProfileIndex);
                    if(!string.IsNullOrWhiteSpace(oldprofile))
                        XmlFileParser.SaveProfile(oldprofile);

                    UOSObjects.GumpIsLoading = true;
                    if(UOSObjects.Player != null && SerialHelper.IsMobile(UOSObjects.Player.Serial))
                    {
                        LinkedProfiles[UOSObjects.Player.Serial] = _profileSelected.GetItem(_profileSelected.SelectedIndex);
                    }

                    XmlFileParser.LoadProfile(this, _profileSelected.GetItem(_profileSelected.SelectedIndex));
                    UOSObjects.GumpIsLoading = false;

                    XmlFileParser.SaveConfig(this);
                }
            }
        }

        internal void OnProfileChanged()
        {
            SetMacroText(null);
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            XmlFileParser.SaveConfig(this);
        }

        private static bool _updated = false;

        public override void Update()
        {
            base.Update();
            if(!_updated)
            {
                _updated = true;
                SetInScreen();
            }
        }

        internal void LoadConfig()
        {
            FileSystemHelper.CreateFolderIfNotExists(Engine.DataPath);
            FileInfo info = new FileInfo(Path.Combine(Engine.DataPath, "assistant.xml"));
            if (!info.Exists)
            {
                const string DEFCONF = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<config>
 <data name=""LastProfile"">Default.xml</data>
 <data name=""SmartProfile"">True</data>
 <snapshots>
  <ownDeath>False</ownDeath>
  <othersDeath>False</othersDeath>
 </snapshots>
 <gump x=""300"" y=""300"" />
</config>
";
                using (StreamWriter w = new StreamWriter(Path.Combine(Engine.DataPath, info.FullName), false))
                {
                    w.Write(DEFCONF);
                    w.Flush();
                }
            }
            XmlFileParser.LoadConfig(info, this);
        }

        //data directory will be in Engine.DataPath + "Data", yes, it could be confusing, but it's to allow for backward compatibility with UOSteam
        private void LoadData()
        {
            string datadir = Path.Combine(Engine.DataPath, "Data");
            FileSystemHelper.CreateFolderIfNotExists(datadir);
            XmlFileParser.LoadSpellDef(new FileInfo(Path.Combine(datadir, "spells.xml")), this);
            XmlFileParser.LoadSkillDef(new FileInfo(Path.Combine(datadir, "skills.xml")), this);
            XmlFileParser.LoadBodyDef(new FileInfo(Path.Combine(datadir, "bodies.xml")));
            XmlFileParser.LoadBuffDef(new FileInfo(Path.Combine(datadir, "bufficons.xml")), this);
            XmlFileParser.LoadFoodDef(new FileInfo(Path.Combine(datadir, "foods.xml")));
        }

        private void LoadProfiles(string selected = null)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(Engine.ProfilePath);
            _profiles = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly);
            if (_profiles.Length < 1)
            {
                _profiles = new[] { Path.Combine(path, "Default.xml") };
                using (StreamWriter w = new StreamWriter(_profiles[0], false))
                {
                    w.Write(XmlFileParser.DEFPROF);
                    w.Flush();
                }
            }
            if (_profileSelected != null)
            {
                int index = -1;
                string[] newnames = new string[_profiles.Length];
                for (int i = _profiles.Length - 1; i >= 0; --i)
                {
                    newnames[i] = Path.GetFileNameWithoutExtension(_profiles[i]);
                    if(selected != null)
                    {
                        if (newnames[i] == selected)
                        {
                            index = i;
                        }
                    }
                    else if (newnames[i] == _profileSelected.GetItem(_profileSelected.SelectedIndex))
                    {
                        index = i;
                        selected = newnames[i];
                    }
                }
                _profileSelected.SetItemsValue(newnames);
                if (index >= 0 && selected != _profileSelected.GetItem(_profileSelected.SelectedIndex))
                {
                    _profileSelected.SelectedIndex = Math.Min(newnames.Length - 1, index);
                }
            }
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            if (sender is AssistCheckbox box)
                Filter.List[(int)box.Tag].OnCheckChanged(box.IsChecked);
        }
        internal static AssistCheckbox[] FiltersCB;
        private void BuildGeneral(int page)
        {
            int starty = (_buttonHeight * 2) - (_buttonHeight >> 2), startx = (WIDTH >> 5) * 13, diffx = (_buttonWidth - (_buttonWidth >> 3)) >> 2, diffy = ((_buttonHeight - (_buttonHeight >> 3)) >> 2);
            AssistScrollArea leftArea = new AssistScrollArea(8, _buttonHeight * 2, (_buttonWidth >> 2) * 30, diffy * 45);
            CreateRectangleArea(this, 3, starty, startx, leftArea.Height + (_buttonHeight >> 1), page, Color.Gray.PackedValue, 1, "Filters");
            FiltersCB = new AssistCheckbox[Filter.List.Count];
            for(int i = 0; i < Filter.List.Count; i++)
            {
                AssistCheckbox f = FiltersCB[i] = CreateCheckBox(leftArea, Filter.List[i].Name, Filter.List[i].Enabled, 0, 2);
                f.Tag = i;
                f.ValueChanged += FilterChanged;
            }
            Add(leftArea, page);
            starty += leftArea.Height + _buttonHeight;
            int w = (WIDTH >> 3) * 2;
            Add(new Label("Opacity", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = _buttonWidth >> 2, Y = starty - (_buttonHeight >> 2) }, page);
            _opacity = new HSliderBar((startx >> 2) + (_buttonWidth >> 1) + (_buttonWidth >> 2), starty - (_buttonHeight >> 3), w, 0, 99, 99, HSliderBarStyle.MetalWidgetRecessedBar);
            _opacity.ValueChanged += OnOpacityChanged;
            Add(_opacity, page);
            startx += _buttonWidth;
            _loadLinkedProfile = new AssistCheckbox(0x00D2, 0x00D3, "Load linked profile", FONT, ScriptTextBox.GRAY_HUE, true)
            {
                X = startx,
                Y = _buttonHeight * 2
            };
            Add(_loadLinkedProfile, page);
            /*_negotiateFeatures = new AssistCheckbox(0x00D2, 0x00D3, "Negotiate features with server", FONT, ScriptTextBox.GRAY_HUE, true)
            {
                X = startx,
                Y = _buttonHeight * 3
            };
            Add(_negotiateFeatures, page);*/
            starty = _buttonHeight * 5;

            //PROFILES
            //here we have the current array numbers, selected and max
            LoadProfiles();
            string[] newnames = new string[_profiles.Length];
            for (int i = _profiles.Length - 1; i >= 0; --i)
            {
                newnames[i] = Path.GetFileNameWithoutExtension(_profiles[i]);
            }
            int mode = 1;//the saved selected profile
            if (mode < 0 || mode >= _profiles.Length)
            {
                mode = 0;//so we have it
            }
            //these profiles are loaded as a string array
            starty += _buttonHeight * 2;
            CreateRectangleArea(this, startx - (_buttonWidth >> 3), starty - (_buttonHeight >> 2), _buttonWidth * 10 + (_buttonWidth >> 2), diffy * 13, page, Color.Gray.PackedValue, 1, "Profiles");
            starty += 2;
            _profileSelected = new AssistCombobox(startx, starty, _buttonWidth * 10, newnames, mode, HEIGHT >> 1);
            _profileSelected.OnBeforeContextMenu += OnBeforeProfileList;
            _profileSelected.OnOptionSelected += OnProfileSelected;
            Add(_profileSelected, page);
            starty += _buttonHeight + 5;
            int height = (_buttonHeight >> 1) + 5;
            AssistNiceButton nb = new AssistNiceButton(startx + diffx, starty, diffx * 16, height, ButtonAction.Activate, "Create New")
            {
                IsSelectable = false,
                ButtonParameter = (int)ButtonType.AddNewProfile
            };
            Add(nb, page);
            /*nb = new AssistNiceButton(nb.X + nb.Width + (_buttonWidth >> 1), starty, diffx * 12, height, ButtonAction.Activate, "Save")
            {
                IsSelectable = false,
                ButtonParameter = (int)ButtonType.SaveCurrentProfile
            };
            Add(nb, page);*/
            _linkSelectedProfile = new AssistNiceButton(nb.X + nb.Width + (_buttonWidth >> 1), starty, diffx * 29, height, ButtonAction.Activate, "Copy this on New")
            {
                IsSelectable = false,
                //ButtonParameter = (int)ButtonType.SetLinkedProfile
            };
            _linkSelectedProfile.LeftClicked += SetLinkedClicked;
            Add(_linkSelectedProfile, page);
            starty += _buttonHeight * 2;
            CreateRectangleArea(this, startx - (_buttonWidth >> 3), starty - (_buttonHeight >> 2), _buttonWidth * 10 + (_buttonWidth >> 2), diffy * 13, page, Color.Gray.PackedValue, 1, "Assistant Window Size");
            starty += diffy;
            Control c;
            Add(c = new Label("X: ", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = startx, Y = starty }, page);
            Add(_assistSizeX = new AssistArrowNumbersTextBox(startx + c.Width + (diffx * 2), starty, _buttonWidth * 3, 10, 500, 800, FONT, 4), page);
            _assistSizeX.Text = _width.ToString();
            starty += _buttonHeight;
            Add(c = new Label("Y: ", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = startx, Y = starty }, page);
            Add(_assistSizeY = new AssistArrowNumbersTextBox(startx + c.Width + (diffx * 2), starty, _buttonWidth * 3, 10, 325, 600, FONT, 4), page);
            _assistSizeY.Text = _height.ToString();
            startx += _buttonWidth * 6;
            Add(new AssistNiceButton(startx, starty - _buttonHeight, _buttonWidth * 3, _buttonHeight * 2, ButtonAction.Activate, "Apply")
            { IsSelectable = false, ButtonParameter = (int)ButtonType.ApplySizeChange }, page);
        }

        private void SetLinkedClicked(object sender, AssistNiceButton e)
        {
            _SetLinkedProfile = !_SetLinkedProfile;
            if (_SetLinkedProfile)
            {
                e.TextLabel.Hue = ScriptTextBox.GREEN_HUE;
                e.TextLabel.Text = "Waiting for New Profile";
            }
            else
            {
                e.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                e.TextLabel.Text = "Copy this on New";
            }
        }
        #endregion

        #region OPTIONS_TAB

        /*private enum SpellDisplayMode
        {
            Words = 1,
            Name = 2,
            Both = 3
        }*/

        private enum ParalyzePoisonHighlight : byte
        {
            Disabled = 0,
            Aura = 1,
            Colorize,
            Both
        }

        internal enum ShareTargetTo : byte
        {
            None = 0x00,
            Party = 0x01,
            Faction = 0x02,
            Alliance = 0x04,
            Guild = 0x08,
            PartyFaction = 0x03,
            PartyAlliance = 0x05,
            PartyGuild = 0x09,
            AllianceGuild = 0x0C,
            FactionGuild = 0x0A,
            FactionAlliance = 0x06,
            PartyFactionGuild = 0x0B,
            PartyFactionAlliance = 0x07,
            PartyAllianceGuild = 0x0D,
            FactionAllianceGuild = 0x0E,
            All = 0x0F
        }

        private enum HealingOptsTarget : byte
        {
            Self = 0x01,
            Last = 0x02,
            LastFriend = 0x04,
            AnyFriend = 0x08,
            FriendsListOnly = 0x10,
            Mount = 0x20,
        }

        internal enum SmartTargetFor : byte
        {
            None = 0,
            Friend = 1,
            Enemy,
            Both
        }

        private int _selectedSubOptionsPage = -1;
        private readonly AssistNiceButton[] _subOptionsPageNiceButtons = new AssistNiceButton[(int)PageType.LastOptions - (int)PageType.Generic];
        private AssistArrowNumbersTextBox _delayBetweenActions,//Generic _maxQueuedItems, _limitOpenRange
            _limitTargetRangeTiles,//Combat
            _startBelowValue, _bandageActionDelay;//Friends
        private AssistCheckbox _useObjectsQueue, _alwaysAcceptsPartyInvites, _displayNewCorpsesName, _countStealthSteps, _snapOnSelfDeath, _snapOnOthersDeath, 
            _searchNewContainers, /*_gauntletBoneCutter,*/ _bandageTimerStart, _bandageTimerEnd, _bandageTimerOverhead, _openDoors, _doubleClickToOpenDoors, //_openCorpses,//Generic
            _clearHandsBeforeCasting, _checkHandsBeforePotions, /*_healthAbovePeopleAndCreatures,*/ _flagsAbovePeopleAndCreatures,
            _highlightCurrentTarget, _limitTargetRange, _blockHealIfPoisonedOrYellowHits, _automaticallyRemount, _preventDuringWarmode, _useSharedTarget,//_useTargetQueue,//Combat
            _includePartyMembers, _considerOnlyThisAsValidFriends, _preventAttackingFriendsInWarmode, _healingEnabled, _scalePriorityBasedOnHits,
            _countSecondsUntilFinishes, _startBelowCheck, _allowHealingWhileHidden, _useDexterityFormulaDelay;//Friends
        private AssistClickableColorBox _highlightCurrentTargetHue;
        private AssistCombobox _openDoorsOptions,//Generic  _commandPrefix, _openCorpsesOptions,
            _smartLastTarget, _shareEnemyTargetOn, _spellShareTargetOn, //Combat
            _friendHealSelection;//Friends
        private AssistScrollArea _friendListArea;//Friends
        private uint _friendSelected;//Friends

        private void BuildOptions()
        {
            //PAGE 2
            int diffx = (_buttonWidth - (_buttonWidth >> 3)) >> 2;
            int width = diffx * 14, startx = diffx * 4;
            int starty = _buttonHeight + (_buttonHeight >> 2);
            for (int pagex = startx, page = (int)PageType.Generic; page < (int)PageType.LastOptions; pagex += width, page++)
            {
                string name = StringHelper.AddSpaceBeforeCapital(((PageType)page).ToString());
                for (int subpage = (int)PageType.Generic; subpage < (int)PageType.LastOptions; subpage++)
                {
                    AssistNiceButton but = new AssistNiceButton(pagex, starty, width, _buttonHeight, ButtonAction.SwitchPage, name, (int)PageType.LastOptions) { ButtonParameter = page };
                    if (page == subpage)
                    {
                        _subOptionsPageNiceButtons[subpage - (int)PageType.Generic] = but;
                    }

                    Add(but, subpage);
                }
                switch ((PageType)page)
                {
#region GENERIC_PAGE
                    case PageType.Generic:
                    {
                        int buttondiffx = _buttonWidth - (_buttonWidth >> 3), buttondiffy = _buttonHeight - (_buttonHeight >> 3);
                        int x = startx - ((buttondiffx >> 2) * 3), y = starty + _buttonHeight + (_buttonHeight >> 1), devx = buttondiffx >> 2;
                        //Add(new Label("Commands prefix", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        /*_commandPrefix = new AssistCombobox(x + devx * 30, y, devx * 11, _commandprefixes, 0, font: 2);
                        Add(_commandPrefix, page);
                        y += _buttonHeight;*/
                        Add(new Label("Delay between Actions", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        Add(_delayBetweenActions = new AssistArrowNumbersTextBox(x + devx * 30, y, devx * 11, 1, 50, 2500, FONT, 4), page);
                        _delayBetweenActions.ValueChanged += (sender, e) => _actionDelay = (uint)e;
                        y += buttondiffy + (_buttonHeight >> 4);
                        _useObjectsQueue = new AssistCheckbox(0x00D2, 0x00D3, "Use objects queue", FONT, ScriptTextBox.GRAY_HUE, true)
                        {
                            X = x,
                            Y = y
                        };
                        Add(_useObjectsQueue, page);
                        /*Add(_maxQueuedItems = new AssistArrowNumbersTextBox(x + devx * 30, y, devx * 11, 5, 1, 100, FONT, 3), page);
                        _maxQueuedItems.ValueChanged += (sender, e) => _useObjectsLimit = (byte)e;*/
                        y += _buttonHeight;
                        _alwaysAcceptsPartyInvites = new AssistCheckbox(0x00D2, 0x00D3, "Always accept party invitations", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_alwaysAcceptsPartyInvites, page);
                        y += _buttonHeight;
                        _displayNewCorpsesName = new AssistCheckbox(0x00D2, 0x00D3, "Display new corpses names", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_displayNewCorpsesName, page);
                        y += _buttonHeight;
                        _countStealthSteps = new AssistCheckbox(0x00D2, 0x00D3, "Count stealth steps", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_countStealthSteps, page);
                        y += _buttonHeight;
                        _searchNewContainers = new AssistCheckbox(0x00D2, 0x00D3, "Search new containers", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_searchNewContainers, page);
                        AssistNiceButton button;
                        Add(button = new AssistNiceButton(x + _searchNewContainers.Width + 2, y, devx * 20, buttondiffy, ButtonAction.Activate, "Exemptions") { IsSelectable = false, ButtonParameter = (int)ButtonType.ExemptionsContSearch }, page);
                        y += _buttonHeight;
                        /*_gauntletBoneCutter = new AssistCheckbox(0x00D2, 0x00D3, "Gauntlet bone cutter", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_gauntletBoneCutter, page);
                        Add(button = new AssistNiceButton(x +_searchNewContainers.Width + 2, y, devx * 20, buttondiffy, ButtonAction.Activate, "Set Blade") { IsSelectable = false, ButtonParameter = (int)ButtonType.BoneCutSetBlade }, page);
                        y += _buttonHeight;*/
                        Label l = new Label("Show Bandage Timer: ", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y };
                        Add(l, page);
                        Add(_bandageTimerStart = new AssistCheckbox(0x00D2, 0x00D3, "Start ", FONT, ScriptTextBox.GRAY_HUE, true) { X = x + l.Width, Y = y }, page);
                        Add(_bandageTimerEnd = new AssistCheckbox(0x00D2, 0x00D3, "End ", FONT, ScriptTextBox.GRAY_HUE, true) { X = x + l.Width + _bandageTimerStart.Width, Y = y }, page);
                        Add(_bandageTimerOverhead = new AssistCheckbox(0x00D2, 0x00D3, "(Overhead)", FONT, ScriptTextBox.GRAY_HUE, true) { X = x + l.Width + _bandageTimerStart.Width + _bandageTimerEnd.Width, Y = y }, page);
                        y += _buttonHeight;
                        Add(_snapOnSelfDeath = new AssistCheckbox(0x00D2, 0x00D3, "Snap my own death", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += _buttonHeight;
                        Add(_snapOnOthersDeath = new AssistCheckbox(0x00D2, 0x00D3, "Snap other players death", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        //from right up
                        y = starty + _buttonHeight + (_buttonHeight >> 3);
                        x = (WIDTH >> 1) + buttondiffx;
                        CreateRectangleArea(this, x - (buttondiffx >> 2), y, devx * 46, _buttonHeight * 2 + (_buttonHeight >> 1), page, Color.Gray.PackedValue, 1, "Open Doors");
                        y += (buttondiffy >> 2) * 2;
                        _openDoors = new AssistCheckbox(0x00D2, 0x00D3, "Enabled", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_openDoors, page);
                        string[] excluders = Enum.GetNames(typeof(ActionExclusion));
                        StringHelper.AddSpaceBeforeCapital(excluders);
                        _openDoorsOptions = new AssistCombobox(x + devx * 18, y, devx * 25, excluders, 0);
                        Add(_openDoorsOptions, page);
                        y += _buttonHeight;
                        _doubleClickToOpenDoors = new AssistCheckbox(0x00D2, 0x00D3, "Use double click to open doors", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_doubleClickToOpenDoors, page);
                        y += _buttonHeight * 2 - (_buttonHeight >> 3);
                        /*CreateRectangleArea(this, x - 5, y, devx * 46, _buttonHeight * 2 + (_buttonHeight >> 1), page, Color.Gray.PackedValue, 1, "Open Corpses");
                        y += (buttondiffy >> 2) * 2;
                        _openCorpses = new AssistCheckbox(0x00D2, 0x00D3, "Enabled", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_openCorpses, page);
                        Add(_openCorpsesOptions = new AssistCombobox(x + devx * 18, y, devx * 25, excluders, 0), page);
                        y += _buttonHeight;
                        Add(new Label("Limit open range to", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        _limitOpenRange = new AssistArrowNumbersTextBox(x + _buttonWidth * 5, y, devx * 12, 1, 0, 10, FONT, 2);
                        _limitOpenRange.ValueChanged += (sender, e) => _openCorpsesRange = (byte)e;
                        Add(_limitOpenRange, page);
                        Add(new Label("tiles", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = _limitOpenRange.X + _limitOpenRange.Width + (_buttonWidth >> 3), Y = y }, page);*/
                        break;
                    }
#endregion

#region COMBAT_PAGE
                    case PageType.Combat:
                    {
                        int x = startx - ((_buttonWidth >> 1) + (diffx >> 1)), buttondiffy = _buttonHeight - (_buttonHeight >> 3), y = starty + _buttonHeight + (_buttonHeight >> 2);
                        CreateRectangleArea(this, x - (_buttonWidth >> 3), y, (_buttonWidth * 6 + diffx * 24) - diffx, _buttonHeight * 3, page, Color.Gray.PackedValue, 1, "Casting");
                        y += (_buttonHeight / 3);
                        Add(new Label("Share spell target on", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        string[] displaymodes = new string[(int)ShareTargetTo.All + 1];
                        for(int i = 0; i<displaymodes.Length; i++)
                        {
                            displaymodes[i] = StringHelper.AddSpaceBeforeCapital(((ShareTargetTo)i).ToString());
                        }
                        _spellShareTargetOn = new AssistCombobox(x + _buttonWidth * 5 + (_buttonWidth >> 1) + (_buttonWidth >> 3), y, diffx * 24, displaymodes, 0);
                        Add(_spellShareTargetOn, page);
                        y += buttondiffy;
                        _clearHandsBeforeCasting = new AssistCheckbox(0x00D2, 0x00D3, "Clear hands before casting", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_clearHandsBeforeCasting, page);
                        y += _buttonHeight * 2 + (_buttonHeight >> 3);
                        //end first rect
                        CreateRectangleArea(this, x - (_buttonWidth >> 3), y, (_buttonWidth * 6 + diffx * 24) - diffx, (_buttonHeight >> 1) * 14 + 2, page, Color.Gray.PackedValue, 1, "Targeting");
                        y += _buttonHeight / 3;
                        Add(new Label("Smart last Target", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        _smartLastTarget = new AssistCombobox(x + _buttonWidth * 5 + (_buttonWidth >> 1) + (_buttonWidth >> 3), y, diffx * 24, Enum.GetNames(typeof(SmartTargetFor)), 0);
                        Add(_smartLastTarget, page);
                        y += buttondiffy + (_buttonHeight >> 3);
                        Add(new Label("Share enemy target on", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        _shareEnemyTargetOn = new AssistCombobox(x + _buttonWidth * 5 + (_buttonWidth >> 1) + (_buttonWidth >> 3), y, diffx * 24, displaymodes, 0);
                        Add(_shareEnemyTargetOn, page);
                        y += buttondiffy;
                        _highlightCurrentTarget = new AssistCheckbox(0x00D2, 0x00D3, "Highlight current target", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_highlightCurrentTarget, page);
                        y += buttondiffy;
                        _highlightCurrentTargetHue = CreateClickableColorBox(x + _buttonWidth, y, 53, "Highlight Color", page);
                        _highlightCurrentTargetHue.ValueChanged += _highlightTargetHue_ValueChanged;
                        _highlightCurrentTarget.ValueChanged += _highlightTargetHue_ValueChanged;
                        y += buttondiffy + (buttondiffy >> 3);
                        _limitTargetRange = new AssistCheckbox(0x00D2, 0x00D3, "Limit target range to", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_limitTargetRange, page);
                        _limitTargetRangeTiles = new AssistArrowNumbersTextBox(x + _buttonWidth * 7, y, diffx * 8, 1, 2, 15, FONT, 2);
                        _limitTargetRangeTiles.ValueChanged += (sender, e) => _smartTargetRangeValue = (byte)e;
                        Add(_limitTargetRangeTiles, page);
                        y += buttondiffy;
                        _blockHealIfPoisonedOrYellowHits = new AssistCheckbox(0x00D2, 0x00D3, "Block heal if poisoned or yellow hits", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_blockHealIfPoisonedOrYellowHits, page);
                        y += buttondiffy;
                        _useSharedTarget = new AssistCheckbox(0x00D2, 0x00D3, "Set 'shared' Target alias in 'enemy'", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_useSharedTarget, page);
                        y = starty + _buttonHeight + (_buttonHeight >> 2);
                        x = _buttonWidth * 6 + diffx * 24 + 3;
                        CreateRectangleArea(this, x - (_buttonWidth >> 3), y, WIDTH - x, _buttonHeight * 4, page, Color.Gray.PackedValue, 1, "Dismount");
                        y += 10;
                        Add(new AssistNiceButton(x + _buttonWidth, y, 120, buttondiffy, ButtonAction.Activate, "Set Mount") { IsSelectable = false, ButtonParameter = (int)ButtonType.RemountSetMount }, page);
                        y += buttondiffy;
                        _automaticallyRemount = new AssistCheckbox(0x00D2, 0x00D3, "Automatically remount", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_automaticallyRemount, page);
                        y += _buttonHeight;
                        _preventDuringWarmode = new AssistCheckbox(0x00D2, 0x00D3, "Prevent dismount in warmode", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_preventDuringWarmode, page);
                        x -= _buttonWidth >> 3;
                        y += _buttonHeight * 2;
                        _checkHandsBeforePotions = new AssistCheckbox(0x00D2, 0x00D3, "Check hands before potions", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_checkHandsBeforePotions, page);
                        y += buttondiffy;
                        /*_healthAbovePeopleAndCreatures = new AssistCheckbox(0x00D2, 0x00D3, "Health above people and creatures", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_healthAbovePeopleAndCreatures, page);
                        y += buttondiffy;*/
                        _flagsAbovePeopleAndCreatures = new AssistCheckbox(0x00D2, 0x00D3, "Flags above people and creatures", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y };
                        Add(_flagsAbovePeopleAndCreatures, page);
                        break;
                    }
#endregion

#region FRIENDS_PAGE
                    case PageType.Friends:
                    {
                        int diffy = _buttonHeight - (_buttonHeight >> 2), buttondiffx = _buttonWidth - (_buttonWidth >> 2);
                        int x = startx - (_buttonWidth >> 1), y = starty + _buttonHeight + (_buttonHeight >> 3) + 2;
                        CreateRectangleArea(this, x - (_buttonWidth >> 3), y, (WIDTH >> 1) - (_buttonWidth >> 2), HEIGHT - (_buttonHeight * 6 + (diffy >> 3)), page, Color.Gray.PackedValue, 1, "Friends List");
                        y += (_buttonHeight >> 3) + (diffy >> 3);
                        //The FRIENDLIST is created here, but only for dimensional and positioning handling, the list is populated later on
                        _friendListArea = new AssistScrollArea(x, y, (WIDTH >> 1) - (_buttonHeight >> 1), HEIGHT - ((_buttonHeight * 6) + (diffy >> 1)));
                        Add(_friendListArea, page);
                        y += _friendListArea.Height + (_buttonHeight >> 3);
                        Add(new AssistNiceButton(x, y, (_friendListArea.Width >> 1) - buttondiffx, diffy, ButtonAction.Activate, "Remove Friend") { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveFriend }, page);
                        Add(new AssistNiceButton(x + (_friendListArea.Width >> 1) + buttondiffx, y, (_friendListArea.Width >> 1) - buttondiffx, diffy, ButtonAction.Activate, "Insert Friend") { IsSelectable = false, ButtonParameter = (int)ButtonType.InsertFriend }, page);
                        x -= _buttonHeight >> 3;
                        diffy += _buttonHeight >> 3;
                        buttondiffx += _buttonWidth >> 3;
                        y += diffy;
                        Add(_includePartyMembers = new AssistCheckbox(0x00D2, 0x00D3, "Include party members", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += diffy;
                        Add(_considerOnlyThisAsValidFriends = new AssistCheckbox(0x00D2, 0x00D3, "Consider only this as valid friends", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += diffy;
                        Add(_preventAttackingFriendsInWarmode = new AssistCheckbox(0x00D2, 0x00D3, "Prevent attacking friends in warmode", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        x = _buttonWidth * 6 + diffx * 21;
                        y = starty + _buttonHeight + (_buttonHeight >> 1);
                        CreateRectangleArea(this, x - (_buttonHeight >> 3), y, WIDTH - x, HEIGHT - (_buttonHeight * 4 + (diffy >> 3)), page, Color.Gray.PackedValue, 1, "Healing Options");
                        y += (_buttonHeight >> 3) + (diffy >> 2);
                        Add(_healingEnabled = new AssistCheckbox(0x00D2, 0x00D3, "Enabled", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        string[] names = Enum.GetNames(typeof(HealingOptsTarget));
                        StringHelper.AddSpaceBeforeCapital(names);
                        Add(_friendHealSelection = new AssistCombobox(x + _healingEnabled.Width + buttondiffx, y, _buttonWidth * 6, names, 0), page);
                        y += diffy * 2;
                        Add(_scalePriorityBasedOnHits = new AssistCheckbox(0x00D2, 0x00D3, "Scale priority based on hits", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += diffy;
                        Add(_countSecondsUntilFinishes = new AssistCheckbox(0x00D2, 0x00D3, "Count seconds until finishes", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += diffy;
                        Add(_startBelowCheck = new AssistCheckbox(0x00D2, 0x00D3, "Start below", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        Add(_startBelowValue = new AssistArrowNumbersTextBox(x + _startBelowCheck.Width + (buttondiffx >> 1), y, buttondiffx * 3, 5, 5, 100, FONT, 3), page);
                        _startBelowValue.ValueChanged += (sender, e) => _autoBandageStartValue = (byte)e;
                        Add(new Label("% hits", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = _startBelowValue.X + _startBelowValue.Width + buttondiffx, Y = y }, page);
                        y += diffy;
                        Add(_allowHealingWhileHidden = new AssistCheckbox(0x00D2, 0x00D3, "Allow healing while hidden", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += diffy * 2;
                        Add(_useDexterityFormulaDelay = new AssistCheckbox(0x00D2, 0x00D3, "Use dexterity formula delay", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = y }, page);
                        y += diffy;
                        Label l = new Label("Bandage action delay", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y };
                        Add(l, page);
                        Add(_bandageActionDelay = new AssistArrowNumbersTextBox(x + l.Width + (buttondiffx >> 2), y, _buttonWidth * 3, 25, 500, 20000, FONT, 5), page);
                        _bandageActionDelay.ValueChanged += (sender, e) => _AutoBandageDelay = (uint)e;
                        break;
                    }
#endregion
                }
            }
        }

        private void _highlightTargetHue_ValueChanged(object sender, EventArgs e)
        {
            _highlightCurrentTargetHue.IsEnabled = _highlightCurrentTarget.IsChecked;
            if(Targeting.LastTargetInfo != null && SerialHelper.IsMobile(Targeting.LastTargetInfo.Serial))
            {
                UOMobile m = UOSObjects.FindMobile(Targeting.LastTargetInfo.Serial);
                if(m != null)
                {
                    ClientPackets.PRecv_MobileIncoming(m);
                }
            }
        }

        internal void UpdateFriendListGump(uint serial = 0)
        {
            _friendListArea.Clear();
            foreach (KeyValuePair<uint, string> kvp in FriendsManager.FriendDictionary)
            {
                var b = CreateSelection(_friendListArea, $"{kvp.Value}: 0x{kvp.Key:X}", 2, (int)ButtonType.FriendsList, (int)ButtonType.FriendsList, kvp.Key);
                if (serial > 0 && kvp.Key == serial)
                    b.IsSelected = true;
            }
        }
#endregion

#region HOTKEYS_TAB
        private AssistMultiSelectionShrinkbox _mainHK,
            _actionsHK, _actionsUseHK, _actionShowNamesHK, _actionCreaturesHK,
            _agentsHK, _agentsDressHK, _agentsUndressHK, _agentsOrganizerHK, _agentsVendorsHK, _agentsVendorsBuyHK, _agentsVendorsSellHK,
            _combatHK, _combatAbilitiesHK, _combatAttackHK, _combatBandageHK, 
                       _combatConsumeHK, _combatConsumePotionsHK, _combatConsumeMiscellaneousHK,
                       _combatToggleHandsHK, _combatEquipWandsHK,
            _skillsHK,
            _spellsHK, _spellsBigHealHK, _spellsMiniHealHK, 
            _targetingHK, _targetingAttackHK, _targetingFriendsHK, 
                          _targetingGetHK, _targetingGetEnemyHK, _targetingGetFriendsHK,
                          _targetingSetHK, 
                          //_targetingTargetHK,
            _macrosHK;
        internal AssistMultiSelectionShrinkbox SkillsHK => _skillsHK;
        internal AssistHotkeyBox _keyName;
        private static readonly string[] _emptyStrArr = new string[] { };

        private bool SpellHKFunc(string input)
        {
            Spell s = Spell.GetByName(input);
            if (s != null)
            {
                Spell.FullCast(s.Number);//.OnCast(new CastSpellFromMacro((ushort)s.Number));
            }
            return true;
        }
        internal void AddSpellsToHotkeys(string classname, List<string> spells)
        {
            var msb = new AssistMultiSelectionShrinkbox(20, 2, 0, classname, spells.ToArray(), ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            msb.OnOptionSelected += HotKey_OnSpellSelected;
            _spellsHK.NestBox(msb);
            for(int i = 0; i < spells.Count; i++)
            {
                HotKeys.AddHotKeyFunc(($"spells.{classname}.{spells[i]}").ToLower(XmlFileParser.Culture).Replace(" ", ""), SpellHKFunc);
            }
        }

        //Hokeys for standardized commands and added Key shortcuts/activaction button for Actions (also Macros/Scripts)
        private void BuildHotkeys(int page)
        {
            //PAGE 3
            int x = _buttonWidth >> 3, y = _buttonHeight + (_buttonHeight >> 1);
            CreateRectangleArea(this, x, y, WIDTH - (y + _buttonWidth * 6), HEIGHT - (y + (_buttonHeight >> 3)), page, Color.Gray.PackedValue, 1, "Controllable Elements", ScriptTextBox.GRAY_HUE, FONT);
            x += _buttonWidth >> 3;
            y += _buttonHeight >> 2;
            AssistScrollArea leftArea = new AssistScrollArea(x, y, WIDTH - (y + _buttonWidth * 6), HEIGHT - (y + (_buttonHeight >> 2)));
            Add(leftArea, page);
            _mainHK = CreateMultiSelection(leftArea, "Main", new string[] { "Ping", "Resyncronize", "Toggle Hotkeys", "Snapshot" }, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _mainHK.OnOptionSelected += HotKey_OnOptionSelected;
            _actionsHK = CreateMultiSelection(leftArea, "Actions", new string[] { "Grab Item", "Drop Current", "Toggle Mounted" }, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _actionsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _actionsUseHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Use", new string[]{"Last Object", "Left Hand", "Right Hand"}, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _actionsUseHK.OnOptionSelected += HotKey_OnOptionSelected;
            _actionsHK.NestBox(_actionsUseHK);
            _actionShowNamesHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Show Names", new string[] {"All", "Corpses", "Mobiles"}, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _actionShowNamesHK.OnOptionSelected += HotKey_OnOptionSelected;
            _actionsHK.NestBox(_actionShowNamesHK);
            _actionCreaturesHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Creatures", new string[] {"Come", "Follow", "Guard", "Kill", "Stay", "Stop"}, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _actionCreaturesHK.OnOptionSelected += HotKey_OnOptionSelected;
            _actionsHK.NestBox(_actionCreaturesHK);
            _agentsHK = CreateMultiSelection(leftArea, "Agents", new string[] { "Autoloot Target", "Toggle Autoloot", "Toggle Scavenger" }, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _agentsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsDressHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Dress", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _agentsDressHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsHK.NestBox(_agentsDressHK);
            _agentsUndressHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Undress", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _agentsUndressHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsHK.NestBox(_agentsUndressHK);
            _agentsOrganizerHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Organizer", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _agentsOrganizerHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsHK.NestBox(_agentsOrganizerHK);
            _agentsVendorsHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Vendors", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            //_agentsVendorsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsHK.NestBox(_agentsVendorsHK);
            _agentsVendorsBuyHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Buy", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _agentsVendorsBuyHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsVendorsHK.NestBox(_agentsVendorsBuyHK);
            _agentsVendorsSellHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Sell", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _agentsVendorsSellHK.OnOptionSelected += HotKey_OnOptionSelected;
            _agentsVendorsHK.NestBox(_agentsVendorsSellHK);
            //Combat
            _combatHK = CreateMultiSelection(leftArea, "Combat", new string[] {}, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            //_combatHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatAbilitiesHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Abilities", new string[] {"Primary", "Secondary", "Stun (Pre-AOS)", "Disarm (Pre-AOS)" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatAbilitiesHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatHK.NestBox(_combatAbilitiesHK);
            _combatAttackHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Attack", new string[] { "Enemy", "Last Target", "Last Combatant" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatAttackHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatHK.NestBox(_combatAttackHK);
            _combatBandageHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Bandage", new string[] { "Self", "Last", "Mount", "Target" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatBandageHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatHK.NestBox(_combatBandageHK);
            _combatConsumeHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Consume", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            //_combatConsumeHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatHK.NestBox(_combatConsumeHK);
            _combatConsumePotionsHK = new AssistMultiSelectionShrinkbox(40, 2, 0, "Potions", new string[] {"Agility", "Cure", "Explosion", "Heal", "Refresh", "Strength", "Nightsight", "Mana", "Invisibility", "Urticant", "Stone to Flesh", "Speed Potion" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatConsumePotionsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatConsumeHK.NestBox(_combatConsumePotionsHK);
            _combatConsumeMiscellaneousHK = new AssistMultiSelectionShrinkbox(40, 2, 0, "Miscellaneous", new string[] { "Enchanted Apple", "Orange Petals", "Wrath Grapes", "Rose of Trinsic", "Smoke Bomb", "Spell Stone", "Healing Stone" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatConsumeMiscellaneousHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatConsumeHK.NestBox(_combatConsumeMiscellaneousHK);
            _combatToggleHandsHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Toggle Hands", new string[] {"Left", "Right"}, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatToggleHandsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatHK.NestBox(_combatToggleHandsHK);
            _combatEquipWandsHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Equip Wands", new string[] { "Clumsy", "Identification", "Heal", "Feeblemind", "Weakness", "Magic Arrow", "Harm", "Fireball", "Greater Heal", "Lightning", "Mana Drain" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _combatEquipWandsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _combatHK.NestBox(_combatToggleHandsHK);
            //Skills
            _skillsHK = CreateMultiSelection(leftArea, "Skills", _emptyStrArr, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _skillsHK.OnOptionSelected += SkillsHotKey_OnOptionSelected;
            //Spells
            _spellsHK = CreateMultiSelection(leftArea, "Spells", new string[] { "Last" }, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _spellsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _spellsBigHealHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Big Heal", new string[] { "Friend", "Self" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _spellsBigHealHK.OnOptionSelected += HotKey_OnOptionSelected;
            _spellsHK.NestBox(_spellsBigHealHK);
            _spellsMiniHealHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Mini Heal", new string[] { "Friend", "Self" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _spellsMiniHealHK.OnOptionSelected += HotKey_OnOptionSelected;
            _spellsHK.NestBox(_spellsMiniHealHK);
            LoadData();
            //Targeting
            _targetingHK = CreateMultiSelection(leftArea, "Targeting", _emptyStrArr, 2, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            //_targetingHK.OnOptionSelected += HotKey_OnOptionSelected;
            _targetingAttackHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Attack", new string[] {"Enemy", "Last"}, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _targetingAttackHK.OnOptionSelected += HotKey_OnOptionSelected;
            _targetingHK.NestBox(_targetingAttackHK);
            _targetingFriendsHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Friends", new string[] { "Add", "Remove" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _targetingFriendsHK.OnOptionSelected += HotKey_OnOptionSelected;
            _targetingHK.NestBox(_targetingFriendsHK);
            _targetingGetHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Get", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            //_targetingGetHK.OnOptionSelected += HotKey_OnOptionSelected;
            _targetingHK.NestBox(_targetingGetHK);
            _targetingGetEnemyHK = new AssistMultiSelectionShrinkbox(30, 2, 0, "Enemy", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _targetingGetHK.NestBox(_targetingGetEnemyHK);
            _generateGetData(_targetingGetEnemyHK, true);
            _targetingGetFriendsHK = new AssistMultiSelectionShrinkbox(30, 2, 0, "Friend", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _targetingGetHK.NestBox(_targetingGetFriendsHK);
            _generateGetData(_targetingGetFriendsHK, false);
            _targetingSetHK = new AssistMultiSelectionShrinkbox(20, 2, 0, "Set", new string[] { "Enemy", "Friend" }, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
            _targetingSetHK.OnOptionSelected += HotKey_OnOptionSelected;
            _targetingHK.NestBox(_targetingSetHK);
            _macrosHK = CreateMultiSelection(leftArea, "Macros", _emptyStrArr, 2, (int)ButtonType.HotKeyList,0x93A, 0x939);
            _macrosHK.OnOptionSelected += MacroHotKey_OnOptionSelected;
            //all the principal data for our server
            x = WIDTH - (y + _buttonWidth * 6) + (_buttonWidth >> 1);
            CreateRectangleArea(this, x, y, WIDTH - (x + (_buttonWidth >> 3)), HEIGHT - (y + _buttonHeight * 8), page, Color.Gray.PackedValue, 1, "Hotkey", ScriptTextBox.GRAY_HUE, FONT);
            x += _buttonWidth >> 3;
            y += _buttonHeight >> 2;
            Add(_keyName = new AssistHotkeyBox(x, y, WIDTH - (x + (_buttonWidth >> 3) + 5), HEIGHT - (y + _buttonHeight * 8), FONT, ScriptTextBox.GRAY_HUE) { IsEnabled = false }, page);
            _keyName.AddButton += _keyName_AddButton;
            _keyName.HotkeyChanged += _keyName_HotkeyChanged;
            _keyName.HotkeyCleared += _keyName_HotkeyCleared;
        }

        private void _keyName_AddButton(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedHK))
            {
                if (_selectedHK.IndexOf('.') < 0)//macro, not an actual hotkey
                {
                    _macroRoutine(_selectedHK);
                }
                else
                {
                    UIManager.Gumps.OfType<AssistHotkeyButtonGump>().FirstOrDefault(s => s._hotkeyName == _selectedHK)?.Dispose();
                    var hotkeyButtonGump = new AssistHotkeyButtonGump(_selectedHK, PrettySelHK, Mouse.Position.X, Mouse.Position.Y);
                    UIManager.Add(hotkeyButtonGump);
                }
            }
        }

        private void _generateGetData(AssistMultiSelectionShrinkbox box, bool enemy)
        {
            Dictionary<string, Targeting.TargetType> targpairs = new Dictionary<string, Targeting.TargetType>()
            {
                { "Criminal", Targeting.TargetType.Criminal },
                { "Enemy", Targeting.TargetType.Enemy },
                { "Gray", Targeting.TargetType.Gray },
                { "Innocent", Targeting.TargetType.Innocent },
                { "Murderer", Targeting.TargetType.Murderer },
                { "Non-Friendly", Targeting.TargetType.Any & ~Targeting.TargetType.Friend },
                { "Friend", Targeting.TargetType.Friend }
            };
            Dictionary<string, Targeting.FilterType> formpairs = new Dictionary<string, Targeting.FilterType>()
            {
                { "Any", Targeting.FilterType.Next },
                { "Both", Targeting.FilterType.AnyForm },
                { "Humanoid", Targeting.FilterType.Humanoid },
                { "Transformation", Targeting.FilterType.Transformation }
            };
            for(int ft = 0; ft < 3; ++ft)
            {
                Targeting.FilterType filter = (Targeting.FilterType)ft;
                AssistMultiSelectionShrinkbox inbox = new AssistMultiSelectionShrinkbox(40, 2, 0, $"{filter}", _emptyStrArr, ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
                box.NestBox(inbox);
                foreach(KeyValuePair<string, Targeting.TargetType> kvp in targpairs)
                {
                    AssistMultiSelectionShrinkbox ttbox = new AssistMultiSelectionShrinkbox(50, 2, 0, $"{kvp.Key}", formpairs.Keys.ToArray(), ScriptTextBox.GRAY_HUE, true, FONT, (int)ButtonType.HotKeyList, 0x93A, 0x939);
                    inbox.NestBox(ttbox);
                    foreach (KeyValuePair<string, Targeting.FilterType> fkvp in formpairs)
                    {
                        _InstanceSB.AppendFormat("{0}.{1}", kvp.Key, fkvp.Key);
                        AssistMultiSelectionShrinkbox msb = ttbox;
                        while ((msb = msb.ParentBox) != null && msb.Name != null)
                        {
                            _InstanceSB.Insert(0, '.');
                            _InstanceSB.Insert(0, msb.Name);
                        }
                        HotKeys.AddHotKeyFunc(_InstanceSB.ToString().ToLower(XmlFileParser.Culture), (input) =>
                        {
                            Targeting.GetTarget(kvp.Value, filter | fkvp.Value, enemy, false);
                            return true;
                        });
                        _InstanceSB.Clear();
                    }
                    ttbox.OnOptionSelected += HotKey_OnOptionSelected;
                }
            }
        }

        private void _keyName_HotkeyCleared(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedHK))
            {
                HotKeys.RemoveHotKey(_selectedHK, onlyclear: true);
            }
        }

        private void _keyName_HotkeyChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedHK) && sender is AssistHotkeyBox box && HotKeys.GetVKfromSDL((int)box.Key, (int)box.Mod, out uint vkey))
            {
                OnHotKeyChanged(ref _selectedHK, box, vkey, _selectedHK.IndexOf('.') < 0);
            }
        }

        private string _selectedHK;
        internal string PrettySelHK { get; private set; }
        internal string SelectedHK
        {
            get => _selectedHK;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    int idx = value.LastIndexOf('.');
                    PrettySelHK = value.Substring(idx < 0 ? 0 : idx);
                    _selectedHK = idx < 0 ? value : value.ToLower(XmlFileParser.Culture);
                    _keyName.IsEnabled = true;
                    HotKeys.GetSDLKeyCodes(_selectedHK, out int key, out int mod);
                    _keyName.SetKey((SDL.SDL_Keycode)key, (SDL.SDL_Keymod)mod);
                }
                else
                {
                    PrettySelHK = _selectedHK = value;
                    _keyName.IsEnabled = false;
                    _keyName.SetKey(SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Keymod.KMOD_NONE);
                }
            }
        }

        private void SkillsHotKey_OnOptionSelected(object sender, Control c)
        {
            if (sender is not AssistMultiSelectionShrinkbox msb || msb.Name == null)
            {
                SelectedHK = null;
            }
            else
            {
                _InstanceSB.AppendFormat("{0}.{1}", msb.Name, msb.SelectedName);
                SelectedHK = _InstanceSB.ToString();//.ToLower(XmlFileParser.Culture);
                _InstanceSB.Clear();
                if (c is Button)
                    HotKeys.PlayFunc(SelectedHK);
            }
        }

        private void HotKey_OnOptionSelected(object sender, Control c)
        {
            if (sender is AssistMultiSelectionShrinkbox msb && msb.Name != null && msb.SelectedName != null)
            {
                _InstanceSB.AppendFormat("{0}.{1}", msb.Name, msb.SelectedName.Split('(')[0]);
                while ((msb = msb.ParentBox) != null && msb.Name != null)
                {
                    _InstanceSB.Insert(0, '.');
                    _InstanceSB.Insert(0, msb.Name);
                }
                SelectedHK = _InstanceSB.Replace(" ", "").ToString();//.ToLower(XmlFileParser.Culture);
                _InstanceSB.Clear();
                if (c is Button)
                    HotKeys.PlayFunc(SelectedHK);
            }
            else
                SelectedHK = null;
        }

        private void HotKey_OnSpellSelected(object sender, Control c)
        {
            if (sender is AssistMultiSelectionShrinkbox msb && msb.Name != null && msb.SelectedName != null)
            {
                string selname = msb.SelectedName;
                _InstanceSB.AppendFormat("{0}.{1}", msb.Name, msb.SelectedName);
                while ((msb = msb.ParentBox) != null && msb.Name != null)
                {
                    _InstanceSB.Insert(0, '.');
                    _InstanceSB.Insert(0, msb.Name);
                }
                SelectedHK = _InstanceSB.Replace(" ", "").ToString();//.ToLower(XmlFileParser.Culture);
                _InstanceSB.Clear();
                if (c is Button)
                    HotKeys.PlayFunc(SelectedHK, selname);
            }
            else
                SelectedHK = null;
        }

        private void MacroHotKey_OnOptionSelected(object sender, Control c)
        {
            if (sender is AssistMultiSelectionShrinkbox msb && msb.Name != null && msb.SelectedName != null)
            {
                if(msb.SelectedIndex == 0)//macro stop case
                {
                    SelectedHK = "macro.stop";
                }
                else
                {
                    SelectedHK = msb.SelectedName;
                }
                if (c is Button)
                {
                    if (msb.SelectedIndex != 0)
                    {
                        HotKeys.PlayFunc("macro.play", SelectedHK);
                    }
                    else
                        HotKeys.PlayFunc(SelectedHK);
                }
                    
            }
            else
                SelectedHK = null;
        }
#endregion

#region Macros
        private AssistScrollArea _macroListArea;
        private AssistNiceButton _playMacro, _recordMacro, _newMacro, _delMacro, _saveMacro;
        internal AssistNiceButton PlayMacro => _playMacro;
        internal AssistNiceButton RecordMacro => _recordMacro;

        private AreaContainer _macroArea;
        private GumpPicTiled _macroPicTiled;
        internal AssistHotkeyBox _macrokeyName;
        internal AssistStbTextBox MacroBox { get; private set; }
        private AssistCheckbox _loopMacro, _noautoInterrupt, _recordAsType, _returnToParent, _StartStopMacroMessages;
        internal bool RecordTypeUse => _recordAsType.IsChecked;
        private string _macroSelected = null;//Macro selected
        internal string MacroSelected
        {
            get => _macroSelected;
            private set
            {
                _macroSelected = value;
            }
        }
        private void BuildMacros(int page)
        {
            int x = _buttonWidth >> 3, y = _buttonHeight + (_buttonHeight >> 1);
            CreateRectangleArea(this, x, y, (WIDTH >> 2) + _buttonWidth + (_buttonWidth >> 1), HEIGHT - (y + _buttonHeight * 2) + (_buttonHeight >> 3), page, Color.Gray.PackedValue, 1, "Macro Names", ScriptTextBox.GRAY_HUE, FONT);
            x += _buttonWidth >> 4;
            y += _buttonHeight >> 2;
            _macroListArea = new AssistScrollArea(x, y, (WIDTH >> 2) + _buttonWidth + (_buttonWidth >> 2) + (_buttonWidth >> 3), HEIGHT - (y + _buttonHeight * 2));
            Add(_macroListArea, page);
            y += _macroListArea.Height + (_buttonHeight >> 3);
            x = _buttonWidth >> 3;
            Control b;
            Add(b = _delMacro = new AssistNiceButton(x, y, (_macroListArea.Width >> 2) + (_buttonWidth >> 1), HEIGHT - (y + (_buttonHeight >> 2)), ButtonAction.Activate, "Remove") { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveMacro }, page);
            x += b.Width + (_buttonWidth >> 3);
            Add(b = _newMacro = new AssistNiceButton(x, y, (_macroListArea.Width >> 2), HEIGHT - (y + (_buttonHeight >> 2)), ButtonAction.Activate, "New") { IsSelectable = false, ButtonParameter = (int)ButtonType.NewMacro }, page);
            x += b.Width + (_buttonWidth >> 3);
            Add(_saveMacro = new AssistNiceButton(x, y, (_macroListArea.Width >> 2) + (_buttonWidth >> 1), HEIGHT - (y + (_buttonHeight >> 2)), ButtonAction.Activate, "Save") { IsSelectable = false, ButtonParameter = (int)ButtonType.SaveMacro }, page);
            x = (_buttonWidth >> 3) * 2 + (WIDTH >> 2) + _buttonWidth + (_buttonWidth >> 1);
            y = _buttonHeight + (_buttonHeight >> 1);
            Add(_macroPicTiled = new GumpPicTiled(x, y, (WIDTH >> 1) - _buttonWidth, HEIGHT - (y + _buttonHeight * 2), 0xBBC), page);
            CreateRectangleArea(this, x, y, _macroPicTiled.Width, _macroPicTiled.Height, page, Color.Red.PackedValue, 1, null);
            _macroArea = new AreaContainer(_macroPicTiled.X + 2, _macroPicTiled.Y + 2, _macroPicTiled.Width - 4, _macroPicTiled.Height - 4, MacroBox = new ScriptTextBox(FONT, _macroPicTiled.Width));
            //MacroBox.TextChanged += MacroBox_TextChanged;
            Add(_macroArea, page);
            Add(b = new AssistNiceButton(x, _macroPicTiled.Y + _macroPicTiled.Height + (_buttonHeight >> 1), _buttonWidth * 5, _buttonHeight, ButtonAction.Activate, "Object Inspector") { IsSelectable = false, ButtonParameter = (int)ButtonType.ObjectInspector }, page);
            Add(_returnToParent = new AssistCheckbox(0x00D2, 0x00D3, "playmacro Returns to Parent", FONT, ScriptTextBox.GRAY_HUE, true) { X = b.X + b.Width + (_buttonWidth >> 3), Y = b.Y + 3 }, page);
            x += _macroPicTiled.Width + (_buttonHeight >> 3);
            Add(b = _playMacro = new AssistNiceButton(x, y, ((WIDTH - x) >> 1) - (_buttonWidth >> 3), _buttonHeight, ButtonAction.Activate, "Play") { IsSelectable = false, ButtonParameter = (int)ButtonType.PlayMacro, IsEnabled = false }, page);
            Add(b = _recordMacro = new AssistNiceButton(x + b.Width + (_buttonWidth >> 3), y, b.Width, _buttonHeight, ButtonAction.Activate, "Record") { IsSelectable = false, ButtonParameter = (int)ButtonType.RecordMacro, IsEnabled = false }, page);
            Add(b = _recordAsType = new AssistCheckbox(0x00D2, 0x00D3, "Rec Type Use", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = b.Y + b.Height + (_buttonHeight >> 3), IsEnabled = false, IsVisible = false}, page);
            Add(b = _loopMacro = new AssistCheckbox(0x00D2, 0x00D3, "Loop", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = b.Y + b.Height + (_buttonHeight >> 3), IsEnabled = false }, page);
            Add(b = _noautoInterrupt = new AssistCheckbox(0x00D2, 0x00D3, "No Interrupt", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = b.Y + b.Height + (_buttonHeight >> 3), IsEnabled = false }, page);
            Add(_macrokeyName = new AssistHotkeyBox(x, b.Y + b.Height + (_buttonHeight >> 2), (WIDTH - x) - (_buttonWidth >> 3), HEIGHT - (y + _buttonHeight * 8), FONT, ScriptTextBox.GRAY_HUE) { IsEnabled = false }, page);
            Add(_StartStopMacroMessages = new AssistCheckbox(0x00D2, 0x00D3, "Init Messages", FONT, ScriptTextBox.YELLOW_HUE, true) { X = x, Y = _macrokeyName.Y + _macrokeyName.Height }, page);
            AssistNiceButton anb;
            Add(anb = new AssistNiceButton(x, _StartStopMacroMessages.Y + _StartStopMacroMessages.Height + 1, _macrokeyName.Width, _StartStopMacroMessages.Height, ButtonAction.Activate, "Display Timers") { IsSelectable = false }, page);
            anb.LeftClicked += TimersButtonClicked;
            _macrokeyName.AddButton += _macrokeyName_AddButton;
            _macrokeyName.HotkeyChanged += _macrokeyName_HotkeyChanged;
            _macrokeyName.HotkeyCleared += _macrokeyName_HotkeyCleared;
            _loopMacro.ValueChanged += _loopMacro_ValueChanged;
            _noautoInterrupt.ValueChanged += _noautoInterrupt_ValueChanged;
            UpdateMacroListGump();
        }

        private void TimersButtonClicked(object sender, AssistNiceButton e)
        {
            var g = UIManager.GetGump<TimersGump>();
            if (g == null || g.IsDisposed)
            {
                UIManager.Add(new TimersGump());
            }
        }

        internal void DisableLoop()
        {
            _loopMacro.Text = "DISABLED";
            _returnToParent.Text = "Macro Loop DISABLED";
            _loopMacro.Hue = _returnToParent.Hue = ScriptTextBox.RED_HUE;
            _loopMacro.IsEnabled = _returnToParent.IsEnabled = false;
        }

        private void _macrokeyName_AddButton(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_macroSelected))
            {
                _macroRoutine(_macroSelected);
            }
        }

        private void _macroRoutine(string selected)
        {
            UIManager.Gumps.OfType<AssistMacroButtonGump>().FirstOrDefault(s => s._macroName == selected)?.Dispose();
            var macroButtonGump = new AssistMacroButtonGump(selected, Mouse.Position.X, Mouse.Position.Y);
            UIManager.Add(macroButtonGump);
        }

        private void _noautoInterrupt_ValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_macroSelected) && ScriptManager.MacroDictionary.TryGetValue(_macroSelected, out HotKeyOpts opts) && opts != null)
            {
                opts.NoAutoInterrupt = _noautoInterrupt.IsChecked;
            }
        }

        private void _loopMacro_ValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_macroSelected) && ScriptManager.MacroDictionary.TryGetValue(_macroSelected, out HotKeyOpts opts) && opts != null)
            {
                opts.Loop = _loopMacro.IsChecked;
            }
        }

        private void _macrokeyName_HotkeyCleared(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_macroSelected))
            {
                HotKeys.RemoveHotKey(_macroSelected);
            }
        }

        private void _macrokeyName_HotkeyChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_macroSelected) && sender is AssistHotkeyBox box && HotKeys.GetVKfromSDL((int)box.Key, (int)box.Mod, out uint vkey))
            {
                OnHotKeyChanged(ref _macroSelected, box, vkey, true);
            }
        }

        private void OnHotKeyChanged(ref string macroselected, AssistHotkeyBox box, uint vkey, bool ismacro)
        {
            if (ismacro)
            {
                if (!ScriptManager.MacroDictionary.TryGetValue(macroselected, out HotKeyOpts opts))
                    ScriptManager.MacroDictionary[macroselected] = new HotKeyOpts(box.PassToCUO, "macro.play", macroselected);
                if (_macroSelected == macroselected && opts.Macro != _macroArea._textBox.Text)
                    opts.Macro = _macroArea._textBox.Text;
                if(vkey > 0)
                    HotKeys.AddHotkey(vkey, new HotKeyOpts(box.PassToCUO, "macro.play", macroselected), box, ref macroselected, this);
            }
            else if (vkey > 0)
                HotKeys.AddHotkey(vkey, new HotKeyOpts(box.PassToCUO, macroselected), box, ref macroselected, this);
        }

        private class AreaContainer : AssistScrollArea
        {
            internal AssistStbTextBox _textBox;
            internal AreaContainer(int x, int y, int w, int h, AssistStbTextBox box) : base(x, y, w, h)
            {
                Add(_textBox = box);
                _textBox.IsEditable = false;
                _textBox.TextChanged += _textBox_TextChanged;
            }

            private void _textBox_TextChanged(object sender, EventArgs e)
            {
                _textBox.Height = _textBox.TotalHeight;

                base.OnPageChanged();
                /*foreach (Control c in Children)
                {
                    if (c is AssistChildType)
                        c.OnPageChanged();
                }*/
            }

            protected override void OnMouseUp(int x, int y, Input.MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);
                _textBox.SetKeyboardFocus();
            }
        }

        internal void UpdateMacroListGump(string macroname = null)
        {
            _macroListArea.Clear();
            List<string> macros = new List<string>(ScriptManager.MacroDictionary.Count + 1) {"Stop Current Macro"};
            foreach (KeyValuePair<string, HotKeyOpts> kvp in ScriptManager.MacroDictionary)
            {
                macros.Add(kvp.Key);
                var b = CreateSelection(_macroListArea, $"{kvp.Key}", 2, (int)ButtonType.MacroList, (int)ButtonType.MacroList, kvp.Key);
                if (macroname != null && kvp.Key == macroname)
                {
                    b.IsSelected = true;
                    SetMacroText(kvp.Value);
                }
                _macroSelected = macroname;
            }
            
            _macrosHK.SetItemsValue(macros.ToArray());
        }

        private void SetMacroText()
        {
            if (!string.IsNullOrEmpty(_macroSelected))
            {
                ScriptManager.MacroDictionary.TryGetValue(_macroSelected, out HotKeyOpts opts);
                SetMacroText(opts);
            }
            else
            {
                SetMacroText(null);
            }
        }

        private void SetMacroText(HotKeyOpts opts)
        {
            if (opts == null || opts.Macro == null)
            {
                _macroArea._textBox.Text = string.Empty;
                _macroArea._textBox.IsEditable = false;
                _noautoInterrupt.IsChecked = false;
                _noautoInterrupt.IsEnabled = false;
                _loopMacro.IsChecked = false;
                _loopMacro.IsEnabled = false;
                _macrokeyName.SetKey(SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Keymod.KMOD_NONE);
                _macrokeyName.IsEnabled = false;
                
                _playMacro.IsEnabled = false;
                _recordMacro.IsEnabled = false;
            }
            else
            {
                _macroArea._textBox.IsEditable = true;
                _macroArea._textBox.Text = opts.Macro;
                _macroArea._textBox.CaretIndex = 0;
                _noautoInterrupt.IsEnabled = true;
                _noautoInterrupt.IsChecked = opts.NoAutoInterrupt;
                _loopMacro.IsEnabled = Engine.Instance.AllowBit(FeatureBit.LoopingMacros);
                if (!_loopMacro.IsEnabled)
                    _loopMacro.Hue = ScriptTextBox.RED_HUE;
                else
                    _loopMacro.Hue = ScriptTextBox.GRAY_HUE;
                _loopMacro.IsChecked = opts.Loop && _loopMacro.IsEnabled;
                HotKeys.GetSDLKeyCodes(opts.Param, out int key, out int mod);
                _macrokeyName.SetKey((SDL.SDL_Keycode)key, (SDL.SDL_Keymod)mod);
                _macrokeyName.IsEnabled = true;

                _playMacro.IsEnabled = true;
                _recordMacro.IsEnabled = true;
            }
        }
#endregion

#region Skills
        /*
        private void BuildSkills(int page)
        {
            int x = _buttonWidth >> 3, y = _buttonHeight + (_buttonHeight >> 1);
            Line.CreateRectangleArea(this, x, y, (WIDTH >> 1) + (WIDTH >> 2), HEIGHT - (y + _buttonHeight * 2), page, Color.Gray.PackedValue, 1, "Skills", ScriptTextBox.GRAY_HUE, FONT);
            x += _buttonWidth >> 3;
            y += _buttonHeight >> 2;
            //y += _buttonHeight >> 2;
            Line.CreateRectangleArea(this, x, y, WIDTH >> 2)
        }
*/
#endregion

#region Agents
        private int _selectedSubAgentsPage = -1;
        private readonly AssistNiceButton[] _subAgentsPageNiceButtons = new AssistNiceButton[(int)PageType.LastAgents - (int)PageType.Autoloot];
        private AssistCheckbox _enableAutoLoot, _disableInGuardZone, //autoloot
            _moveConflictingItems,//dress
            _organizerComplete, _organizerLoop, _organizerStack,//organizer
            _enableScavenger, _stackAtFeet,//scavenger
            _enableBuySell, _buyComplete;//vendors
        internal AssistCheckbox EnabledScavenger => _enableScavenger;
        internal AssistCheckbox StackOresAtFeet => _stackAtFeet;
        internal AssistCheckbox EnableBuySell => _enableBuySell;
        internal int SelectedBuyOrSell => _BuySellCombo.SelectedIndex;
        private AssistNiceButton _autolootContainer, _insertautolootItem, _removeautolootItem, //autoloot
            _removeDressList, _dressButton, _undressButton, _importDress, _newItemDress, _removeItemDress, _clearAllDress, _typeOrSerialDress, _setUndressCont,//dress
            _playOrganizer, _removeOrganizer, _organizerSetCont, _removeOrganizerItem, _insertOrganizerItem, _newOrganizer,//organizer
            _insertScavenged, _removeScavenged, _clearScavenged, _setScavengedContainer,//scavenger
            _newBuySellList, _removeBuySellList, _removeBuySellItem, _insertBuySellItem;
        private AssistCombobox _BuySellCombo;
        internal AssistNiceButton PlayOrganizer { get { return _playOrganizer; } }
        private AssistScrollArea _autolootArea, //autoloot
            _dressItemsArea, _dressListsArea,//dress
            _organizerListArea,//organizer
            _vendorsListArea;//vendors
        private AssistScrollArea _organizerItems,//organizer
            _scavengerItems,//scavenger
            _vendorsItemsArea;//vendors
        private int[] _organizerItemsWidth, _scavengerItemsWidth, _BuySellItemsWidth;
        private AssistArrowNumbersTextBox _autolootAmount, _sellMaxAmount;
        private ushort _dressListSelected,//dress
            _organizerListSelected, _organizerItemSelected,//organizer
            _vendorsItemSelected;//vendors
        private ItemDisplay _scavengerItemSelected;//scavenger
        private Layer _dressItemSelected;
        private Label _sellLimitLabel, _disableBuySellLabel;

        internal Dictionary<ushort, (ushort, string)> ItemsToLoot { get; } = new Dictionary<ushort, (ushort, string)>();
        private ushort _lootSelected;

        internal void DisableAutoLoot()
        {
            _enableAutoLoot.IsEnabled = _autolootContainer.IsEnabled = _disableInGuardZone.IsEnabled = _autolootArea.IsEnabled = _insertautolootItem.IsEnabled = _removeautolootItem.IsEnabled = _autolootAmount.IsEnabled = false;
            _enableAutoLoot.Hue = _autolootContainer.TextLabel.Hue = _disableInGuardZone.Hue = _insertautolootItem.TextLabel.Hue = _removeautolootItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
            _autolootContainer.TextLabel.Text = "Autoloot is Disabled";
            _agentsHK.SetItemsValue(new string[] { "Toggle Scavenger" });
        }

        private void BuildAgents()
        {
            //PAGE 5
            int diffx = (_buttonWidth - (_buttonWidth >> 3)) >> 2;
            int width = diffx * 14, startx = diffx * 4;
            int starty = _buttonHeight + (_buttonHeight >> 2);
            for (int pagex = startx, page = (int)PageType.Autoloot; page < (int)PageType.LastAgents; pagex += width, page++)
            {
                string name = StringHelper.AddSpaceBeforeCapital(((PageType)page).ToString());
                for (int subpage = (int)PageType.Autoloot; subpage < (int)PageType.LastAgents; subpage++)
                {
                    AssistNiceButton but = new AssistNiceButton(pagex, starty, width, _buttonHeight, ButtonAction.SwitchPage, name, (int)PageType.LastAgents) { ButtonParameter = page };
                    if (page == subpage)
                    {
                        _subAgentsPageNiceButtons[subpage - (int)PageType.Autoloot] = but;
                    }

                    Add(but, subpage);
                }
                switch ((PageType)page)
                {
#region AUTOLOOT_PAGE
                    case PageType.Autoloot:
                    {
                        int buttondiffx = _buttonWidth - (_buttonWidth >> 3), buttondiffy = _buttonHeight - (_buttonHeight >> 3);
                        int x = startx - ((buttondiffx >> 2) * 2), y = starty + (_buttonHeight >> 1) + _buttonHeight, devx = buttondiffx >> 2;
                        
                        _enableAutoLoot = new AssistCheckbox(0x00D2, 0x00D3, "Enabled", FONT, ScriptTextBox.GRAY_HUE, true)
                        {
                            X = x,
                            Y = y
                        };
                        Add(_enableAutoLoot, page);
                        Add(_autolootContainer = new AssistNiceButton(_enableAutoLoot.X + _enableAutoLoot.Width + _buttonWidth, y, devx * 30, _enableAutoLoot.Height, ButtonAction.Activate, "Set Container", (int)ButtonType.SetAutolootContainer, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.SetAutolootContainer }, page);
                        y += _buttonHeight + (_buttonHeight >> 3);
                        _disableInGuardZone = new AssistCheckbox(0x00D2, 0x00D3, "Disable inside guards zone", FONT, ScriptTextBox.GRAY_HUE, true)
                        {
                            X = x,
                            Y = y
                        };
                        Add(_disableInGuardZone, page);
                        y += _buttonHeight + (_buttonHeight >> 2);
                        x -= (buttondiffx >> 2);
                        Line[] l = CreateRectangleArea(this, x, y, _autolootContainer.Width + _autolootContainer.X + (buttondiffx >> 2), HEIGHT - (_disableInGuardZone.Y + (_buttonHeight * 3)) , page, Color.Gray.PackedValue, 1, "Loot Items", ScriptTextBox.GRAY_HUE, FONT);
                        y += (buttondiffy >> 1);
                        x += 2;
                        _autolootArea = new AssistScrollArea(x, y, l[2].Width - 6, l[0].Height - ((buttondiffy >> 2) * 3));
                        Add(_autolootArea, page);
                        y = l[2].Y + (_buttonHeight >> 2);
                        x -= 2;
                        Add(_removeautolootItem = new AssistNiceButton(x, y, (_autolootArea.Width >> 1) - (_buttonWidth >> 2), _buttonHeight - (_buttonHeight >> 2), ButtonAction.Activate, "Remove Item") { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveAutolootItem }, page);
                        Add(_insertautolootItem = new AssistNiceButton(x + (_autolootArea.Width >> 1) + (buttondiffx >> 1), y, (_autolootArea.Width >> 1) - (_buttonWidth >> 2), (_buttonHeight - (_buttonHeight >> 2)), ButtonAction.Activate, "Insert Item") { IsSelectable = false, ButtonParameter = (int)ButtonType.InsertAutolootItem }, page);
                        x = l[2].X + l[2].Width + 4;
                        Label lb;
                        Add(lb = new Label("Limit", true, ScriptTextBox.GRAY_HUE, font: FONT) { X = x, Y = y }, page);
                        x += lb.Width + 2;
                        Add(_autolootAmount = new AssistArrowNumbersTextBox(x, y, _buttonWidth * 3, 100, 0, 60000, FONT, 6, true, FontStyle.None) { Text = "0", IsEnabled = false }, page);
                        _autolootAmount.ValueChanged += AutolootAmount_ValueChanged;
                        if (!Engine.Instance.AllowBit(FeatureBit.AutolootAgent))
                        {
                            DisableAutoLoot();
                        }
                        break;
                    }
#endregion

#region DRESS_PAGE
                    case PageType.Dress:
                    {
                        int buttondiffx = _buttonWidth - (_buttonWidth >> 3), buttondiffy = _buttonHeight - (_buttonHeight >> 3);
                        int x = startx - ((buttondiffx >> 2) * 3), y = starty + _buttonHeight;

                        _moveConflictingItems = new AssistCheckbox(0x00D2, 0x00D3, "Move conflicting items", FONT, ScriptTextBox.GRAY_HUE, true)
                        {
                            X = x,
                            Y = y
                        };
                        Add(_moveConflictingItems, page);
                        y += _buttonHeight + (_buttonHeight >> 3);
                        Line[] l = CreateRectangleArea(this, x, y, _buttonWidth * 7, HEIGHT - (_buttonHeight * 5), page, Color.Gray.PackedValue, 1, "Dress Lists", ScriptTextBox.GRAY_HUE, FONT);
                        y += (buttondiffy >> 1);
                        x += 2;
                        _dressListsArea = new AssistScrollArea(x, y, l[2].Width - 6, l[0].Height - ((buttondiffy >> 2) * 3));
                        Add(_dressListsArea, page);
                        x -= 2;
                        y += l[0].Height;
                        Add(_removeDressList = new AssistNiceButton(x, y, (l[2].Width >> 2) + (_buttonWidth >> 1), _buttonHeight, ButtonAction.Activate, "Remove", (int)ButtonType.RemoveDressList, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveDressList, IsEnabled = false }, page);
                        _removeDressList.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x = l[2].X + l[2].Width - ((l[2].Width >> 2) + (_buttonWidth >> 1));
                        Add(new AssistNiceButton(x, y, (l[2].Width >> 2) + (_buttonWidth >> 1), _buttonHeight, ButtonAction.Activate, "New", (int)ButtonType.CreateDressList, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.CreateDressList }, page);
                        x = l[2].X + l[2].Width + (_buttonWidth >> 1);
                        y = starty + _buttonHeight + (_buttonHeight >> 2);
                        l = CreateRectangleArea(this, x, y, WIDTH - (x + _buttonWidth * 4), HEIGHT - (_buttonHeight * 4), page, Color.Gray.PackedValue, 1, "Dress Items", ScriptTextBox.GRAY_HUE, FONT);
                        y += (buttondiffy >> 1);
                        x += 2;
                        _dressItemsArea = new AssistScrollArea(x, y, l[2].Width - 6, l[0].Height - ((buttondiffy >> 2) * 3));
                        Add(_dressItemsArea, page);
                        y = l[2].Y + (_buttonHeight >> 2);
                        x -= 2;
                        Add(_setUndressCont = new AssistNiceButton(x, y, _dressItemsArea.Width, _buttonHeight - (_buttonHeight >> 2), ButtonAction.Activate, "Set Undress Container") { IsSelectable = false, ButtonParameter = (int)ButtonType.SetUndressContainer, IsEnabled = false }, page);
                        _setUndressCont.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x = l[0].X + l[2].Width + (_buttonWidth >> 2);
                        y = l[0].Y;
                        Add(_dressButton = new AssistNiceButton(x, y, WIDTH - (x + (_buttonWidth >> 2)), _setUndressCont.Height + (_buttonHeight >> 1), ButtonAction.Activate, "Dress") { IsSelectable = false, ButtonParameter = (int)ButtonType.DressSelectedList, IsEnabled = false }, page);
                        _dressButton.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        y += _dressButton.Height + (_buttonHeight >> 2);
                        Add(_undressButton = new AssistNiceButton(x, y, _dressButton.Width, _dressButton.Height, ButtonAction.Activate, "Undress") { IsSelectable = false, ButtonParameter = (int)ButtonType.UndressSelectedList, IsEnabled = false }, page);
                        _undressButton.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        y += _dressButton.Height + (_buttonHeight >> 2);
                        Add(_importDress = new AssistNiceButton(x, y, _dressButton.Width, _dressButton.Height, ButtonAction.Activate, "Import") { IsSelectable = false, ButtonParameter = (int)ButtonType.ImportCurrentlyDressed, IsEnabled = false }, page);
                        _importDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        y += _dressButton.Height + (_buttonHeight >> 2);
                        Add(_newItemDress = new AssistNiceButton(x, y, _dressButton.Width, _dressButton.Height, ButtonAction.Activate, "New Item") { IsSelectable = false, ButtonParameter = (int)ButtonType.AddItemToDressList, IsEnabled = false }, page);
                        _newItemDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        y += _dressButton.Height + (_buttonHeight >> 2);
                        Add(_removeItemDress = new AssistNiceButton(x, y, _dressButton.Width, _dressButton.Height, ButtonAction.Activate, "Remove") { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveItemFromDressList, IsEnabled = false }, page);
                        _removeItemDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        y += _dressButton.Height + (_buttonHeight >> 1);
                        Add(_clearAllDress = new AssistNiceButton(x, y, _dressButton.Width, _dressButton.Height, ButtonAction.Activate, "Clear All") { IsSelectable = false, ButtonParameter = (int)ButtonType.ClearSelectedDressList, IsEnabled = false }, page);
                        _clearAllDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        y += _dressButton.Height + (_buttonHeight >> 1);
                        Add(_typeOrSerialDress = new AssistNiceButton(x, y, _dressButton.Width, _buttonHeight, ButtonAction.Activate, "Use Serial") { IsSelectable = false, ButtonParameter = (int)ButtonType.DressTypeOrSerial }, page);
                        _typeOrSerialDress.TextLabel.Hue = ScriptTextBox.BLUE_HUE;
                        break;
                    }
#endregion

#region ORGANIZER_PAGE
                    case PageType.Organizer:
                    {
                        int buttondiffx = _buttonWidth - (_buttonWidth >> 3), buttondiffy = _buttonHeight - (_buttonHeight >> 3);
                        int x = startx - ((buttondiffx >> 2) * 3), y = starty + _buttonHeight;

                        Add(_playOrganizer = new AssistNiceButton(x, y, _buttonWidth + (_buttonWidth >> 2), _buttonHeight * 2, ButtonAction.Activate, "Play", (int)ButtonType.PlaySelectedOrganizer, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.PlaySelectedOrganizer, IsEnabled = false }, page);
                        _playOrganizer.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        Add(_organizerComplete = new AssistCheckbox(0x00D2, 0x00D3, "Complete", FONT, ScriptTextBox.GRAY_HUE, true) { X = x + _playOrganizer.Width + (_buttonWidth >> 3), Y = y + 2, IsEnabled = false }, page);
                        _organizerComplete.Hue = ScriptTextBox.RED_HUE;
                        _organizerComplete.ValueChanged += OrganizerComplete_ValueChanged;
                        y = _organizerComplete.Y + _organizerComplete.Height + (_buttonHeight >> 2);
                        Add(_organizerLoop = new AssistCheckbox(0x00D2, 0x00D3, "Loop", FONT, ScriptTextBox.GRAY_HUE, true) { X = _organizerComplete.X, Y = y, IsEnabled = false }, page);
                        _organizerLoop.Hue = ScriptTextBox.RED_HUE;
                        _organizerLoop.ValueChanged += OrganizerLoop_ValueChanged;
                        y = _organizerLoop.Y;
                        Add(_organizerStack = new AssistCheckbox(0x00D2, 0x00D3, "Stack", FONT, ScriptTextBox.GRAY_HUE, true) { X = _organizerLoop.X + _organizerLoop.Width + (_buttonWidth >> 3), Y = y, IsEnabled = false }, page);
                        _organizerStack.Hue = ScriptTextBox.RED_HUE;
                        _organizerStack.ValueChanged += OrganizerStack_ValueChanged;
                        y += _buttonHeight + (_buttonHeight >> 3);
                        Line[] l = CreateRectangleArea(this, x, y, _buttonWidth * 5 + (_buttonWidth >> 1), HEIGHT - (_buttonHeight * 6), page, Color.Gray.PackedValue, 1, "Organizer Lists", ScriptTextBox.GRAY_HUE, FONT);
                        y += (buttondiffy >> 1);
                        x += 2;
                        _organizerListArea = new AssistScrollArea(x, y, l[2].Width - 6, l[0].Height - ((buttondiffy >> 2) * 3));
                        Add(_organizerListArea, page);
                        x -= 2;
                        y += l[0].Height;
                        Add(_removeOrganizer = new AssistNiceButton(x + 4, y - 4, (l[2].Width >> 2) + (_buttonWidth >> 1), _buttonHeight, ButtonAction.Activate, "Remove", (int)ButtonType.RemoveOrganizerList, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveOrganizerList, IsEnabled = false }, page);
                        _removeOrganizer.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x = l[2].X + l[2].Width - ((l[2].Width >> 2) + (_buttonWidth >> 1));
                        Add(_newOrganizer = new AssistNiceButton(x - 4, y - 4, (l[2].Width >> 2) + (_buttonWidth >> 1), _buttonHeight, ButtonAction.Activate, "New", (int)ButtonType.CreateOrganizerList, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.CreateOrganizerList }, page);
                        x = l[2].X + l[2].Width + (_buttonWidth >> 2);
                        y = starty + _buttonHeight + (_buttonHeight >> 2);
                        l = CreateRectangleArea(this, x, y, WIDTH - (x + (_buttonWidth * 8)), HEIGHT - (_buttonHeight * 4), page, Color.Gray.PackedValue, 1, "Item", ScriptTextBox.GRAY_HUE, FONT);
                        _organizerItemsWidth = new int[4];
                        _organizerItemsWidth[0] = l[2].Width - 4;
                        int tmpx = x;
                        Add(_organizerSetCont = new AssistNiceButton(x + 4, y + l[0].Height + ((buttondiffy >> 1) - 4), l[2].Width - 8, _buttonHeight, ButtonAction.Activate, "Set Containers", (int)ButtonType.SetOrganizerContainers, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.SetOrganizerContainers, IsEnabled = false }, page);
                        _organizerSetCont.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, _buttonWidth * 2 + (buttondiffx >> 1), l[0].Height, page, Color.Gray.PackedValue, 1, "Graphic", ScriptTextBox.GRAY_HUE, FONT);
                        _organizerItemsWidth[1] = l[2].Width - 2;
                        Add(_removeOrganizerItem = new AssistNiceButton(x + _buttonWidth, _organizerSetCont.Y, (l[2].Width - 8) + _buttonWidth, _buttonHeight, ButtonAction.Activate, "Remove", (int)ButtonType.RemoveItemFromOrganizer, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveItemFromOrganizer, IsEnabled = false }, page);
                        _removeOrganizerItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, _buttonWidth * 2 + (buttondiffx >> 1), l[0].Height, page, Color.Gray.PackedValue, 1, "Hue", ScriptTextBox.GRAY_HUE, FONT);
                        _organizerItemsWidth[2] = l[2].Width - 2;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, _buttonWidth * 2 + (buttondiffx >> 1), l[0].Height, page, Color.Gray.PackedValue, 1, "Amount", ScriptTextBox.GRAY_HUE, FONT);
                        _organizerItemsWidth[3] = l[2].Width - 2;
                        Add(_insertOrganizerItem = new AssistNiceButton((x + 4) - _buttonWidth, _organizerSetCont.Y, (l[2].Width - 8) + _buttonWidth, _buttonHeight, ButtonAction.Activate, "Insert", (int)ButtonType.InsertItemIntoOrganizer, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.InsertItemIntoOrganizer, IsEnabled = false }, page);
                        _insertOrganizerItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, 16, l[0].Height, page, Color.Gray.PackedValue, 1, "", ScriptTextBox.GRAY_HUE, FONT);
                        Add(_organizerItems = new AssistScrollArea(tmpx, y + (buttondiffy >> 1), (_organizerItemsWidth.Sum() + (2 * _organizerItemsWidth.Length) + 12), l[0].Height - ((buttondiffy >> 2) * 3)), page);
                        break;
                    }
#endregion

#region SCAVENGER_PAGE
                    case PageType.Scavenger:
                    {
                        int buttondiffx = _buttonWidth - (_buttonWidth >> 3), buttondiffy = _buttonHeight - (_buttonHeight >> 3);
                        int x = startx - ((buttondiffx >> 2) * 3) + 2, y = starty + _buttonHeight + 6;
                        int tmpy = y;
                        _enableScavenger = new AssistCheckbox(0x00D2, 0x00D3, "Enable", FONT, ScriptTextBox.GRAY_HUE, true) { X = x, Y = tmpy };
                        _enableScavenger.ValueChanged += EnableScavenger_ValueChanged;
                        Add(_enableScavenger, page);
                        int temp = _buttonHeight + (_buttonHeight >> 1);
                        tmpy += temp;
                        Add(_insertScavenged = new AssistNiceButton(x, tmpy, _buttonWidth * 4, _buttonHeight, ButtonAction.Activate, "Insert", (int)ButtonType.InsertScavengeItem, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.InsertScavengeItem }, page);
                        tmpy += temp;
                        Add(_removeScavenged = new AssistNiceButton(x, tmpy, _buttonWidth * 4, _buttonHeight, ButtonAction.Activate, "Remove", (int)ButtonType.RemoveScavengeItem, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveScavengeItem }, page);
                        tmpy += temp;
                        Add(_clearScavenged = new AssistNiceButton(x, tmpy, _buttonWidth * 4, _buttonHeight, ButtonAction.Activate, "Clear All", (int)ButtonType.ClearScavengeItems, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.ClearScavengeItems }, page);
                        tmpy += temp;
                        Add(_setScavengedContainer = new AssistNiceButton(x, tmpy, _buttonWidth * 4, _buttonHeight, ButtonAction.Activate, "Set Container", (int)ButtonType.ScavengeDestinationCont, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.ScavengeDestinationCont }, page);
                        x += _setScavengedContainer.Width + 4;
                        temp = x;
                        Line[] l = CreateRectangleArea(this, x, y, WIDTH - (x + (_buttonWidth * 7)), HEIGHT - (_buttonHeight * 4), page, Color.Gray.PackedValue, 1, "Item", ScriptTextBox.GRAY_HUE, FONT);
                        x += l[2].Width - 1;
                        _scavengerItemsWidth = new int[3];
                        _scavengerItemsWidth[0] = l[2].Width - 4;
                        l = CreateRectangleArea(this, x, y, _buttonWidth * 3, l[0].Height, page, Color.Gray.PackedValue, 1, "Graphic", ScriptTextBox.GRAY_HUE, FONT);
                        x += l[2].Width - 1;
                        _scavengerItemsWidth[1] = l[2].Width - 2;
                        l = CreateRectangleArea(this, x, y, _buttonWidth * 3, l[0].Height, page, Color.Gray.PackedValue, 1, "Hue", ScriptTextBox.GRAY_HUE, FONT);
                        _scavengerItemsWidth[2] = l[2].Width - 2;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, y, 16, l[0].Height, page, Color.Gray.PackedValue, 1, "", ScriptTextBox.GRAY_HUE, FONT);
                        Add(_scavengerItems = new AssistScrollArea(temp, y + (buttondiffy >> 1), (_scavengerItemsWidth.Sum() + (2 * _scavengerItemsWidth.Length) + 12), l[0].Height - ((buttondiffy >> 2) * 3)), page);

                        _stackAtFeet = new AssistCheckbox(0x00D2, 0x00D3, "Stack resources at your feet (when not scavenged)", FONT, ScriptTextBox.GRAY_HUE, true) { X = temp - _setScavengedContainer.Width - 4, Y = l[0].Y + l[0].Height + (buttondiffy >> 1) };
                        _stackAtFeet.ValueChanged += StackScavenger_ValueChanged;
                        Add(_stackAtFeet, page);
                        break;
                    }
#endregion

#region VENDORS_PAGE
                    case PageType.Vendors:
                    {
                        int buttondiffx = _buttonWidth - (_buttonWidth >> 3), buttondiffy = _buttonHeight - (_buttonHeight >> 3);
                        int x = startx - ((buttondiffx >> 2) * 3), y = starty + _buttonHeight;
                        Add(_BuySellCombo = new AssistCombobox(x, y, _buttonWidth * 2 + (_buttonWidth >> 1), new[] { "Buy", "Sell" }, 0, HEIGHT >> 2), page);
                        _BuySellCombo.OnOptionSelected += BuySellCombo_OnOptionSelected;
                        Add(_enableBuySell = new AssistCheckbox(0x00D2, 0x00D3, "Enabled", FONT, ScriptTextBox.GRAY_HUE, true) { X = _BuySellCombo.X + _BuySellCombo.Width + (_buttonWidth >> 3), Y = y + 2 }, page);
                        _enableBuySell.ValueChanged += EnableBuySell_ValueChanged;
                        Add(_disableBuySellLabel = new Label("DISABLED!", true, ScriptTextBox.RED_HUE) { X = _enableBuySell.X, Y = _enableBuySell.Y, IsVisible = false }, page);
                        y += _BuySellCombo.Height + (_buttonHeight >> 2);
                        Line[] l = CreateRectangleArea(this, x, y, _buttonWidth * 5 + (_buttonWidth >> 1), HEIGHT - (_buttonHeight * 5), page, Color.Gray.PackedValue, 1, "Lists", ScriptTextBox.GRAY_HUE, FONT);
                        y += (buttondiffy >> 1);
                        x += 2;
                        Add(_vendorsListArea = new AssistScrollArea(x, y, l[2].Width - 6, l[0].Height - ((buttondiffy >> 2) * 3)), page);
                        x -= 2;
                        y += l[0].Height;
                        Add(_removeBuySellList = new AssistNiceButton(x + 4, y - 4, (l[2].Width >> 2) + (_buttonWidth >> 1), _buttonHeight, ButtonAction.Activate, "Remove", (int)ButtonType.RemoveBuySellList, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveBuySellList }, page);
                        _removeBuySellList.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x = l[2].X + l[2].Width - ((l[2].Width >> 2) + (_buttonWidth >> 1));
                        Add(_newBuySellList = new AssistNiceButton(x - 4, y - 4, (l[2].Width >> 2) + (_buttonWidth >> 1), _buttonHeight, ButtonAction.Activate, "New", (int)ButtonType.NewBuySellList, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.NewBuySellList }, page);
                        x = l[2].X + l[2].Width + (_buttonWidth >> 2);
                        y = starty + _buttonHeight + (_buttonHeight >> 2);
                        l = CreateRectangleArea(this, x, y, WIDTH - (x + (_buttonWidth * 10) + (_buttonWidth >> 1)), HEIGHT - (_buttonHeight * 4), page, Color.Gray.PackedValue, 1, "Graphic", ScriptTextBox.GRAY_HUE, FONT);
                        _BuySellItemsWidth = new int[3];
                        _BuySellItemsWidth[0] = l[2].Width - 4;
                        int tmpx = x;
                        Add(_buyComplete = new AssistCheckbox(0x00D2, 0x00D3, "Complete", FONT, ScriptTextBox.GRAY_HUE, true) { X = x + 4, Y = y + l[0].Height + ((buttondiffy >> 1) - 4), IsEnabled = false }, page);
                        _buyComplete.ValueChanged += BuyComplete_ValueChanged;
                        Add(_sellLimitLabel = new Label("Limit per sell:", true, ScriptTextBox.GRAY_HUE) { X = _buyComplete.X, Y = _buyComplete.Y + 2, IsVisible = false }, page);
                        Add(_sellMaxAmount = new AssistArrowNumbersTextBox(_buyComplete.X + _sellLimitLabel.Width, _buyComplete.Y, _buyComplete.Width, 1, 1, 999, FONT, 4, true, FontStyle.None, 0) { IsEnabled = false, IsVisible = false }, page);
                        _sellMaxAmount.ValueChanged += SellMaxAmount_ValueChanged;
                        _buyComplete.Hue = ScriptTextBox.RED_HUE;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, _buttonWidth * 3 + (_buttonWidth >> 1), l[0].Height, page, Color.Gray.PackedValue, 1, "Amount", ScriptTextBox.GRAY_HUE, FONT);
                        _BuySellItemsWidth[1] = l[2].Width - 2;
                        Add(_removeBuySellItem = new AssistNiceButton(x + _buttonWidth * 3 + (_buttonWidth >> 2), _buyComplete.Y, _buttonWidth * 3, _buttonHeight, ButtonAction.Activate, "Remove", (int)ButtonType.RemoveBuySellItem, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.RemoveBuySellItem, IsEnabled = false }, page);
                        _removeBuySellItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, _buttonWidth * 6 + (_buttonWidth >> 2), l[0].Height, page, Color.Gray.PackedValue, 1, "Item", ScriptTextBox.GRAY_HUE, FONT);
                        _BuySellItemsWidth[2] = l[2].Width - 2;
                        Add(_insertBuySellItem = new AssistNiceButton(x + _buttonWidth * 3 + (_buttonWidth >> 1), _buyComplete.Y, _buttonWidth * 3, _buttonHeight, ButtonAction.Activate, "Insert", (int)ButtonType.InsertBuySellItem, TEXT_ALIGN_TYPE.TS_CENTER) { IsSelectable = false, ButtonParameter = (int)ButtonType.InsertBuySellItem, IsEnabled = false }, page);
                        _insertBuySellItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        x += l[2].Width - 1;
                        l = CreateRectangleArea(this, x, l[0].Y, 16, l[0].Height, page, Color.Gray.PackedValue, 1, "", ScriptTextBox.GRAY_HUE, FONT);
                        Add(_vendorsItemsArea = new AssistScrollArea(tmpx, y + (buttondiffy >> 1), (_BuySellItemsWidth.Sum() + (2 * _BuySellItemsWidth.Length) + 12), l[0].Height - ((buttondiffy >> 2) * 3)), page);
                        break;
                    }
#endregion
                }
            }
        }

        private void SellMaxAmount_ValueChanged(object sender, int e)
        {
            if(_sellMaxAmount.Tag is Vendors.IBuySell ibs)
            {
                ibs.MaxAmount = (ushort)Math.Max(Math.Min(e, 999), 0);
            }
        }

        private void BuyComplete_ValueChanged(object sender, EventArgs e)
        {
            if(_buyComplete.Tag is Vendors.IBuySell ibs)
            {
                ibs.Complete = _buyComplete.IsChecked;
            }
        }

        private void EnableBuySell_ValueChanged(object sender, EventArgs e)
        {
            if(_BuySellCombo.SelectedIndex == 1)
            {
                Vendors.Sell.SellEnabled = _enableBuySell.IsChecked;
            }
            else
            {
                Vendors.Buy.BuyEnabled = _enableBuySell.IsChecked;
            }
        }

        internal void UpdateVendorsListGump(Vendors.IBuySell selected = null, bool noclear = false)
        {
            if(!noclear)
                _vendorsListArea.Clear();
            bool found = false;
            Utility.Collections.OrderedDictionary<Vendors.IBuySell, List<BuySellEntry>> buysell = null;
            if(_BuySellCombo.SelectedIndex == 1)
            {
                if (!DisableBuySell(Engine.Instance.AllowBit(FeatureBit.SellAgent)))
                {
                    buysell = Vendors.Sell.SellList;
                    _enableBuySell.IsChecked = Vendors.Sell.SellEnabled;
                    if (selected == null)
                        selected = Vendors.Sell.SellSelected;
                    else
                        Vendors.Sell.SellSelected = null;
                    _buyComplete.Tag = null;
                    _sellMaxAmount.Tag = selected;
                }
                else
                    selected = null;
            }
            else
            {
                if (!DisableBuySell(Engine.Instance.AllowBit(FeatureBit.BuyAgent)))
                {
                    buysell = Vendors.Buy.BuyList;
                    _enableBuySell.IsChecked = Vendors.Buy.BuyEnabled;
                    if (selected == null)
                        selected = Vendors.Buy.BuySelected;
                    else
                        Vendors.Buy.BuySelected = null;
                    _sellMaxAmount.Tag = null;
                    _buyComplete.Tag = selected;
                }
                else
                    selected = null;
            }
            if (buysell != null)
            {
                if (!noclear)
                {
                    foreach (var key in buysell.Keys)
                    {
                        var b = CreateSingleTextSelection(_vendorsListArea, 2, (int)ButtonType.BuySellList, (int)ButtonType.BuySellList, key, TEXT_ALIGN_TYPE.TS_CENTER, _vendorsListArea.Width, key.Name, ScriptTextBox.GRAY_HUE, 30);
                        b.TextBoxes[0].Tag = key;
                        b.TextBoxes[0].TextChanged += SABSODVTextBox_TextChanged;
                        b.TextBoxes[0].FocusLost += BuySellList_FocusLost; ;
                        if (selected != null && key == selected)
                        {
                            b.IsSelected = found = true;
                            key.Selected = key;
                            b.TextBoxes[0].Priority = ClickPriority.High;
                        }
                    }
                }
                else
                {
                    List<NiceButtonStbText> list = new List<NiceButtonStbText>(_vendorsListArea.FindControls<NiceButtonStbText>());//.SelectMany(s => s.Children.OfType<NiceButtonStbText>()));
                    foreach (NiceButtonStbText b in list)
                    {
                        if (b.TextBoxes[0].Tag is Vendors.IBuySell key)
                        {
                            if (selected != null && key == selected)
                            {
                                b.IsSelected = found = true;
                                key.Selected = key;
                                b.TextBoxes[0].Priority = ClickPriority.High;
                            }
                            else
                            {
                                b.IsSelected = false;
                            }
                        }
                    }
                }
            }
            UpdateVendorsItemsGump(selected);
            if (!found)
            {
                _sellMaxAmount.Text = "";
                _enableBuySell.IsEnabled = _enableBuySell.IsChecked = _buyComplete.IsChecked = _sellMaxAmount.IsEnabled = _buyComplete.IsEnabled = _removeBuySellList.IsEnabled = false;
                _sellLimitLabel.Hue = _buyComplete.Hue = _removeBuySellList.TextLabel.Hue = ScriptTextBox.RED_HUE;
            }
            else
            {
                _sellMaxAmount.Text = selected.MaxAmount.ToString();
                _buyComplete.IsChecked = selected.Complete;
                _sellMaxAmount.IsEnabled = _sellMaxAmount.IsVisible;
                _buyComplete.IsEnabled = _buyComplete.IsVisible;
                _enableBuySell.IsEnabled = _removeBuySellList.IsEnabled = true;
                _sellLimitLabel.Hue = _buyComplete.Hue = _removeBuySellList.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
            }
        }

        private void BuySellList_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is Vendors.IBuySell ibs)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text))
                {
                    _changedPlaceHolder.Text = ibs.Name;
                }
                else
                {
                    string oldname = ibs.Name;
                    ibs.Name = _changedPlaceHolder.Text;
                    //_changedPlaceHolder.Text = ibs.Name;
                    AddVendorsListToHotkeys();
                    OnAgentNameChanged(Vendors.Prepend, oldname, ibs.Name);
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void DressList_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is DressList dl)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text))
                {
                    _changedPlaceHolder.Text = dl.Name;
                }
                else
                {
                    string oldname = dl.Name;
                    dl.Name = _changedPlaceHolder.Text;
                    //_changedPlaceHolder.Text = dl.Name;
                    AddDressListToHotkeys();
                    OnAgentNameChanged(DressList.Prepend, oldname, dl.Name);
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void OrganizerList_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is Organizer or)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text))
                {
                    _changedPlaceHolder.Text = or.Name;
                }
                else
                {
                    string oldname = or.Name.ToLower(XmlFileParser.Culture);
                    or.Name = _changedPlaceHolder.Text;
                    //_changedPlaceHolder.Text = or.Name;
                    AddOrganizerListToHotkeys();
                    OnAgentNameChanged(Organizer.Prepend, oldname, or.Name.ToLower(XmlFileParser.Culture));
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void OnAgentNameChanged(string[] prepend, string oldname, string newname)
        {
            foreach (string s in prepend)
            {
                (uint vk, HotKeyOpts opts, bool key) vo = HotKeys.RemoveHotKey($"{s}{oldname}", true);
                if (vo.vk > 0 && vo.opts != null)
                {
                    HotKeys.AddHotkey(vo.vk, new HotKeyOpts(vo.opts.Options, $"{s}{newname}", vo.opts.Param), null, ref newname, this, true);
                    if (vo.key)
                        UOSObjects.Gump.SelectedHK = $"{s}{newname}";
                }
            }
        }

        internal bool DisableBuySell(bool enabled)
        {
            if (!enabled)
            {
                _enableBuySell.IsVisible = false;
                _disableBuySellLabel.IsVisible = true;
            }
            else
            {
                _disableBuySellLabel.IsVisible = false;
                _enableBuySell.IsVisible = true;
            }
            return !enabled;
        }

        internal void UpdateVendorsItemsGump(Vendors.IBuySell listselected, ushort itemselected = ushort.MaxValue)
        {
            _vendorsItemsArea.Clear();
            bool hasselection = false;
            if (listselected != null)
            {
                _insertBuySellItem.IsEnabled = true;
                _insertBuySellItem.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                List<BuySellEntry> bselist = listselected.BuySellItems;
                if (bselist != null && bselist.Count > 0)
                {
                    for (ushort i = 0; i < bselist.Count; ++i)
                    {
                        var bsl = bselist[i];
                        var b = CreateTextSelection(_vendorsItemsArea, 0, (int)ButtonType.BuySellItemList, (int)ButtonType.BuySellItemList, i, TEXT_ALIGN_TYPE.TS_CENTER, _BuySellItemsWidth, 0, false, $"0x{bsl.ItemID:X4}", $"{bsl.Amount}", UOSObjects.GetDefaultItemName(bsl.ItemID));
                        b.TextBoxes[0].NumbersOnly = true;
                        b.TextBoxes[1].IsEditable = false;
                        b.TextBoxes[0].Tag = b.TextBoxes[1].Tag = bsl;
                        b.TextBoxes[0].TextChanged += SABSODVTextBox_TextChanged;
                        b.TextBoxes[0].FocusLost += VendorsItemAmount_FocusLost;
                        if (itemselected < ushort.MaxValue && i == itemselected)
                        {
                            b.IsSelected = hasselection = true;
                        }
                    }
                }
            }
            else
            {
                _insertBuySellItem.IsEnabled = false;
                _insertBuySellItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
            }
            if (!hasselection)
            {
                _removeBuySellItem.IsEnabled = false;
                _removeBuySellItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
            }
            else
            {
                _removeBuySellItem.IsEnabled = true;
                _removeBuySellItem.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
            }
        }

        private void VendorsItemAmount_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is BuySellEntry bse)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text) || !ushort.TryParse(_changedPlaceHolder.Text, out ushort amount) || amount == 0)
                {
                    _changedPlaceHolder.Text = "1";
                    bse.Amount = 1;
                }
                else
                {
                    _changedPlaceHolder.Text = (bse.Amount = Math.Min(amount, (ushort)999)).ToString();
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void BuySellCombo_OnOptionSelected(object sender, int e)
        {
            if(e == 1)
            {
                _buyComplete.IsVisible = _buyComplete.IsEnabled = false;
                _sellLimitLabel.IsVisible = _sellMaxAmount.IsVisible = true;
                UpdateVendorsListGump(Vendors.Sell.SellSelected);
            }
            else
            {
                _sellLimitLabel.IsVisible = _sellMaxAmount.IsVisible = _sellMaxAmount.IsEnabled = false;
                _buyComplete.IsVisible = true;
                UpdateVendorsListGump(Vendors.Buy.BuySelected);
            }
        }

        internal void AddVendorsListToHotkeys()
        {
            List<string> names = new List<string>();
            foreach(Vendors.IBuySell ibs in Vendors.Buy.BuyList.Keys)
            {
                names.Add(ibs.Name);
                //"agents.vendors.buy.buy-2"
                HotKeys.AddHotKeyFunc($"{Vendors.Prepend[0]}{ibs.Name.ToLower(XmlFileParser.Culture)}", (input) =>
                {
                    UpdateVendorsListGump(ibs);
                    return true;
                });
            }
            _agentsVendorsBuyHK.SetItemsValue(names.ToArray());
            names.Clear();
            foreach (Vendors.IBuySell ibs in Vendors.Sell.SellList.Keys)
            {
                names.Add(ibs.Name);
                HotKeys.AddHotKeyFunc($"{Vendors.Prepend[1]}{ibs.Name.ToLower(XmlFileParser.Culture)}", (input) =>
                {
                    UpdateVendorsListGump(ibs);
                    return true;
                });
            }
            _agentsVendorsSellHK.SetItemsValue(names.ToArray());
        }

        private void EnableScavenger_ValueChanged(object sender, EventArgs e)
        {
            Scavenger.OnEnabledChanged();
            if (!_enableScavenger.IsChecked)
            {
                UOSObjects.Player.SendMessage(MsgLevel.Warning, "Scavenger Disabled");
            }
            else
            {
                UOSObjects.Player.SendMessage(MsgLevel.Friend, "Scavenger Enabled");
            }
        }

        private void StackScavenger_ValueChanged(object sender, EventArgs e)
        {
            Scavenger.OnStackChanged();
        }

        private void OrganizerStack_ValueChanged(object sender, EventArgs e)
        {
            if (sender is AssistCheckbox cb && cb.Tag is Organizer organizer)
                organizer.Stack = cb.IsChecked;
        }

        private void OrganizerLoop_ValueChanged(object sender, EventArgs e)
        {
            if (sender is AssistCheckbox cb && cb.Tag is Organizer organizer)
                organizer.Loop = cb.IsChecked;
        }

        private void OrganizerComplete_ValueChanged(object sender, EventArgs e)
        {
            if (sender is AssistCheckbox cb && cb.Tag is Organizer organizer)
                organizer.Complete = cb.IsChecked;
        }

        internal void UpdateOrganizerListGump(ushort selected = ushort.MaxValue)
        {
            _organizerListArea.Clear();
            bool found = false;
            for (ushort i = 0; i < Organizer.Organizers.Count; ++i)
            {
                Organizer or = Organizer.Organizers[i];
                if (or != null)
                {
                    var b = CreateSingleTextSelection(_organizerListArea, 2, (int)ButtonType.OrganizerList, (int)ButtonType.OrganizerList, i, TEXT_ALIGN_TYPE.TS_CENTER, _organizerListArea.Width, or.Name, ScriptTextBox.GRAY_HUE, 30);
                    b.TextBoxes[0].Tag = or;
                    b.TextBoxes[0].TextChanged += SABSODVTextBox_TextChanged;
                    b.TextBoxes[0].FocusLost += OrganizerList_FocusLost;
                    if (selected < ushort.MaxValue && i == selected)
                    {
                        b.IsSelected = found = true;
                        b.TextBoxes[0].Priority = ClickPriority.High;
                        UpdateOrganizerCheckbuttons(or);
                    }
                }
            }
            if (!found)
            {
                UpdateOrganizerCheckbuttons(null);
            }
        }

        private void UpdateOrganizerCheckbuttons(Organizer or)
        {
            _organizerComplete.Tag = _organizerLoop.Tag = _organizerStack.Tag = or;
            if (or != null)
            {
                _organizerComplete.IsChecked = or.Complete;
                _organizerLoop.IsChecked = or.Loop;
                _organizerStack.IsChecked = or.Stack;
            }
            else
            {
                _organizerComplete.Tag = _organizerLoop.Tag = _organizerStack.Tag = null;
                _organizerComplete.IsChecked = _organizerLoop.IsChecked = _organizerStack.IsChecked = false;
            }
        }

        internal void AddOrganizerListToHotkeys()
        {
            List<string> names = new List<string>();
            foreach (Organizer ol in Organizer.Organizers)
            {
                if (ol != null)
                {
                    names.Add(ol.Name);
                    HotKeys.AddHotKeyFunc($"{Organizer.Prepend[0]}{ol.Name.ToLower(XmlFileParser.Culture)}", (input) => 
                    { 
                        if(ol.Items.Count > 0) 
                            ol.Organize(); 
                        return true; 
                    });
                }
            }
            _agentsOrganizerHK.SetItemsValue(names.ToArray());
        }

        private void KeyboardFocusFix(object sender)
        {
            if (sender != null && UIManager.KeyboardFocusControl == sender)
            {
                UIManager.KeyboardFocusControl = null;
                if (sender is AssistStbTextBox stb)
                {
                    stb.Priority = ClickPriority.Default;
                }
            }
        }

        internal void UpdateOrganizerItemsGump(ushort selected = ushort.MaxValue)
        {
            _organizerItems.Clear();
            Organizer ol = null;
            if (_organizerListSelected < ushort.MaxValue && _organizerListSelected < Organizer.Organizers.Count)
            {
                ol = Organizer.Organizers[_organizerListSelected];
                if (ol != null)
                {
                    if (ol.Items.Count > 0)
                    {
                        bool hasselection = false;
                        _playOrganizer.IsEnabled = true;// Engine.Instance.AllowBit(FeatureBit.RestockAgent);
                        if (!Organizer.IsTimerActive && _playOrganizer.IsEnabled)
                            _playOrganizer.TextLabel.Hue = ScriptTextBox.GREEN_HUE;
                        for (ushort i = 0; i < ol.Items.Count; ++i)
                        {
                            ItemDisplay oi = ol.Items[i];

                            var b = CreateTextSelection(_organizerItems, 0, (int)ButtonType.OrganizerListItem, (int)ButtonType.OrganizerListItem, i, TEXT_ALIGN_TYPE.TS_CENTER, _organizerItemsWidth, 1, false, oi.Name, $"0x{oi.Graphic:X}", oi.Hue < 0 ? "All" : oi.Hue.ToString(), oi.Amount == 0 ? "All" : oi.Amount.ToString());
                            //b.TextBoxes[0].IsEditable = false;
                            b.TextBoxes[1].NumbersOnly = b.TextBoxes[2].NumbersOnly = true;
                            b.TextBoxes[0].Tag = b.TextBoxes[1].Tag = b.TextBoxes[2].Tag = oi;
                            b.TextBoxes[0].TextChanged += SABSODVTextBox_TextChanged;
                            b.TextBoxes[1].TextChanged += SABSODVTextBox_TextChanged;
                            b.TextBoxes[2].TextChanged += SABSODVTextBox_TextChanged;
                            b.TextBoxes[0].FocusLost += OrganizerName_FocusLost;
                            b.TextBoxes[1].FocusLost += OrganizerHue_FocusLost;
                            b.TextBoxes[2].FocusLost += OrganizerAmount_FocusLost;
                            if (selected < ushort.MaxValue && i == selected)
                            {
                                b.IsSelected = hasselection = true;
                            }
                        }
                        if (_removeOrganizerItem.IsEnabled = hasselection)
                            _removeOrganizerItem.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                        else
                            _removeOrganizerItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    }
                    else
                    {
                        if (!Organizer.IsTimerActive)
                        {
                            _removeOrganizerItem.IsEnabled = _playOrganizer.IsEnabled = false;
                            _removeOrganizerItem.TextLabel.Hue = _playOrganizer.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        }
                    }
                }
            }
            if (ol == null)
            {
                _removeOrganizerItem.IsEnabled = _playOrganizer.IsEnabled = _removeOrganizer.IsEnabled = _organizerSetCont.IsEnabled = _organizerComplete.IsEnabled = _organizerLoop.IsEnabled = _organizerStack.IsEnabled = _insertOrganizerItem.IsEnabled = false;
                _removeOrganizerItem.TextLabel.Hue = _playOrganizer.TextLabel.Hue = _removeOrganizer.TextLabel.Hue = _organizerSetCont.TextLabel.Hue = _insertOrganizerItem.TextLabel.Hue = _organizerComplete.Hue = _organizerLoop.Hue = _organizerStack.Hue = ScriptTextBox.RED_HUE;
            }
            else
            {
                _removeOrganizer.IsEnabled = _organizerSetCont.IsEnabled = _organizerComplete.IsEnabled = _organizerLoop.IsEnabled = _organizerStack.IsEnabled = _insertOrganizerItem.IsEnabled = true;
                _removeOrganizer.TextLabel.Hue = _organizerSetCont.TextLabel.Hue = _insertOrganizerItem.TextLabel.Hue = _organizerComplete.Hue = _organizerLoop.Hue = _organizerStack.Hue = ScriptTextBox.GRAY_HUE;
            }
        }

        internal void OrganizerStatus(bool start)
        {
            if (_organizerListSelected < Organizer.Organizers.Count)
            {
                Organizer ol = Organizer.Organizers[_organizerListSelected];
                if (ol != null)
                {
                    if (start)
                    {
                        _playOrganizer.TextLabel.Text = "Stop";
                        _removeOrganizer.TextLabel.Hue = _playOrganizer.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        _removeOrganizer.IsEnabled = false;
                    }
                    else
                    {
                        _playOrganizer.TextLabel.Text = "Play";
                        _removeOrganizer.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                        _removeOrganizer.IsEnabled = true;
                        if (ol.Items.Count > 0)
                        {
                            _playOrganizer.IsEnabled = true;
                            _playOrganizer.TextLabel.Hue = ScriptTextBox.GREEN_HUE;
                        }
                        else
                        {
                            _playOrganizer.IsEnabled = false;
                            _playOrganizer.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        }
                    }
                }
                else if(!start)
                    _playOrganizer.TextLabel.Text = "Play";
            }
        }

        private void SABSODVTextBox_TextChanged(object sender, EventArgs e)
        {
            if (sender is AssistStbTextBox stb)
                _changedPlaceHolder = stb;
        }

        private AssistStbTextBox _changedPlaceHolder;

        private void OrganizerName_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is ItemDisplay oi)
            {
                if (string.IsNullOrWhiteSpace(_changedPlaceHolder.Text))
                {
                    _changedPlaceHolder.Text = oi.Name = UOSObjects.GetDefaultItemName(oi.Graphic);
                }
                else
                {
                    oi.Name = _changedPlaceHolder.Text;
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void OrganizerHue_FocusLost(object sender, EventArgs e)
        {
            if(_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is ItemDisplay oi)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text) || !short.TryParse(_changedPlaceHolder.Text, out short hue) || hue < 0)
                {
                    _changedPlaceHolder.Text = "All";
                    oi.Hue = -1;
                }
                else
                {
                    _changedPlaceHolder.Text = (oi.Hue = hue).ToString();
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void OrganizerAmount_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is ItemDisplay oi)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text) || !uint.TryParse(_changedPlaceHolder.Text, out uint amount) || amount == 0)
                {
                    _changedPlaceHolder.Text = "All";
                    oi.Amount = 0;
                }
                else
                {
                    _changedPlaceHolder.Text = (oi.Amount = amount).ToString();
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        internal void UpdateScavengerItemsGump(ItemDisplay selected = null)
        {
            _scavengerItems.Clear();
            var sa = Scavenger.ItemIDsHues;
            if(Scavenger.ItemIDsHues.Count > 0)
            {
                bool hasselection = false;
                foreach(KeyValuePair<ushort, List<ItemDisplay>> kvp in sa)
                {
                    foreach (ItemDisplay id in kvp.Value)
                    {
                        var b = CreateTextSelection(_scavengerItems, 0, (int)ButtonType.ScavengerListItem, (int)ButtonType.ScavengerListItem, id, TEXT_ALIGN_TYPE.TS_CENTER, _scavengerItemsWidth, 1, true, id.Name, $"0x{kvp.Key:X4}", id.Hue < 0 ? "All" : id.Hue.ToString());
                        b.Checkbox.IsChecked = id.Enabled;
                        //b.TextBoxes[0].IsEditable = false;
                        b.Checkbox.Tag = b.TextBoxes[0].Tag = b.TextBoxes[1].Tag = id;
                        b.TextBoxes[0].FocusLost += ScavengerName_FocusLost;
                        b.TextBoxes[1].NumbersOnly = true;
                        b.TextBoxes[0].TextChanged += SABSODVTextBox_TextChanged;
                        b.TextBoxes[1].TextChanged += SABSODVTextBox_TextChanged;
                        b.TextBoxes[1].FocusLost += ScavengerHue_FocusLost;
                        b.Checkbox.ValueChanged += ScavengedCheckbox_ValueChanged;
                        if (selected != null && selected.Equals(id))
                        {
                            b.IsSelected = hasselection = true;
                        }
                    }
                    if (_removeScavenged.IsEnabled = hasselection)
                        _removeScavenged.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                    else
                        _removeScavenged.TextLabel.Hue = ScriptTextBox.RED_HUE;
                }
            }
            else
            {
                _removeScavenged.IsEnabled = false;
                _removeScavenged.TextLabel.Hue = ScriptTextBox.RED_HUE;
            }
        }

        private void ScavengedCheckbox_ValueChanged(object sender, EventArgs e)
        {
            if(sender is AssistCheckbox cb && cb.Tag is ItemDisplay id)
            {
                id.Enabled = cb.IsChecked;
            }
        }

        private void ScavengerHue_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is ItemDisplay id)
            {
                if (string.IsNullOrEmpty(_changedPlaceHolder.Text) || !short.TryParse(_changedPlaceHolder.Text, out short hue) || hue < 0)
                {
                    _changedPlaceHolder.Text = "All";
                    id.Hue = -1;
                }
                else
                {
                    _changedPlaceHolder.Text = (id.Hue = hue).ToString();
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void ScavengerName_FocusLost(object sender, EventArgs e)
        {
            if (_changedPlaceHolder != null && sender == _changedPlaceHolder && _changedPlaceHolder.Tag is ItemDisplay id)
            {
                if (string.IsNullOrWhiteSpace(_changedPlaceHolder.Text))
                {
                    _changedPlaceHolder.Text = id.Name = UOSObjects.GetDefaultItemName(id.Graphic);
                }
                else
                {
                    id.Name = _changedPlaceHolder.Text;
                }
            }
            KeyboardFocusFix(sender);
            _changedPlaceHolder = null;
        }

        private void AutolootAmount_ValueChanged(object sender, int e)
        {
            if(_autolootAmount.IsEnabled && ItemsToLoot.TryGetValue(_lootSelected, out (ushort, string) val))
            {
                ItemsToLoot[_lootSelected] = ((ushort)e, val.Item2);
            }
        }

        internal void UpdateAutolootList(ushort graphic = 0)
        {
            _autolootArea.Clear();
            foreach (KeyValuePair<ushort, (ushort, string)> kvp in ItemsToLoot)
            {
                var b = CreateSelection(_autolootArea, $"{kvp.Value.Item2}", 2, (int)ButtonType.AutolootList, (int)ButtonType.AutolootList, kvp.Key);
                if (graphic > 0 && kvp.Key == graphic)
                    b.IsSelected = true;
            }
        }

        internal void UpdateDressListGump(ushort selected = ushort.MaxValue)
        {
            _dressListsArea.Clear();
            for (ushort i = 0; i < DressList.DressLists.Count; ++i)
            {
                DressList dl = DressList.DressLists[i];
                if (dl != null)
                {
                    var b = CreateSingleTextSelection(_dressListsArea, 2, (int)ButtonType.DressList, (int)ButtonType.DressList, i, TEXT_ALIGN_TYPE.TS_CENTER, _dressListsArea.Width, dl.Name, ScriptTextBox.GRAY_HUE, 30);
                    b.TextBoxes[0].Tag = dl;
                    b.TextBoxes[0].TextChanged += SABSODVTextBox_TextChanged;
                    b.TextBoxes[0].FocusLost += DressList_FocusLost;
                    if (selected < ushort.MaxValue && i == selected)
                    {
                        b.IsSelected = true;
                        b.TextBoxes[0].Priority = ClickPriority.High;
                    }
                }
            }
        }

        internal void AddDressListToHotkeys()
        {
            List<string> names = new List<string>();
            foreach(DressList dl in DressList.DressLists)
            {
                if (dl != null)
                {
                    names.Add(dl.Name);
                    HotKeys.AddHotKeyFunc($"{DressList.Prepend[0]}{dl.Name.ToLower(XmlFileParser.Culture)}", (input) => { dl.Dress(); return true; });
                    HotKeys.AddHotKeyFunc($"{DressList.Prepend[1]}{dl.Name.ToLower(XmlFileParser.Culture)}", (input) => { dl.Undress(); return true; });
                }
            }
            _agentsDressHK.SetItemsValue(names.ToArray());
            _agentsUndressHK.SetItemsValue(names.ToArray());
        }

        internal void UpdateDressItemsGump(Layer selected = Layer.Invalid)
        {
            _dressItemsArea.Clear();
            DressList dl = null;
            if (_dressListSelected < ushort.MaxValue && _dressListSelected < DressList.DressLists.Count)
            {
                dl = DressList.DressLists[_dressListSelected];
                if(dl != null)
                {
                    if (dl.LayerItems.Count > 0)
                    {
                        bool hasselection = false;
                        _dressButton.IsEnabled = _undressButton.IsEnabled = _clearAllDress.IsEnabled = true;
                        _dressButton.TextLabel.Hue = _undressButton.TextLabel.Hue = _clearAllDress.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                        foreach (KeyValuePair<Layer, DressItem> kvp in dl.LayerItems)
                        {
                            var b = CreateSelection(_dressItemsArea, $"{StringHelper.AddSpaceBeforeCapital(kvp.Key.ToString())}: 0x{kvp.Value.TypeID}", 2, (int)ButtonType.DressListItem, (int)ButtonType.DressListItem, kvp.Key);
                            if (selected > 0 && kvp.Key == selected)
                            {
                                b.IsSelected = hasselection = true;
                            }
                        }
                        if (_removeItemDress.IsEnabled = hasselection)
                            _removeItemDress.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                        else
                            _removeItemDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    }
                    else
                    {
                        _removeItemDress.IsEnabled = _dressButton.IsEnabled = _undressButton.IsEnabled = _clearAllDress.IsEnabled = false;
                        _removeItemDress.TextLabel.Hue = _dressButton.TextLabel.Hue = _undressButton.TextLabel.Hue = _clearAllDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    }
                }
            }
            if(dl == null)
            {
                _removeDressList.IsEnabled = _setUndressCont.IsEnabled = _newItemDress.IsEnabled = _importDress.IsEnabled = false;
                _removeDressList.TextLabel.Hue = _setUndressCont.TextLabel.Hue = _newItemDress.TextLabel.Hue = _importDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
            }
            else
            {
                _removeDressList.IsEnabled = _setUndressCont.IsEnabled = _newItemDress.IsEnabled = _importDress.IsEnabled = true;
                _removeDressList.TextLabel.Hue = _setUndressCont.TextLabel.Hue = _newItemDress.TextLabel.Hue = _importDress.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
            }
        }
#endregion

        public override void OnButtonClick(int buttonID)
        {
            ButtonType bt = (ButtonType)buttonID;
            switch (bt)
            {
                case ButtonType.SetAutolootContainer:
                {
                    void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                    {
                        if (UOSObjects.Player.Backpack == null)
                            return;
                        if (SerialHelper.IsItem(serial))
                        {
                            UOItem i = UOSObjects.FindItem(serial);
                            if(i != null)
                            {
                                if (i.Serial == UOSObjects.Player.Backpack.Serial)
                                {
                                    AutoLootContainer = 0;
                                }
                                else if (i.IsContainer && UOSObjects.Player.Backpack.ContainsItemBySerial(i.Serial, true))
                                {
                                    AutoLootContainer = i.Serial;
                                }
                                else
                                {
                                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Autoloot: Not a container or not inside player backpack");
                                    return;
                                }
                                XmlFileParser.SaveData();
                            }
                        }
                        else if (UOSObjects.Player.Serial == serial)
                        {
                            AutoLootContainer = 0;
                            XmlFileParser.SaveData();
                        }
                        else
                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Autoloot: Invalid Container Target");
                    }
                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), "Autoloot Container: Select AutoLoot Container");
                    Targeting.OneTimeTarget(false, onTargetSelected);
                    break;
                }
                case ButtonType.AutolootList:
                {
                    AssistNiceButton but = AssistNiceButton.GetSelected(_autolootArea, (int)ButtonType.AutolootList);
                    if (but != null)
                    {
                        _lootSelected = (ushort)but.Tag;
                        if (ItemsToLoot.TryGetValue(_lootSelected, out (ushort, string) val))
                        {
                            _autolootAmount.IsEnabled = true;
                            _autolootAmount.Text = (_autolootAmount.Tag = val.Item1).ToString();
                        }
                        else
                        {
                            _autolootAmount.IsEnabled = false;
                            _autolootAmount.Text = "0";
                        }
                    }
                    break;
                }
                case ButtonType.InsertAutolootItem:
                {
                    void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                    {
                        if (SerialHelper.IsItem(serial))
                        {
                            if (!ItemsToLoot.ContainsKey(itemid))
                            {
                                string s = UOSObjects.GetDefaultItemName(itemid);
                                UOItem i = UOSObjects.FindItem(serial);
                                if (string.IsNullOrEmpty(s) && i != null)
                                    s = i.Name;
                                if (string.IsNullOrEmpty(s))
                                    return;
                                ItemsToLoot[itemid] = (0, $"{s}: 0x{itemid:X}");
                                UpdateAutolootList();
                                XmlFileParser.SaveData();
                            }
                        }
                        else
                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Autoloot: Invalid target");
                    }
                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), "Autoloot List: Target item to add");
                    Targeting.OneTimeTarget(false, onTargetSelected);
                    break;
                }
                case ButtonType.RemoveAutolootItem:
                {
                    ItemsToLoot.Remove(_lootSelected);
                    _lootSelected = 0;
                    List<AssistNiceButton> list = new List<AssistNiceButton>(_autolootArea.FindControls<AssistNiceButton>());//.SelectMany(s => s.Children.OfType<AssistNiceButton>()));
                    for (int i = 0; i < list.Count; i++)
                    {
                        AssistNiceButton but = list[i];
                        if (but != null && but.IsSelected)
                        {
                            if (i > 0)
                                _lootSelected = (ushort)list[i - 1].Tag;
                            else if (i + 1 < list.Count)
                                _lootSelected = (ushort)list[i + 1].Tag;
                            break;
                        }
                    }
                    UpdateAutolootList(_lootSelected);
                    XmlFileParser.SaveData();
                    break;
                }
                case ButtonType.DressList:
                {
                    NiceButtonStbText but = NiceButtonStbText.GetSelected(_dressListsArea, (int)ButtonType.DressList);
                    if (but != null)
                    {
                        _dressListSelected = (ushort)but.Tag;
                    }
                    UpdateDressItemsGump();
                    break;
                }
                case ButtonType.CreateDressList:
                {
                    _dressListSelected = DressList.CreateOne();
                    UpdateDressListGump(_dressListSelected);
                    UpdateDressItemsGump();
                    AddDressListToHotkeys();
                    XmlFileParser.SaveData();
                    break;
                }
                case ButtonType.RemoveDressList:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList dl = DressList.DressLists[_dressListSelected];
                        if (dl != null)
                        {
                            HotKeys.RemoveHotKey($"{DressList.Prepend[0]}{dl.Name.ToLower(XmlFileParser.Culture)}", true);
                            HotKeys.RemoveHotKey($"{DressList.Prepend[1]}{dl.Name.ToLower(XmlFileParser.Culture)}", true);
                            DressList.DressLists[_dressListSelected] = null;
                            _dressListSelected = ushort.MaxValue;
                            List<NiceButtonStbText> list = new List<NiceButtonStbText>(_dressListsArea.FindControls<NiceButtonStbText>());//.SelectMany(s => s.Children.OfType<NiceButtonStbText>()));
                            for (int i = 0; i < list.Count; i++)
                            {
                                NiceButtonStbText but = list[i];
                                if (but != null && but.IsSelected)
                                {
                                    if (i > 0)
                                        _dressListSelected = (ushort)list[i - 1].Tag;
                                    else if (i + 1 < list.Count)
                                        _dressListSelected = (ushort)list[i + 1].Tag;
                                    break;
                                }
                            }
                            UpdateDressListGump(_dressListSelected);
                            UpdateDressItemsGump();
                            AddDressListToHotkeys();
                            XmlFileParser.SaveData();
                        }
                    }
                    break;
                }
                case ButtonType.DressListItem:
                {
                    AssistNiceButton but = AssistNiceButton.GetSelected(_dressItemsArea, (int)ButtonType.DressListItem);
                    if (but != null)
                    {
                        _dressItemSelected = (Layer)but.Tag;
                        _removeItemDress.IsEnabled = true;
                        _removeItemDress.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                        if(_dressListSelected < DressList.DressLists.Count)
                        {
                            if (DressList.DressLists[_dressListSelected].LayerItems.TryGetValue(_dressItemSelected, out var item))
                            {
                                TypeDress = item.UsesType;
                            }
                        }
                    }
                    else
                    {
                        _removeItemDress.IsEnabled = false;
                        _removeItemDress.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    }
                    break;
                }
                case ButtonType.AddItemToDressList:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList dl = DressList.DressLists[_dressListSelected];
                        if (dl != null)
                        {
                            void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                            {
                                if (SerialHelper.IsItem(serial))
                                {
                                    UOItem item = UOSObjects.FindItem(serial);
                                    if (item != null && item.Layer > Layer.Invalid && item.Layer < Layer.Mount && item.Layer != Layer.Beard && item.Layer != Layer.Hair && item.Layer != Layer.Backpack)
                                    {
                                        dl.LayerItems[item.Layer] = new DressItem(serial, itemid, TypeDress);
                                        UpdateDressItemsGump(item.Layer);
                                        XmlFileParser.SaveData();
                                    }
                                    else
                                        UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Dress Item: Target is NOT a dressable type");
                                }
                                else
                                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Dress Item: Invalid target");
                            }
                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), "Dress Item: Target item to add");
                            Targeting.OneTimeTarget(false, onTargetSelected);
                        }
                    }
                    break;
                }
                case ButtonType.RemoveItemFromDressList:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList dl = DressList.DressLists[_dressListSelected];
                        if (dl != null)
                        {
                            if (dl.LayerItems.Remove(_dressItemSelected))
                            {
                                _dressItemSelected = Layer.Invalid;
                                List<AssistNiceButton> list = new List<AssistNiceButton>(_dressItemsArea.FindControls<AssistNiceButton>());//.SelectMany(s => s.Children.OfType<AssistNiceButton>()));
                                for (int i = 0; i < list.Count; i++)
                                {
                                    AssistNiceButton but = list[i];
                                    if (but != null && but.IsSelected)
                                    {
                                        if (i > 0)
                                            _dressItemSelected = (Layer)list[i - 1].Tag;
                                        else if (i + 1 < list.Count)
                                            _dressItemSelected = (Layer)list[i + 1].Tag;
                                        break;
                                    }
                                }
                                UpdateDressItemsGump(_dressItemSelected);
                                XmlFileParser.SaveData();
                            }
                        }
                    }
                    break;
                }
                case ButtonType.UndressSelectedList:
                case ButtonType.DressSelectedList:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList dl = DressList.DressLists[_dressListSelected];
                        if (dl != null)
                        {
                            if (bt == ButtonType.DressSelectedList)
                                dl.Dress();
                            else
                                dl.Undress();
                        }
                    }
                    break;
                }
                case ButtonType.ImportCurrentlyDressed:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList.DressLists[_dressListSelected]?.ImportCurrentItems();
                        UpdateDressItemsGump();
                        XmlFileParser.SaveData();
                    }
                    break;
                }
                case ButtonType.DressTypeOrSerial:
                {
                    TypeDress = !TypeDress;
                    if(_dressListSelected < DressList.DressLists.Count)
                    {
                        if (DressList.DressLists[_dressListSelected].LayerItems.TryGetValue(_dressItemSelected, out var item) && AssistNiceButton.GetSelected(_dressItemsArea, (int)ButtonType.DressListItem) != null)
                        {
                            if (item.UsesType != TypeDress)
                            {
                                item.SetUseType(TypeDress);
                                UpdateDressItemsGump(_dressItemSelected);
                                XmlFileParser.SaveData();
                            }
                        }
                    }
                    break;
                }
                case ButtonType.ClearSelectedDressList:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList.ClearAll(_dressListSelected);
                        UpdateDressItemsGump();
                        XmlFileParser.SaveData();
                    }
                    break;
                }
                case ButtonType.SetUndressContainer:
                {
                    if (_dressListSelected < DressList.DressLists.Count)
                    {
                        DressList dl = DressList.DressLists[_dressListSelected];
                        if (dl != null)
                        {
                            void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                            {
                                if (SerialHelper.IsItem(serial))
                                {
                                    UOItem item = UOSObjects.FindItem(serial);
                                    if (item != null && item.IsContainer)
                                    {
                                        if (item.RootContainer == UOSObjects.Player)
                                        {
                                            dl.UndressBag = serial;
                                            XmlFileParser.SaveData();
                                        }
                                        else
                                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Undress Container: container must be inside your backpack!");
                                    }
                                    else
                                        UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Undress Container: Target is NOT a container");
                                }
                                else
                                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Undress Container: Invalid target");
                            }
                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), "Undress Container: Target a container");
                            Targeting.OneTimeTarget(false, onTargetSelected);
                        }
                    }
                    break;
                }
                case ButtonType.RenameBuySellList:
                {
                    AddVendorsListToHotkeys();
                    XmlFileParser.SaveData();
                    break;
                }
                case ButtonType.BuySellList:
                {
                    NiceButtonStbText but = NiceButtonStbText.GetSelected(_vendorsListArea, (int)ButtonType.BuySellList);
                    Vendors.IBuySell selected = null;
                    if (but != null)
                    {
                        selected = (Vendors.IBuySell)but.Tag;
                    }
                    UpdateVendorsListGump(selected, true);
                    break;
                }
                case ButtonType.RemoveBuySellList:
                {
                    Vendors.IBuySell selected;
                    Utility.Collections.OrderedDictionary<Vendors.IBuySell, List<BuySellEntry>> dict;
                    if (_BuySellCombo.SelectedIndex == 1)
                    {
                        selected = Vendors.Sell.SellSelected;
                        Vendors.Sell.SellSelected = null;
                        dict = Vendors.Sell.SellList;
                    }
                    else
                    {
                        selected = Vendors.Buy.BuySelected;
                        Vendors.Buy.BuySelected = null;
                        dict = Vendors.Buy.BuyList;
                    }
                    if (selected != null && dict != null)
                    {
                        HotKeys.RemoveHotKey($"{Vendors.Prepend[_BuySellCombo.SelectedIndex]}{selected.Name.ToLower(XmlFileParser.Culture)}", true);
                        int idx = dict.IndexOf(selected);
                        if (idx >= 0)
                        {
                            if (idx > 0)
                                selected.Selected = dict.GetItem(idx - 1).Key;
                            else if (idx + 1 < dict.Count)
                                selected.Selected = dict.GetItem(idx + 1).Key;
                            dict.RemoveAt(idx);
                        }
                        UpdateVendorsListGump(selected.Selected);
                        AddVendorsListToHotkeys();
                        XmlFileParser.SaveData();
                    }
                    break;
                }
                case ButtonType.NewBuySellList:
                {
                    Vendors.IBuySell bse;
                    if(_BuySellCombo.SelectedIndex == 1)
                    {
                        bse = Vendors.Sell.CreateOne();
                    }
                    else
                    {
                        bse = Vendors.Buy.CreateOne();
                    }
                    if (bse != null)
                    {
                        UpdateVendorsListGump(bse);
                        AddVendorsListToHotkeys();
                        XmlFileParser.SaveData();
                    }
                    break;
                }
                case ButtonType.BuySellItemList:
                {
                    NiceButtonStbText but = NiceButtonStbText.GetSelected(_vendorsItemsArea, (int)ButtonType.BuySellItemList);
                    if (but != null)
                    {
                        _vendorsItemSelected = (ushort)but.Tag;
                        _removeBuySellItem.IsEnabled = true;
                        _removeBuySellItem.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                    }
                    else
                    {
                        _removeBuySellItem.IsEnabled = false;
                        _removeBuySellItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    }
                    break;
                }
                case ButtonType.RemoveBuySellItem:
                {
                    Vendors.IBuySell ibs;
                    if(_BuySellCombo.SelectedIndex == 1)
                    {
                        ibs = Vendors.Sell.SellSelected;
                    }
                    else
                    {
                        ibs = Vendors.Buy.BuySelected;
                    }
                    if (ibs != null)
                    {
                        var list = ibs.BuySellItems;
                        if (list != null && _vendorsItemSelected < list.Count)
                        {
                            list.RemoveAt(_vendorsItemSelected);
                            if (_vendorsItemSelected >= list.Count)
                            {
                                if (_vendorsItemSelected > 0)
                                    --_vendorsItemSelected;
                                else
                                    _vendorsItemSelected = ushort.MaxValue;
                            }
                            UpdateVendorsItemsGump(ibs, _vendorsItemSelected);
                            XmlFileParser.SaveData();
                        }
                    }
                    break;
                }
                case ButtonType.InsertBuySellItem:
                {
                    Vendors.IBuySell ibs;
                    string prepend;
                    if (_BuySellCombo.SelectedIndex == 1)
                    {
                        ibs = Vendors.Sell.SellSelected;
                        prepend = "Sell";
                    }
                    else
                    {
                        ibs = Vendors.Buy.BuySelected;
                        prepend = "Buy";
                    }
                    if (ibs != null)
                    {
                        var list = ibs.BuySellItems;
                        if (list != null)
                        {
                            void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                            {
                                if (SerialHelper.IsItem(serial))
                                {
                                    UOItem item = UOSObjects.FindItem(serial);
                                    if (item != null && item.Movable)
                                    {
                                        BuySellEntry bse = new BuySellEntry(item.Graphic, 1);

                                        if (!list.Contains(bse))
                                        {
                                            list.Add(bse);
                                            UpdateVendorsItemsGump(ibs, (ushort)(list.Count - 1));
                                            XmlFileParser.SaveData();
                                        }
                                        else
                                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), $"{prepend}: Item is already listed");
                                    }
                                    else
                                        UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), $"{prepend}: Target is NOT valid or non movable");
                                }
                                else
                                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), $"{prepend}: Invalid target");
                            }
                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), $"{prepend}: Target item to add");
                            Targeting.OneTimeTarget(false, onTargetSelected);
                        }
                    }
                    break;
                }
                case ButtonType.OrganizerList:
                {
                    NiceButtonStbText but = NiceButtonStbText.GetSelected(_organizerListArea, (int)ButtonType.OrganizerList);
                    if (but != null)
                    {
                        _organizerListSelected = (ushort)but.Tag;
                        UpdateOrganizerCheckbuttons(_organizerListSelected < Organizer.Organizers.Count ? Organizer.Organizers[_organizerListSelected] : null);
                    }
                    else
                        UpdateOrganizerCheckbuttons(null);
                    UpdateOrganizerItemsGump();
                    break;
                }
                case ButtonType.PlaySelectedOrganizer:
                {
                    if (_organizerListSelected < Organizer.Organizers.Count)
                    {
                        Organizer ol = Organizer.Organizers[_organizerListSelected];
                        if (ol != null)
                        {
                            if (!Organizer.IsTimerActive)
                            {
                                ol.Organize();
                            }
                            else
                            {
                                Organizer.Stop();
                            }
                        }
                    }
                    break;
                }
                case ButtonType.CreateOrganizerList:
                {
                    _organizerListSelected = Organizer.CreateOne();
                    UpdateOrganizerListGump(_organizerListSelected);
                    UpdateOrganizerItemsGump();
                    AddOrganizerListToHotkeys();
                    XmlFileParser.SaveData();
                    break;
                }
                case ButtonType.RemoveOrganizerList:
                {
                    if (_organizerListSelected < Organizer.Organizers.Count)
                    {
                        Organizer ol = Organizer.Organizers[_organizerListSelected];
                        if (ol != null)
                        {
                            HotKeys.RemoveHotKey($"{Organizer.Prepend[0]}{ol.Name.ToLower(XmlFileParser.Culture)}", true);
                            Organizer.Organizers[_organizerListSelected] = null;
                            _organizerListSelected = ushort.MaxValue;
                            List<NiceButtonStbText> list = new List<NiceButtonStbText>(_organizerListArea.FindControls<NiceButtonStbText>());//.SelectMany(s => s.Children.OfType<NiceButtonStbText>()));
                            for (int i = 0; i < list.Count; i++)
                            {
                                NiceButtonStbText but = list[i];
                                if (but != null && but.IsSelected)
                                {
                                    if (i > 0)
                                        _organizerListSelected = (ushort)list[i - 1].Tag;
                                    else if (i + 1 < list.Count)
                                        _organizerListSelected = (ushort)list[i + 1].Tag;
                                    break;
                                }
                            }
                            UpdateOrganizerListGump(_organizerListSelected);
                            UpdateOrganizerItemsGump();
                            AddOrganizerListToHotkeys();
                            XmlFileParser.SaveData();
                        }
                    }
                    break;
                }
                case ButtonType.SetOrganizerContainers:
                {
                    if (_organizerListSelected < Organizer.Organizers.Count)
                    {
                        Organizer ol = Organizer.Organizers[_organizerListSelected];
                        if (ol != null)
                        {
                            ol.ContainerSelection();
                        }
                    }
                    break;
                }
                case ButtonType.RemoveItemFromOrganizer:
                {
                    if (_organizerListSelected < Organizer.Organizers.Count)
                    {
                        Organizer ol = Organizer.Organizers[_organizerListSelected];
                        if (ol != null)
                        {
                            if (_organizerItemSelected < ol.Items.Count)
                            {
                                ol.Items.RemoveAt(_organizerItemSelected);
                                if (_organizerItemSelected >= ol.Items.Count)
                                {
                                    if (_organizerItemSelected > 0)
                                        --_organizerItemSelected;
                                    else
                                        _organizerItemSelected = ushort.MaxValue;
                                }
                                UpdateOrganizerItemsGump(_organizerItemSelected);
                                XmlFileParser.SaveData();
                            }
                        }
                    }
                    break;
                }
                case ButtonType.InsertItemIntoOrganizer:
                {
                    if (_organizerListSelected < Organizer.Organizers.Count)
                    {
                        Organizer ol = Organizer.Organizers[_organizerListSelected];
                        if (ol != null)
                        {
                            void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                            {
                                if (SerialHelper.IsItem(serial))
                                {
                                    UOItem item = UOSObjects.FindItem(serial);
                                    if (item != null && item.Movable)
                                    {
                                        ItemDisplay oi = new ItemDisplay(item.Graphic, item.DisplayName);
                                        if (!ol.Items.Contains(oi))
                                        {
                                            ol.Items.Add(oi);
                                            UpdateOrganizerItemsGump((ushort)(ol.Items.Count - 1));
                                            XmlFileParser.SaveData();
                                        }
                                        else
                                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Organizer: Item is already listed");
                                    }
                                    else
                                        UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Organizer: Target is NOT valid or non movable");
                                }
                                else
                                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Error), "Organizer: Invalid target");
                            }
                            UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), "Organizer: Target item to add");
                            Targeting.OneTimeTarget(false, onTargetSelected);
                        }
                    }
                    break;
                }
                case ButtonType.OrganizerListItem:
                {
                    NiceButtonStbText but = NiceButtonStbText.GetSelected(_organizerItems, (int)ButtonType.OrganizerListItem);
                    if (but != null)
                    {
                        _organizerItemSelected = (ushort)but.Tag;
                        _removeOrganizerItem.IsEnabled = true;
                        _removeOrganizerItem.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                    }
                    else
                    {
                        _removeOrganizerItem.IsEnabled = false;
                        _removeOrganizerItem.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    }
                    break;
                }
                case ButtonType.InsertScavengeItem:
                {
                    Scavenger.AddToHotBag();
                    break;
                }
                case ButtonType.RemoveScavengeItem:
                {
                    if(_scavengerItemSelected != null)
                    {
                        _scavengerItemSelected = Scavenger.Remove(_scavengerItemSelected);
                        UpdateScavengerItemsGump(_scavengerItemSelected);
                        XmlFileParser.SaveData();
                    }
                    break;
                }
                case ButtonType.ScavengerListItem:
                {
                    NiceButtonStbText but = NiceButtonStbText.GetSelected(_scavengerItems, (int)ButtonType.ScavengerListItem);
                    if (but != null && but.Tag is ItemDisplay id)
                    {
                        _scavengerItemSelected = id;
                        _removeScavenged.IsEnabled = true;
                        _removeScavenged.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                    }
                    else
                    {
                        _removeScavenged.IsEnabled = false;
                        _removeScavenged.TextLabel.Hue = ScriptTextBox.RED_HUE;
                        _scavengerItemSelected = null;
                    }
                    break;
                }
                case ButtonType.ScavengeDestinationCont:
                {
                    Scavenger.SetHotBag();
                    break;
                }
                case ButtonType.ApplySizeChange:
                {
                    if (int.TryParse(_assistSizeX.Text, out int x) && int.TryParse(_assistSizeY.Text, out int y) && (x != WIDTH || y != HEIGHT))
                    {
                        WIDTH = x;
                        HEIGHT = y;
                        XmlFileParser.SaveData();
                        UIManager.Add(UOSObjects.Gump = new AssistantGump() { X = 200, Y = 200 });
                    }
                    break;
                }
                case ButtonType.FriendsList:
                {
                    AssistNiceButton but = AssistNiceButton.GetSelected(_friendListArea, (int)ButtonType.FriendsList);
                    if (but != null)
                    {
                        _friendSelected = (uint)but.Tag;
                    }
                    break;
                }
                case ButtonType.InsertFriend:
                {
                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(MsgLevel.Friend), "Friend List: Target mobile to add");
                    Targeting.OneTimeTarget(false, Targeting.OnFriendTargetSelected);
                    break;
                }
                case ButtonType.RemoveFriend:
                {
                    FriendsManager.FriendDictionary.Remove(_friendSelected);
                    _friendSelected = 0;
                    List<AssistNiceButton> list = new List<AssistNiceButton>(_friendListArea.FindControls<AssistNiceButton>());//.SelectMany(s => s.Children.OfType<AssistNiceButton>()));
                    for(int i = 0; i < list.Count; i++)
                    {
                        AssistNiceButton but = list[i];
                        if (but != null && but.IsSelected)
                        {
                            if (i > 0)
                                _friendSelected = (uint)list[i - 1].Tag;
                            else if (i + 1 < list.Count)
                                _friendSelected = (uint)list[i + 1].Tag;
                            break;
                        }
                    }
                    UpdateFriendListGump(_friendSelected);
                    XmlFileParser.SaveData();
                    break;
                }
                case ButtonType.MacroList:
                {
                    AssistNiceButton but = AssistNiceButton.GetSelected(_macroListArea, (int)ButtonType.MacroList);
                    if (but != null && (string)but.Tag != _macroSelected)
                    {
                        _macroSelected = (string)but.Tag;
                        SetMacroText();
                    }
                    break;
                }
                case ButtonType.NewMacro:
                {
                    UIManager.Add(new NewMacroGump());
                    break;
                }
                case ButtonType.RemoveMacro:
                {
                    if (_macroSelected != null)
                    {
                        ScriptManager.MacroDictionary.Remove(_macroSelected);
                        UIManager.Gumps.OfType<AssistMacroButtonGump>().FirstOrDefault(s => s._macroName == _macroSelected)?.Dispose();
                    }
                    _macroSelected = null;
                    List<AssistNiceButton> list = new List<AssistNiceButton>(_macroListArea.FindControls<AssistNiceButton>());//.SelectMany(s => s.Children.OfType<AssistNiceButton>()));
                    for (int i = 0; i < list.Count; i++)
                    {
                        AssistNiceButton but = list[i];
                        if (but != null && but.IsSelected)
                        {
                            if (i > 0)
                                _macroSelected = (string)list[i - 1].Tag;
                            else if (i + 1 < list.Count)
                                _macroSelected = (string)list[i + 1].Tag;
                            break;
                        }
                    }
                    UpdateMacroListGump(_macroSelected);
                    XmlFileParser.SaveData();
                    break;
                }
                case ButtonType.SaveMacro:
                case ButtonType.PlayMacro:
                {
                    if(!string.IsNullOrEmpty(_macroSelected) && ScriptManager.MacroDictionary.TryGetValue(_macroSelected, out HotKeyOpts opts))
                    {
                        if (opts.Macro != _macroArea._textBox.Text)
                        {
                            opts.Macro = _macroArea._textBox.Text;
                            XmlFileParser.SaveData();
                        }
                        if (bt == ButtonType.PlayMacro)
                            ScriptManager.PlayScript(opts, false, true);
                    }
                    break;
                }
                case ButtonType.RecordMacro:
                {
                    OnRecordMacroButton();
                    break;
                }
                case ButtonType.ObjectInspector:
                {
                    void onTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
                    {
                        UOEntity entity;
                        if (SerialHelper.IsValid(serial) && (entity = UOSObjects.FindEntity(serial)) != null)
                            UIManager.Add(new ObjectInspectorGump(entity));
                    }
                    Targeting.OneTimeTarget(false, onTargetSelected);
                    break;
                }
                /*case ButtonType.SaveCurrentProfile:
                {
                    XmlFileParser.SaveData();
                    break;
                }*/
                case ButtonType.AddNewProfile:
                {
                    UIManager.Add(new NewProfileGump());
                    break;
                }
                case ButtonType.ExemptionsContSearch:
                {
                    UIManager.Add(new SearchExemption());
                    break;
                }
                case ButtonType.RemountSetMount:
                {
                    Targeting.OneTimeTarget(false, Targeting.OnSetMountTarget);
                    break;
                }
                default:
                {
                    
                    break;
                }
            }
        }

        internal Dictionary<uint, string> LinkedProfiles = new Dictionary<uint, string>();

        private void OnRecordMacroButton(bool forcestop = false)
        {
            if (!string.IsNullOrEmpty(_macroSelected) && ScriptManager.MacroDictionary.TryGetValue(_macroSelected, out HotKeyOpts opts))
            {
                ScriptManager.Recording = forcestop ? false : !ScriptManager.Recording;
                if (ScriptManager.Recording)
                {
                    _recordMacro.TextLabel.Text = "Stop";
                    _recordMacro.TextLabel.Hue = ScriptTextBox.GREEN_HUE;
                    _recordAsType.IsVisible = _recordAsType.IsEnabled = true;
                    _newMacro.TextLabel.Hue = _saveMacro.TextLabel.Hue = _delMacro.TextLabel.Hue = _loopMacro.Hue = _noautoInterrupt.Hue = _playMacro.TextLabel.Hue = ScriptTextBox.RED_HUE;
                    _newMacro.IsEnabled = _saveMacro.IsEnabled = _delMacro.IsEnabled = _macroListArea.IsEnabled = _loopMacro.IsEnabled = _noautoInterrupt.IsEnabled = _playMacro.IsEnabled = false;
                }
                else
                {
                    _recordMacro.TextLabel.Text = "Record";
                    _recordMacro.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                    _recordAsType.IsVisible = _recordAsType.IsEnabled = false;
                    _newMacro.TextLabel.Hue = _saveMacro.TextLabel.Hue = _delMacro.TextLabel.Hue = _loopMacro.Hue = _noautoInterrupt.Hue = _playMacro.TextLabel.Hue = ScriptTextBox.GRAY_HUE;
                    _newMacro.IsEnabled = _saveMacro.IsEnabled = _delMacro.IsEnabled = _macroListArea.IsEnabled = _loopMacro.IsEnabled = _noautoInterrupt.IsEnabled = _playMacro.IsEnabled = true;
                }
            }
        }

#region Core Functions

        internal static Line[] CreateRectangleArea(Gump g, int startx, int starty, int width, int height, int topage = 0, uint linecolor = 0xAFAFAF, int linewidth = 1, string toplabel = null, ushort textcolor = 999, byte textfont = 0xFF)
        {
            Line[] lines = new Line[3];

            if (!string.IsNullOrEmpty(toplabel))
            {
                Label l = new Label(toplabel, true, textcolor, font: textfont);
                int rwidth = (width - l.Width) >> 1;
                l.X = startx + rwidth + 2;
                l.Y = Math.Max(0, starty - ((l.Height + 1) >> 1));
                g.Add(l, topage);

                if (rwidth > 0)
                {
                    g.Add(new Line(startx, starty, rwidth, linewidth, linecolor), topage);
                    g.Add(new Line(startx + width - rwidth, starty, rwidth, linewidth, linecolor), topage);
                }
            }
            else
            {
                g.Add(new Line(startx, starty, width, linewidth, linecolor), topage);
            }

            g.Add(lines[0] = new Line(startx, starty, linewidth, height, linecolor), topage);
            g.Add(lines[1] = new Line(startx + width - 1, starty, linewidth, height, linecolor), topage);
            g.Add(lines[2] = new Line(startx, starty + height - 1, width, linewidth, linecolor), topage);

            return lines;
        }

        internal static AssistCheckbox CreateCheckBox(AssistScrollArea area, string text, bool ischecked, int x, int y, ushort inactiveimg = 0x00D2, ushort activeimg = 0x00D3)
        {
            AssistCheckbox box = new AssistCheckbox(inactiveimg, activeimg, text, FONT, ScriptTextBox.GRAY_HUE, true)
            {
                IsChecked = ischecked
            };

            /*if (x != 0)
            {
                AssistScrollAreaItem item = new AssistScrollAreaItem();
                box.X = x;
                box.Y = y;

                item.Add(box);
                area.Add(item);
            }
            else
            {
                box.Y = y;

                area.Add(box);
            }*/
            area.Add(box);

            return box;
        }

        private AssistClickableColorBox CreateClickableColorBox(int x, int y, ushort hue, string text, int page)
        {
            /*uint color = 0xFF7F7F7F;

            if (hue != 0xFFFF)
                color = HuesLoader.Instance.GetPolygoneColor(12, hue);*/

            AssistClickableColorBox box = new AssistClickableColorBox(x, y, 13, 14, hue);//, color);
            Add(box, page);
            Add(new Label(text, true, ScriptTextBox.GRAY_HUE) { X = x + box.Width * 2, Y = y }, page);
            return box;
        }

        private NiceButtonStbText CreateTextSelection(AssistScrollArea area, int y, int group, int index, object tag, TEXT_ALIGN_TYPE align, int[] width, byte labelentry = 0, bool hascheckbox = false, params string[] text)
        {
            if (width.Length != text.Length)
                new Exception($"zero text parameters or width Length ({width.Length}) is not equal to text Length ({text.Length}) - parameters must be equal in length or arrays");

            NiceButtonStbText but = new NiceButtonStbText(0, y, _buttonHeight - (_buttonHeight >> 2), ButtonAction.Activate, FONT, group, align, width, labelentry, hascheckbox, text) { ButtonParameter = index, Tag = tag };
            area.Add(but);
            return but;
        }

        private NiceButtonStbText CreateSingleTextSelection(AssistScrollArea area, int y, int group, int index, object tag, TEXT_ALIGN_TYPE align, int width, string text, ushort hue, int len = 30)
        {
            NiceButtonStbText but = new NiceButtonStbText(0, y, width, _buttonHeight - (_buttonHeight >> 2), text, ButtonAction.Activate, align, FONT, group, hue, len) { ButtonParameter = index, Tag = tag };
            area.Add(but);
            return but;
        }

        private AssistNiceButton CreateSelection(AssistScrollArea area, string text, int y, int group, int index, object tag)
        {
            AssistNiceButton but = new AssistNiceButton(0, y, area.Width - (_buttonHeight >> 1), _buttonHeight - (_buttonHeight >> 2), ButtonAction.Activate, text, group) { ButtonParameter = index, Tag = tag };

            area.Add(but);
            return but;
        }

        private AssistMultiSelectionShrinkbox CreateMultiSelection(AssistScrollArea area, string text, string[] items, int y, int group, ushort buttonimg, ushort pressbuttonimg)
        {
            AssistMultiSelectionShrinkbox msb = new AssistMultiSelectionShrinkbox(0, y, area.Width - (_buttonWidth >> 1), text, items, ScriptTextBox.GRAY_HUE, true, FONT, group, buttonimg, pressbuttonimg);
            area.Add(msb);
            return msb;
        }
#endregion

#region Volatile Variables (not saved)
        internal bool ToggleHotKeys { get; set; }
#endregion

#region AssistantVariables
        internal string LastProfile
        {
            get => _profileSelected.GetItem(_profileSelected.SelectedIndex);
            set
            {
                LoadProfiles();
                for (int i = 0; i < _profiles.Length; i++)
                {
                    if (Path.GetFileNameWithoutExtension(_profiles[i]) == value)
                    {
                        _profileSelected.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        internal bool ReturnToParentScript
        {
            get => _returnToParent.IsChecked;
            set => _returnToParent.IsChecked = value;
        }

        internal bool AutoLoot
        {
            get => _enableAutoLoot.IsChecked;
            set => _enableAutoLoot.IsChecked = value;
        }
        internal uint AutoLootContainer { get; set; }
        internal bool NoAutoLootInGuards { get; set; }

        internal bool SmartProfile
        {
            get => _loadLinkedProfile.IsChecked;
            set => _loadLinkedProfile.IsChecked = value;
        }

        /*internal bool NegotiateFeatures
        {
            get => _negotiateFeatures.IsChecked;
            set => _negotiateFeatures.IsChecked = value;
        }*/

        internal bool UseObjectsQueue
        {
            get => _useObjectsQueue.IsChecked;
            set => _useObjectsQueue.IsChecked = value;
        }

        /*internal bool UseTargetQueue
        {
            get => _useTargetQueue.IsChecked;
            set => _useTargetQueue.IsChecked = value;
        }*/

        internal bool ShowBandageTimerStart
        {
            get => _bandageTimerStart.IsChecked;
            set => _bandageTimerStart.IsChecked = value;
        }

        internal bool ShowBandageTimerEnd
        {
            get => _bandageTimerEnd.IsChecked;
            set => _bandageTimerEnd.IsChecked = value;
        }

        internal bool ShowBandageTimerOverhead
        {
            get => _bandageTimerOverhead.IsChecked;
            set => _bandageTimerOverhead.IsChecked = value;
        }

        internal bool SnapOwnDeath
        {
            get => _snapOnSelfDeath.IsChecked;
            set => _snapOnSelfDeath.IsChecked = value;
        }

        internal bool SnapOtherDeath
        {
            get => _snapOnOthersDeath.IsChecked;
            set => _snapOnOthersDeath.IsChecked = value;
        }

        internal bool ShowCorpseNames
        {
            get => _displayNewCorpsesName.IsChecked;
            set => _displayNewCorpsesName.IsChecked = value;
        }

        /*internal bool OpenCorpses
        {
            get => _openCorpses.IsChecked;
            set => _openCorpses.IsChecked = value;
        }*/

        /*internal bool ShowMobileHits
        {
            get => _healthAbovePeopleAndCreatures.IsChecked;
            set => _healthAbovePeopleAndCreatures.IsChecked = value;
        }*/

        internal bool HandsBeforePotions
        {
            get => _checkHandsBeforePotions.IsChecked;
            set => _checkHandsBeforePotions.IsChecked = value;
        }

        internal bool HandsBeforeCasting
        {
            get => _clearHandsBeforeCasting.IsChecked;
            set => _clearHandsBeforeCasting.IsChecked = value;
        }

        internal bool HighlightCurrentTarget
        {
            get => _highlightCurrentTarget.IsChecked;
            set => _highlightCurrentTarget.IsChecked = value;
        }

        internal ushort HLTargetHue => _highlightCurrentTarget.IsChecked ? _highlightCurrentTargetHue.Hue : (ushort)0;
        internal ushort HighlightCurrentTargetHue
        {
            get => _highlightCurrentTargetHue.Hue;
            set => _highlightCurrentTargetHue.Hue = value;
        }

        internal bool BlockInvalidHeal
        {
            get => _blockHealIfPoisonedOrYellowHits.IsChecked;
            set => _blockHealIfPoisonedOrYellowHits.IsChecked = value;
        }

        internal bool SharedTargetInAliasEnemy
        {
            get => _useSharedTarget.IsChecked;
            set => _useSharedTarget.IsChecked = value;
        }
        /*internal bool BoneCutter
        {
            get => _gauntletBoneCutter.IsChecked;
            set => _gauntletBoneCutter.IsChecked = value;
        }*/

        internal bool AutoMount
        {
            get => _automaticallyRemount.IsChecked;
            set => _automaticallyRemount.IsChecked = value;
        }

        internal bool AutoBandage
        {
            get => _healingEnabled.IsChecked;
            set => _healingEnabled.IsChecked = value;
        }

        internal bool AutoBandageScale
        {
            get => _scalePriorityBasedOnHits.IsChecked;
            set => _scalePriorityBasedOnHits.IsChecked = value;
        }

        internal bool AutoBandageCount
        {
            get => _countSecondsUntilFinishes.IsChecked;
            set => _countSecondsUntilFinishes.IsChecked = value;
        }

        internal bool AutoBandageStart
        {
            get => _startBelowCheck.IsChecked;
            set => _startBelowCheck.IsChecked = value;
        }

        internal bool AutoBandageFormula
        {
            get => _useDexterityFormulaDelay.IsChecked;
            set => _useDexterityFormulaDelay.IsChecked = value;
        }

        internal bool AutoBandageHidden
        {
            get => _allowHealingWhileHidden.IsChecked;
            set => _allowHealingWhileHidden.IsChecked = value;
        }

        internal bool OpenDoors
        {
            get => _openDoors.IsChecked;
            set => _openDoors.IsChecked = value;
        }

        internal bool UseDoors
        {
            get => _doubleClickToOpenDoors.IsChecked;
            set => _doubleClickToOpenDoors.IsChecked = value;
        }

        internal bool ShowMobileFlags
        {
            get => _flagsAbovePeopleAndCreatures.IsChecked;
            set => _flagsAbovePeopleAndCreatures.IsChecked = value;
        }

        internal bool CountStealthSteps
        {
            get => _countStealthSteps.IsChecked;
            set => _countStealthSteps.IsChecked = value;
        }

        internal bool FriendsListOnly
        {
            get => _considerOnlyThisAsValidFriends.IsChecked;
            set => _considerOnlyThisAsValidFriends.IsChecked = value;
        }

        internal bool FriendsParty
        {
            get => _includePartyMembers.IsChecked;
            set => _includePartyMembers.IsChecked = value;
        }

        internal bool MoveConflictingItems 
        { 
            get => _moveConflictingItems.IsChecked; 
            set => _moveConflictingItems.IsChecked = value; 
        }

        internal bool PreventDismount
        {
            get => _preventDuringWarmode.IsChecked;
            set => _preventDuringWarmode.IsChecked = value;
        }

        internal bool PreventAttackFriends
        {
            get => _preventAttackingFriendsInWarmode.IsChecked;
            set => _preventAttackingFriendsInWarmode.IsChecked = value;
        }

        internal bool AutoSearchContainers
        {
            get => _searchNewContainers.IsChecked;
            set => _searchNewContainers.IsChecked = value;
        }

        internal bool AutoAcceptParty
        {
            get => _alwaysAcceptsPartyInvites.IsChecked;
            set => _alwaysAcceptsPartyInvites.IsChecked = value;
        }

        internal bool StartStopMacroMessages
        {
            get => _StartStopMacroMessages.IsChecked;
            set => _StartStopMacroMessages.IsChecked = value;
        }

        /*[PropertyAttributeType(AccessAttribute.ProfileAttribute)]
        internal byte CommandPrefix
        {
            get => (byte)_commandPrefix.SelectedIndex;
            private set
            {
                if (value < _commandprefixes.Length)
                {
                    _commandPrefix.SelectedIndex = value;
                }
            }
        }*/

        /*private byte _openCorpsesRange;
        internal byte OpenCorpsesRange
        {
            get
            {
                return _openCorpsesRange;
            }
            set => _limitOpenRange.Text = (_openCorpsesRange = Math.Max((byte)1, Math.Min(value, (byte)10))).ToString();
        }*///byte limit 10

        /*private byte _useObjectsLimit;
        internal byte UseObjectsLimit
        {
            get
            {
                return _useObjectsLimit;
            }
            set => _maxQueuedItems.Text = (_useObjectsLimit = Math.Max((byte)1, Math.Min(value, (byte)100))).ToString();
        }*///limit 100

        internal bool SmartTargetRange
        {
            get => _limitTargetRange.IsChecked;
            set => _limitTargetRange.IsChecked = value;
        }
        private byte _smartTargetRangeValue;

        internal byte SmartTargetRangeValue
        {
            get
            {
                if (SmartTargetRange)
                    return _smartTargetRangeValue;
                return (byte)UOSObjects.Player.VisRange;
            }
            set => _limitTargetRangeTiles.Text = (_smartTargetRangeValue = Math.Max((byte)1, Math.Min(value, (byte)15))).ToString();
        }//limit 15

        //internal byte FixedSeason { get; set; }//byte

        internal byte SmartTarget
        {
            get => (byte)_smartLastTarget.SelectedIndex;
            set
            {
                if (value < _smartLastTarget.GetItemsLength)
                {
                    _smartLastTarget.SelectedIndex = value;
                }
            }
        }//enum 4

        internal byte EnemyTargetShare
        {
            get => (byte)_shareEnemyTargetOn.SelectedIndex;
            set
            {
                if (value < _shareEnemyTargetOn.GetItemsLength)
                {
                    _shareEnemyTargetOn.SelectedIndex = value;
                }
            }
        }//byte enum 1
        private byte _autoBandageStartValue;
        internal byte AutoBandageStartValue
        {
            get
            {
                return _autoBandageStartValue;
            }
            set => _startBelowValue.Text = (_autoBandageStartValue = Math.Max((byte)1, Math.Min(value, (byte)100))).ToString();
        }

        internal byte SpellsTargetShare
        {
            get => (byte)_spellShareTargetOn.SelectedIndex;
            set
            {
                if (value < _spellShareTargetOn.GetItemsLength)
                {
                    _spellShareTargetOn.SelectedIndex = value;
                }
            }
        }//byte enum 1

        internal byte OpenDoorsMode
        {
            get => (byte)_openDoorsOptions.SelectedIndex;
            set
            {
                if (value < _openDoorsOptions.GetItemsLength)
                {
                    _openDoorsOptions.SelectedIndex = value;
                }
            }
        }//byte enum 2

        /*internal byte OpenCorpsesMode
        {
            get => (byte)_openCorpsesOptions.SelectedIndex;
            set
            {
                if (value < _openCorpsesOptions.GetItemsLength)
                {
                    _openCorpsesOptions.SelectedIndex = value;
                }
            }
        }//byte enum 2*/

        internal byte CustomCaptionMode { get; set; }//byte

        internal uint GrabHotBag { get; set; }

        internal uint MountSerial { get; set; } = uint.MaxValue;//uint

        internal uint BladeSerial { get; set; }//uint

        internal uint AutoBandageTarget
        {
            get => (uint)(_friendHealSelection.SelectedIndex);
            set
            {
                if (value < _friendHealSelection.GetItemsLength)
                    _friendHealSelection.SelectedIndex = (int)value;
            }
        }//uint enum 5

        private uint _AutoBandageDelay;
        internal uint AutoBandageDelay
        {
            get => _AutoBandageDelay;
            set
            {
                _bandageActionDelay.Text = (_AutoBandageDelay = Math.Min(value, 20000)).ToString();
            }
        }//limit 20000
        
        private uint _actionDelay;
        internal uint ActionDelay
        {
            get
            {
                return _actionDelay;
            }
            set
            {
                _delayBetweenActions.Text = (_actionDelay = Math.Max(50, Math.Min(value, 2500))).ToString();
            }
        }//limit 50<->2500

        private bool _TypeDress;
        internal bool TypeDress
        {
            get => _TypeDress;
            set
            {
                if (value != _TypeDress)
                {
                    _TypeDress = value;
                    if (_TypeDress)
                    {
                        _typeOrSerialDress.TextLabel.Hue = ScriptTextBox.GREEN_HUE;
                        _typeOrSerialDress.TextLabel.Text = "Use Type";
                    }
                    else
                    {
                        _typeOrSerialDress.TextLabel.Hue = ScriptTextBox.BLUE_HUE;
                        _typeOrSerialDress.TextLabel.Text = "Use Serial";
                    }
                }
            }
        }

        private bool _SetLinkedProfile = false;
#endregion
    }
}
