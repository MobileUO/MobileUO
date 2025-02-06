﻿using Microsoft.Xna.Framework;
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
        private readonly Rectangle[] _spriteBounds;
        // MobileUO: don't switch this to byte[] or graphics will break!
        private readonly int[] _spriteTextureIndices;

        public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format, int maxSpriteCount)
        {
            _device = device;
            _width = width;
            _height= height;
            _format = format;

            _textureList = new List<Texture2D>();
            _spriteBounds = new Rectangle[maxSpriteCount];
            _spriteTextureIndices = new int[maxSpriteCount];
        }


        public int TexturesCount => _textureList.Count;


        public unsafe void AddSprite<T>(uint hash, Span<T> pixels, int width, int height) where T : unmanaged
        {
            if (IsHashExists(hash))
            {
                return;
            }

            var index = _textureList.Count - 1;

            if (index < 0)
            {
                index = 0;
                CreateNewTexture2D(width, height);
            }

            // ref Rectangle pr = ref _spriteBounds[hash];
            var pr = new Rectangle(0, 0, width, height);
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
                    sizeof(T) * pixels.Length
                );
            }

            // MobileUO: keep setting the spriteBounds
            _spriteBounds[hash] = pr;
            _spriteTextureIndices[hash] = index;
        }

        // MobileUO: TODO: figure out how to get packer working correctly
        private void CreateNewTexture2D(int width, int height)
        {
            Texture2D texture = new Texture2D(_device, width, height, false, _format);
            _textureList.Add(texture);

            _packer?.Dispose();
            _packer = new Packer(_width, _height);
        }

        public Texture2D GetTexture(uint hash, out Rectangle bounds)
        {
            if (IsHashExists(hash))
            {
                bounds = _spriteBounds[(int)hash];
              
                return _textureList[_spriteTextureIndices[(int)hash]];
            }

            bounds = Rectangle.Empty;
            return null;
        }

        // MobileUO: keep as int > 0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHashExists(uint hash) => _spriteTextureIndices[(int)hash] > 0;

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
