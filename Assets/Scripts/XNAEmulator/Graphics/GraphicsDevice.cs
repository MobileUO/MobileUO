using UnityEngine;
using XNAEmulator.Graphics;
using UnityGraphics = UnityEngine.Graphics;

namespace Microsoft.Xna.Framework.Graphics
{
    public class GraphicsDevice
    {
        private readonly bool[] modifiedSamplers = new bool[16];

        public GraphicsDevice(PresentationParameters presentationParameters=null)
        {
            // TODO: Complete member initialization
            //NOTE: For now, just assume 16 slots are fine instead of trying to find GLDevice.MaxTextureSlots equivalent in Unity
            //int slots1 = Math.Min(this.GLDevice.MaxTextureSlots, 16);
            int slots1 = 16;
            this.SamplerStates = new SamplerStateCollection(slots1, this.modifiedSamplers);
            Viewport = new Viewport(0, 0, Screen.width, Screen.height);
            pPublicCachedParams = new PresentationParameters().Clone();
        }

        public Viewport Viewport { get; set; }

        public Rectangle ScissorRectangle { get; set; }
        public Color BlendFactor { get; set; }
        public BlendState BlendState { get; set; }
        public DepthStencilState DepthStencilState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public Texture2D[] Textures = new Texture2D[4];
        public SamplerStateCollection SamplerStates { get; }
        private PresentationParameters pPublicCachedParams;
        public PresentationParameters PresentationParameters
        {
            get
            {
                return this.pPublicCachedParams;
            }
            set { pPublicCachedParams = value; }
        }
        public IndexBuffer Indices { get; set; }
        public VertexBuffer VertexBuffer { get; private set; }

        internal void SetRenderTarget(RenderTarget2D renderTarget)
        {
            if (renderTarget != null)
            {
                UnityEngine.Graphics.SetRenderTarget(renderTarget.UnityTexture as RenderTexture);
                GL.LoadPixelMatrix(0, renderTarget.Width, renderTarget.Height, 0);
            }
            else
            {
                UnityEngine.Graphics.SetRenderTarget(null);
                GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            }
        }

        internal void Clear(Color color)
        {
            var unityColor = new UnityEngine.Color((float)color.R / 255, (float)color.G / 255, (float)color.B / 255, (float)color.A / 255);
            GL.Clear(true, color != Color.Transparent, unityColor);
        }

        public void Clear(ClearOptions options, Vector4 color, int depth, int stencil)
        {
            GL.Clear(depth != 0, color != Vector4.Zero, UnityEngine.Color.black);
        }

        public void Clear(ClearOptions options, Color color, int depth, int stencil)
        {
            GL.Clear(depth != 0 || (options & ClearOptions.DepthBuffer) != 0, color != Color.Transparent, UnityEngine.Color.black);
        }

        public void SetVertexBuffer(VertexBuffer dynamicVertexBuffer)
        {
            if (dynamicVertexBuffer == null)
            {
                UnityEngine.Debug.LogWarning("SetVertexBuffer: null buffer passed.");
                VertexBuffer = null;
                return;
            }

            VertexBuffer = dynamicVertexBuffer;
        }

        private MeshHolder reusedMesh = new MeshHolder(1);

        public void DrawIndexedPrimitives(
            PrimitiveType primitiveType,
            int baseVertex,
            int minVertexIndex,
            int numVertices,
            int startIndex,
            int primitiveCount,
            Material hueMaterial = null,
            int Hue = 0)
        {
            if (VertexBuffer == null || Indices == null || Textures[0] == null)
            {
                Debug.LogWarning("DrawIndexedPrimitives: missing vertex/index/texture.");
                return;
            }

            var vertexData = VertexBuffer.GetRawVertexData();
            var indexData = Indices.GetRawIndexData();

            int quadCount = primitiveCount / 2;

            reusedMesh.Clear();

            for (int i = 0; i < quadCount; i++)
            {
                int baseIdx = baseVertex + i * 4;
                if (baseIdx + 3 >= vertexData.Length)
                    break;

                reusedMesh.AddQuad(vertexData[baseIdx]);
            }

            reusedMesh.FinalizeMesh();

            var mat = hueMaterial;
            mat.mainTexture = Textures[0].UnityTexture;
            mat.SetColor(Hue, reusedMesh.CurrentHue);
            mat.SetPass(0);

            UnityGraphics.DrawMeshNow(reusedMesh.Mesh, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
        }
    }
}