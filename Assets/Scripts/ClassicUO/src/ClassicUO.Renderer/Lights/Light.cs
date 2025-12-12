using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Renderer.Lights
{
    public sealed class Light
    {
        // MobileUO: TODO: #19: temporarily made public
        public readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly LightsLoader _lightsLoader;

        public Light(LightsLoader lightsLoader, GraphicsDevice device)
        {
            _lightsLoader = lightsLoader;
            // MobileUO: use atlas size from settings - cap at 2048 (CUO is 2048)
            _atlas = new TextureAtlas(device, Math.Min(UserPreferences.SpriteSheetSize.CurrentValue, 2048), Math.Min(UserPreferences.SpriteSheetSize.CurrentValue, 2048), SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[lightsLoader.File.Entries.Length];
        }

        public ref readonly SpriteInfo GetLight(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var lightInfo = _lightsLoader.GetLight(idx);
                if (!lightInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        lightInfo.Pixels,
                        lightInfo.Width,
                        lightInfo.Height,
                        out spriteInfo.UV
                    );
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
