// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class Line : Control
    {
        private readonly Texture2D _texture;

        public Line(int x, int y, int w, int h, uint color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            _texture = SolidColorTextureCache.GetTexture(new Color { PackedValue = color });
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            batcher.Draw
            (
                _texture,
                new Rectangle
                (
                    x,
                    y,
                    Width,
                    Height
                ),
                hueVector
            );

            return true;
        }

        // MobileUO: need to keep for assistant
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
    }
}