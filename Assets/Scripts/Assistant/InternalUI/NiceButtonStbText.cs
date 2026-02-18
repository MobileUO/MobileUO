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

using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class NiceButtonStbText : HitBox
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;
        private byte _SelectedArea = 0;
        internal Label TextLabel { get; }
        internal AssistStbTextBox[] TextBoxes { get; }
        internal AssistCheckbox Checkbox { get; }

        public NiceButtonStbText(int x, int y, int h, ButtonAction action, byte font, int groupnumber, TEXT_ALIGN_TYPE align, int[] width, byte labelentrynum = 0, bool hascheckbox = false, params string[] text) : base(x, y, width.Sum() + (width.Length * 3), h)
        {
            if (width.Length < text.Length)
                throw new System.Exception("the width and numberonly array must contain the same number of elements of text parameter");
            else if(text.Length < 1)
                throw new System.Exception("the text parameter cannot be empty!");
            else if(labelentrynum >= text.Length)
                throw new System.Exception("the labelentrynum must be lower than the number of text element!");
            _action = action;
            TextBoxes = new AssistStbTextBox[text.Length - 1];
            int subx = 0, xtra = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                if (i > 0)
                {
                    if (i == 1)
                        subx -= xtra;
                    subx += width[i - 1] + 3;
                }
                else if(hascheckbox)
                {
                    Add(Checkbox = new AssistCheckbox(0x00D2, 0x00D3, "", font, ScriptTextBox.GRAY_HUE, true) { Priority = ClickPriority.High });
                    subx = xtra = Checkbox.Width;
                    if (width[0] - subx < 0)
                        throw new System.Exception("the primary width must be greater than zero, but the checkbox is eating all the space available");
                }
                if(i == labelentrynum)
                {
                    Add(TextLabel = new Label(text[i], true, 999, width[i] - (i == 0 ? xtra : 0), 0xFF, FontStyle.BlackBorder | FontStyle.Cropped, align) { X = subx });
                    TextLabel.Y = (h - TextLabel.Height) >> 1;
                }
                else
                {
                    Add(TextBoxes[(i > labelentrynum ? i - 1 : i)] = new AssistStbTextBox(font, -1, width[i] - (i == 0 ? subx : 0), true, FontStyle.BlackBorder | FontStyle.Cropped, 999, align) { Width = width[i], Text = text[i], X = subx });
                    if (i == 0)
                    {
                        TextBoxes[0].Width -= xtra;
                    }
                }
            }
            for(int i = 0; i < TextBoxes.Length; ++i)
            {
                TextBoxes[i].Y = TextLabel.Y;
                TextBoxes[i].Height = TextLabel.Height;
                TextBoxes[i].FocusEnter += NiceButtonStbText_FocusEnter;
                TextBoxes[i].FocusLost += NiceButtonStbText_FocusLost;
            }
            _groupnumber = groupnumber;
        }

        public NiceButtonStbText(int x, int y, int w, int h, string text, ButtonAction action, TEXT_ALIGN_TYPE align, byte font, int groupnumber, ushort hue, int maxlenght = 30) : base(x, y, w, h)
        {
            _action = action;
            TextBoxes = new AssistStbTextBox[1];
            Add(TextBoxes[0] = new AssistStbTextBox(font, maxlenght, w, true, FontStyle.BlackBorder | FontStyle.Cropped, hue, align) { Width = w, Height = h, Text = text, X = x, Y = y });
            TextBoxes[0].IsEditable = true;
            TextBoxes[0].FocusEnter += NiceButtonStbText_FocusEnter;
            TextBoxes[0].FocusLost += NiceButtonStbText_FocusLost;
            _groupnumber = groupnumber;
        }

        private void NiceButtonStbText_FocusLost(object sender, System.EventArgs e)
        {
            Parent?.OnFocusLost();
        }

        private void NiceButtonStbText_FocusEnter(object sender, System.EventArgs e)
        {
            Parent?.OnFocusEnter();
        }

        public int ButtonParameter { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected && IsSelectable;
            set
            {
                if (!IsSelectable)
                    return;

                _isSelected = value;

                if (value)
                {
                    Control p = Parent;

                    if (p == null)
                        return;

                    IEnumerable<NiceButtonStbText> list = p.FindControls<NiceButtonStbText>();
                    foreach (var b in list)
                        if (b != this && b._groupnumber == _groupnumber)
                            b.IsSelected = false;
                }
            }
        }

        internal static NiceButtonStbText GetSelected(Control asa, int group)
        {
            IEnumerable<NiceButtonStbText> list = asa.FindControls<NiceButtonStbText>();

            foreach (var b in list)
                if (b._groupnumber == group && b.IsSelected)
                    return b;

            return null;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (_action != ButtonAction.SwitchPage)
                {
                    IsSelected = true;
                    bool found = false;
                    for (int i = TextBoxes.Length - 1; i >= 0 && !found; --i)
                    {
                        if (x >= TextBoxes[i].X && x <= TextBoxes[i].X + TextBoxes[i].Width)
                        {
                            TextBoxes[i].Priority = ClickPriority.High;
                            found = true;
                            _SelectedArea = (byte)(i + 1);
                        }
                    }
                    if (!found)
                        _SelectedArea = 0;

                    OnButtonClick(ButtonParameter);
                }
                else
                    ChangePage(ButtonParameter);
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);
            if (button == MouseButtonType.Left)
            {
                if (_action != ButtonAction.SwitchPage)
                {
                    for (int i = TextBoxes.Length - 1; i >= 0; --i)
                    {
                        if (x >= TextBoxes[i].X && x <= TextBoxes[i].X + TextBoxes[i].Width)
                        {
                            TextBoxes[i].Priority = ClickPriority.High;
                            break;
                        }
                    }
                }
            }
        }

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            //float layerDepth = layerDepthRef;

            bool draw = base.Draw(batcher, x, y);// base.AddToRenderLists(renderLists, x, y, ref layerDepth);
            if (IsSelected)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha, true);

                /*renderLists.AddGumpNoAtlas(
                    batcher =>
                    {*/
                        if (_SelectedArea > 0)
                            batcher.Draw(_texture, new Vector2(x + TextBoxes[_SelectedArea - 1].X, y), new Rectangle(0, 0, TextBoxes[_SelectedArea - 1].Width, Height), hueVector);//, layerDepth);
                        else if (TextLabel != null)
                            batcher.Draw(_texture, new Vector2(x + TextLabel.X, y), new Rectangle(0, 0, TextLabel.Width, Height), hueVector);//, layerDepth);
                        else
                            batcher.Draw(_texture, new Vector2(x + TextBoxes[0].X, y), new Rectangle(0, 0, TextBoxes[0].Width, Height), hueVector);//, layerDepth);

                        /*return true;
                    }
                );*/
            }
            return draw;
        }
    }
}
