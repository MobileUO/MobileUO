using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
    
    public class DynamicIndexBuffer : IndexBuffer
    {
        public DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int maxIndices, BufferUsage writeOnly) : base(graphicsDevice)
        {   
            
        }
    }
    public class IndexBuffer : GraphicsResource
    {
        protected IndexBuffer(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            
        }
        public IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize sixteenBits, int maxIndices, BufferUsage writeOnly) : base(graphicsDevice)
        {
        }

        public void SetData(short[] generateIndexArray)
        {
        }

        public void SetData<T>(
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct
        {
            //ErrorCheck(data, startIndex, elementCount);

            //GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            //FNA3D.FNA3D_SetIndexBufferData(
            //    GraphicsDevice.GLDevice,
            //    buffer,
            //    0,
            //    handle.AddrOfPinnedObject() + (startIndex * MarshalHelper.SizeOf<T>()),
            //    elementCount * MarshalHelper.SizeOf<T>(),
            //    SetDataOptions.None
            //);
            //handle.Free();
        }

        public override void Dispose()
        {
        }

        public void SetDataPointerEXT(int i, IntPtr indicesBufferPtr, int indicesBufferLength, SetDataOptions none)
        {
            throw new NotImplementedException();
        }
    }
}