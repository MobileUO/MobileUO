// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
// MobileUO: added import
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldViewportGump : Gump
    {
        public const int BORDER_WIDTH = 5;
        private readonly BorderControl _borderControl;
        private readonly Button _button;
        private bool _clicked;
        private Point _lastSize,
            _savedSize;
        private readonly GameScene _scene;
        private readonly SystemChatControl _systemChatControl;

        public WorldViewportGump(World world, GameScene scene) : base(world, 0, 0)
        {
            _scene = scene;
            AcceptMouseInput = false;
            CanMove = !ProfileManager.CurrentProfile.GameWindowLock;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            LayerOrder = UILayer.Under;
            // MobileUO: don't adjust position of camera by border
            X = scene.Camera.Bounds.X /*- BORDER_WIDTH*/;
            Y = scene.Camera.Bounds.Y /*- BORDER_WIDTH*/;
            _savedSize = _lastSize = new Point(
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            _button = new Button(0, 0x837, 0x838, 0x838);
            
            // MobileUO: Upscale resize button on mobile
            if (UnityEngine.Application.isMobilePlatform)
            {
                _button.Width *= 2;
                _button.Height *= 2;
                _button.ContainsByBounds = true;
            }

            _button.MouseDown += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    _clicked = true;
                }
            };

            _button.MouseUp += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    Point n = ResizeGameWindow(_lastSize);

                    UIManager.GetGump<OptionsGump>()?.UpdateVideo();

                    if (Client.Game.UO.Version >= ClientVersion.CV_200)
                    {
                        NetClient.Socket.Send_GameWindowSize((uint)n.X, (uint)n.Y);
                    }

                    _clicked = false;
                }
            };

            _button.SetTooltip(ResGumps.ResizeGameWindow);
            Width = scene.Camera.Bounds.Width + BORDER_WIDTH * 2;
            Height = scene.Camera.Bounds.Height + BORDER_WIDTH * 2;

            _borderControl = new BorderControl(0, 0, Width, Height, 4);

            _borderControl.DragEnd += (sender, e) =>
            {
                UIManager.GetGump<OptionsGump>()?.UpdateVideo();
            };

            UIManager.SystemChat = _systemChatControl = new SystemChatControl(
                this,
                BORDER_WIDTH,
                BORDER_WIDTH,
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            Add(_borderControl);
            Add(_button);
            Add(_systemChatControl);
            Resize();
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (Mouse.IsDragging)
            {
                Point offset = Mouse.LDragOffset;

                _lastSize = _savedSize;

                if (_clicked && offset != Point.Zero)
                {
                    int w = _lastSize.X + offset.X;
                    int h = _lastSize.Y + offset.Y;

                    // MobileUO: use MinimumViewportWidth and MinimumViewportHeight variables
                    if (w < GameScene.MinimumViewportWidth)
                        w = GameScene.MinimumViewportWidth;

                    if (h < GameScene.MinimumViewportHeight)
                        h = GameScene.MinimumViewportHeight;

                    if (w > Client.Game.Window.ClientBounds.Width - BORDER_WIDTH)
                    {
                        w = Client.Game.Window.ClientBounds.Width - BORDER_WIDTH;
                    }

                    if (h > Client.Game.Window.ClientBounds.Height - BORDER_WIDTH)
                    {
                        h = Client.Game.Window.ClientBounds.Height - BORDER_WIDTH;
                    }

                    _lastSize.X = w;
                    _lastSize.Y = h;
                }

                if (
                    _scene.Camera.Bounds.Width != _lastSize.X
                    || _scene.Camera.Bounds.Height != _lastSize.Y
                )
                {
                    Width = _lastSize.X + BORDER_WIDTH * 2;
                    Height = _lastSize.Y + BORDER_WIDTH * 2;
                    _scene.Camera.Bounds.Width = _lastSize.X;
                    _scene.Camera.Bounds.Height = _lastSize.Y;

                    Resize();
                }
            }
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);

            Point position = Location;

            if (position.X + Width - BORDER_WIDTH > Client.Game.Window.ClientBounds.Width)
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - BORDER_WIDTH);
            }

            if (position.X < -BORDER_WIDTH)
            {
                position.X = -BORDER_WIDTH;
            }

            if (position.Y + Height - BORDER_WIDTH > Client.Game.Window.ClientBounds.Height)
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - BORDER_WIDTH);
            }

            if (position.Y < -BORDER_WIDTH)
            {
                position.Y = -BORDER_WIDTH;
            }

            Location = position;
            // MobileUO: don't adjust position of camera by border
            _scene.Camera.Bounds.X = position.X /*+ BORDER_WIDTH*/;
            _scene.Camera.Bounds.Y = position.Y /*+ BORDER_WIDTH*/;

            UIManager.GetGump<OptionsGump>()?.UpdateVideo();
            UpdateGameWindowPos();
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            // MobileUO: don't adjust position of camera by border
            _scene.Camera.Bounds.X = ScreenCoordinateX /*+ BORDER_WIDTH*/;
            _scene.Camera.Bounds.Y = ScreenCoordinateY /*+ BORDER_WIDTH*/;

            // MobileUO: need to save the position of the game window any time it moves - otherwise mouse will be incorrect in game space
            ProfileManager.CurrentProfile.GameWindowPosition = new Point(
                Math.Max(0, _scene.Camera.Bounds.X),
                Math.Max(0, _scene.Camera.Bounds.Y)
            );

            UpdateGameWindowPos();
        }

        private void UpdateGameWindowPos()
        {
            if (_scene != null)
            {
                _scene.UpdateDrawPosition = true;
            }
        }

        private void Resize()
        {
            _borderControl.Width = Width;
            _borderControl.Height = Height;
            _button.X = Width - (_button.Width >> 1);
            _button.Y = Height - (_button.Height >> 1);
            _scene.Camera.Bounds.Width = _systemChatControl.Width = Width - BORDER_WIDTH * 2;
            _scene.Camera.Bounds.Height = _systemChatControl.Height = Height - BORDER_WIDTH * 2;
            _systemChatControl.Resize();
            WantUpdateSize = true;

            UpdateGameWindowPos();
        }

        public void SetGameWindowPosition(Point pos)
        {
            Location = pos;

            // MobileUO: don't adjust position of camera by border
            _scene.Camera.Bounds.X = ScreenCoordinateX /*+ BORDER_WIDTH*/;
            _scene.Camera.Bounds.Y = ScreenCoordinateY /*+ BORDER_WIDTH*/;

            UpdateGameWindowPos();
        }

        public Point ResizeGameWindow(Point newSize)
        {
            // MobileUO: use MinimumViewportWidth and MinimumViewportHeight variables
            if (newSize.X < GameScene.MinimumViewportWidth)
                newSize.X = GameScene.MinimumViewportWidth;

            if (newSize.Y < GameScene.MinimumViewportHeight)
                newSize.Y = GameScene.MinimumViewportHeight;

            //Resize();
            _lastSize = _savedSize = newSize;

            if (
                _scene.Camera.Bounds.Width != _lastSize.X
                || _scene.Camera.Bounds.Height != _lastSize.Y
            )
            {
                _scene.Camera.Bounds.Width = _lastSize.X;
                _scene.Camera.Bounds.Height = _lastSize.Y;
                Width = _scene.Camera.Bounds.Width + BORDER_WIDTH * 2;
                Height = _scene.Camera.Bounds.Height + BORDER_WIDTH * 2;

                Resize();
            }

            return newSize;
        }

        public override bool Contains(int x, int y)
        {
            if (
                x >= BORDER_WIDTH
                && x < Width - BORDER_WIDTH * 2
                && y >= BORDER_WIDTH
                && y
                    < Height
                        - BORDER_WIDTH * 2
                        - (
                            _systemChatControl?.TextBoxControl != null
                            && _systemChatControl.IsActive
                                ? _systemChatControl.TextBoxControl.Height
                                : 0
                        )
            )
            {
                return false;
            }

            return base.Contains(x, y);
        }
    }

    internal class BorderControl : Control
    {
        private int _borderSize;

        private ushort h_border = 0x0A8C;
        private ushort v_border = 0x0A8D;
        private ushort h_bottom_border = 0x0A8C;
        private ushort v_right_border = 0x0A8D;
        private ushort t_left = 0xffff, t_right = 0xffff, b_left = 0xffff, b_right = 0xffff;

        public int BorderSize { get { return _borderSize; } set { _borderSize = value; } }
        public ushort H_Border { get { return h_border; } set { h_border = value; } }
        public ushort V_Border { get { return v_border; } set { v_border = value; } }
        public ushort V_Right_Border { get { return v_right_border; } set { v_right_border = value; } }
        public ushort H_Bottom_Border { get { return h_bottom_border; } set { h_bottom_border = value; } }
        public ushort T_Left { get { return t_left; } set { t_left = value; } }
        public ushort T_Right { get { return t_right; } set { t_right = value; } }
        public ushort B_Left { get { return b_left; } set { b_left = value; } }
        public ushort B_Right { get { return b_right; } set { b_right = value; } }

        public BorderControl(int x, int y, int w, int h, int borderSize)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _borderSize = borderSize;
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Hue { get; set; }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            if (Hue != 0)
            {
                hueVector.X = Hue;
                hueVector.Y = 1;
            }

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(h_border);

            // sopra
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(x, y, Width, _borderSize),
                gumpInfo.UV,
                hueVector
            );

            // sotto
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(x, y + Height - _borderSize, Width, _borderSize),
                gumpInfo.UV,
                hueVector
            );

            gumpInfo = ref Client.Game.UO.Gumps.GetGump(v_border);
            //sx
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(x, y, _borderSize, Height),
                gumpInfo.UV,
                hueVector
            );

            //dx
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(
                    x + Width - _borderSize,
                    y + (gumpInfo.UV.Width >> 1),
                    _borderSize,
                    Height - _borderSize
                ),
                gumpInfo.UV,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }
    }
}
