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

using ClassicUO.Assets;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistIntegralColorBox : Control
    {
        private Color[] _Color = new Color[32];

        public AssistIntegralColorBox(int width, int height, ushort hue)
        {
            CanMove = false;

            Width = Math.Max(width, 96);
            Height = height;
            Hue = hue;

            WantUpdateSize = false;
        }

        private ushort _Hue;
        public ushort Hue 
        {
            get => _Hue;
            set
            {
                if(value != _Hue)
                {
                    _Hue = value;
                    for(ushort i = 0; i < 32; ++i)
                    {
                        uint pol = ClassicUO.Client.Game.UO.FileManager.Hues.GetPolygoneColor(i, value);
                        (byte b, byte g, byte r, byte a) = HuesHelper.GetBGRA(pol);

                        _Color[i] = new Color(r, g, b);
                    }
                }
            }
        }

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            //float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);
            int step = Width / _Color.Length;
            hueVector.Y = 20;
            /*renderLists.AddGumpNoAtlas(
                batcher =>
                {*/
                    for(int i = 0; i < _Color.Length; ++i)
                    {
                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(_Color[i]),
                            new Rectangle(x + i * step, y, step, Height),
                            hueVector//,
                            //layerDepth
                        );
                    }
                    
                    /*return true;
                }
            );*/

            return true;
        }
    }
}
