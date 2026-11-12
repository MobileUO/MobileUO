using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace Microsoft.Xna.Framework.Graphics
{
    public class Texture2D : GraphicsResource, IDisposable
    {
        //This hash doesn't work as intended since it's not based on the contents of the UnityTexture but its instanceID
        //which will be different as old textures are discarded and new ones are created
        public Texture UnityTexture { get; protected set; }

        private bool _isFromTextureAtlas;
        public bool IsFromTextureAtlas
        {
            get => _isFromTextureAtlas;
            set
            {
                if (_isFromTextureAtlas == value) return;
                _isFromTextureAtlas = value;
                if (value && UseComputeAtlasUpload)
                {
                    if (UnityTexture is UnityEngine.Texture2D oldTex)
                        UnityEngine.Object.Destroy(oldTex);
                    InitAtlasRT();
                }
            }
        }

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
            var tex = new UnityEngine.Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            tex.filterMode = defaultFilterMode;
            tex.wrapMode = TextureWrapMode.Clamp;
            UnityTexture = tex;
        }

        private void InitAtlasRT()
        {
            var desc = new RenderTextureDescriptor(Width, Height)
            {
                depthBufferBits = 0,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                msaaSamples = 1,
                enableRandomWrite = true
            };
            var rt = new RenderTexture(desc);
            rt.filterMode = defaultFilterMode;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();
            UnityTexture = rt;
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

        internal void SetData(byte[] data)
        {
            tempByteData = data;
            UnityMainThreadDispatcher.Dispatch(SetDataBytes);
        }

        private void SetDataBytes()
        {
            try
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

        [StructLayout(LayoutKind.Sequential)]
        private struct GpuSpriteDesc
        {
            public int PixelOffset;
            public int DstX, DstY;
            public int Width, Height;
        }

        private struct SpriteDescExtended
        {
            public Texture AtlasTex;
            public int PixelOffset;
            public int DstX, DstY;
            public int Width, Height;
        }

        private const int COMPUTE_PIXEL_CAP = 1 << 20;  // 1 M pixels = 4 MB
        private const int COMPUTE_DESC_CAP = 512;

        private static bool? _useComputeAtlas;
        private static ComputeShader _atlasCS;
        private static int _atlasKernel;
        private static CommandBuffer _computeCb;
        private static GraphicsBuffer _gpuPixelBuf;
        private static GraphicsBuffer _gpuDescBuf;
        private static NativeArray<uint> _cpuPixelArr;
        private static int _cpuPixelCount;
        private static SpriteDescExtended[] _pendingDescs;
        private static int _pendingDescCount;
        private static GpuSpriteDesc[] _gpuDescTemp;
        private static readonly Texture[] _pageBuffer = new Texture[8];

        internal static bool UseComputeAtlasUpload
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _useComputeAtlas ??= InitComputePath();
        }

        private static bool InitComputePath()
        {
            if (!UnityEngine.SystemInfo.supportsComputeShaders) return false;
            _atlasCS = Resources.Load<ComputeShader>("AtlasSpriteUpload");
            if (_atlasCS == null) return false;
            _atlasKernel = _atlasCS.FindKernel("UploadSprites");
            _computeCb = new CommandBuffer { name = "AtlasSpriteUpload" };
            _gpuPixelBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, COMPUTE_PIXEL_CAP, 4);
            _gpuDescBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, COMPUTE_DESC_CAP, 20);
            _cpuPixelArr = new NativeArray<uint>(COMPUTE_PIXEL_CAP, Allocator.Persistent);
            _pendingDescs = new SpriteDescExtended[COMPUTE_DESC_CAP];
            _gpuDescTemp = new GpuSpriteDesc[COMPUTE_DESC_CAP];
            return true;
        }

        private static void GrowComputeBuffers(int minPixels, int minDescs)
        {
            if (minPixels > _gpuPixelBuf.count)
            {
                int newCap = Math.Max(minPixels, _gpuPixelBuf.count * 2);
                _gpuPixelBuf.Dispose();
                _gpuPixelBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newCap, 4);
                var newArr = new NativeArray<uint>(newCap, Allocator.Persistent);
                if (_cpuPixelCount > 0)
                    NativeArray<uint>.Copy(_cpuPixelArr, newArr, _cpuPixelCount);
                _cpuPixelArr.Dispose();
                _cpuPixelArr = newArr;
            }
            if (minDescs > _gpuDescBuf.count)
            {
                int newCap = Math.Max(minDescs, _gpuDescBuf.count * 2);
                _gpuDescBuf.Dispose();
                _gpuDescBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newCap, 20);
                var newDescs = new SpriteDescExtended[newCap];
                Array.Copy(_pendingDescs, newDescs, _pendingDescCount);
                _pendingDescs = newDescs;
                _gpuDescTemp = new GpuSpriteDesc[newCap];
            }
        }

        private static readonly int _id_Pixels = Shader.PropertyToID("_Pixels");
        private static readonly int _id_Descs = Shader.PropertyToID("_Descs");
        private static readonly int _id_Atlas = Shader.PropertyToID("_Atlas");

        public static void FlushAtlasComputeUploads()
        {
            if (_pendingDescCount == 0 || _useComputeAtlas != true) return;

            _gpuPixelBuf.SetData(_cpuPixelArr, 0, 0, _cpuPixelCount);

            int pageCount = 0;
            for (int i = 0; i < _pendingDescCount; i++)
            {
                var tex = _pendingDescs[i].AtlasTex;
                bool found = false;
                for (int p = 0; p < pageCount; p++)
                    if (_pageBuffer[p] == tex) { found = true; break; }
                if (!found && pageCount < _pageBuffer.Length)
                    _pageBuffer[pageCount++] = tex;
            }

            for (int p = 0; p < pageCount; p++)
            {
                var page = _pageBuffer[p];
                int gpuDescCount = 0;
                int maxW = 0, maxH = 0;

                for (int i = 0; i < _pendingDescCount; i++)
                {
                    ref var d = ref _pendingDescs[i];
                    if (d.AtlasTex != page) continue;
                    _gpuDescTemp[gpuDescCount++] = new GpuSpriteDesc
                    {
                        PixelOffset = d.PixelOffset,
                        DstX = d.DstX,
                        DstY = d.DstY,
                        Width = d.Width,
                        Height = d.Height
                    };
                    if (d.Width > maxW) maxW = d.Width;
                    if (d.Height > maxH) maxH = d.Height;
                }

                if (gpuDescCount == 0) continue;

                _gpuDescBuf.SetData(_gpuDescTemp, 0, 0, gpuDescCount);

                _computeCb.Clear();
                _computeCb.SetComputeBufferParam(_atlasCS, _atlasKernel, _id_Pixels, _gpuPixelBuf);
                _computeCb.SetComputeBufferParam(_atlasCS, _atlasKernel, _id_Descs, _gpuDescBuf);
                _computeCb.SetComputeTextureParam(_atlasCS, _atlasKernel, _id_Atlas, page);
                _computeCb.DispatchCompute(
                    _atlasCS, _atlasKernel,
                    (maxW + 7) / 8,
                    (maxH + 7) / 8,
                    gpuDescCount
                );
                UnityEngine.Graphics.ExecuteCommandBuffer(_computeCb);
            }

            for (int p = 0; p < pageCount; p++) _pageBuffer[p] = null;

            _cpuPixelCount = 0;
            _pendingDescCount = 0;
        }

        private static readonly HashSet<UnityEngine.Texture2D> _pendingApplySet = new HashSet<UnityEngine.Texture2D>();
        private static readonly Queue<UnityEngine.Texture2D> _pendingApplyQueue = new Queue<UnityEngine.Texture2D>();
        private static void MarkPendingApply(UnityEngine.Texture2D tex)
        {
            if (_pendingApplySet.Add(tex))
                _pendingApplyQueue.Enqueue(tex);
        }

        public static void FlushPendingApply()
        {
            while (_pendingApplyQueue.Count > 0)
            {
                var tex = _pendingApplyQueue.Dequeue();
                _pendingApplySet.Remove(tex);
                if (tex != null)
                    tex.Apply(false, false);
            }
        }

        private unsafe void SetDataPointerEXTInt(int level, Rectangle? rect, IntPtr data, int dataLength)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException(nameof(data));

            var unityTex = UnityTexture;
            if (unityTex == null)
                throw new InvalidOperationException("UnityTexture is not initialized");

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

                if (x < 0 || y < 0 || x + w > unityTex.width || y + h > unityTex.height)
                {
                    Debug.LogError($"Texture width: {unityTex.width}, height: {unityTex.height}, rect: {x},{y},{w},{h}");
                    throw new ArgumentException("The specified block is outside the texture bounds.");
                }

                byte* src = (byte*)data.ToPointer();
                int rectRowBytes = w * 4;

                if (rect.HasValue && UseComputeAtlasUpload && unityTex is RenderTexture)
                {
                    int spritePixels = w * h;
                    if (_cpuPixelCount + spritePixels > _cpuPixelArr.Length ||
                        _pendingDescCount >= _pendingDescs.Length)
                        GrowComputeBuffers(_cpuPixelCount + spritePixels, _pendingDescCount + 1);

                    uint* dst = (uint*)NativeArrayUnsafeUtility.GetUnsafePtr(_cpuPixelArr) + _cpuPixelCount;
                    Buffer.MemoryCopy(src, dst, (long)spritePixels * 4, (long)spritePixels * 4);

                    _pendingDescs[_pendingDescCount++] = new SpriteDescExtended
                    {
                        AtlasTex = unityTex,
                        PixelOffset = _cpuPixelCount,
                        DstX = x,
                        DstY = y,
                        Width = w,
                        Height = h
                    };
                    _cpuPixelCount += spritePixels;
                    // Atlas textures (IsFromTextureAtlas=true) never call GetData() so no pixel cache needed.
                }
                else
                {
                    var destTex = unityTex as UnityEngine.Texture2D;
                    if (destTex == null)
                        throw new InvalidOperationException("UnityTexture is not a Texture2D");

                    var rawDst = destTex.GetRawTextureData<byte>();
                    byte* dst = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(rawDst);
                    int texRowBytes = destTex.width * 4;

                    for (int row = 0; row < h; row++)
                    {
                        byte* srcRow = src + (tempInvertY ? row : (h - 1 - row)) * rectRowBytes;
                        byte* dstRow = dst + (y + row) * texRowBytes + x * 4;
                        Buffer.MemoryCopy(srcRow, dstRow, texRowBytes - x * 4, rectRowBytes);
                    }
                    MarkPendingApply(destTex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in SetDataPointerEXT: {ex.Message}");
                throw;
            }
        }
    }
}