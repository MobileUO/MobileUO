using System;
using ClassicUO.Renderer;

namespace Microsoft.Xna.Framework.Graphics
{
    public class DynamicVertexBuffer : VertexBuffer
    {
        public DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int maxVertices, BufferUsage writeOnly)
        {   
            
        }
        
        public DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type type, int maxVertices, BufferUsage writeOnly)
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
        }

        public void Dispose()
        {
        }
    }
}