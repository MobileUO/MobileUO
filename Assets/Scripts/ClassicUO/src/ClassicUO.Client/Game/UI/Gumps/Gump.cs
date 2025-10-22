// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    public class Gump : Control
    {
        // MobileUO: added variables
        private Button closeButton;
        public static bool CloseButtonsEnabled;

        public Gump(World world, uint local, uint server)
        {
            World = world;
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

        public World World { get; }

        public bool CanBeSaved => GumpType != Gumps.GumpType.None;

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }

        public uint MasterGumpSerial { get; set; }


        public override void Update()
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

            base.Update();
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


        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int) GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());
        }

        public void CenterXInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                // Convert physical width to unscaled (logical) width
                float logicalWidth = windowBounds.Width / scale;
                // Center in logical coordinates
                X = (int)((logicalWidth - Width) / 2);
            }
            else
            {
                X = (windowBounds.Width - Width) / 2;
            }
        }

        public void CenterYInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                float logicalHeight = windowBounds.Height / scale;
                Y = (int)((logicalHeight - Height) / 2);
            }
            else
            {
                Y = (windowBounds.Height - Height) / 2;
            }
        }

        public void CenterXInViewPort()
        {
            var camera = Client.Game.Scene.Camera;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                // Compute the camera's physical center, then convert to logical coordinates.
                float logicalCenterX = (camera.Bounds.X + camera.Bounds.Width / 2f);
                // Set element X so that its center aligns with the camera's logical center.
                X = (int)(logicalCenterX - ((Width / scale) / 2f));
            }
            else
            {
                X = camera.Bounds.X + ((camera.Bounds.Width - Width) / 2);
            }
        }

        public void CenterYInViewPort()
        {
            var camera = Client.Game.Scene.Camera;
            if (ProfileManager.CurrentProfile.GlobalScaling)
            {
                float scale = ProfileManager.CurrentProfile.GlobalScale;
                float logicalCenterY = (camera.Bounds.Y + camera.Bounds.Height / 2f);
                Y = (int)(logicalCenterY - ((Height / scale) / 2f));
            }
            else
            {
                Y = camera.Bounds.Y + ((camera.Bounds.Height - Height) / 2);
            }
        }

        public void SetInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            Rectangle bounds = Bounds;
            bounds.X += windowBounds.X;
            bounds.Y += windowBounds.Y;

            if (windowBounds.Intersects(bounds))
            {
                return;
            }

            X = 0;
            Y = 0;
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
            if (!IsDisposed && LocalSerial != 0)
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

                GameActions.ReplyGump
                (
                    LocalSerial,
                    // Seems like MasterGump serial does not work as expected.
                    /*MasterGumpSerial != 0 ? MasterGumpSerial :*/ ServerSerial,
                    buttonID,
                    switches.ToArray(),
                    entries.ToArray()
                );

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