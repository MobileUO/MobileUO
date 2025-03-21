using System;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Assets;

#if ENABLE_INTERNAL_ASSISTANT
using Assistant;
#endif

namespace ClassicUO.Game.UI.Gumps
{
    internal class AssistantHotkeyButtonGump : AnchorableGump
    {
        public string _hotkeyName;
        private Texture2D backgroundTexture;
        private Label label;

        public AssistantHotkeyButtonGump(World world, string hotkeyName, int x, int y) : this(world)
        {
            X = x;
            Y = y;
            _hotkeyName = hotkeyName;
            BuildGump();
        }

        public AssistantHotkeyButtonGump(World world) : base(world, 0, 0)
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

            label = new Label(_hotkeyName, true, 1001, Width, 255, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
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

            if (ProfileManager.CurrentProfile.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt && Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                RunHotkey();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.CurrentProfile.CastSpellsByOneClick || button != MouseButtonType.Left)
                return false;

            RunHotkey();
            
            return true;
        }

        private void RunHotkey()
        {
#if ENABLE_INTERNAL_ASSISTANT
            HotKeys.PlayFunc(_hotkeyName);
#endif
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            // MobileUO: CUO 0.1.11.0 drops ResetHueVector()
            var hueVector = ShaderHueTranslator.GetHueVector(0);
            //ResetHueVector();
            hueVector.Z = 0.9f;

            batcher.Draw2D(backgroundTexture, x, y, Width, Height, ref hueVector);

            hueVector.Z = 1;
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Gray), x, y, Width, Height, hueVector);

            base.Draw(batcher, x, y);
            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            if (string.IsNullOrEmpty(_hotkeyName) == false)
            {
                // hack to give hotkey buttons a unique id for use in anchor groups
                int hotkeyid = _hotkeyName.GetHashCode();

                LocalSerial = (uint) hotkeyid + 2000;

                base.Save(writer);

                writer.WriteAttributeString("name", _hotkeyName);
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _hotkeyName = xml.GetAttribute("name");

            if (string.IsNullOrEmpty(_hotkeyName) == false)
            {
                BuildGump();
            }
        }
    }
}