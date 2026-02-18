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

using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistScrollArea : Control
    {
        private AssistScrollBar _scrollBar;
        private int _visibleHeight;

        public AssistScrollArea
        (
            int x,
            int y,
            int w,
            int h
        )
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _visibleHeight = h;

            AcceptMouseInput = true;
            WantUpdateSize = false;
            CanMove = false;
            ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
        }

        public ScrollbarBehaviour ScrollbarBehaviour { get; set; }

        public override void Update()
        {
            base.Update();
            if (_scrollBar == null || _scrollBar.IsDisposed) return;

            if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
            {
                _scrollBar.IsVisible = true;
            }
            else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
            {
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }

            if (AreaChanged)
            {
                AreaChanged = false;
                OnRefresh();
            }
        }

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_scrollBar == null || _scrollBar.IsDisposed)
                return true;

            //_scrollBar.AddToRenderLists(renderLists, x + _scrollBar.X, y + _scrollBar.Y, ref layerDepthRef);
            _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);
            //float layerDepth = layerDepthRef;

            /*renderLists.AddGumpNoAtlas(
                batcher =>
                {*/
                    if (batcher.ClipBegin(x, y, Width - 14, _visibleHeight))
                    {
                        //RenderLists childRenderLists = new();

                        for (int i = 1; i < Children.Count; i++)
                        {
                            Control child = Children[i];

                            if (!child.IsVisible)
                            {
                                continue;
                            }

                            int finalY = y + child.Y - _scrollBar.Value;

                            //child.AddToRenderLists(childRenderLists, x + child.X, finalY, ref layerDepth);
                            child.Draw(batcher, x + child.X, finalY);
                        }
                        //childRenderLists.DrawRenderLists(batcher, sbyte.MaxValue);
                        batcher.ClipEnd();
                    }
                    /*return true;
                }
            );*/

            return true;
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;

                    break;

                case MouseEventType.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;

                    break;
            }
        }

        public override void Remove(Control c)
        {
            base.Remove(c);
            AreaChanged = true;
        }

        internal bool AreaChanged = true;
        public override T Add<T>(T c, int page = 0)
        {
            if(Children.Count == 0 || _scrollBar == null || _scrollBar.IsDisposed)
            {
                _scrollBar = new AssistScrollBar(Width - 14, 0, Height);
                _scrollBar.MinValue = _scrollBar.MaxValue = 0;
                _scrollBar.Parent = this;
                _scrollBar.ValueChanged += ScrollBar_ValueChanged;
            }
            c.Parent = this;
            AreaChanged = true;
            return c;
        }

        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            CalculateScrollBarMaxValue();
        }

        public override void Clear()
        {
            for (int i = 1; i < Children.Count; i++)
            {
                Children[i]
                    .Dispose();
            }
        }

        public void OnRefresh()
        {
            ReArrangeChildren();
            _scrollBar.IsVisible = Height > _visibleHeight;
            CalculateScrollBarMaxValue();
        }

        private void CalculateScrollBarMaxValue()
        {
            if (_scrollBar == null || _scrollBar.IsDisposed) return;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

            int startY = 0, endY = 0;

            for (int i = 1; i < Children.Count; i++)
            {
                Control c = Children[i];

                if (c.IsVisible && !c.IsDisposed)
                {
                    if (c.Y < startY)
                    {
                        startY = c.Y;
                    }

                    if (c.Bounds.Bottom > endY)
                    {
                        endY = c.Bounds.Bottom;
                    }
                }
            }

            _scrollBar.MaxValue = endY;

            int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;

            if (height > 0)
            {
                _scrollBar.MaxValue = height;

                if (maxValue)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
            }
            else
            {
                _scrollBar.Value = _scrollBar.MaxValue = 0;
            }

            for (int i = 1; i < Children.Count; i++)
            {
                Control c = Children[i];
                if(c.IsVisible && !c.IsDisposed)
                    c.UpdateOffset(0, -_scrollBar.Value);
            }
        }

        private void ReArrangeChildren()
        {
            for (int i = 1, height = 0; i < Children.Count; ++i)
            {
                Control c = Children[i];
                if (c.IsVisible && !c.IsDisposed)
                {
                    c.Y = height;
                    height += c.Height;
                }
            }
        }
    }
}