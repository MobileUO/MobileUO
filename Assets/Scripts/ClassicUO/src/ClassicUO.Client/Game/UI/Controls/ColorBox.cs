// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class ColorBox : Control
    {
        private ushort hue;
        protected Vector3 hueVector;

        public ColorBox(int width, int height, ushort hue)
        {
            CanMove = false;
            Width = width;
            Height = height;
            Hue = hue;
            WantUpdateSize = false;
        }

        public ushort Hue
        {
            get => hue; set
            {
                hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);
            }
        }

        public override void AlphaChanged(float oldValue, float newValue)
        {
            base.AlphaChanged(oldValue, newValue);
            hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.White),
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
    }
}