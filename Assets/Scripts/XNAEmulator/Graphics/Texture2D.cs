using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Microsoft.Xna.Framework.Graphics
{
    public class Texture2D : GraphicsResource, IDisposable
    {
        //This hash doesn't work as intended since it's not based on the contents of the UnityTexture but its instanceID
        //which will be different as old textures are discarded and new ones are created
        public Texture UnityTexture { get; protected set; }

        public bool IsFromTextureAtlas { get; set; }

        public static FilterMode defaultFilterMode = FilterMode.Point;

        // Textures written to but not yet Apply'd — flushed with a per-frame time budget.
        private static readonly HashSet<UnityEngine.Texture2D> _pendingApplySet = new HashSet<UnityEngine.Texture2D>();
        private static readonly Queue<UnityEngine.Texture2D> _pendingApplyQueue = new Queue<UnityEngine.Texture2D>();
        private static readonly Stopwatch _flushTimer = new Stopwatch();
        private const long FlushBudgetMs = 2;

        private static void MarkPendingApply(UnityEngine.Texture2D tex)
        {
            if (_pendingApplySet.Add(tex))
                _pendingApplyQueue.Enqueue(tex);
        }

        public static void FlushPendingApplies()
        {
            if (_pendingApplyQueue.Count == 0) return;
            _flushTimer.Restart();
            while (_pendingApplyQueue.Count > 0)
            {
                var tex = _pendingApplyQueue.Dequeue();
                _pendingApplySet.Remove(tex);
                if (tex != null)
                    tex.Apply(false, false);
                if (_flushTimer.ElapsedMilliseconds >= FlushBudgetMs)
                    break;
            }
        }

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
                    var texToRemove = UnityTexture as UnityEngine.Texture2D;
                    _pendingApplySet.Remove(texToRemove);
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

        internal void SetData(byte[] data)
        {
            tempByteData = data;
            UnityMainThreadDispatcher.Dispatch(SetDataBytes);
        }

        private unsafe void SetDataBytes()
        {
            try
            {
                var destText = UnityTexture as UnityEngine.Texture2D;
                var dst = destText.GetRawTextureData<byte>();
                int rowBytes = Width * 4;
                int h = Height;
                byte* dstPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(dst);
                fixed (byte* srcBase = tempByteData)
                {
                    for (int y = 0; y < h; y++)
                    {
                        byte* srcRow = srcBase + (h - 1 - y) * rowBytes;
                        byte* dstRow = dstPtr + y * rowBytes;
                        Buffer.MemoryCopy(srcRow, dstRow, rowBytes, rowBytes);
                    }
                }
                MarkPendingApply(destText);
            }
            finally
            {
                tempByteData = null;
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
                int w = Width;
                int h = Height;
                var destText = UnityTexture as UnityEngine.Texture2D;
                var dst = destText.GetRawTextureData<uint>();
                for (int y = 0; y < h; y++)
                {
                    int srcRowStart = (h - 1 - y) * w;
                    int dstRowStart = y * w;
                    for (int x = 0; x < w; x++)
                        dst[dstRowStart + x] = tempColorData[srcRowStart + x].PackedValue;
                }
                MarkPendingApply(destText);
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

        private unsafe void SetDataUInt()
        {
            try
            {
                int w = Width;
                int h = Height;
                int count = (tempElementCount == 0) ? tempUIntData.Length : tempElementCount;
                var destText = UnityTexture as UnityEngine.Texture2D;
                var dst = destText.GetRawTextureData<uint>();
                uint* dstPtr = (uint*)NativeArrayUnsafeUtility.GetUnsafePtr(dst);
                fixed (uint* srcBase = tempUIntData)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int srcRow = tempInvertY ? y : (h - 1 - y);
                        int srcRowStart = tempStartOffset + srcRow * w;
                        int rowLen = Math.Min(w, count - srcRowStart);
                        if (rowLen <= 0) break;
                        Buffer.MemoryCopy(
                            srcBase + srcRowStart,
                            dstPtr + y * w,
                            (long)(h - y) * w * 4,
                            (long)rowLen * 4
                        );
                    }
                }
                MarkPendingApply(destText);
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
                byte[] imageData;
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                var texture = new UnityEngine.Texture2D(2, 2);
                if (!texture.LoadImage(imageData))
                {
                    Debug.LogError("Failed to load texture from stream.");
                    throw new InvalidOperationException("Failed to load texture from stream.");
                }

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

        private unsafe void SetDataPointerEXTInt(int level, Rectangle? rect, IntPtr data, int dataLength)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException(nameof(data));

            var destTex = UnityTexture as UnityEngine.Texture2D;
            if (destTex == null)
                throw new InvalidOperationException("UnityTexture is not a Texture2D");

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

                if (x < 0 || y < 0 || x + w > destTex.width || y + h > destTex.height)
                {
                    Debug.LogError($"Texture width: {destTex.width}, height: {destTex.height}, rect: {x},{y},{w},{h}");
                    throw new ArgumentException("The specified block is outside the texture bounds.");
                }

                var rawDst = destTex.GetRawTextureData<byte>();
                byte* src = (byte*)data.ToPointer();
                byte* dst = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(rawDst);
                int texRowBytes = destTex.width * 4;
                int rectRowBytes = w * 4;

                for (int row = 0; row < h; row++)
                {
                    byte* srcRow = src + (tempInvertY ? row : (h - 1 - row)) * rectRowBytes;
                    byte* dstRow = dst + (y + row) * texRowBytes + x * 4;
                    Buffer.MemoryCopy(srcRow, dstRow, texRowBytes - x * 4, rectRowBytes);
                }

                MarkPendingApply(destTex);
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

        public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            if (!UnityMainThreadDispatcher.IsMainThread())
            {
                Debug.LogError("GetData must be called from the main thread.");
                throw new InvalidOperationException("GetData must be called from the main thread.");
            }

            if (data == null || data.Length == 0)
                throw new ArgumentException("data cannot be null or empty");

            if (data.Length < startIndex + elementCount)
                throw new ArgumentException($"The data array length is {data.Length}, but {elementCount} elements were requested from start index {startIndex}.");

            var destTex = UnityTexture as UnityEngine.Texture2D;
            if (destTex == null)
                throw new InvalidOperationException("UnityTexture is not a Texture2D");

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

                var rawSrc = destTex.GetRawTextureData<byte>();
                int elementSizeInBytes = Marshal.SizeOf(typeof(T));
                int texRowBytes = Math.Max(destTex.width >> level, 1) * elementSizeInBytes;
                int rectRowBytes = w * elementSizeInBytes;
                int xMip = x >> level;
                int yMip = y >> level;

                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    unsafe
                    {
                        byte* dstPtr = (byte*)handle.AddrOfPinnedObject() + startIndex * elementSizeInBytes;
                        byte* srcPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(rawSrc);
                        for (int row = 0; row < h; row++)
                        {
                            int srcRow = (h - 1) - row;
                            int srcOffset = (yMip + srcRow) * texRowBytes + xMip * elementSizeInBytes;
                            int copyLen = Math.Min(rectRowBytes, rawSrc.Length - srcOffset);
                            if (copyLen <= 0) break;
                            Buffer.MemoryCopy(srcPtr + srcOffset, dstPtr + row * rectRowBytes, copyLen, copyLen);
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
                throw new InvalidOperationException("Texture is not initialized.");

            var texture2D = UnityTexture as UnityEngine.Texture2D;
            if (texture2D == null)
                throw new InvalidOperationException("UnityTexture is not a Texture2D.");

            if (texture2D.width != width || texture2D.height != height)
                throw new ArgumentException("Texture dimensions do not match the requested width and height.");

            try
            {
                byte[] pngData = texture2D.EncodeToPNG();
                if (pngData == null)
                    throw new InvalidOperationException("Failed to encode texture to PNG.");
                stream.Write(pngData, 0, pngData.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in SaveAsPng: {ex.Message}");
                throw;
            }
        }
    }
}
