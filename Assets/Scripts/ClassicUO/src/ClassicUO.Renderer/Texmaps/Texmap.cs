using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Renderer.Texmaps
{
    public sealed class Texmap
    {
        // MobileUO: TODO: #19: temporarily made public
        public readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly TexmapsLoader _texmapsLoader;

        public Texmap(TexmapsLoader texmapsLoader, GraphicsDevice device)
        {
            _texmapsLoader = texmapsLoader;
            // MobileUO: use atlas size from settings - cap at 2048 (CUO is 2048)
            _atlas = new TextureAtlas(device, Math.Min(UserPreferences.SpriteSheetSize.CurrentValue, 2048), Math.Min(UserPreferences.SpriteSheetSize.CurrentValue, 2048), SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[texmapsLoader.File.Entries.Length];
        }

        public ref readonly SpriteInfo GetTexmap(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var texmapInfo = _texmapsLoader.GetTexmap(idx);
                if (!texmapInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        texmapInfo.Pixels,
                        texmapInfo.Width,
                        texmapInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, texmapInfo.Width, texmapInfo.Height, texmapInfo.Pixels);
                }
            }

            return ref spriteInfo;
        }

        // MobileUO: added way to clear sprite arrays when toggling using sprite sheets or not
        public void ClearSpriteInfo()
        {
            Array.Clear(_spriteInfos, 0, _spriteInfos.Length);
        }
    }
}
