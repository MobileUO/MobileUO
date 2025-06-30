using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
    
    public class DynamicIndexBuffer : IndexBuffer
    {
        public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int maxIndices, BufferUsage writeOnly) : base(graphicsDevice, indexElementSize, maxIndices, writeOnly)
        {   
            
        }
    }
    public class IndexBuffer : GraphicsResource
    {
        public short[] Data { get; private set; }

        protected IndexBuffer(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            
        }
        public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize sixteenBits, int maxIndices, BufferUsage writeOnly) : base(graphicsDevice)
        {
            Data = new short[maxIndices];
        }

        public void SetData(short[] generateIndexArray)
        {
            if (generateIndexArray == null) 
                throw new ArgumentNullException(nameof(generateIndexArray));

            Data = new short[generateIndexArray.Length];
            Array.Copy(generateIndexArray, Data, generateIndexArray.Length);
        }

        public override void Dispose()
        {
            Data = null;
        }

        public void SetDataPointerEXT(int i, IntPtr indicesBufferPtr, int indicesBufferLength, SetDataOptions none)
        {
            int indexSize = sizeof(short);
            int indexCount = indicesBufferLength / indexSize;
            int startIndex = i / indexSize;

            if (Data == null || Data.Length < startIndex + indexCount)
            {
                var newSize = startIndex + indexCount;
                var oldData = Data;
                var newData = new short[newSize];
                if (oldData != null)
                    Array.Copy(oldData, newData, oldData.Length);
                Data = newData;
            }

            for (int j = 0; j < indexCount; j++)
            {
                IntPtr ptr = IntPtr.Add(indicesBufferPtr, j * indexSize);
                Data[startIndex + j] = Marshal.ReadInt16(ptr);
            }
        }

        public short[] GetRawIndexData()
        {
            return Data;
        }
    }
}