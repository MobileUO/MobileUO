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

using Assistant;
using ClassicUO.Assets;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class AssistCheckbox : Control
    {
        private bool _isChecked;
        private readonly RenderedText _text;
        private ushort _inactive, _active;

        public AssistCheckbox
        (
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        )
        {
            _inactive = inactive;
            _active = active;

            ref readonly var gumpInfoInactive = ref Client.Game.UO.Gumps.GetGump(inactive);
            ref readonly var gumpInfoActive = ref Client.Game.UO.Gumps.GetGump(active);

            if (gumpInfoInactive.Texture == null || gumpInfoActive.Texture == null)
            {
                Dispose();

                return;
            }

            Width = gumpInfoInactive.UV.Width;

            _text = RenderedText.Create
            (
                text,
                color,
                font,
                isunicode,
                maxWidth: maxWidth
            );

            Width += _text.Width;

            Height = Math.Max(gumpInfoInactive.UV.Width, _text.Height);
            CanMove = false;
            AcceptMouseInput = true;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged();
                }
            }
        }

        public string Text
        {
            get => _text.Text;
            set
            {
                if (!string.IsNullOrEmpty(value) && _text.Text != value)
                {
                    _text.Text = value;
                    _text.CreateTexture();
                }
            }
        }

        public ushort Hue
        {
            get => _text.Hue;
            set
            {
                if (_text.Hue != value)
                {
                    _text.Hue = value;
                    _text.CreateTexture();
                }
            }
        }

        public event EventHandler ValueChanged;

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            var ok = base.Draw(batcher, x, y);//base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            //float layerDepth = layerDepthRef;

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                IsChecked ? _active : _inactive
            );
            var texture = gumpInfo.Texture;
            var sourceRectangle = gumpInfo.UV;
            /*renderLists.AddGumpWithAtlas
            (
                (batcher) =>
                {*/
                    batcher.Draw(
                        texture,
                        new Vector2(x, y),
                        sourceRectangle,
                        ShaderHueTranslator.GetHueVector(0)//,
                        //layerDepth
                     );

            /*    return true;
            }
        );*/

            /*renderLists.AddGumpNoAtlas
            (
                (batcher) =>
                {*/
            _text.Draw(batcher, x + sourceRectangle.Width + 2, y);//, layerDepth);

                    /*return true;
                }
            );*/

            return ok;
        }

        protected void OnCheckedChanged()
        {
            ValueChanged?.Raise(this);
            if(!UOSObjects.GumpIsLoading && UOSObjects.Gump != null)
            {
                XmlFileParser.SaveConfig(UOSObjects.Gump);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && MouseIsOver)
            {
                IsChecked = !IsChecked;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _text?.Destroy();
        }
    }
}