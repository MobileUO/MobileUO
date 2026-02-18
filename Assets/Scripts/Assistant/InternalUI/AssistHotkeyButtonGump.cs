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
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AssistHotkeyButtonGump : AnchorableGump
    {
        public string _hotkeyName;
        public string _prettyName;
        private Texture2D backgroundTexture;
        private Label label;

        public AssistHotkeyButtonGump(string hotkeyName, string prettyname, int x, int y) : this()
        {
            X = x;
            Y = y;
            _hotkeyName = hotkeyName;
            _prettyName = prettyname;
            BuildGump();
        }

        public AssistHotkeyButtonGump() : base(ClassicUO.Client.Game.UO.World, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }

        public override GumpType GumpType => GumpType.AssistantHotkeyButton;

        private void BuildGump()
        {
            Width = 88;
            Height = 44;

            label = new Label(_prettyName, true, 1001, Width, 255, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 0,
                Width = Width - 10,
            };
            label.Y = (Height >> 1) - (label.Height >> 1);
            Add(label);

            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
        }

        protected override void OnMouseEnter(int x, int y)
        {
            label.Hue = 53;
            backgroundTexture = SolidColorTextureCache.GetTexture(Color.DimGray);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            label.Hue = 1001;
            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
            base.OnMouseExit(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            Point offset = Mouse.LDragOffset;

            if (button == MouseButtonType.Left && !Keyboard.Alt && Math.Abs(offset.X) < 10 && Math.Abs(offset.Y) < 10)
            {
                RunHotkey();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return false;
            return true;
        }

        private void RunHotkey()
        {
            if (!Engine.Instance.Initialized) return;

            HotKeys.PlayFunc(_hotkeyName);
        }

        //public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            //float layerDepth = layerDepthRef;
            Vector3 hueVector = new Vector3(0, 0, 0.85f);

            /*renderLists.AddGumpNoAtlas(
                batcher =>
                {*/
            batcher.Draw(backgroundTexture, new Rectangle(x, y, Width, Height), hueVector);//, layerDepth);
                    hueVector.Z = 0;
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Gray), x, y, Width, Height, hueVector);//, layerDepth);
            /*return true;
        }
    );*/

            return base.Draw(batcher, x, y);//base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        public override void Save(XmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(_hotkeyName))
            {
                // hack to give hotkey buttons a unique id for use in anchor groups
                int hotkeyid = _hotkeyName.GetHashCode();

                LocalSerial = (uint) hotkeyid + 2000;

                base.Save(writer);

                writer.WriteAttributeString("name", _hotkeyName);
                writer.WriteAttributeString("prettyname", _prettyName);
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _hotkeyName = xml.GetAttribute("name");
            _prettyName = xml.GetAttribute("prettyname");

            if (!string.IsNullOrEmpty(_hotkeyName) && !string.IsNullOrEmpty(_prettyName))
            {
                BuildGump();
            }
        }
    }
}