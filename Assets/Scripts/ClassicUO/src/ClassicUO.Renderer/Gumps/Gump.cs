using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Renderer.Gumps
{
    public sealed class Gump
    {
        // MobileUO: TODO: #19: temporarily made public
        public readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly GumpsLoader _gumpsLoader;

        public GumpsLoader GetGumpsLoader => _gumpsLoader;

        public Gump(GumpsLoader gumpsLoader, GraphicsDevice device)
        {
            _gumpsLoader = gumpsLoader;
            // MobileUO: use atlas size from settings 
            _atlas = new TextureAtlas(device, UserPreferences.SpriteSheetSize.CurrentValue, UserPreferences.SpriteSheetSize.CurrentValue, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[gumpsLoader.File.Entries.Length];
        }

        public ref readonly SpriteInfo GetGump(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var gumpInfo = PNGLoader.Instance.LoadGumpTexture(idx);
                bool loadedFromPNG = gumpInfo.Pixels != null && !gumpInfo.Pixels.IsEmpty;

                if (gumpInfo.Pixels.IsEmpty)
                {
                    gumpInfo = _gumpsLoader.GetGump(idx);
                }
                if (!gumpInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        gumpInfo.Pixels,
                        gumpInfo.Width,
                        gumpInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, gumpInfo.Width, gumpInfo.Height, gumpInfo.Pixels);

                    // Clear the pixel cache from PNG Loader since it's now in the atlas
                    if (loadedFromPNG)
                    {
                        PNGLoader.Instance.ClearGumpPixelCache(idx);
                    }
                }
            }

            return ref spriteInfo;
        }

        public bool PixelCheck(uint idx, int x, int y, double scale = 1f) => _picker.Get(idx, x, y, scale: scale);

        // MobileUO: added way to clear sprite arrays when toggling using sprite sheets or not
        public void ClearSpriteInfo()
        {
            Array.Clear(_spriteInfos, 0, _spriteInfos.Length);
        }
    }
}
