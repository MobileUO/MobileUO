using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using StbRectPackSharp;

namespace ClassicUO.Renderer
{
    class TextureAtlas : IDisposable
    {
        private readonly int _width, _height;
        private readonly SurfaceFormat _format;
        private readonly GraphicsDevice _device;
        private readonly List<Texture2D> _textureList;
        private Packer _packer;

        public static TextureAtlas Shared { get; }

        static TextureAtlas()
        {
            const int TEXTURE_SIZE = 1024 * 4;

            Shared = new TextureAtlas 
            (
                Client.Game.GraphicsDevice,
                TEXTURE_SIZE,
                TEXTURE_SIZE,
                SurfaceFormat.Color
            );
        }


        public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format)
        {
            _device = device;
            _width = width;
            _height= height;
            _format = format;

            _textureList = new List<Texture2D>();
        }


        public int TexturesCount => _textureList.Count;


        public unsafe Texture2D AddSprite<T>(Span<T> pixels, int width, int height, out Rectangle pr) where T : unmanaged
        {
            var index = _textureList.Count - 1;
            pr = new Rectangle(0, 0, width, height);

            // MobileUO: handle 0x0 textures - this shouldn't happen unless the client data is missing newer textures
            if (width <= 0 || height <= 0)
            {
                Utility.Logging.Log.Trace($"Texture width and height must be greater than zero. Width: {width} Height: {height} Index: {index}");
                return null;
            }

            if (index < 0)
            {
                index = 0;
                CreateNewTexture2D(width, height);
            }

            // ref Rectangle pr = ref _spriteBounds[hash];
            //pr = new Rectangle(0, 0, width, height);
            // MobileUO: TODO: figure out how to get packer working correctly
            //while (!_packer.PackRect(width, height, null, out pr))
            {
                CreateNewTexture2D(width, height);
                index = _textureList.Count - 1;
            }

            Texture2D texture = _textureList[index];

            fixed (T* src = pixels)
            {
                texture.SetDataPointerEXT
                (
                    0,
                    pr,
                    (IntPtr)src,
                    sizeof(T) * width * height
                );
            }

            return texture;
        }

        // MobileUO: TODO: figure out how to get packer working correctly
        private void CreateNewTexture2D(int width, int height)
        {
            Utility.Logging.Log.Trace($"creating texture: {width}x{height} for Atlas {_width}x{_height} {_format}");
            Texture2D texture = new Texture2D(_device, width, height, false, _format);
            _textureList.Add(texture);

            _packer?.Dispose();
            _packer = new Packer(_width, _height);
        }

      
        public void SaveImages(string name)
        {
            for (int i = 0, count = TexturesCount; i < count; ++i)
            {
                var texture = _textureList[i];

                using (var stream = System.IO.File.Create($"atlas/{name}_atlas_{i}.png"))
                {
                    texture.SaveAsPng(stream, texture.Width, texture.Height);
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
