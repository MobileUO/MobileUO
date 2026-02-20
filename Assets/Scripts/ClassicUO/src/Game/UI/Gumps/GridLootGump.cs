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

using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridLootGump : Gump
    {
        private readonly AlphaBlendControl _background;
        private readonly NiceButton _buttonPrev, _buttonNext, _setlootbag,
            // MobileUO: added close button
            _buttonClose;
        private readonly Item _corpse;
        private readonly Label _currentPageLabel;
        private readonly Label _corpseNameLabel;
        private readonly bool _hideIfEmpty;

        private int _currentPage = 1;
        private int _pagesCount;

        private static int _lastX = ProfileManager.Current.GridLootType == 2 ? 200 : 100;
        private static int _lastY = 100;

        private const int MAX_WIDTH = 300;
        private const int MAX_HEIGHT = 420;

        public GridLootGump(uint local) : base(local, 0)
        {
            _corpse = World.Items.Get(local);

            if (_corpse == null)
            {
                Dispose();

                return;
            }

            if (World.Player.ManualOpenedCorpses.Contains(LocalSerial))
                World.Player.ManualOpenedCorpses.Remove(LocalSerial);
            else if (World.Player.AutoOpenedCorpses.Contains(LocalSerial) &&
                     ProfileManager.Current != null && ProfileManager.Current.SkipEmptyCorpse)
            {
                IsVisible = false;
                _hideIfEmpty = true;
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            _background = new AlphaBlendControl();
            //_background.Width = MAX_WIDTH;
            //_background.Height = MAX_HEIGHT;
            Add(_background);

            Width = _background.Width;
            Height = _background.Height;

            _setlootbag = new NiceButton(3, Height - 23, 100, 20, ButtonAction.Activate, "Set loot bag") { ButtonParameter = 2, IsSelectable = false };
            Add(_setlootbag);

            _buttonPrev = new NiceButton(Width - 80, Height - 20, 40, 20, ButtonAction.Activate, "<<") {ButtonParameter = 0, IsSelectable = false};
            _buttonNext = new NiceButton(Width - 40, Height - 20, 40, 20, ButtonAction.Activate, ">>") {ButtonParameter = 1, IsSelectable = false};

            _buttonNext.IsVisible = _buttonPrev.IsVisible = false;


            Add(_buttonPrev);
            Add(_buttonNext);

            // MobileUO: added close button
            _buttonClose = new NiceButton(
                0,
                0,
                40,
                40,
                ButtonAction.Activate,
                "X"
            )
            {
                ButtonParameter = 3,
                IsSelectable = false
            };

            Add(_buttonClose);

            Add(_currentPageLabel = new Label("1", true, 999, align: IO.Resources.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = Width / 2 - 5,
                Y = Height - 20,
            });

            Add(
                _corpseNameLabel = new Label(
                    GetCorpseName(),
                    true,
                    0x0481,
                    align: TEXT_ALIGN_TYPE.TS_CENTER,
                    maxwidth: 300
                )
                {
                    Width = 300,
                    X = 0,
                    Y = 0
                }
            );
        }

      
        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _currentPage--;

                if (_currentPage <= 1)
                {
                    _currentPage = 1;
                    _buttonPrev.IsVisible = false;
                }
                _buttonNext.IsVisible = true;
                ChangePage(_currentPage);

                _currentPageLabel.Text = ActivePage.ToString();
                _currentPageLabel.X = Width / 2 - _currentPageLabel.Width / 2;
            }
            else if (buttonID == 1)
            {
                _currentPage++;

                if (_currentPage >= _pagesCount)
                {
                    _currentPage = _pagesCount;
                    _buttonNext.IsVisible = false;
                }

                _buttonPrev.IsVisible = true;

                ChangePage(_currentPage);

                _currentPageLabel.Text = ActivePage.ToString();
                _currentPageLabel.X = Width / 2 - _currentPageLabel.Width / 2;
            }
            else if (buttonID == 2)
            {
                GameActions.Print("Target the container to Grab items into.");
                TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            }
            // MobileUO: added close button
            else if (buttonID == 3)
            {
                Dispose();
            }
            else
                base.OnButtonClick(buttonID);
        }



        protected override void UpdateContents()
        {
            const int GRID_ITEM_SIZE = 50;

            int x = 20;
            int y = 20;

            foreach (GridLootItem gridLootItem in Children.OfType<GridLootItem>())
                gridLootItem.Dispose();

            int count = 0;
            _pagesCount = 1;

            _background.Width = x;
            _background.Height = y;

            int line = 1;
            int row = 0;

            for (var i = _corpse.Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                if (it.IsLootable)
                {
                    GridLootItem gridItem = new GridLootItem(this, it, GRID_ITEM_SIZE);

                    if (x >= MAX_WIDTH - 20)
                    {
                        x = 20;
                        ++line;

                        y += gridItem.Height + 20;

                        if (y >= MAX_HEIGHT - 60)
                        {
                            _pagesCount++;
                            y = 20;
                            //line = 1;
                        }
                    }

                    gridItem.X = x;
                    gridItem.Y = y + 20;
                    Add(gridItem, _pagesCount);

                    x += gridItem.Width + 20;
                    ++row;
                    ++count;
                }
            }

            _background.Width = (GRID_ITEM_SIZE + 20) * row + 20;
            _background.Height = 20 + 40 + (GRID_ITEM_SIZE + 20) * line + 40;


            if (_background.Height >= MAX_HEIGHT - 40)
            {
                _background.Height = MAX_HEIGHT;
            }

            _background.Width = MAX_WIDTH;

            if (ActivePage <= 1)
            {
                ActivePage = 1;
                _buttonNext.IsVisible = _pagesCount > 1;
                _buttonPrev.IsVisible = false;
            }
            else if (ActivePage >= _pagesCount)
            {
                ActivePage = _pagesCount;
                _buttonNext.IsVisible = false;
                _buttonPrev.IsVisible = _pagesCount > 1;
            }
            else if (ActivePage > 1 && ActivePage < _pagesCount)
            {
                _buttonNext.IsVisible = true;
                _buttonPrev.IsVisible = true;
            }

            if (count == 0)
            {
                GameActions.Print("[GridLoot]: Corpse is empty!");
                Dispose();
            }
            else if ((_hideIfEmpty && !IsVisible))
            {
                IsVisible = true;
            }
        }

        public override void Dispose()
        {
            if (_corpse != null)
            {
                if (_corpse == SelectedObject.CorpseObject)
                    SelectedObject.CorpseObject = null;
            }

            _lastX = X;
            _lastY = Y;

            base.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible || IsDisposed)
                return false;

            ResetHueVector();
            base.Draw(batcher, x, y);
            ResetHueVector();
            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            return true;
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (_corpse == null || _corpse.IsDestroyed || _corpse.OnGround && _corpse.Distance > 3)
            {
                Dispose();

                return;
            }

            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (_background.Width < 100)
                _background.Width = 100;
            if (_background.Height < 120)
                _background.Height = 120;

            Width = _background.Width;
            Height = _background.Height;

            _buttonPrev.X = Width - 80;
            _buttonPrev.Y = Height - 23;
            _buttonNext.X = Width - 40;
            _buttonNext.Y = Height - 20;
            _setlootbag.X = 3;
            _setlootbag.Y = Height - 23;
            _currentPageLabel.X = Width / 2 - 5;
            _currentPageLabel.Y = Height - 20;

            // MobileUO: added close button
            _buttonClose.X = Width - _buttonClose.Width - 3;
            _buttonClose.Y = 3;

            _corpseNameLabel.Text = GetCorpseName();

            WantUpdateSize = true;

            if (_corpse != null && !_corpse.IsDestroyed && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = _corpse;
                SelectedObject.LastObject = _corpse;
                SelectedObject.CorpseObject = _corpse;
            }
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (_corpse != null && !_corpse.IsDestroyed) SelectedObject.CorpseObject = null;
        }

        private string GetCorpseName()
        {
            return _corpse.Name?.Length > 0 ? _corpse.Name : "a corpse";
        }

        // MobileUO: only loot item if user clicks it twice
        private uint _selectedItemSerial; // 0 = none
        internal uint SelectedItemSerial => _selectedItemSerial;

        internal void HandleItemClick(uint serial, Item item, ushort amount)
        {
            if (serial == 0 || item == null)
                return;

            if (_selectedItemSerial == serial || !ProfileManager.Current.DoubleClickForGridLoot)
            {
                // second click on the same item -> loot it
                GameActions.GrabItem(item, amount);
                _selectedItemSerial = 0;
            }
            else
            {
                // first click on this item -> set as selected
                _selectedItemSerial = serial;
            }
        }

        private class GridLootItem : Control
        {
            private readonly TextureControl _texture;
            private readonly GridLootGump _gump;

            public GridLootItem(GridLootGump gump, uint serial, int size)
            {
                _gump = gump;
                LocalSerial = serial;

                Item item = World.Items.Get(serial);

                if (item == null)
                {
                    Dispose();

                    return;
                }

                CanMove = false;

                HSliderBar amount = new HSliderBar(0, 0, size, 1, item.Amount, item.Amount, HSliderBarStyle.MetalWidgetRecessedBar, true, color: 0xFFFF, drawUp: true);
                Add(amount);

                amount.IsVisible = amount.IsEnabled = amount.MaxValue > 1;


                AlphaBlendControl background = new AlphaBlendControl();
                background.Y = 15;
                background.Width = size;
                background.Height = size;
                Add(background);


                _texture = new TextureControl();
                _texture.IsPartial = item.ItemData.IsPartialHue;
                _texture.ScaleTexture = true;
                _texture.Hue = item.Hue;
                _texture.Texture = ArtLoader.Instance.GetTexture(item.DisplayedGraphic);
                _texture.Y = 15;
                _texture.Width = size;
                _texture.Height = size;
                _texture.CanMove = false;

                if (World.ClientFeatures.TooltipsEnabled) _texture.SetTooltip(item);

                Add(_texture);


                _texture.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        // MobileUO: handle item click depending on if double click to loot is enabled
                        var serial = LocalSerial;
                        var item = World.Items.Get(serial);

                        _gump.HandleItemClick(serial, item, (ushort)amount.Value);
                    }
                };

                Width = background.Width;
                Height = background.Height + 15;

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();
                base.Draw(batcher, x, y);
                ResetHueVector();

                batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x, y + 15, Width, Height - 15, ref _hueVector);

                if (_texture.MouseIsOver)
                {
                    _hueVector.Z = 0.7f;
                    batcher.Draw2D(Texture2DCache.GetTexture(Color.Yellow), x + 1, y + 15, Width - 1, Height - 15, ref _hueVector);
                    _hueVector.Z = 0;
                }

                return true;
            }
        }
    }
}