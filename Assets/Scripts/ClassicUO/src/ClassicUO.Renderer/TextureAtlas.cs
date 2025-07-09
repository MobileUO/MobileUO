using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbRectPackSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Renderer
{
    public class TextureAtlas : IDisposable
    {
        private readonly int _width,
            _height;
        private readonly SurfaceFormat _format;
        private readonly GraphicsDevice _device;
        private readonly List<Texture2D> _textureList;
        private Packer _packer;
        private bool _useSpriteSheet;

        public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format)
        {
            _device = device;
            _width = width;
            _height = height;
            _format = format;
            _useSpriteSheet = UserPreferences.UseSpriteSheet.CurrentValue == (int)PreferenceEnums.UseSpriteSheet.On;

            _textureList = new List<Texture2D>();
        }

        public int TexturesCount => _textureList.Count;

        public unsafe Texture2D AddSprite(
            ReadOnlySpan<uint> pixels,
            int width,
            int height,
            out Rectangle pr
        )
        {
            // MobileUO: reset texture list because we are swapping whether or not we are using sprite sheets
            if (_useSpriteSheet != (UserPreferences.UseSpriteSheet.CurrentValue == (int)PreferenceEnums.UseSpriteSheet.On))
            {
                _packer?.Dispose();
                _packer = new Packer(_width, _height);
                _textureList.Clear();
                _useSpriteSheet = UserPreferences.UseSpriteSheet.CurrentValue == (int)PreferenceEnums.UseSpriteSheet.On;
            }

            var index = _textureList.Count - 1;
            //pr = new Rectangle(0, 0, width, height);

            // MobileUO: handle 0x0 textures - this shouldn't happen unless the client data is missing newer textures
            if (width <= 0 || height <= 0)
            {
                Utility.Logging.Log.Trace($"Texture width and height must be greater than zero. Width: {width} Height: {height} Index: {index}");
                pr = new Rectangle(0, 0, width, height);
                return null;
            }

            if (index < 0)
            {
                index = 0;
                CreateNewTexture2D(width, height);
            }

            //ref Rectangle pr = ref _spriteBounds[hash];
            //pr = new Rectangle(0, 0, width, height);
            // MobileUO: TODO: figure out how to get packer working correctly
            if (_useSpriteSheet)
            {
                while (!_packer.PackRect(width, height, out pr))
                {
                    CreateNewTexture2D(width, height);
                    index = _textureList.Count - 1;
                }
            }
            else
            {
                pr = new Rectangle(0, 0, width, height);

                CreateNewTexture2D(width, height);
                index = _textureList.Count - 1;
            }

            // MobileUO: TODO: #19: added logging output
            //SaveImages("test");

            //Utility.Logging.Log.Trace("Packed rect: " + pr);

            Texture2D texture = _textureList[index];
            if (_useSpriteSheet)
                texture.IsFromTextureAtlas = true;

            fixed (uint* src = pixels)
            {
                texture.SetDataPointerEXT(0, pr, (IntPtr)src, sizeof(uint) * width * height);
            }

            return texture;
        }

        // MobileUO: TODO: figure out how to get packer working correctly
        private void CreateNewTexture2D(int width, int height)
        {
            // MobileUO: TODO: #19: added logging output; added toggle for sprite sheet
            var textureWidth = width;
            var textureHeight = height;
            if (_useSpriteSheet)
            {
                textureWidth = _width;
                textureHeight = _height;
            }

            //Utility.Logging.Log.Trace($"creating texture: {width}x{height} for Atlas {textureWidth}x{textureHeight} {_format}");
            Texture2D texture = new Texture2D(_device, textureWidth, textureHeight, false, _format);
            _textureList.Add(texture);

            _packer?.Dispose();
            _packer = new Packer(_width, _height);
        }

        public void SaveImages(string name)
        {
            // MobileUO: TODO: #19: added logging output
            Utility.Logging.Log.Trace($"Saving images");
            for (int i = 0, count = TexturesCount; i < count; ++i)
            {
                Utility.Logging.Log.Trace($"Texture {i}");
                var texture = _textureList[i];

                using (var stream = System.IO.File.Create($"atlas/{name}_atlas_{i}.png"))
                {
                    texture.SaveAsPng(stream, texture.Width, texture.Height);

                    string relativePath = $"atlas/{name}_atlas_{i}.png";
                    string fullPath = Path.GetFullPath(relativePath);
                    Utility.Logging.Log.Trace($"File created at: {fullPath}");
                }
            }
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _textureList)
            {
                if (!texture.IsDisposed)
                {
                    texture.Dispose();
                }
            }

            _packer.Dispose();
            _textureList.Clear();
        }
    }
}
