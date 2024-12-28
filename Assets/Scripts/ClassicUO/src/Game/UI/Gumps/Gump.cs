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
using System.IO;
using System.Xml;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Gump : Control
    {
        // MobileUO: added variables
        private Button closeButton;
        public static bool CloseButtonsEnabled;
        
        public Gump(uint local, uint server)
        {
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        // MobileUO: NOTE: Make sure close button is always on top
        // MobileUO: added function
        protected override void OnChildAdded()
        {
            UpdateCloseButton();
        }

        // MobileUO: added function
        private void InitCloseButton()
        {
            if ((closeButton == null || closeButton.IsDisposed) && CloseButtonsEnabled && (CanCloseWithRightClick || CanCloseWithEsc))
            {
                closeButton = new Button(MOBILE_CLOSE_BUTTON_ID, 1150, 1152, 1151);
                closeButton.Width = (int) Math.Round(closeButton.Width * 1.25f);
                closeButton.Height = (int) Math.Round(closeButton.Height * 1.5f);
                closeButton.ContainsByBounds = true;
                closeButton.ButtonAction = ButtonAction.Activate;
            }
        }

        // MobileUO: added function
        public void UpdateCloseButton()
        {
            InitCloseButton();
            if (closeButton != null)
            {
                closeButton.IsEnabled = CloseButtonsEnabled && (CanCloseWithRightClick || CanCloseWithEsc);
                closeButton.IsVisible = closeButton.IsEnabled;
                //Force insert closeButton, might be needed if it was somehow removed from Children in the meanwhile
                if (closeButton.Parent != this)
                {
                    closeButton.Parent = this;
                }
            }
        }

        public bool BlockMovement { get; set; }

        public bool CanBeSaved => GumpType != Gumps.GumpType.None;

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }


        public override bool CanMove
        {
            get => !BlockMovement && base.CanMove;
            set => base.CanMove = value;
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (InvalidateContents)
            {
                UpdateContents();
                InvalidateContents = false;
            }

            if (ActivePage == 0)
            {
                ActivePage = 1;
            }

            base.Update(totalTime, frameTime);
        }

        public override void Dispose()
        {
            Item it = World.Items.Get(LocalSerial);

            if (it != null && it.Opened)
            {
                it.Opened = false;
            }

            base.Dispose();

            // MobileUO: added dispose
            if (closeButton != null && closeButton.IsDisposed == false)
            {
                closeButton.Dispose();
                closeButton = null;
            }
        }

        public virtual void Save(BinaryWriter writer)
        {
            // the header         
            Type type = GetType();
            ushort typeLen = (ushort) type.FullName.Length;
            writer.Write(typeLen);
            writer.WriteUTF8String(type.FullName);
            writer.Write(X);
            writer.Write(Y);
        }

        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int) GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());
        }

        public void SetInScreen()
        {
            if (Bounds.Width >= 0 &&
                Bounds.X <= Client.Game.Window.ClientBounds.Width &&
                Bounds.Height >= 0 &&
                Bounds.Y <= Client.Game.Window.ClientBounds.Height)
            {
                return;
            }

            X = 0;
            Y = 0;
        }

        public virtual void Restore(BinaryReader reader)
        {
        }

        public virtual void Restore(XmlElement xml)
        {
        }

        public void RequestUpdateContents()
        {
            InvalidateContents = true;
        }

        protected virtual void UpdateContents()
        {
        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width - (Width >> 2);
            int halfHeight = Height - (Height >> 2);

            if (X < -halfWidth)
            {
                position.X = -halfWidth;
            }

            if (Y < -halfHeight)
            {
                position.Y = -halfHeight;
            }

            if (X > Client.Game.Window.ClientBounds.Width - (Width - halfWidth))
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - halfWidth);
            }

            if (Y > Client.Game.Window.ClientBounds.Height - (Height - halfHeight))
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - halfHeight);
            }

            Location = position;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return IsVisible && base.Draw(batcher, x, y);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0 && !SerialHelper.IsValidLocalGumpSerial(LocalSerial))
            {
                List<uint> switches = new List<uint>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case StbTextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump(LocalSerial, ServerSerial, buttonID, switches.ToArray(), entries.ToArray());

                if (CanMove)
                {
                    UIManager.SavePosition(ServerSerial, Location);
                }
                else
                {
                    UIManager.RemovePosition(ServerSerial);
                }

                Dispose();
            }
        }

        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
            {
                return;
            }

            if (ServerSerial != 0)
            {
                OnButtonClick(0);
            }

            base.CloseWithRightClick();
        }

        public override void ChangePage(int pageIndex)
        {
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
        }
    }
}