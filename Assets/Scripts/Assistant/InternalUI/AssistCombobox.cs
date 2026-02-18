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
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistCombobox : Control
    {
        private readonly byte _font;
        private readonly Label _label;
        private int _selectedIndex;
        private string[] _items;
        private int _maxHeight;

        public AssistCombobox(int x, int y, int width, string[] items, int selected = -1, int maxHeight = 200, bool showArrow = true, string emptyString = "", byte font = 1)
        {
            X = x;
            Y = y;
            Width = width;
            Height = 25;
            SelectedIndex = selected;
            _font = font;
            _items = items;
            _maxHeight = maxHeight;

            Add
            (
                new ResizePic(0x0BB8)
                {
                    Width = width, Height = Height,
                }
            );

            string initialText = selected > -1 ? items[selected] : emptyString;

            Add
            (
                _label = new Label(initialText, true, ScriptTextBox.WHITE_DARK, width - 18, font, FontStyle.BlackBorder | FontStyle.ExtraHeight, TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = 2, Y = 5
                }
            );

            if (showArrow)
            {
                Add(new GumpPic(width - 18, 2, 0x00FC, 0));
            }
        }


        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;

                if (_items != null && value >= 0 && value < _items.Length)
                {
                    _label.Text = _items[value];

                    OnOptionSelected?.Invoke(this, value);
                }
            }
        }


        public event EventHandler<int> OnOptionSelected;

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            /*float layerDepth = layerDepthRef;
            renderLists.AddGumpNoAtlas
            (
                (batcher) =>
                {
                    // work-around to allow clipping children
                    RenderLists comboBoxRenderLists = new();
                    base.AddToRenderLists(comboBoxRenderLists, x, y, ref layerDepth);*/

            if (batcher.ClipBegin(x, y, Width, Height))
            {
                base.Draw(batcher, x, y);
                //comboBoxRenderLists.DrawRenderLists(batcher, sbyte.MaxValue);
                batcher.ClipEnd();
            }
                /*return true;
                }
            );*/

            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            OnBeforeContextMenu?.Invoke(this, null);

            UIManager.Add(new ComboboxGump(ScreenCoordinateX, ScreenCoordinateY + Offset.Y, Width + 10, _maxHeight, _items, _font, this));

            base.OnMouseUp(x, y, button);
        }

        internal string GetItem(int idx)
        {
            if (idx >= 0 && idx < _items.Length)
                return _items[idx];
            return null;
        }

        internal uint GetItemsLength => (uint)_items.Length;

        internal void SetItemsValue(string[] items)
        {
            _items = items;
        }

        public event EventHandler OnBeforeContextMenu;

        class ComboboxGump : Gump
        {
            private AssistCombobox _combobox;

            public ComboboxGump(int x, int y, int width, int maxHeight, string[] items, byte font, AssistCombobox combobox) : base(ClassicUO.Client.Game.UO.World, 0, 0)
            {
                CanMove = false;
                AcceptMouseInput = true;
                X = x;
                Y = y;

                IsModal = true;
                LayerOrder = UILayer.Over;
                ModalClickOutsideAreaClosesThisControl = true;

                _combobox = combobox;

                ResizePic background;
                Add(background = new ResizePic(0x0BB8));
                background.AcceptMouseInput = false;

                HoveredLabel[] labels = new HoveredLabel[items.Length];

                for (int i = 0; i < items.Length; i++)
                {
                    string item = items[i];

                    if (item == null)
                    {
                        item = string.Empty;
                    }

                    HoveredLabel label = new HoveredLabel(item, true, ScriptTextBox.BLUE_HUE, ScriptTextBox.GREEN_HUE, ScriptTextBox.WHITE_DARK, width, font, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
                    {
                        X = 1,
                        //Y = i * ELEMENT_HEIGHT,
                        DrawBackgroundCurrentIndex = true,
                        IsVisible = item.Length != 0,
                        Tag = i
                    };

                    label.MouseUp += LabelOnMouseUp;

                    labels[i] = label;
                }

                int totalHeight = Math.Min(maxHeight, labels.Sum(l => l.Height));
                int maxWidth = labels.Length > 0 ? Math.Max(width, labels.Max(o => o.X + o.Width)) : width;

                AssistScrollArea area = new AssistScrollArea(0, 0, maxWidth + 15, totalHeight);

                foreach (HoveredLabel label in labels)
                {
                    label.Width = maxWidth;
                    area.Add(label);
                }

                Add(area);

                background.Width = maxWidth;
                background.Height = totalHeight;
            }

            private void LabelOnMouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _combobox.SelectedIndex = (int)((Label)sender).Tag;

                    Dispose();
                }
            }
        }
    }
}
