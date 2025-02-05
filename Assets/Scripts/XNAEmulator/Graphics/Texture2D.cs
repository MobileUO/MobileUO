using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.Xna.Framework.Graphics
{
    public class Texture2D : GraphicsResource, IDisposable
    {
        //This hash doesn't work as intended since it's not based on the contents of the UnityTexture but its instanceID
        //which will be different as old textures are discarded and new ones are created 
        public Texture UnityTexture { get; protected set; }

        public static FilterMode defaultFilterMode = FilterMode.Point;

        protected Texture2D(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {

        }

        public Rectangle Bounds => new Rectangle(0, 0, Width, Height);

        public Texture2D(GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice)
        {
            Width = width;
            Height = height;
            UnityMainThreadDispatcher.Dispatch(InitTexture);
        }

        private void InitTexture()
        {
            UnityTexture = new UnityEngine.Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            UnityTexture.filterMode = defaultFilterMode;
            UnityTexture.wrapMode = TextureWrapMode.Clamp;
        }

        public Texture2D(GraphicsDevice graphicsDevice, int width, int height, bool v, SurfaceFormat surfaceFormat) :
            this(graphicsDevice, width, height)
        {
        }

        public int Width { get; protected set; }

        public int Height { get; protected set; }

        public bool IsDisposed { get; private set; }

        public override void Dispose()
        {
            if (UnityTexture != null)
            {
                if (UnityTexture is RenderTexture renderTexture)
                {
                    renderTexture.Release();
                }
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEngine.Object.Destroy(UnityTexture);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(UnityTexture);
                }
#else
                UnityEngine.Object.Destroy(UnityTexture);
#endif
            }
            UnityTexture = null;
            IsDisposed = true;
        }

        private byte[] tempByteData;

        internal void SetData(byte[] data)
        {
            tempByteData = data;
            UnityMainThreadDispatcher.Dispatch(SetDataBytes);
        }

        private void SetDataBytes()
        {
            var dataLength = tempByteData.Length;
            var destText = UnityTexture as UnityEngine.Texture2D;
            var dst = destText.GetRawTextureData<byte>();
            var tmp = new byte[dataLength];
            var textureBytesWidth = Width * 4;
            var textureBytesHeight = Height;

            for (int i = 0; i < dataLength; i++)
            {
                int x = i % textureBytesWidth;
                int y = i / textureBytesWidth;
                y = textureBytesHeight - y - 1;
                var index = y * textureBytesWidth + x;
                var colorByte = tempByteData[index];
                tmp[i] = colorByte;
            }
            
            dst.CopyFrom(tmp);
            destText.Apply();
            tempByteData = null;
        }

        private Color[] tempColorData;

        internal void SetData(Color[] data)
        {
            tempColorData = data;
            UnityMainThreadDispatcher.Dispatch(SetDataColor);
        }

        private void SetDataColor()
        {
            var dataLength = tempColorData.Length;
            var destText = UnityTexture as UnityEngine.Texture2D;
            var dst = destText.GetRawTextureData<uint>();
            var tmp = new uint[dataLength];
            var textureWidth = Width;

            for (int i = 0; i < dataLength; i++)
            {
                int x = i % textureWidth;
                int y = i / textureWidth;
                var index = y * textureWidth + (textureWidth - x - 1);
                var color = tempColorData[dataLength - index - 1];
                tmp[i] = color.PackedValue;
            }
            
            dst.CopyFrom(tmp);
            destText.Apply();
            tempColorData = null;
        }

        private uint[] tempUIntData;
        private int tempStartOffset;
        private int tempElementCount;
        private bool tempInvertY;

        internal void SetData(uint[] data, int startOffset = 0, int elementCount = 0, bool invertY = false)
        {
            tempUIntData = data;
            tempStartOffset = startOffset;
            tempElementCount = elementCount;
            tempInvertY = invertY;
            UnityMainThreadDispatcher.Dispatch(SetDataUInt);
        }

        private void SetDataUInt()
        {
            var textureWidth = Width;
            var textureHeight = Height;

            if (tempElementCount == 0)
            {
                tempElementCount = tempUIntData.Length;
            }

            var destText = UnityTexture as UnityEngine.Texture2D;
            var dst = destText.GetRawTextureData<uint>();
            var dstLength = dst.Length;
            var tmp = new uint[dstLength];

            for (int i = 0; i < tempElementCount; i++)
            {
                int x = i % textureWidth;
                int y = i / textureWidth;
                if (tempInvertY)
                {
                    y = textureHeight - y - 1;
                }
                var index = y * textureWidth + (textureWidth - x - 1);
                if (index < tempElementCount && i < dstLength)
                {
                    tmp[i] = tempUIntData[tempElementCount + tempStartOffset - index - 1];
                }
            }
            
            dst.CopyFrom(tmp);
            destText.Apply();

            tempUIntData = null;
        }

        public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
            Console.WriteLine("Texture2D.FromStream is not implemented yet.");
            if (!UnityMainThreadDispatcher.IsMainThread())
                return null;
            var texture = new Texture2D(graphicsDevice, 2, 2);
            return texture;

        }

        // https://github.com/FNA-XNA/FNA/blob/85a8457420278087dc7a81f16661ff68e67b75af/src/Graphics/Texture2D.cs#L213
        public void SetDataPointerEXT(int level, Rectangle? rect, IntPtr data, int dataLength)
        {
            UnityMainThreadDispatcher.Dispatch(() => SetDataPointerEXTInt(level, rect, data, dataLength));
        }

        private void SetDataPointerEXTInt(int level, Rectangle? rect, IntPtr data, int dataLength)
        {
            if (data == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var destTex = UnityTexture as UnityEngine.Texture2D;
            if (destTex == null)
            {
                throw new InvalidOperationException("UnityTexture is not a Texture2D");
            }

            // Create a temporary buffer to hold the data
            byte[] buffer = new byte[dataLength];
            Marshal.Copy(data, buffer, 0, dataLength);

            int x, y, w, h;
            if (rect.HasValue)
            {
                x = rect.Value.X;
                y = rect.Value.Y;
                w = rect.Value.Width;
                h = rect.Value.Height;
            }
            else
            {
                x = 0;
                y = 0;
                w = Math.Max(Width >> level, 1);
                h = Math.Max(Height >> level, 1);
            }

            var colors = new Color32[w * h];

            // Copy data from the buffer to the colors array, flipping vertically
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    int bufferIndex = (row * w + col) * 4;
                    int colorIndex = ((h - 1 - row) * w) + col;

                    // Ensure the buffer index is within bounds
                    if (bufferIndex + 3 < buffer.Length)
                    {
                        // Create the Color32 object, assuming the buffer is in RGBA format
                        colors[colorIndex] = new Color32(
                            buffer[bufferIndex + 0], // R
                            buffer[bufferIndex + 1], // G
                            buffer[bufferIndex + 2], // B
                            buffer[bufferIndex + 3]  // A
                        );
                    }
                }
            }

            destTex.SetPixels32(x, y, w, h, colors, level);
            destTex.Apply();
        }

        // https://github.com/FNA-XNA/FNA/blob/85a8457420278087dc7a81f16661ff68e67b75af/src/Graphics/Texture2D.cs#L268
        public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
        {
            GetData(0, null, data, startIndex, elementCount);
        }

        public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("data cannot be null or empty");
            }
            if (data.Length < startIndex + elementCount)
            {
                throw new ArgumentException(
                    $"The data array length is {data.Length}, but {elementCount} elements were requested from start index {startIndex}."
                );
            }

            var destTex = UnityTexture as UnityEngine.Texture2D;
            if (destTex == null)
            {
                throw new InvalidOperationException("UnityTexture is not a Texture2D");
            }

            int x, y, w, h;
            if (rect.HasValue)
            {
                x = rect.Value.X;
                y = rect.Value.Y;
                w = rect.Value.Width;
                h = rect.Value.Height;
            }
            else
            {
                x = 0;
                y = 0;
                w = Math.Max(Width >> level, 1);
                h = Math.Max(Height >> level, 1);
            }

            Color32[] colors = destTex.GetPixels32(level);
            int elementSizeInBytes = Marshal.SizeOf(typeof(T));
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr dataPtr = handle.AddrOfPinnedObject() + (startIndex * elementSizeInBytes);

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    int colorIndex = (row * w) + col;
                    int dataIndex = ((h - 1 - row) * w + col) * elementSizeInBytes;

                    if (colorIndex < colors.Length && dataIndex < elementCount * elementSizeInBytes)
                    {
                        Marshal.StructureToPtr(colors[colorIndex], dataPtr + dataIndex, false);
                    }
                }
            }

            handle.Free();
        }
    }
}