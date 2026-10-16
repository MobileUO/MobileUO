// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Renderer
{
    public partial class FontResources
    {
        // MobileUO: commented out for now
        //[FileEmbed.FileEmbed("fonts/regular_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetRegularFont();

        //[FileEmbed.FileEmbed("fonts/bold_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetBoldFont();

        //[FileEmbed.FileEmbed("fonts/map1_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetMap1Font();

        //[FileEmbed.FileEmbed("fonts/map2_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetMap2Font();

        //[FileEmbed.FileEmbed("fonts/map3_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetMap3Font();

        //[FileEmbed.FileEmbed("fonts/map4_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetMap4Font();

        //[FileEmbed.FileEmbed("fonts/map5_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetMap5Font();

        //[FileEmbed.FileEmbed("fonts/map6_font.xnb")]
        //public static partial ReadOnlySpan<byte> GetMap6Font();
    }

    public static class Fonts
    {
        public static void Initialize(GraphicsDevice device)
        {
            // MobileUO: removed "ClassicUO.Renderer.fonts." and ".xnb" from fonts
            Regular = SpriteFont.Create(device, "regular_font");
            Bold = SpriteFont.Create(device, "bold_font");

            Map1 = SpriteFont.Create(device, "map1_font");
            Map2 = SpriteFont.Create(device, "map2_font");
            Map3 = SpriteFont.Create(device, "map3_font");
            Map4 = SpriteFont.Create(device, "map4_font");
            Map5 = SpriteFont.Create(device, "map5_font");
            Map6 = SpriteFont.Create(device, "map6_font");

            // MobileUO: TODO: Implement this newer way
            //Regular = SpriteFont.Create(device, FontResources.GetRegularFont());
            //Bold = SpriteFont.Create(device, FontResources.GetBoldFont());
            //Map1 = SpriteFont.Create(device, FontResources.GetMap1Font());
            //Map2 = SpriteFont.Create(device, FontResources.GetMap2Font());
            //Map3 = SpriteFont.Create(device, FontResources.GetMap3Font());
            //Map4 = SpriteFont.Create(device, FontResources.GetMap4Font());
            //Map5 = SpriteFont.Create(device, FontResources.GetMap5Font());
            //Map6 = SpriteFont.Create(device, FontResources.GetMap6Font());
        }

        public static SpriteFont Regular { get; private set; }
        public static SpriteFont Bold { get; private set; }
        public static SpriteFont Map1 { get; private set; }
        public static SpriteFont Map2 { get; private set; }
        public static SpriteFont Map3 { get; private set; }
        public static SpriteFont Map4 { get; private set; }
        public static SpriteFont Map5 { get; private set; }
        public static SpriteFont Map6 { get; private set; }
    }
}