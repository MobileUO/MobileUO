using ClassicUO.Renderer;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
    public class DynamicVertexBuffer : VertexBuffer
    {
        public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int maxVertices, BufferUsage writeOnly) : base(graphicsDevice, vertexDeclaration, maxVertices, writeOnly)
        {   
            
        }
        
        public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type type, int maxVertices, BufferUsage writeOnly) : base(graphicsDevice, null, maxVertices, writeOnly)
        {   
            
        }
    }
    public class VertexBuffer : GraphicsResource
    {
        internal UltimaBatcher2D.PositionNormalTextureColor4[] Data;

        public VertexBuffer()
        {
            
        }
        
        public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int maxVertices, BufferUsage writeOnly)
        {
            Data = new UltimaBatcher2D.PositionNormalTextureColor4[maxVertices];
        }

        internal void SetData(UltimaBatcher2D.PositionNormalTextureColor4[] vertexInfo)
        {
            Data = vertexInfo;
        }

        public void SetDataPointerEXT(
            int offsetInBytes,
            IntPtr data,
            int dataLength,
            SetDataOptions options)
        {
            int vertexSize = UltimaBatcher2D.PositionNormalTextureColor4.SIZE_IN_BYTES;
            int vertexCount = dataLength / vertexSize;

            if (Data == null || Data.Length < (offsetInBytes / vertexSize) + vertexCount)
            {
                Data = new UltimaBatcher2D.PositionNormalTextureColor4[(offsetInBytes / vertexSize) + vertexCount];
            }

            // Copy from unmanaged memory into managed array
            for (int i = 0; i < vertexCount; i++)
            {
                IntPtr vertexPtr = IntPtr.Add(data, i * vertexSize);
                Data[(offsetInBytes / vertexSize) + i] =
                    Marshal.PtrToStructure<UltimaBatcher2D.PositionNormalTextureColor4>(vertexPtr);
            }
        }

        public UltimaBatcher2D.PositionNormalTextureColor4[] GetRawVertexData()
        {
            return Data;
        }

        public void Dispose()
        {
        }
    }
}