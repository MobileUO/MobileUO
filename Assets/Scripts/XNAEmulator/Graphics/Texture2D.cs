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

        public bool IsFromTextureAtlas { get; set; }

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

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing && UnityTexture != null)
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
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private byte[] tempByteData;
        private Rectangle tempRect;
        private bool tempHasRect;

        internal void SetData(byte[] data)
        {
            tempByteData = data;
            tempHasRect = false;
            UnityMainThreadDispatcher.Dispatch(SetDataBytes);
        }

        internal void SetData(byte[] data, Rectangle rect)
        {
            tempByteData = data;
            tempRect = rect;
            tempHasRect = true;
            UnityMainThreadDispatcher.Dispatch(SetDataBytes);
        }

        public void SetData<T>(
            int level,
            Rectangle? rect,
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                // Simplest case: we only support byte[] for now
                var byteArray = data as byte[];
                if (byteArray == null)
                {
                    // copy to a new byte[]
                    byteArray = new byte[elementCount];
                    Buffer.BlockCopy(data, startIndex, byteArray, 0, elementCount);
                }

                Rectangle r;
                if (rect.HasValue)
                {
                    r = rect.Value;
                }
                else
                {
                    r = new Rectangle(0, 0, Math.Max(Width >> level, 1), Math.Max(Height >> level, 1));
                }

                // Use the rect-aware byte path
                SetData(byteArray, r);
                return;
            }

            throw new NotSupportedException($"SetData<{typeof(T).Name}> not implemented.");

            // MobileUO: TODO: might need to implement this?
            //if (data == null)
            //{
            //    throw new ArgumentNullException("data");
            //}
            //if (startIndex < 0)
            //{
            //    throw new ArgumentOutOfRangeException("startIndex");
            //}
            //if (data.Length < (elementCount + startIndex))
            //{
            //    throw new ArgumentOutOfRangeException("elementCount");
            //}

            //int x, y, w, h;
            //if (rect.HasValue)
            //{
            //    x = rect.Value.X;
            //    y = rect.Value.Y;
            //    w = rect.Value.Width;
            //    h = rect.Value.Height;
            //}
            //else
            //{
            //    x = 0;
            //    y = 0;
            //    w = Math.Max(Width >> level, 1);
            //    h = Math.Max(Height >> level, 1);
            //}
            //int elementSize = MarshalHelper.SizeOf<T>();
            //int requiredBytes = (w * h * GetFormatSizeEXT(Format)) / GetBlockSizeSquaredEXT(Format);
            //int availableBytes = elementCount * elementSize;
            //if (requiredBytes > availableBytes)
            //{
            //    throw new ArgumentOutOfRangeException("rect", "The region you are trying to upload is larger than the amount of data you provided.");
            //}

            //GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            //FNA3D.FNA3D_SetTextureData2D(
            //    GraphicsDevice.GLDevice,
            //    texture,
            //    x,
            //    y,
            //    w,
            //    h,
            //    level,
            //    handle.AddrOfPinnedObject() + startIndex * elementSize,
            //    elementCount * elementSize
            //);
            //handle.Free();
        }

        private void SetDataBytes()
        {
            try
            {
                var dataLength = tempByteData.Length;
                var destText = UnityTexture as UnityEngine.Texture2D;
                if (destText == null)
                {
                    Debug.LogError("UnityTexture is not a Texture2D in SetDataBytes.");
                    return;
                }

                var dst = destText.GetRawTextureData<byte>();
                var textureBytesWidth = Width * 4;

                if (!tempHasRect)
                {
                    // full texture, dataLength == Width * Height * 4
                    var tmp = new byte[dataLength];
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
                }
                else
                {
                    //  sub-rectangle upload (FontStashSharp path)
                    var rect = tempRect;
                    int w = rect.Width;
                    int h = rect.Height;

                    // dataLength should be w * h * 4
                    if (dataLength < w * h * 4)
                    {
                        Debug.LogError($"SetDataBytes: dataLength ({dataLength}) < expected ({w * h * 4}).");
                        return;
                    }

                    // We'll write directly into dst (full texture) at the rect position.
                    // dst layout: row-major, bottom-left origin
                    // incoming tempByteData: row-major, TOP-left origin
                    for (int row = 0; row < h; row++)
                    {
                        int srcRow = row;               // 0 = top row of rect buffer
                        //int dstRow = (Height - (rect.Y + h)) + row; // convert to Unity bottom-left

                        // Convert XNA-top rect.Y + row to Unity-bottom row index
                        int dstRow = (Height - 1 - rect.Y) - row;

                        int srcRowStart = srcRow * w * 4;
                        int dstRowStart = (dstRow * Width + rect.X) * 4;

                        int bytesToCopy = w * 4;

                        for (int i = 0; i < bytesToCopy; i++)
                        {
                            dst[dstRowStart + i] = tempByteData[srcRowStart + i];
                        }
                    }
                }

                destText.Apply();
            }
            finally
            {
                tempByteData = null;
                tempHasRect = false;
            }
        }

        private Color[] tempColorData;

        internal void SetData(Color[] data)
        {
            tempColorData = data;
            UnityMainThreadDispatcher.Dispatch(SetDataColor);
        }

        private void SetDataColor()
        {
            try
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
            }
            finally
            {
                tempColorData = null;
            }
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
            try
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
            }
            finally
            {
                tempUIntData = null;
            }
        }

        public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
            if (!UnityMainThreadDispatcher.IsMainThread())
            {
                Debug.LogError("FromStream must be called from the main thread.");
                throw new InvalidOperationException("FromStream must be called from the main thread.");
            }

            try
            {
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
                    Debug.LogError("Failed to load texture from stream.");
                    throw new InvalidOperationException("Failed to load texture from stream.");
                }

                // Initialize the XNA texture wrapper
                var xnaTexture = new Texture2D(graphicsDevice, texture.width, texture.height)
                {
                    UnityTexture = texture
                };
                return xnaTexture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in FromStream: {ex.Message}");
                throw;
            }
        }

        // https://github.com/FNA-XNA/FNA/blob/85a8457420278087dc7a81f16661ff68e67b75af/src/Graphics/Texture2D.cs#L213
        public void SetDataPointerEXT(int level, Rectangle? rect, IntPtr data, int dataLength, bool invertY = false)
        {
            if (!UnityMainThreadDispatcher.IsMainThread())
            {
                Debug.LogError("SetDataPointerEXT must be called from the main thread.");
                throw new InvalidOperationException("SetDataPointerEXT must be called from the main thread.");
            }

            tempInvertY = invertY;
            SetDataPointerEXTInt(level, rect, data, dataLength);
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

            try
            {
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

                // MobileUO: TODO: #19: added logging output
                //Debug.Log($"Texture width: {destTex.width}, height: {destTex.height}, rect: {x},{y},{w},{h}");

                // Check if dimensions are valid
                if (x < 0 || y < 0 || x + w > destTex.width || y + h > destTex.height)
                {
                    Debug.LogError($"Texture width: {destTex.width}, height: {destTex.height}, rect: {x},{y},{w},{h}");
                    throw new ArgumentException("The specified block is outside the texture bounds.");
                }

                var colors = new Color32[w * h];

                // Copy data from the buffer to the colors array, flipping vertically
                for (int row = 0; row < h; row++)
                {
                    for (int col = 0; col < w; col++)
                    {
                        int bufferIndex = (row * w + col) * 4;
                        int colorIndex = ((h - 1 - row) * w) + col;

                        if (tempInvertY)
                        {
                            colorIndex = row * w + col;
                        }

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
                        else
                        {
                            Debug.LogError($"Buffer index out of bounds: {bufferIndex}");
                        }
                    }
                }

                destTex.SetPixels32(x, y, w, h, colors, level);
                destTex.Apply();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in SetDataPointerEXT: {ex.Message}");
                throw;
            }
        }

        // https://github.com/FNA-XNA/FNA/blob/85a8457420278087dc7a81f16661ff68e67b75af/src/Graphics/Texture2D.cs#L268
        public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
        {
            GetData(0, null, data, startIndex, elementCount);
        }

        public void GetData<T>(T[] data) where T : struct
        {
            GetData(
                0,
                null,
                data,
                0,
                data.Length
            );
        }

        public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            if (!UnityMainThreadDispatcher.IsMainThread())
            {
                Debug.LogError("GetData must be called from the main thread.");
                throw new InvalidOperationException("GetData must be called from the main thread.");
            }

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

            try
            {
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

                try
                {
                    IntPtr dataPtr = handle.AddrOfPinnedObject() + (startIndex * elementSizeInBytes);

                    // Compute the actual dimensions of this mipmap level:
                    int mipWidth = Math.Max(destTex.width >> level, 1);
                    int mipHeight = Math.Max(destTex.height >> level, 1);

                    // Convert rect origin from base-texture coords into this mip level’s coords:
                    int xMip = x >> level;
                    int yMip = y >> level;

                    for (int row = 0; row < h; row++)
                    {
                        // srcY: the Y row in the full mipmap we’re sampling from
                        int srcY = yMip + row;

                        // destY: the Y row in the output buffer, flipped so row 0 ends up at the bottom
                        int destY = (h - 1) - row;

                        for (int col = 0; col < w; col++)
                        {
                            // srcX: the X column in the full mipmap we’re sampling from
                            int srcX = xMip + col;

                            // Flatten (x, y) into a single index into the Color32[] array:
                            int srcIndex = srcY * mipWidth + srcX;

                            // Compute the linear index within the destination rectangle (width = w):
                            int destIndex = destY * w + col;

                            // Safety check to avoid overruns in either array:
                            if (srcIndex >= 0 && srcIndex < colors.Length &&
                                destIndex >= 0 && destIndex < elementCount)
                            {
                                // Marshal the Color32 at srcIndex into the pinned T[] memory
                                Marshal.StructureToPtr(
                                    colors[srcIndex],
                                    dataPtr + destIndex * elementSizeInBytes,
                                    false
                                );
                            }
                        }
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in GetData: {ex.Message}");
                throw;
            }
        }

        // https://github.com/FNA-XNA/FNA/blob/6a3ab36e521edfc6879b388037aadf9b832ec69e/src/Graphics/Texture2D.cs#L388C3-L389C4
        public void SaveAsPng(Stream stream, int width, int height)
        {
            if (!UnityMainThreadDispatcher.IsMainThread())
            {
                Debug.LogError("SaveAsPng must be called from the main thread.");
                throw new InvalidOperationException("SaveAsPng must be called from the main thread.");
            }

            if (UnityTexture == null)
            {
                throw new InvalidOperationException("Texture is not initialized.");
            }

            var texture2D = UnityTexture as UnityEngine.Texture2D;

            if (texture2D == null)
            {
                throw new InvalidOperationException("UnityTexture is not a Texture2D.");
            }

            // Ensure the texture dimensions match the requested width and height
            if (texture2D.width != width || texture2D.height != height)
            {
                throw new ArgumentException("Texture dimensions do not match the requested width and height.");
            }

            try
            {
                // Encode the texture to PNG format
                byte[] pngData = texture2D.EncodeToPNG();

                if (pngData == null)
                {
                    throw new InvalidOperationException("Failed to encode texture to PNG.");
                }

                // Write the PNG data to the provided stream
                stream.Write(pngData, 0, pngData.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in SaveAsPng: {ex.Message}");
                throw;
            }
        }

        public void SaveToFile(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                SaveAsPng(fs, Width, Height);
            }

            Debug.Log($"Saved texture to {filePath}");
        }
    }
}