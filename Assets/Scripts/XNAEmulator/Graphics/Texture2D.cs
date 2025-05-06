using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer.Lights;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
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

        public Rectangle Bounds => new Rectangle(0, 0, width, height);

        public Texture2D(GraphicsDevice graphicsDevice, int w, int h) : base(graphicsDevice)
        {
            UnityTexture = new UnityEngine.Texture2D(w, h, TextureFormat.RGBA32, false, false) { filterMode = defaultFilterMode, wrapMode = TextureWrapMode.Clamp };
            /*Width = width;
            Height = height;
            UnityMainThreadDispatcher.Dispatch(InitTexture);*/
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

        public int width => UnityTexture == null ? 0 : UnityTexture.width;//{ get; protected set; }

        public int height => UnityTexture == null ? 0 : UnityTexture.height;//{ get; protected set; }

        public int Width => width;//{ get; protected set; }

        public int Height => height;//{ get; protected set; }

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

        internal void SetData(byte[] data)
        {
#if !UNITY_WEBGL && !UNITY_EDITOR
            UnityMainThreadDispatcher.DispatchAsync(() => SetDataBytes(data));
#else
            UnityMainThreadDispatcher.Dispatch(() => SetDataBytes(data));
#endif
            //UnityMainThreadDispatcher.DispatchAsync(() => SetDataBytes(data));
            //UnityMainThreadDispatcher.Dispatch(() => SetDataBytes(data));
        }

        private void SetDataBytes(byte[] data)
        {
            var dataLength = data.Length;
            var destText = UnityTexture as UnityEngine.Texture2D;
            NativeArray<byte> dst = destText.GetRawTextureData<byte>();
            //var tmp = new byte[dataLength];
            var textureBytesWidth = width * 4;
            var textureBytesHeight = height;

            for (int i = 0; i < dataLength; i++)
            {
                int x = i % textureBytesWidth;
                int y = i / textureBytesWidth;
                y = textureBytesHeight - y - 1;
                var index = y * textureBytesWidth + x;
                var colorByte = data[index];
                dst[i] = colorByte;
            }

            //dst.CopyFrom(tmp);
            destText.Apply(false);
        }

        internal void SetData(Color[] data)
        {
#if !UNITY_WEBGL && !UNITY_EDITOR
            UnityMainThreadDispatcher.DispatchAsync(() => SetDataColor(data));
#else
            UnityMainThreadDispatcher.Dispatch(() => SetDataColor(data));
#endif

            //UnityMainThreadDispatcher.DispatchAsync(() => SetDataColor(data));
            //UnityMainThreadDispatcher.Dispatch(() => SetDataColor(data));
        }

        private void SetDataColor(Color[] data)
        {
            var dataLength = data.Length;
            var destText = UnityTexture as UnityEngine.Texture2D;

            unsafe
            {
                fixed (Color* p = data)
                {
                    IntPtr ptr = (IntPtr)p;
                    destText.LoadRawTextureData(ptr, data.Length * sizeof(uint));
                }
            }

            destText.Apply(false);
        }

        internal void SetData(uint[] data, int startOffset = 0, int elementCount = 0, bool invertY = false)
        {
            if (invertY)
            {
#if !UNITY_WEBGL && !UNITY_EDITOR
                UnityMainThreadDispatcher.DispatchAsync(() => SetDataUInt(data, startOffset, elementCount));
#else
                UnityMainThreadDispatcher.Dispatch(() => SetDataUInt(data, startOffset, elementCount));
#endif
            }
            else
            {
#if !UNITY_WEBGL && !UNITY_EDITOR
                UnityMainThreadDispatcher.DispatchAsync(() => SetDataUIntInverted(data, startOffset, elementCount).Forget());
#else
                UnityMainThreadDispatcher.Dispatch(() => SetDataUIntInverted(data, startOffset, elementCount));
#endif
            }
        }

        private void SetDataUInt(uint[] data, int startOffset, int elementCount)
        {
            var textureWidth = width;
            var textureHeight = height;

            if (elementCount == 0)
            {
                elementCount = data.Length;
            }

            var destText = UnityTexture as UnityEngine.Texture2D;

            unsafe
            {
                fixed (uint* p = data)
                {
                    IntPtr ptr = (IntPtr)(p + startOffset);
                    destText.LoadRawTextureData(ptr, elementCount * 4);
                }
            }

            destText.Apply();
        }

        private void SetDataUIntInverted(uint[] data, int startOffset, int elementCount)
        {
            var textureWidth = width;

            if (elementCount == 0)
            {
                elementCount = data.Length;
            }

            var destText = UnityTexture as UnityEngine.Texture2D;

            NativeArray<uint> dst = destText.GetRawTextureData<uint>();
            int dstLength = dst.Length;

            for (int i = 0; i < elementCount; ++i)
            {
                int x = i % textureWidth;
                int y = i / textureWidth;

                int index = y * textureWidth + (textureWidth - x - 1);
                if (index < elementCount && i < dstLength)
                {
                    dst[i] = data[elementCount + startOffset - index - 1];
                }
            }
            destText.Apply();
        }

        public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
            if (!UnityMainThreadDispatcher.IsMainThread())
            {
                Debug.Log($"FromStream must be called from the main thread.");
                throw new InvalidOperationException("FromStream must be called from the main thread.");
            }

            // Read the stream into a byte array
            byte[] imageData;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                imageData = memoryStream.ToArray();
            }

            // Create a new Unity texture
            var texture = new UnityEngine.Texture2D(2, 2);
            if (!texture.LoadImage(imageData))
            {
                Debug.Log("Failed to load texture from stream.");
                throw new InvalidOperationException("Failed to load texture from stream.");
            }

            // Initialize the XNA texture wrapper
            var xnaTexture = new Texture2D(graphicsDevice, texture.width, texture.height)
            {
                UnityTexture = texture
            };


            return xnaTexture;
        }

        // https://github.com/FNA-XNA/FNA/blob/85a8457420278087dc7a81f16661ff68e67b75af/src/Graphics/Texture2D.cs#L213
        public void SetDataPointerEXT(int level, Rectangle? rect, IntPtr data, int dataLength, bool invertY = false)
        {
            //tempInvertY = invertY;
            UnityMainThreadDispatcher.Dispatch(() => SetDataPointerEXTInt(level, rect, data, dataLength, invertY));
        }

        private void SetDataPointerEXTInt(int level, Rectangle? rect, IntPtr data, int dataLength, bool invertY)
        {
            if (data == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var destTex = UnityTexture as UnityEngine.Texture2D ?? throw new InvalidOperationException("UnityTexture is not a Texture2D");

            // Create a temporary buffer to hold the data <- this is slow
            Span<uint> colors = destTex.GetRawTextureData<uint>().AsSpan();//new byte[dataLength];
            //Marshal.Copy(data, buffer, 0, dataLength);

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

            // Check if dimensions are valid
            /*if (x < 0 || y < 0 || x + w > destTex.width || y + h > destTex.height)
            {
                Debug.Log($"Texture width: {destTex.width}, height: {destTex.height}, rect: {x},{y},{w},{h}");
                throw new ArgumentException("The specified block is outside the texture bounds.");
            }*/

            //var colors = new Color32[w * h];

            // Copy data from the buffer to the colors array, flipping vertically
            unsafe
            {
                uint* buffer = (uint*)data;
                for (int row = 0; row < h; row++)
                {
                    for (int col = 0; col < w; col++)
                    {
                        int bufferIndex = (row * w + col);// * 4;
                        int colorIndex = ((h - 1 - row) * w) + col;

                        if (invertY)
                        {
                            colorIndex = row * w + col;
                        }

                        // Ensure the buffer index is within bounds
                        if (bufferIndex < dataLength)//+ 3
                        {
                            colors[colorIndex] = buffer[bufferIndex];
                            // Create the Color32 object, assuming the buffer is in RGBA format

                            /*colors[colorIndex] = new Color32(
                                buffer[bufferIndex + 0], // R
                                buffer[bufferIndex + 1], // G
                                buffer[bufferIndex + 2], // B
                                buffer[bufferIndex + 3]  // A
                            );*/
                        }
                    }
                }
            }

            //destTex.SetPixels32(x, y, w, h, colors, level);
            destTex.Apply();
        }

        public void GetData(uint[] data, int startIndex, int elementCount)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("data cannot be null");
            }
            if (data.Length < startIndex + elementCount)
            {
                throw new ArgumentException(
                    "The data passed has a length of " + data.Length.ToString() +
                    " but " + elementCount.ToString() + " pixels have been requested."
                );
            }

            if (UnityTexture is UnityEngine.Texture2D text2d)
            {
                byte[] src = text2d.GetRawTextureData();
                for (int i = 0, d = 0; i + 3 < src.Length && d < data.Length; ++d, i += 4)
                {
                    data[d] = (uint)(src[i] | (src[i + 1] << 8) | (src[i + 2] << 16) | (src[i + 3] << 24));
                }
            }
        }

        public void GetData(int level, Rectangle? rect, uint[] data, int startIndex, int elementCount)
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

            if (UnityTexture is UnityEngine.Texture2D destTex)
            {
                int subX, subY, subW, subH;
                if (rect == null)
                {
                    subX = 0;
                    subY = 0;
                    subW = width >> level;
                    subH = height >> level;
                }
                else
                {
                    subX = rect.Value.X;
                    subY = rect.Value.Y;
                    subW = rect.Value.Width;
                    subH = rect.Value.Height;
                }

                UnityEngine.Color[] c = destTex.GetPixels(subX, subY, subW, subH, 0);

                for (int i = startIndex, e = 0; i < data.Length && i < elementCount && e < c.Length; ++i, ++e)
                {
                    data[i] = c[e].ToHex();
                }
            }
        }

        // https://github.com/FNA-XNA/FNA/blob/6a3ab36e521edfc6879b388037aadf9b832ec69e/src/Graphics/Texture2D.cs#L388C3-L389C4
        public void SaveAsPng(FileStream stream, int w, int h)
        {
            if (UnityTexture is UnityEngine.Texture2D text)
            {
                stream.Write(text.EncodeToPNG());
            }
        }
    }
}