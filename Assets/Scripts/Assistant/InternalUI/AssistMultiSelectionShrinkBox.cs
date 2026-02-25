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
using System.Linq;

using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistMultiSelectionShrinkbox : Control
    {
        private readonly int _buttongroup;
        private readonly ushort _buttonimg, _pressedbuttonimg;
        private readonly Label _label;
        //this particular list will be used when inside a scroll area or similar situations where you want to nest a multi selection shrinkbox inside another one,
        //so that when the parent is deactivated, all the child will be made non visible
        private readonly List<AssistMultiSelectionShrinkbox> _nestedBoxes = new List<AssistMultiSelectionShrinkbox>();
        private readonly GumpPic _arrow;
        private AssistNiceButton[] _buttons;
        private readonly bool _useArrow2;
        private string[] _items;
        private bool _opened;
        private Button[] _pics;
        private int _selectedIndex;

        public AssistMultiSelectionShrinkbox(int x, int y, int width, string indextext, string[] items, ushort hue = 0x0453, bool unicode = false, byte font = 9, int group = 0, ushort button = 0, ushort pressedbutton = 0, bool useArrow2 = false) : this(x, y, width, indextext, hue, unicode, font, group, button, pressedbutton, useArrow2)
        {
            SetItemsValue(items);
        }

        private AssistMultiSelectionShrinkbox(int x, int y, int width, string indextext, ushort hue, bool unicode, byte font, int group, ushort button, ushort pressedbutton, bool userArrow2 = false)
        {
            WantUpdateSize = false;
            X = x;
            Y = y;
            if (button > 0)
            {
                _buttonimg = button;
                if (pressedbutton > 0)
                    _pressedbuttonimg = pressedbutton;
                else
                    _pressedbuttonimg = button;
            }
            _buttongroup = group;
            Width = width;
            _useArrow2 = userArrow2;

            Add(_label = new Label(indextext, unicode, hue, font: font, align: TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 18
            });
            Height = _label.Height;

            Add(_arrow = new GumpPic(1, 1, (ushort)(userArrow2 ? 0x0827 : 0x15E1), 0));
            _arrow.ContainsByBounds = true;

            _arrow.MouseUp += (sender, state) =>
            {
                if (state.Button == MouseButtonType.Left) Opened = !_opened;
            };
        }

        internal bool Opened
        {
            get => _opened;
            set
            {
                if (_opened != value)
                {
                    _opened = value;

                    if (_opened)
                    {
                        _arrow.Graphic = (ushort)(_useArrow2 ? 0x0826 : 0x15E2);
                        OnBeforeContextMenu?.Invoke(this, null);
                        GenerateButtons();

                        foreach (AssistMultiSelectionShrinkbox msb in _nestedBoxes)
                        {
                            msb.IsVisible = true;
                        }
                    }
                    else
                    {
                        _arrow.Graphic = (ushort)(_useArrow2 ? 0x0827 : 0x15E1);
                        ClearButtons();
                        Height = _label.Height;
                        OnAfterContextMenu?.Invoke(this, null);

                        foreach (AssistMultiSelectionShrinkbox msb in _nestedBoxes)
                        {
                            msb.IsVisible = false;
                        }
                    }

                    if (Parent is AssistScrollArea area)
                        area.AreaChanged = true;
                }
            }
        }

        public int SelectedIndex => _selectedIndex;

        public string SelectedName
        {
            get
            {
                if (_items != null && _selectedIndex >= 0 && _selectedIndex < _items.Length)
                    return _items[_selectedIndex];
                return null;
            }
        }

        internal uint GetItemsLength => (uint)_items.Length;

        public string Name => _label == null ? null : _label.Text;

        public AssistMultiSelectionShrinkbox ParentBox { get; private set; }

        internal bool NestBox(AssistMultiSelectionShrinkbox box)
        {
            if (_nestedBoxes.Contains(box))
                return false;

            if (Parent is AssistScrollArea area)
            {
                _arrow.IsVisible = true;
                _nestedBoxes.Add(box);
                box.Width = Width - box.X;
                area.Add(box);
                if (!_opened) box.IsVisible = false;
                box.ParentBox = this;
                area.AreaChanged = true;

                return true;
            }

            return false;
        }

        internal void SetItemsValue(string[] items)
        {
            _items = items;
            if (_opened)
                GenerateButtons();
            _arrow.IsVisible = items.Length > 0 || _nestedBoxes.Count > 0;
        }

        internal void SetItemsValue(Dictionary<int, string> items)
        {
            _items = items.Select(o => o.Value).ToArray();
            if (_opened)
                GenerateButtons();
            _arrow.IsVisible = items.Count > 0 || _nestedBoxes.Count > 0;
        }

        private void GenerateButtons()
        {
            ClearButtons();
            _buttons = new AssistNiceButton[_items.Length];

            if (_buttonimg > 0)
                _pics = new Button[_items.Length];

            var index = 0;
            int width = 0;
            int height = 0;
            int lh = _label.Height + 2;

            foreach (string item in _items)
            {
                int w, h;

                if (_label.Unicode)
                    w = Client.Game.UO.FileManager.Fonts.GetWidthUnicode(_label.Font, item);
                else
                    w = Client.Game.UO.FileManager.Fonts.GetWidthASCII(_label.Font, item);

                if (w > width)
                {
                    if (_label.Unicode)
                        h = Client.Game.UO.FileManager.Fonts.GetHeightUnicode(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
                    else
                        h = Client.Game.UO.FileManager.Fonts.GetHeightASCII(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
                    width = w;
                    height = h + 2;
                }
            }

            foreach (var item in _items)
            {
                var but = new AssistNiceButton(20, index * height + lh, width, height, ButtonAction.Activate, item, _buttongroup, TEXT_ALIGN_TYPE.TS_LEFT) { Tag = index };
                if (_buttonimg > 0)
                {
                    Add(_pics[index] = new Button(index, _buttonimg, _pressedbuttonimg) { X = 6, Y = index * height + lh + 2, ButtonAction = (ButtonAction)0xBEEF, Tag = index });
                    _pics[index].MouseUp += Selection_MouseClick;
                    _pics[index].ContainsByBounds = true;
                }
                but.MouseUp += Selection_MouseClick;
                _buttons[index] = but;
                Add(but);
                index++;
            }

            var totalHeight = _buttons.Length > 0 ? _buttons.Sum(o => o.Height) + lh : lh;

            Height = totalHeight;

            Control p = Parent;
            while (p != null)
            {
                if (p is AssistScrollArea area)
                {
                    area.AreaChanged = true;
                    break;
                }
                p = p.Parent;
            }
        }

        public override T Add<T>(T c, int page = 0)
        {
            c.UpdateOffset(Offset.X, Offset.Y);
            return base.Add(c, page);
        }

        private void ClearButtons()
        {
            if (_buttons != null)
            {
                for (int i = _buttons.Length - 1; i >= 0; --i)
                {
                    _buttons[i]?.Dispose();
                    _buttons[i] = null;
                }
            }

            if (_pics != null)
            {
                for (int i = _pics.Length - 1; i >= 0; --i)
                {
                    _pics[i]?.Dispose();
                    _pics[i] = null;
                }
            }
            Control p = Parent;
            while (p != null)
            {
                if (p is AssistScrollArea area)
                {
                    area.AreaChanged = true;
                    break;
                }
                p = p.Parent;
            }
        }

        private void Selection_MouseClick(object sender, MouseEventArgs e)
        {
            if (sender is Control c)
            {
                _selectedIndex = (int)c.Tag;
                if (sender is Button)
                    _buttons[SelectedIndex].IsSelected = true;
                if (_buttongroup > 0)
                    OnGroupSelection();
                if (_items != null && _selectedIndex >= 0 && _selectedIndex < _items.Length) OnOptionSelected?.Invoke(this, c);
            }
        }

        private void OnGroupSelection()
        {
            if (Parent != null && Parent is AssistScrollArea area)
            {
                var list = area.FindControls<AssistMultiSelectionShrinkbox>();
                foreach (AssistMultiSelectionShrinkbox msb in list)
                {
                    if (msb._buttongroup == _buttongroup && msb != this && msb._buttons != null)
                    {
                        foreach (AssistNiceButton button in msb._buttons)
                        {
                            if (button != null)
                                button.IsSelected = false;
                        }
                    }
                }
            }
        }

        public event EventHandler<Control> OnOptionSelected;
        public event EventHandler OnBeforeContextMenu;
        public event EventHandler OnAfterContextMenu;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (_label.Bounds.Contains(Mouse.Position.X - ScreenCoordinateX, Mouse.Position.Y - ScreenCoordinateY) && button == MouseButtonType.Left)
                Opened = !_opened;

            return base.OnMouseDoubleClick(x, y, button);
        }
    }
}
