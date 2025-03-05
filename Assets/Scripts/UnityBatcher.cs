using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
using Color = UnityEngine.Color;
using CompareFunction = Microsoft.Xna.Framework.Graphics.CompareFunction;
using Quaternion = UnityEngine.Quaternion;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using UnityTexture = UnityEngine.Texture;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using UnityCamera = UnityEngine.Camera;

namespace ClassicUO.Renderer
{
    internal sealed class UltimaBatcher2D : IDisposable
    {
        private static readonly float[] _cornerOffsetX = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
        private static readonly float[] _cornerOffsetY = new float[] { 0.0f, 0.0f, 1.0f, 1.0f };

        private BlendState _blendState;
        private SamplerState _sampler;
        private RasterizerState _rasterizerState;
        private bool _started;
        private DepthStencilState _stencil;
        private bool _useScissor;
        private int _numSprites;
        private Matrix _transformMatrix;
        private Matrix _projectionMatrix = new Matrix(0f,                         //(float)( 2.0 / (double)viewport.Width ) is the actual value we will use
                                                      0.0f, 0.0f, 0.0f, 0.0f, 0f, //(float)( -2.0 / (double)viewport.Height ) is the actual value we will use
                                                      0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f);
        private readonly BasicUOEffect _basicUOEffect;

        private Material hueMaterial;
        private Material xbrMaterial;

        private MeshHolder reusedMesh = new MeshHolder(1);

        public float scale = 1;
        
        public bool UseGraphicsDrawTexture;

        private Mesh draw2DMesh;
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Hue = Shader.PropertyToID("_Hue");
        private static readonly int HueTex1 = Shader.PropertyToID("_HueTex1");
        private static readonly int HueTex2 = Shader.PropertyToID("_HueTex2");
        private static readonly int HueTex3 = Shader.PropertyToID("_HueTex3");
        private static readonly int Brightlight = Shader.PropertyToID("_Brightlight");
        private static readonly int UvMirrorX = Shader.PropertyToID("_uvMirrorX");
        private static readonly int Scissor = Shader.PropertyToID("_Scissor");
        private static readonly int ScissorRect = Shader.PropertyToID("_ScissorRect");
        private static readonly int TextureSize = Shader.PropertyToID("textureSize");

        // MobileUO: TODO: flag to use depths while trying to figure out the depth issue
        private bool USE_DEPTH = false;
        private bool LOG_DEPTH = false;
        private bool DIVIDE_DEPTH = false; // if depth values are 100 or lower, they will render. Something clips them at over 100 (100.1 or 101 or higher)

        public UltimaBatcher2D(GraphicsDevice device)
        {
            if (USE_DEPTH)
            {
                UnityCamera.main.nearClipPlane = 0.01f;
                UnityCamera.main.farClipPlane = 10000f;
            }

            GraphicsDevice = device;
            _blendState = BlendState.AlphaBlend;
            //_rasterizerState = RasterizerState.CullNone;
            _sampler = SamplerState.PointClamp;

            _rasterizerState = new RasterizerState
            {
                CullMode = Microsoft.Xna.Framework.Graphics.CullMode.CullCounterClockwiseFace,
                FillMode = FillMode.Solid,
                DepthBias = 0,
                MultiSampleAntiAlias = true,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0,
            };

            _stencil = Stencil;
            _basicUOEffect = new BasicUOEffect(device);

            hueMaterial = new Material(UnityEngine.Resources.Load<Shader>("HueShader"));
            xbrMaterial = new Material(UnityEngine.Resources.Load<Shader>("XbrShader"));
        }

        public Matrix TransformMatrix => _transformMatrix;

        private Effect CustomEffect;

        private DepthStencilState Stencil { get; } = new DepthStencilState
        {
            StencilEnable = false,
            DepthBufferEnable = false,
            StencilFunction = CompareFunction.NotEqual,
            ReferenceStencil = -1,
            StencilMask = -1,
            StencilFail = StencilOperation.Keep,
            StencilDepthBufferFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Keep
        };

        public GraphicsDevice GraphicsDevice { get; }

        public int TextureSwitches, FlushesDone;

        public void SetBrightlight(float f)
        {
            // MobileUO: pass Brightlight value to shader
            hueMaterial.SetFloat(Brightlight, f);
            _basicUOEffect.Brighlight.SetValue(f);
        }

        public void DrawString(SpriteFont spriteFont, string text, int x, int y, XnaVector3 color)
        {
            if (String.IsNullOrEmpty(text))
                return;

            Texture2D textureValue = spriteFont.Texture;
            List<Rectangle> glyphData = spriteFont.GlyphData;
            List<Rectangle> croppingData = spriteFont.CroppingData;
            List<XnaVector3> kerning = spriteFont.Kerning;
            List<char> characterMap = spriteFont.CharacterMap;

            XnaVector2 curOffset = XnaVector2.Zero;
            bool firstInLine = true;

            XnaVector2 baseOffset = XnaVector2.Zero;
            float axisDirX = 1;
            float axisDirY = 1;

            foreach (char c in text)
            {
                // Special characters
                if (c == '\r') continue;

                if (c == '\n')
                {
                    curOffset.X = 0.0f;
                    curOffset.Y += spriteFont.LineSpacing;
                    firstInLine = true;

                    continue;
                }

                /* Get the List index from the character map, defaulting to the
				 * DefaultCharacter if it's set.
				 */
                int index = characterMap.IndexOf(c);

                if (index == -1)
                {
                    if (!spriteFont.DefaultCharacter.HasValue)
                    {
                        throw new ArgumentException(
                            "Text contains characters that cannot be" +
                            " resolved by this SpriteFont.",
                            "text"
                        );
                    }

                    index = characterMap.IndexOf(
                        spriteFont.DefaultCharacter.Value
                    );
                }

                /* For the first character in a line, always push the width
				 * rightward, even if the kerning pushes the character to the
				 * left.
				 */
                XnaVector3 cKern = kerning[index];

                if (firstInLine)
                {
                    curOffset.X += Math.Abs(cKern.X);
                    firstInLine = false;
                }
                else
                    curOffset.X += (spriteFont.Spacing + cKern.X);

                // Calculate the character origin
                Rectangle cCrop = croppingData[index];
                Rectangle cGlyph = glyphData[index];

                float offsetX = baseOffset.X + (
                                    curOffset.X + cCrop.X
                                ) * axisDirX;

                float offsetY = baseOffset.Y + (
                                    curOffset.Y + cCrop.Y
                                ) * axisDirY;

                Draw
                (
                    textureValue,
                    new XnaVector2
                    (
                        x + (int) Math.Round(offsetX),
                        y + (int) Math.Round(offsetY)
                    ),
                    cGlyph,
                    color
                );

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

        // MobileUO: TODO: deprecated, to be deleted
        public bool DrawSprite(Texture2D texture, int x, int y, bool mirror, ref XnaVector3 hue)
        {
            if (texture.UnityTexture == null)
            {
                return false;
            }
            
            int w = texture.Width;
            int h = texture.Height;

            if (UseGraphicsDrawTexture)
            {
                var rect = new Rect(x * scale, y * scale, w * scale, h * scale);
                hueMaterial.SetColor(Hue, new Color(hue.X,hue.Y,hue.Z));
                hueMaterial.SetFloat(UvMirrorX, mirror ? 1 : 0);
                Graphics.DrawTexture(rect,
                    texture.UnityTexture,new Rect(0,0,1,1),
                    0, 0,0,0, hueMaterial);
            }
            else
            {
                var vertex = new PositionNormalTextureColor4();

                if (mirror)
                {
                    vertex.Position0.x = x + w;
                    vertex.Position0.y = y + h;
                    vertex.Position0.z = 0;
                    vertex.Normal0.x = 0;
                    vertex.Normal0.y = 0;
                    vertex.Normal0.z = 1;
                    vertex.TextureCoordinate0.x = 0;
                    vertex.TextureCoordinate0.y = 1;
                    vertex.TextureCoordinate0.z = 0;

                    vertex.Position1.x = x;
                    vertex.Position1.y = y + h;
                    vertex.Position0.z = 0;
                    vertex.Normal1.x = 0;
                    vertex.Normal1.y = 0;
                    vertex.Normal1.z = 1;
                    vertex.TextureCoordinate1.x = 1;
                    vertex.TextureCoordinate1.y = 1;
                    vertex.TextureCoordinate1.z = 0;

                    vertex.Position2.x = x + w;
                    vertex.Position2.y = y;
                    vertex.Position2.z = 0;
                    vertex.Normal2.x = 0;
                    vertex.Normal2.y = 0;
                    vertex.Normal2.z = 1;
                    vertex.TextureCoordinate2.x = 0;
                    vertex.TextureCoordinate2.y = 0;
                    vertex.TextureCoordinate2.z = 0;

                    vertex.Position3.x = x;
                    vertex.Position3.y = y;
                    vertex.Position3.z = 0;
                    vertex.Normal3.x = 0;
                    vertex.Normal3.y = 0;
                    vertex.Normal3.z = 1;
                    vertex.TextureCoordinate3.x = 1;
                    vertex.TextureCoordinate3.y = 0;
                    vertex.TextureCoordinate3.z = 0;
                }
                else
                {
                    vertex.Position0.x = x;
                    vertex.Position0.y = y + h;
                    vertex.Position0.z = 0;
                    vertex.Normal0.x = 0;
                    vertex.Normal0.y = 0;
                    vertex.Normal0.z = 1;
                    vertex.TextureCoordinate0.x = 0;
                    vertex.TextureCoordinate0.y = 1;
                    vertex.TextureCoordinate0.z = 0;

                    vertex.Position1.x = x + w;
                    vertex.Position1.y = y + h;
                    vertex.Position1.z = 0;
                    vertex.Normal1.x = 0;
                    vertex.Normal1.y = 0;
                    vertex.Normal1.z = 1;
                    vertex.TextureCoordinate1.x = 1;
                    vertex.TextureCoordinate1.y = 1;
                    vertex.TextureCoordinate1.z = 0;

                    vertex.Position2.x = x;
                    vertex.Position2.y = y;
                    vertex.Position2.z = 0;
                    vertex.Normal2.x = 0;
                    vertex.Normal2.y = 0;
                    vertex.Normal2.z = 1;
                    vertex.TextureCoordinate2.x = 0;
                    vertex.TextureCoordinate2.y = 0;
                    vertex.TextureCoordinate2.z = 0;

                    vertex.Position3.x = x + w;
                    vertex.Position3.y = y;
                    vertex.Position3.z = 0;
                    vertex.Normal3.x = 0;
                    vertex.Normal3.y = 0;
                    vertex.Normal3.z = 1;
                    vertex.TextureCoordinate3.x = 1;
                    vertex.TextureCoordinate3.y = 0;
                    vertex.TextureCoordinate3.z = 0;
                }

                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
            
                RenderVertex(vertex, texture, hue);
            }

            return true;
        }

        // MobileUO: TODO: deprecated, to be deleted
        public void DrawSpriteRotated(Texture2D texture, int x, int y, float width, float height, ref XnaVector3 hue, float angle)
        {
            if (texture.UnityTexture == null)
            {
                return;
            }

            var vertex = new PositionNormalTextureColor4();

            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);

             // Rotation Calculations
            float rotationMatrix1X = cos;
            float rotationMatrix1Y = sin;
            float rotationMatrix2X = -sin;
            float rotationMatrix2Y = cos;

            var cornerX = (_cornerOffsetX[0] - 0) * width;
            var cornerY = (_cornerOffsetY[0] - 0) * height;
            vertex.Position0.x = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position0.y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.Normal0.x = 0;
            vertex.Normal0.y = 0;
            vertex.Normal0.z = 1;
            vertex.TextureCoordinate0.x = _cornerOffsetX[0];
            vertex.TextureCoordinate0.y = _cornerOffsetY[0];
            vertex.TextureCoordinate0.z = 0;

            cornerX = (_cornerOffsetX[1] - 0) * width;
            cornerY = (_cornerOffsetY[1] - 0) * height;
            vertex.Position1.x = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position1.y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.Normal1.x = 0;
            vertex.Normal1.y = 0;
            vertex.Normal1.z = 1;
            vertex.TextureCoordinate1.x = _cornerOffsetX[1];
            vertex.TextureCoordinate1.y = _cornerOffsetY[1];
            vertex.TextureCoordinate1.z = 0;

            cornerX = (_cornerOffsetX[2] - 0) * width;
            cornerY = (_cornerOffsetY[2] - 0) * height;
            vertex.Position2.x = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position2.y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.TextureCoordinate2.x = _cornerOffsetX[2];
            vertex.TextureCoordinate2.y = _cornerOffsetY[2];
            vertex.TextureCoordinate2.z = 0;

            cornerX = (_cornerOffsetX[3] - 0) * width;
            cornerY = (_cornerOffsetY[3] - 0) * height;
            vertex.Position3.x = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position3.y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.TextureCoordinate3.x = _cornerOffsetX[3];
            vertex.TextureCoordinate3.y = _cornerOffsetY[3];
            vertex.TextureCoordinate3.z = 0;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            RenderVertex(vertex, texture, hue);
        }

        // ==========================
        // === UO drawing methods ===
        // ==========================
        public struct YOffsets
        {
            public int Top;
            public int Right;
            public int Left;
            public int Bottom;
        }

        // MobileUO: TODO: deprecated, to be deleted
        [MethodImpl(256)]
        public bool DrawSpriteLand
        (
            Texture2D texture, 
            int x, 
            int y, 
            int sx,
            int sy,
            float swidth,
            float sheight,
            ref YOffsets yOffsets, 
            ref XnaVector3 normalTop, 
            ref XnaVector3 normalRight, 
            ref XnaVector3 normalLeft, 
            ref XnaVector3 normalBottom, 
            ref XnaVector3 hue,
            float depth
        )
        {
            if (texture.UnityTexture == null)
            {
                return false;
            }

            // MobileUO: TODO: temp fix to keep things stable - hopefully future commit makes depth work
            if(!USE_DEPTH)
                depth = 0;

            if(DIVIDE_DEPTH)
                depth = depth / 1000f;

            if(LOG_DEPTH)
                Log.Info($"Depth: {depth}");

            float sourceX = ((sx + 0.5f) / (float)texture.Width);
            float sourceY = ((sy + 0.5f) / (float)texture.Height);
            float sourcwW = ((swidth - 1f) / (float)texture.Width);
            float sourceH = ((sheight - 1f) / (float)texture.Height);

            //float sourceX = ((sx) / (float)texture.Width);
            //float sourceY = ((sy) / (float)texture.Height);
            //float sourcwW = ((swidth) / (float)texture.Width);
            //float sourceH = ((sheight) / (float)texture.Height);
            
            var vertex = new PositionNormalTextureColor4();

            vertex.TextureCoordinate0.x = (_cornerOffsetX[0] * sourcwW) + sourceX;
            vertex.TextureCoordinate0.y = (_cornerOffsetY[0] * sourceH) + sourceY;
            vertex.TextureCoordinate0.z = 0;

            vertex.TextureCoordinate1.x = (_cornerOffsetX[1] * sourcwW) + sourceX;
            vertex.TextureCoordinate1.y = (_cornerOffsetY[1] * sourceH) + sourceY;
            vertex.TextureCoordinate1.z = 0;

            vertex.TextureCoordinate2.x = (_cornerOffsetX[2] * sourcwW) + sourceX;
            vertex.TextureCoordinate2.y = (_cornerOffsetY[2] * sourceH) + sourceY;
            vertex.TextureCoordinate2.z = 0;

            vertex.TextureCoordinate3.x = (_cornerOffsetX[3] * sourcwW) + sourceX;
            vertex.TextureCoordinate3.y = (_cornerOffsetY[3] * sourceH) + sourceY;
            vertex.TextureCoordinate3.z = 0;

            vertex.Normal0 = normalTop;
            vertex.Normal1 = normalRight;
            vertex.Normal2 = normalLeft;
            vertex.Normal3 = normalBottom;

            // Top
            vertex.Position0.x = x + 22;
            vertex.Position0.y = y - yOffsets.Top;
            vertex.Position0.z = depth;

            // Right
            vertex.Position1.x = x + 44;
            vertex.Position1.y = y + (22 - yOffsets.Right);
            vertex.Position1.z = depth;

            // Left
            vertex.Position2.x = x;
            vertex.Position2.y = y + (22 - yOffsets.Left);
            vertex.Position2.z = depth;

            // Bottom
            vertex.Position3.x = x + 22;
            vertex.Position3.y = y + (44 - yOffsets.Bottom);
            vertex.Position3.z = depth;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            RenderVertex(vertex, texture, hue);

            return true;
        }

        public void DrawStretchedLand
        (
            Texture2D texture,
            XnaVector2 position,
            Rectangle sourceRect,
            ref YOffsets yOffsets,
            ref XnaVector3 normalTop,
            ref XnaVector3 normalRight,
            ref XnaVector3 normalLeft,
            ref XnaVector3 normalBottom,
            XnaVector3 hue,
            float depth
        )
        {
            // MobileUO: TODO: since we are currently not using ref _vertexInfo[_numSprites - 1] array, use the old rendering method
            // If we switch to using _vertexInfo, then the below should work since the Draw method calls SetVertex() which does our vertex setup in the ref _vertexInfo array
            DrawSpriteLand(
                texture,
                (int)position.X,
                (int)position.Y,
                sourceRect.X,
                sourceRect.Y,
                sourceRect.Width,
                sourceRect.Height,
                ref yOffsets,
                ref normalTop,
                ref normalRight,
                ref normalLeft,
                ref normalBottom,
                ref hue,
                depth
                );

            return;

            if (texture.UnityTexture == null)
            {
                return;
            }

            Draw
            (
                texture,
                position,
                sourceRect,
                hue,
                0f,
                XnaVector2.Zero,
                0f,
                SpriteEffects.None,
                depth
            );

            //ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites - 1];
            var vertex = new PositionNormalTextureColor4();
            // we need to apply an offset to the texture
            float sourceX = ((sourceRect.X + 0.5f) / (float)texture.Width);
            float sourceY = ((sourceRect.Y + 0.5f) / (float)texture.Height);
            float sourceW = ((sourceRect.Width - 1f) / (float)texture.Width);
            float sourceH = ((sourceRect.Height - 1f) / (float)texture.Height);

            vertex.TextureCoordinate0.x = (_cornerOffsetX[0] * sourceW) + sourceX;
            vertex.TextureCoordinate0.y = (_cornerOffsetY[0] * sourceH) + sourceY;
            vertex.TextureCoordinate1.x = (_cornerOffsetX[1] * sourceW) + sourceX;
            vertex.TextureCoordinate1.y = (_cornerOffsetY[1] * sourceH) + sourceY;
            vertex.TextureCoordinate2.x = (_cornerOffsetX[2] * sourceW) + sourceX;
            vertex.TextureCoordinate2.y = (_cornerOffsetY[2] * sourceH) + sourceY;
            vertex.TextureCoordinate3.x = (_cornerOffsetX[3] * sourceW) + sourceX;
            vertex.TextureCoordinate3.y = (_cornerOffsetY[3] * sourceH) + sourceY;
            vertex.TextureCoordinate0.z = 0;
            vertex.TextureCoordinate1.z = 0;
            vertex.TextureCoordinate2.z = 0;
            vertex.TextureCoordinate3.z = 0;

            vertex.Normal0 = normalTop;
            vertex.Normal1 = normalRight;
            vertex.Normal2 = normalLeft;
            vertex.Normal3 = normalBottom;

            // Top
            vertex.Position0.x = position.X + 22;
            vertex.Position0.y = position.Y - yOffsets.Top;
            
            // Right
            vertex.Position1.x = position.X + 44;
            vertex.Position1.y = position.Y + (22 - yOffsets.Right);

            // Left
            vertex.Position2.x = position.X;
            vertex.Position2.y = position.Y + (22 - yOffsets.Left);

            // Bottom
            vertex.Position3.x = position.X + 22;
            vertex.Position3.y = position.Y + (44 - yOffsets.Bottom);


            vertex.Position0.z = depth;
            vertex.Position1.z = depth;
            vertex.Position2.z = depth;
            vertex.Position3.z = depth;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            RenderVertex(vertex, texture, hue);
        }

        // MobileUO: TODO: deprecated, to be deleted
        public void DrawSpriteShadow(Texture2D texture, int x, int y, bool flip)
        { 
            if (texture.UnityTexture == null)
            {
                return;
            }
            
            var vertex = new PositionNormalTextureColor4();

            float width = texture.Width;
            float height = texture.Height * 0.5f;

            float translatedY = y + height - 10;

            float ratio = height / width;

            if (flip)
            {
                vertex.Position0.x = x + width;
                vertex.Position0.y = translatedY + height;
                vertex.Position0.z = 0;
                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;
                vertex.TextureCoordinate0.x = 0;
                vertex.TextureCoordinate0.y = 1;
                vertex.TextureCoordinate0.z = 0;

                vertex.Position1.x = x;
                vertex.Position1.y = translatedY + height;
                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;
                vertex.TextureCoordinate1.x = 1;
                vertex.TextureCoordinate1.y = 1;
                vertex.TextureCoordinate1.z = 0;

                vertex.Position2.x = x + width * (ratio + 1f);
                vertex.Position2.y = translatedY;
                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;
                vertex.TextureCoordinate2.x = 0;
                vertex.TextureCoordinate2.y = 0;
                vertex.TextureCoordinate2.z = 0;

                vertex.Position3.x = x + width * ratio;
                vertex.Position3.y = translatedY;
                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;
                vertex.TextureCoordinate3.x = 1;
                vertex.TextureCoordinate3.y = 0;
                vertex.TextureCoordinate3.z = 0;
            }
            else
            {
                vertex.Position0.x = x;
                vertex.Position0.y = translatedY + height;
                vertex.Position0.z = 0;
                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;
                vertex.TextureCoordinate0.x = 0;
                vertex.TextureCoordinate0.y = 1;
                vertex.TextureCoordinate0.z = 0;

                vertex.Position1.x = x + width;
                vertex.Position1.y = translatedY + height;
                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;
                vertex.TextureCoordinate1.x = 1;
                vertex.TextureCoordinate1.y = 1;
                vertex.TextureCoordinate1.z = 0;

                vertex.Position2.x = x + width * ratio;
                vertex.Position2.y = translatedY;
                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;
                vertex.TextureCoordinate2.x = 0;
                vertex.TextureCoordinate2.y = 0;
                vertex.TextureCoordinate2.z = 0;

                vertex.Position3.x = x + width * (ratio + 1f);
                vertex.Position3.y = translatedY;
                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;
                vertex.TextureCoordinate3.x = 1;
                vertex.TextureCoordinate3.y = 0;
                vertex.TextureCoordinate3.z = 0;
            }

            vertex.Hue0.z = vertex.Hue1.z = vertex.Hue2.z = vertex.Hue3.z = vertex.Hue0.x = vertex.Hue1.x = vertex.Hue2.x = vertex.Hue3.x = 0;

            vertex.Hue0.y = vertex.Hue1.y = vertex.Hue2.y = vertex.Hue3.y = ShaderHueTranslator.SHADER_SHADOW;

            RenderVertex(vertex, texture, vertex.Hue0);
        }

        public void DrawShadow(Texture2D texture, XnaVector2 position, Rectangle sourceRect, bool flip, float depth)
        {
            if (texture.UnityTexture == null)
            {
                return;
            }

            // MobileUO: TODO: temp fix to keep things stable - hopefully future commit makes depth work
            if(!USE_DEPTH)
                depth = 0;

            if(DIVIDE_DEPTH)
                depth = depth / 1000f;

            if(LOG_DEPTH)
                Log.Info($"Depth: {depth}");

            float width = sourceRect.Width;
            float height = sourceRect.Height * 0.5f;
            float translatedY = position.Y + height - 10;
            float ratio = height / width;

            //EnsureSize();

            //ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
            var vertex = new PositionNormalTextureColor4();

            vertex.Position0.x = position.X + width * ratio;
            vertex.Position0.y = translatedY;

            vertex.Position1.x = position.X + width * (ratio + 1f);
            vertex.Position1.y = translatedY;

            vertex.Position2.x = position.X;
            vertex.Position2.y = translatedY + height;

            vertex.Position3.x = position.X + width;
            vertex.Position3.y = translatedY + height;

            vertex.Position0.z = depth;
            vertex.Position1.z = depth;
            vertex.Position2.z = depth;
            vertex.Position3.z = depth;


            float sourceX = ((sourceRect.X + 0.5f) / (float)texture.Width);
            float sourceY = ((sourceRect.Y + 0.5f) / (float)texture.Height);
            float sourceW = ((sourceRect.Width - 1f) / (float)texture.Width);
            float sourceH = ((sourceRect.Height - 1f) / (float)texture.Height);

            byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

            vertex.TextureCoordinate0.x = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate0.y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate1.x = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate1.y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate2.x = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate2.y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate3.x = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate3.y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate0.z = 0;
            vertex.TextureCoordinate1.z = 0;
            vertex.TextureCoordinate2.z = 0;
            vertex.TextureCoordinate3.z = 0;
           
            vertex.Normal0.x = 0;
            vertex.Normal0.y = 0;
            vertex.Normal0.z = 1;

            vertex.Normal1.x = 0;
            vertex.Normal1.y = 0;
            vertex.Normal1.z = 1;

            vertex.Normal2.x = 0;
            vertex.Normal2.y = 0;
            vertex.Normal2.z = 1;

            vertex.Normal3.x = 0;
            vertex.Normal3.y = 0;
            vertex.Normal3.z = 1;

            vertex.Hue0.z = vertex.Hue1.z = vertex.Hue2.z = vertex.Hue3.z = vertex.Hue0.x = vertex.Hue1.x = vertex.Hue2.x = vertex.Hue3.x = 0;
            vertex.Hue0.y = vertex.Hue1.y = vertex.Hue2.y = vertex.Hue3.y = ShaderHueTranslator.SHADER_SHADOW;

            //PushSprite(texture);
            RenderVertex(vertex, texture, vertex.Hue0);
        }

        private void RenderVertex(PositionNormalTextureColor4 vertex, Texture2D texture, Vector3 hue)
        {
            vertex.Position0 *= scale;
            vertex.Position1 *= scale;
            vertex.Position2 *= scale;
            vertex.Position3 *= scale;

            reusedMesh.Populate(vertex);

            var mat = hueMaterial;
            mat.mainTexture = texture.UnityTexture;
            mat.SetColor(Hue, new Color(hue.x,hue.y,hue.z));
            mat.SetPass(0);

            Graphics.DrawMeshNow(reusedMesh.Mesh, Vector3.zero, Quaternion.identity);
        }

        public void DrawCharacterSitted
        (
            Texture2D texture,
            XnaVector2 position,
            Rectangle sourceRect,
            XnaVector3 mod,
            XnaVector3 hue,
            bool flip,
            float depth
        )
        { 
            if (texture.UnityTexture == null)
            {
                return;
            }

            // MobileUO: TODO: temp fix to keep things stable - hopefully future commit makes depth work
            if(!USE_DEPTH)
                depth = 0;

            if(DIVIDE_DEPTH)
                depth = depth / 1000f;

            if(LOG_DEPTH)
                Log.Info($"Depth: {depth}");

            float h03 = sourceRect.Height * mod.X;
            float h06 = sourceRect.Height * mod.Y;
            float h09 = sourceRect.Height * mod.Z;

            float sittingOffset = flip ? -8.0f : 8.0f;

            float width = sourceRect.Width;
            float widthOffset = sourceRect.Width + sittingOffset;

            if (mod.X != 0.0f)
            {
                //ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
                var vertex = new PositionNormalTextureColor4();

                vertex.Position0.x = position.X + sittingOffset;
                vertex.Position0.y = position.Y;

                vertex.Position1.x = position.X + widthOffset;
                vertex.Position1.y = position.Y;

                vertex.Position2.x = position.X + sittingOffset;
                vertex.Position2.y = position.Y + h03;

                vertex.Position3.x = position.X + widthOffset;
                vertex.Position3.y = position.Y + h03;

                vertex.Position0.z = depth;
                vertex.Position1.z = depth;
                vertex.Position2.z = depth;
                vertex.Position3.z = depth;

                float sourceX = ((sourceRect.X + 0.5f) / (float)texture.Width);
                float sourceY = ((sourceRect.Y + 0.5f) / (float)texture.Height);
                float sourceW = ((sourceRect.Width - 1f) / (float)texture.Width);
                float sourceH = ((sourceRect.Height - 1f) / (float)texture.Height);

                byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

                vertex.TextureCoordinate0.x = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate0.y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
                vertex.TextureCoordinate1.x = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate1.y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
                vertex.TextureCoordinate2.x = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate2.y = (_cornerOffsetY[2 ^ effects] * sourceH * mod.X) + sourceY;
                vertex.TextureCoordinate3.x = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate3.y = (_cornerOffsetY[3 ^ effects] * sourceH * mod.X) + sourceY;
                vertex.TextureCoordinate0.z = 0;
                vertex.TextureCoordinate1.z = 0;
                vertex.TextureCoordinate2.z = 0;
                vertex.TextureCoordinate3.z = 0;

                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;

                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;

                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;

                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;

                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                RenderVertex(vertex, texture, hue);
            }

            if (mod.Y != 0.0f)
            {
                //ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
                var vertex = new PositionNormalTextureColor4();

                vertex.Position0.x = position.X + sittingOffset;
                vertex.Position0.y = position.Y + h03;

                vertex.Position1.x = position.X + widthOffset;
                vertex.Position1.y = position.Y + h03;

                vertex.Position2.x = position.X;
                vertex.Position2.y = position.Y + h06;

                vertex.Position3.x = position.X + width;
                vertex.Position3.y = position.Y + h06;

                vertex.Position0.z = depth;
                vertex.Position1.z = depth;
                vertex.Position2.z = depth;
                vertex.Position3.z = depth;

                float sourceX = ((sourceRect.X + 0.5f) / (float)texture.Width);
                float sourceY = ((sourceRect.Y + 0.5f + h03) / (float)texture.Height);
                float sourceW = ((sourceRect.Width - 1f) / (float)texture.Width);
                float sourceH = ((sourceRect.Height - 1f - h03) / (float)texture.Height);

                byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

                vertex.TextureCoordinate0.x = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate0.y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
                vertex.TextureCoordinate1.x = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate1.y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
                vertex.TextureCoordinate2.x = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate2.y = (_cornerOffsetY[2 ^ effects] * sourceH * mod.Y) + sourceY;
                vertex.TextureCoordinate3.x = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate3.y = (_cornerOffsetY[3 ^ effects] * sourceH * mod.Y) + sourceY;
                vertex.TextureCoordinate0.z = 0;
                vertex.TextureCoordinate1.z = 0;
                vertex.TextureCoordinate2.z = 0;
                vertex.TextureCoordinate3.z = 0;

                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;

                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;

                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;

                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;

                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                RenderVertex(vertex, texture, hue);
            }

            if (mod.Z != 0.0f)
            {
                //ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
                var vertex = new PositionNormalTextureColor4();

                vertex.Position0.x = position.X;
                vertex.Position0.y = position.Y + h06;

                vertex.Position1.x = position.X + width;
                vertex.Position1.y = position.Y + h06;

                vertex.Position2.x = position.X;
                vertex.Position2.y = position.Y + h09;

                vertex.Position3.x = position.X + width;
                vertex.Position3.y = position.Y + h09;

                vertex.Position0.z = depth;
                vertex.Position1.z = depth;
                vertex.Position2.z = depth;
                vertex.Position3.z = depth;

                float sourceX = ((sourceRect.X + 0.5f) / (float)texture.Width);
                float sourceY = ((sourceRect.Y + 0.5f + h06) / (float)texture.Height);
                float sourceW = ((sourceRect.Width - 1f) / (float)texture.Width);
                float sourceH = ((sourceRect.Height - 1f - h06) / (float)texture.Height);

                byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

                vertex.TextureCoordinate0.x = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate0.y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
                vertex.TextureCoordinate1.x = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate1.y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
                vertex.TextureCoordinate2.x = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate2.y = (_cornerOffsetY[2 ^ effects] * sourceH * mod.Z) + sourceY;
                vertex.TextureCoordinate3.x = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
                vertex.TextureCoordinate3.y = (_cornerOffsetY[3 ^ effects] * sourceH * mod.Z) + sourceY;
                vertex.TextureCoordinate0.z = 0;
                vertex.TextureCoordinate1.z = 0;
                vertex.TextureCoordinate2.z = 0;
                vertex.TextureCoordinate3.z = 0;

                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;

                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;

                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;

                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;

                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                RenderVertex(vertex, texture, hue);
            }
        }

        // MobileUO: TODO: deprecated, to be deleted
        public bool Draw2D(Texture2D texture, int x, int y, ref XnaVector3 hue)
        {
            if (texture.UnityTexture == null)
            {
                return false;
            }
            
            if (UseGraphicsDrawTexture)
            {
                var rect = new Rect(x * scale, y * scale, texture.Width * scale, texture.Height * scale);
                hueMaterial.SetColor(Hue, new Color(hue.X,hue.Y,hue.Z));
                hueMaterial.SetFloat(UvMirrorX, 0);
                Graphics.DrawTexture(rect,
                    texture.UnityTexture,new Rect(0,0,1,1),
                    0, 0,0,0, hueMaterial);
            }
            else
            {
                var vertex = new PositionNormalTextureColor4();

                vertex.Position0.x = x;
                vertex.Position0.y = y;
                vertex.Position0.z = 0;
                vertex.TextureCoordinate0.x = 0;
                vertex.TextureCoordinate0.y = 0;
                vertex.TextureCoordinate0.z = 0;

                vertex.Position1.x = x + texture.Width;
                vertex.Position1.y = y;
                vertex.Position1.z = 0;
                vertex.TextureCoordinate1.x = 1;
                vertex.TextureCoordinate1.y = 0;
                vertex.TextureCoordinate1.z = 0;

                vertex.Position2.x = x;
                vertex.Position2.y = y + texture.Height;
                vertex.Position2.z = 0;
                vertex.TextureCoordinate2.x = 0;
                vertex.TextureCoordinate2.y = 1;
                vertex.TextureCoordinate2.z = 0;

                vertex.Position3.x = x + texture.Width;
                vertex.Position3.y = y + texture.Height;
                vertex.Position3.z = 0;
                vertex.TextureCoordinate3.x = 1;
                vertex.TextureCoordinate3.y = 1;
                vertex.TextureCoordinate3.z = 0;

                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
                
                RenderVertex(vertex, texture, hue);
            }

            return true;
        }

        // MobileUO: TODO: deprecated, to be deleted
        public bool Draw2D(Texture2D texture, int x, int y, int sx, int sy, float swidth, float sheight, ref XnaVector3 hue)
        { 
            if (texture.UnityTexture == null)
            {
                return false;
            }
            
            //float minX = ((sx + 0.5f) / (float)texture.Width);
            //float minY = ((sy + 0.5f) / (float)texture.Height);
            //float maxX = (((sx + swidth) - 1f) / (float)texture.Width);
            //float maxY = (((sy + sheight) - 1f) / (float)texture.Height);

            float minX = sx / (float)texture.Width;
            float maxX = (sx + swidth) / texture.Width;
            float minY = sy / (float)texture.Height;
            float maxY = (sy + sheight) / texture.Height;

            if (UseGraphicsDrawTexture)
            {
                hueMaterial.SetColor(Hue, new Color(hue.X,hue.Y,hue.Z));
                hueMaterial.SetFloat(UvMirrorX, 0);
                //NOTE: given sourceRect needs to be flipped vertically for some reason
                Graphics.DrawTexture(new Rect(x * scale, y * scale, swidth * scale, sheight * scale),
                    texture.UnityTexture,new Rect(minX, 1 - maxY, maxX - minX, maxY - minY),
                    0, 0,0,0, hueMaterial);
            }
            else
            {
                var vertex = new PositionNormalTextureColor4();

                vertex.Position0.x = x;
                vertex.Position0.y = y;
                vertex.Position0.z = 0;
                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;
                vertex.TextureCoordinate0.x = minX;
                vertex.TextureCoordinate0.y = minY;
                vertex.TextureCoordinate0.z = 0;
                vertex.Position1.x = x + swidth;
                vertex.Position1.y = y;
                vertex.Position1.z = 0;
                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;
                vertex.TextureCoordinate1.x = maxX;
                vertex.TextureCoordinate1.y = minY;
                vertex.TextureCoordinate1.z = 0;
                vertex.Position2.x = x;
                vertex.Position2.y = y + sheight;
                vertex.Position2.z = 0;
                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;
                vertex.TextureCoordinate2.x = minX;
                vertex.TextureCoordinate2.y = maxY;
                vertex.TextureCoordinate2.z = 0;
                vertex.Position3.x = x + swidth;
                vertex.Position3.y = y + sheight;
                vertex.Position3.z = 0;
                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;
                vertex.TextureCoordinate3.x = maxX;
                vertex.TextureCoordinate3.y = maxY;
                vertex.TextureCoordinate3.z = 0;
                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
                
                RenderVertex(vertex, texture, hue);
            }

            return true;
        }

        // MobileUO: TODO: deprecated, to be deleted
        public bool Draw2D(Texture2D texture, float dx, float dy, float dwidth, float dheight, float sx, float sy, float swidth, float sheight, ref XnaVector3 hue, float angle = 0.0f)
        {
            if (texture.UnityTexture == null)
            {
                return false;
            }
            
            float minX = sx / texture.Width, maxX = (sx + swidth) / texture.Width;
            float minY = sy / texture.Height, maxY = (sy + sheight) / texture.Height;

            var vertex = new PositionNormalTextureColor4();

            float x = dx;
            float y = dy;
            float w = dx + dwidth;
            float h = dy + dheight;

            if (angle != 0.0f)
            {
                angle = (float)(angle * Math.PI) / 180.0f;

                float ww = dwidth * 0.5f;
                float hh = dheight * 0.5f;

                float sin = (float)Math.Sin(angle);
                float cos = (float)Math.Cos(angle);

                float tempX = -ww;
                float tempY = -hh;
                float rotX = tempX * cos - tempY * sin;
                float rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position0.x = rotX;
                vertex.Position0.y = rotY;

                tempX = dwidth - ww;
                tempY = -hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position1.x = rotX;
                vertex.Position1.y = rotY;

                tempX = -ww;
                tempY = dheight - hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position2.x = rotX;
                vertex.Position2.y = rotY;

                tempX = dwidth - ww;
                tempY = dheight - hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position3.x = rotX;
                vertex.Position3.y = rotY;
            }
            else
            {
                vertex.Position0.x = x;
                vertex.Position0.y = y;

                vertex.Position1.x = w;
                vertex.Position1.y = y;

                vertex.Position2.x = x;
                vertex.Position2.y = h;

                vertex.Position3.x = w;
                vertex.Position3.y = h;

                if (UseGraphicsDrawTexture)
                {
                    hueMaterial.SetColor(Hue, new Color(hue.X,hue.Y,hue.Z));
                    hueMaterial.SetFloat(UvMirrorX, 0);
                    //NOTE: given sourceRect needs to be flipped vertically for some reason
                    Graphics.DrawTexture(new Rect(x * scale, y * scale, dwidth * scale, dheight * scale),
                        texture.UnityTexture, new Rect(minX, 1 - maxY, maxX - minX, maxY - minY),
                        0, 0, 0, 0, hueMaterial);
                    return true;
                }
            }

            vertex.Position0.z = 0;
            vertex.Normal0.x = 0;
            vertex.Normal0.y = 0;
            vertex.Normal0.z = 1;
            vertex.TextureCoordinate0.x = minX;
            vertex.TextureCoordinate0.y = minY;
            vertex.TextureCoordinate0.z = 0;

            vertex.Position1.z = 0;
            vertex.Normal1.x = 0;
            vertex.Normal1.y = 0;
            vertex.Normal1.z = 1;
            vertex.TextureCoordinate1.x = maxX;
            vertex.TextureCoordinate1.y = minY;
            vertex.TextureCoordinate1.z = 0;

            vertex.Position2.z = 0;
            vertex.Normal2.x = 0;
            vertex.Normal2.y = 0;
            vertex.Normal2.z = 1;
            vertex.TextureCoordinate2.x = minX;
            vertex.TextureCoordinate2.y = maxY;
            vertex.TextureCoordinate2.z = 0;

            vertex.Position3.z = 0;
            vertex.Normal3.x = 0;
            vertex.Normal3.y = 0;
            vertex.Normal3.z = 1;
            vertex.TextureCoordinate3.x = maxX;
            vertex.TextureCoordinate3.y = maxY;
            vertex.TextureCoordinate3.z = 0;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            RenderVertex(vertex, texture, hue);

            return true;
        }

        // MobileUO: TODO: deprecated, to be deleted
        public bool Draw2D(Texture2D texture, float x, float y, float width, float height, ref XnaVector3 hue)
        {
            if (texture.UnityTexture == null)
            {
                return false;
            }
            
            if (UseGraphicsDrawTexture)
            {
                if (CustomEffect is XBREffect xbrEffect)
                {
                    // MobileUO: _vectorSize no longer exists
                    //xbrMaterial.SetVector(TextureSize, new Vector4(xbrEffect._vectorSize.X, xbrEffect._vectorSize.Y));
                    Graphics.DrawTexture(new Rect(x * scale, y * scale, width * scale, height * scale), texture.UnityTexture, xbrMaterial);
                }
                else
                {
                    hueMaterial.SetColor(Hue, new Color(hue.X,hue.Y,hue.Z));
                    hueMaterial.SetFloat(UvMirrorX, 0);
                    Graphics.DrawTexture(new Rect(x * scale, y * scale, width * scale, height * scale), texture.UnityTexture, hueMaterial);
                }
            }
            else
            {
                var vertex = new PositionNormalTextureColor4();

                vertex.Position0.x = x;
                vertex.Position0.y = y;
                vertex.Position0.z = 0;
                vertex.Normal0.x = 0;
                vertex.Normal0.y = 0;
                vertex.Normal0.z = 1;
                vertex.TextureCoordinate0.x = 0;
                vertex.TextureCoordinate0.y = 0;
                vertex.TextureCoordinate0.z = 0;

                vertex.Position1.x = x + width;
                vertex.Position1.y = y;
                vertex.Position1.z = 0;
                vertex.Normal1.x = 0;
                vertex.Normal1.y = 0;
                vertex.Normal1.z = 1;
                vertex.TextureCoordinate1.x = 1;
                vertex.TextureCoordinate1.y = 0;
                vertex.TextureCoordinate1.z = 0;

                vertex.Position2.x = x;
                vertex.Position2.y = y + height;
                vertex.Position2.z = 0;
                vertex.Normal2.x = 0;
                vertex.Normal2.y = 0;
                vertex.Normal2.z = 1;
                vertex.TextureCoordinate2.x = 0;
                vertex.TextureCoordinate2.y = 1;
                vertex.TextureCoordinate2.z = 0;

                vertex.Position3.x = x + width;
                vertex.Position3.y = y + height;
                vertex.Position3.z = 0;
                vertex.Normal3.x = 0;
                vertex.Normal3.y = 0;
                vertex.Normal3.z = 1;
                vertex.TextureCoordinate3.x = 1;
                vertex.TextureCoordinate3.y = 1;
                vertex.TextureCoordinate3.z = 0;

                vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;
                
                RenderVertex(vertex, texture, hue);
            }

            return true;
        }

        public void DrawTiled
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle sourceRectangle,
            XnaVector3 hue
        )
        {
            if (texture.UnityTexture == null)
            {
                return;
            }
            
            int h = destinationRectangle.Height;

            Rectangle rect = sourceRectangle;
            XnaVector2 pos = new XnaVector2(destinationRectangle.X, destinationRectangle.Y);

            while (h > 0)
            {
                pos.X = destinationRectangle.X;
                int w = destinationRectangle.Width;

                rect.Height = Math.Min(h, sourceRectangle.Height);

                while (w > 0)
                {
                    rect.Width = Math.Min(w, sourceRectangle.Width);

                    Draw
                    (
                        texture,
                        pos,
                        rect,
                        hue
                    );

                    w -= sourceRectangle.Width;
                    pos.X += sourceRectangle.Width;
                }

                h -= sourceRectangle.Height;
                pos.Y += sourceRectangle.Height;
            }
        }

        public bool DrawRectangle
        (
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            XnaVector3 hue,
            float depth = 0f
        )
        {
            Rectangle rect = new Rectangle(x, y, width, 1);
            Draw(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);

            rect.X += width;
            rect.Width = 1;
            rect.Height += height;
            Draw(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);


            rect.X = x;
            rect.Y = y + height;
            rect.Width = width;
            rect.Height = 1;
            Draw(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);


            rect.X = x;
            rect.Y = y;
            rect.Width = 1;
            rect.Height = height;
            Draw(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);

            return true;
        }

        // MobileUO: TODO: deprecated, to be deleted
        public void DrawLine(Texture2D texture, int startX, int startY, int endX, int endY, int originX, int originY)
        {
            if (texture.UnityTexture == null)
            {
                return;
            }
            
            var vertex = new PositionNormalTextureColor4();

            const int WIDTH = 1;
            XnaVector2 begin = new XnaVector2(startX, startY);
            XnaVector2 end = new XnaVector2(endX, endY);

            Rectangle r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + WIDTH, WIDTH);

            float angle = (float)(Math.Atan2(end.Y - begin.Y, end.X - begin.X) * 57.295780);
            angle = -(float)(angle * Math.PI) / 180.0f;


            float ww = r.Width * 0.5f;
            float hh = r.Height * 0.5f;


            float rotSin = (float) Math.Sin(angle);
            float rotCos = (float) Math.Cos(angle);


            float sinx = rotSin * ww;
            float cosx = rotCos * ww;
            float siny = rotSin * hh;
            float cosy = rotCos * hh;


            vertex.Position0.x = originX;
            vertex.Position0.y = originY;
            vertex.Position0.x += cosx - -siny;
            vertex.Position0.y -= sinx + -cosy;
            vertex.TextureCoordinate0.x = 0;
            vertex.TextureCoordinate0.y = 0;
            vertex.TextureCoordinate0.z = 0;

            vertex.Position1.x = originX;
            vertex.Position1.y = originY;
            vertex.Position1.x += cosx - siny;
            vertex.Position1.y += -sinx + -cosy;
            vertex.TextureCoordinate1.x = 0;
            vertex.TextureCoordinate1.y = 1;
            vertex.TextureCoordinate1.z = 0;

            vertex.Position2.x = originX;
            vertex.Position2.y = originY;
            vertex.Position2.x += -cosx - -siny;
            vertex.Position2.y += sinx + cosy;
            vertex.TextureCoordinate2.x = 1;
            vertex.TextureCoordinate2.y = 0;
            vertex.TextureCoordinate2.z = 0;

            vertex.Position3.x = originX;
            vertex.Position3.y = originY;
            vertex.Position3.x += -cosx - siny;
            vertex.Position3.y += sinx + -cosy;
            vertex.TextureCoordinate3.x = 1;
            vertex.TextureCoordinate3.y = 1;
            vertex.TextureCoordinate3.z = 0;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = XnaVector3.Zero;

            RenderVertex(vertex, texture, XnaVector3.Zero);
        }

        public void DrawLine
        (
            Texture2D texture,
            XnaVector2 start,
            XnaVector2 end,
            XnaVector3 color,
            float stroke
        )
        {
            if (texture.UnityTexture == null)
            {
                return;
            }

            var radians = ClassicUO.Utility.MathHelper.AngleBetweenVectors(start, end);
            XnaVector2.Distance(ref start, ref end, out var length);

            Draw
            (
                texture, 
                start,
                texture.Bounds,
                color,
                radians, 
                XnaVector2.Zero,
                new XnaVector2(length, stroke), 
                SpriteEffects.None,
                0
            );
        }


        public void Draw
        (
            Texture2D texture, 
            XnaVector2 position,
            XnaVector3 color
        )
        {
            AddSprite(texture, 0f, 0f, 1f, 1f, position.X, position.Y, texture.Width, texture.Height, color, 0f, 0f, 0f, 1f, 0f, 0);
        }

        public void Draw
        (
            Texture2D texture, 
            XnaVector2 position,
            Rectangle? sourceRectangle,
            XnaVector3 color
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            float destW, destH;

            if (sourceRectangle.HasValue)
            {
                sourceX = sourceRectangle.Value.X / (float)texture.Width;
                sourceY = sourceRectangle.Value.Y / (float)texture.Height;
                sourceW = sourceRectangle.Value.Width / (float)texture.Width;
                sourceH = sourceRectangle.Value.Height / (float)texture.Height;
                destW = sourceRectangle.Value.Width;
                destH = sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                destW = texture.Width;
                destH = texture.Height;
            }

            AddSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, destW, destH, color, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0);
        }

        public void Draw
        (
            Texture2D texture,
            XnaVector2 position,
            Rectangle? sourceRectangle,
            XnaVector3 color,
            float rotation,
            XnaVector2 origin, 
            float scale, 
            SpriteEffects effects,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            float destW = scale;
            float destH = scale;

            if (sourceRectangle.HasValue)
            {
                sourceX = sourceRectangle.Value.X / (float)texture.Width;
                sourceY = sourceRectangle.Value.Y / (float)texture.Height;
                sourceW = Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), Utility.MathHelper.MachineEpsilonFloat) / (float)texture.Width;
                sourceH = Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), Utility.MathHelper.MachineEpsilonFloat) / (float)texture.Height;
                destW *= sourceRectangle.Value.Width;
                destH *= sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                destW *= texture.Width;
                destH *= texture.Height;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                position.X,
                position.Y,
                destW,
                destH,
                color,
                origin.X / sourceW / (float)texture.Width,
                origin.Y / sourceH / (float)texture.Height,
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation),
                layerDepth,
                (byte)(effects & (SpriteEffects)0x03)
            );
        }

        public void Draw
        (
            Texture2D texture,
            XnaVector2 position,
            Rectangle? sourceRectangle,
            XnaVector3 color,
            float rotation,
            XnaVector2 origin,
            XnaVector2 scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                sourceX = sourceRectangle.Value.X / (float)texture.Width;
                sourceY = sourceRectangle.Value.Y / (float)texture.Height;
                sourceW = Math.Sign(sourceRectangle.Value.Width) * Math.Max(Math.Abs(sourceRectangle.Value.Width), Utility.MathHelper.MachineEpsilonFloat) / (float)texture.Width;
                sourceH = Math.Sign(sourceRectangle.Value.Height) * Math.Max(Math.Abs(sourceRectangle.Value.Height), Utility.MathHelper.MachineEpsilonFloat) / (float)texture.Height;
                scale.X *= sourceRectangle.Value.Width;
                scale.Y *= sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                scale.X *= texture.Width;
                scale.Y *= texture.Height;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                position.X,
                position.Y,
                scale.X,
                scale.Y,
                color,
                origin.X / sourceW / (float)texture.Width,
                origin.Y / sourceH / (float)texture.Height,
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation),
                layerDepth,
                (byte)(effects & (SpriteEffects)0x03)
            );
        }

        public void Draw
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            XnaVector3 color
        )
        {
            AddSprite(
                texture,
                0.0f,
                0.0f,
                1.0f,
                1.0f,
                destinationRectangle.X,
                destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height,
                color,
                0.0f,
                0.0f,
                0.0f,
                1.0f,
                0.0f,
                0
            );
        }

        public void Draw
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            XnaVector3 color
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                sourceX = sourceRectangle.Value.X / (float)texture.Width;
                sourceY = sourceRectangle.Value.Y / (float)texture.Height;
                sourceW = sourceRectangle.Value.Width / (float)texture.Width;
                sourceH = sourceRectangle.Value.Height / (float)texture.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                destinationRectangle.X,
                destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height,
                color,
                0.0f,
                0.0f,
                0.0f,
                1.0f,
                0.0f,
                0
            );
        }

        public void Draw
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            XnaVector3 color,
            float rotation,
            XnaVector2 origin,
            SpriteEffects effects,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                sourceX = sourceRectangle.Value.X / (float)texture.Width;
                sourceY = sourceRectangle.Value.Y / (float)texture.Height;
                sourceW = Math.Sign(sourceRectangle.Value.Width) * Math.Max(
                    Math.Abs(sourceRectangle.Value.Width),
                    Utility.MathHelper.MachineEpsilonFloat
                ) / (float)texture.Width;
                sourceH = Math.Sign(sourceRectangle.Value.Height) * Math.Max(
                    Math.Abs(sourceRectangle.Value.Height),
                    Utility.MathHelper.MachineEpsilonFloat
                ) / (float)texture.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                destinationRectangle.X,
                destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height,
                color,
                origin.X / sourceW / (float)texture.Width,
                origin.Y / sourceH / (float)texture.Height,
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation),
                layerDepth,
                (byte)(effects & (SpriteEffects)0x03)
            );
        }

        private void AddSprite
        (
            Texture2D texture,
            float sourceX,
            float sourceY,
            float sourceW,
            float sourceH,
            float destinationX,
            float destinationY,
            float destinationW,
            float destinationH,
            XnaVector3 color,
            float originX,
            float originY,
            float rotationSin,
            float rotationCos,
            float depth,
            byte effects
        )
        {
            //EnsureSize();

            //ref var vertex = ref _vertexInfo[_numSprites];

            var sprite = new PositionNormalTextureColor4();

            SetVertex
            (   
                //ref vertex,
                ref sprite,
                sourceX, sourceY, sourceW, sourceH,
                destinationX, destinationY, destinationW, destinationH,
                color,
                originX, originY,
                rotationSin, rotationCos,
                depth, effects
            );

            RenderVertex(sprite, texture, color);

            //_textureInfo[_numSprites] = texture;
            //++_numSprites;
        }

        public void Begin()
        {
            hueMaterial.SetTexture(HueTex1, GraphicsDevice.Textures[1].UnityTexture);
            hueMaterial.SetTexture(HueTex2, GraphicsDevice.Textures[2].UnityTexture);
            //hueMaterial.SetTexture(HueTex3, GraphicsDevice.Textures[3].UnityTexture);
            Begin(null, Matrix.Identity);
        }

        public void Begin(Effect effect)
        {
            CustomEffect = effect;
            Begin(effect, Matrix.Identity);
        }

        public void Begin(Effect customEffect, Matrix transform_matrix)
        {
            //EnsureNotStarted();
            //_started = true;

            TextureSwitches = 0;
            FlushesDone = 0;

            CustomEffect = customEffect;
            _transformMatrix = transform_matrix;
        }

        public void End()
        {
            CustomEffect = null;
        }

        private void SetVertex
        (
            ref PositionNormalTextureColor4 sprite,
            float sourceX,
            float sourceY,
            float sourceW,
            float sourceH,
            float destinationX,
            float destinationY,
            float destinationW,
            float destinationH,
            XnaVector3 color,
            float originX,
            float originY,
            float rotationSin,
            float rotationCos,
            float depth,
            byte effects
        )
        {
            // MobileUO: TODO: temp fix to keep things stable - hopefully future commit makes depth work
            if(!USE_DEPTH)
                depth = 0;

            if(DIVIDE_DEPTH)
                depth = depth / 1000f;

            if(LOG_DEPTH)
                Log.Info($"Depth: {depth}");

            float cornerX = -originX * destinationW;
            float cornerY = -originY * destinationH;
            sprite.Position0.x = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position0.y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);

            cornerX = (1.0f - originX) * destinationW;
            cornerY = -originY * destinationH;
            sprite.Position1.x = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position1.y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);

            cornerX = -originX * destinationW;
            cornerY = (1.0f - originY) * destinationH;
            sprite.Position2.x = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position2.y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);

            cornerX = (1.0f - originX) * destinationW;
            cornerY = (1.0f - originY) * destinationH;
            sprite.Position3.x = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position3.y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);


            sprite.TextureCoordinate0.x = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate0.y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            sprite.TextureCoordinate1.x = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate1.y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            sprite.TextureCoordinate2.x = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate2.y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
            sprite.TextureCoordinate3.x = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate3.y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;

            sprite.TextureCoordinate0.z = 0;
            sprite.TextureCoordinate1.z = 0;
            sprite.TextureCoordinate2.z = 0;
            sprite.TextureCoordinate3.z = 0;


            sprite.Position0.z = depth;
            sprite.Position1.z = depth;
            sprite.Position2.z = depth;
            sprite.Position3.z = depth;


            sprite.Hue0 = color;
            sprite.Hue1 = color;
            sprite.Hue2 = color;
            sprite.Hue3 = color;

            sprite.Normal0.x = 0;
            sprite.Normal0.y = 0;
            sprite.Normal0.z = 1;

            sprite.Normal1.x = 0;
            sprite.Normal1.y = 0;
            sprite.Normal1.z = 1;

            sprite.Normal2.x = 0;
            sprite.Normal2.y = 0;
            sprite.Normal2.z = 1;

            sprite.Normal3.x = 0;
            sprite.Normal3.y = 0;
            sprite.Normal3.z = 1;
        }

        //Because XNA's Blend enum starts with 1, we duplicate BlendMode.Zero for 0th index
        //and also for indexes 12-15 where Unity's BlendMode enum doesn't have a match to XNA's Blend enum
        //and we don't need those anyways
        private static readonly BlendMode[] BlendModesMatchingXna =
        {
            BlendMode.Zero,
            BlendMode.Zero,
            BlendMode.One,
            BlendMode.SrcColor,
            BlendMode.OneMinusSrcColor,
            BlendMode.SrcAlpha,
            BlendMode.OneMinusSrcAlpha,
            BlendMode.DstAlpha,
            BlendMode.OneMinusDstAlpha,
            BlendMode.DstColor,
            BlendMode.OneMinusDstColor,
            BlendMode.SrcAlphaSaturate,
            BlendMode.Zero,
            BlendMode.Zero,
            BlendMode.Zero,
            BlendMode.Zero
        };

        private static void SetMaterialBlendState(Material mat, BlendState blendState)
        {
            var src = BlendModesMatchingXna[(int) blendState.ColorSourceBlend];
            var dst = BlendModesMatchingXna[(int) blendState.ColorDestinationBlend];
            SetMaterialBlendState(mat, src, dst);
        }

        private static void SetMaterialBlendState(Material mat, BlendMode src, BlendMode dst)
        {
            mat.SetFloat(SrcBlend, (float) src);
            mat.SetFloat(DstBlend, (float) dst);
        }

        private void ApplyStates()
        {
            // GraphicsDevice.BlendState = _blendState;
            SetMaterialBlendState(hueMaterial, _blendState);

            GraphicsDevice.DepthStencilState = _stencil;

            // GraphicsDevice.RasterizerState = _useScissor ? _rasterizerState : RasterizerState.CullNone;
            // GraphicsDevice.SamplerStates[0] = _sampler;

            // MobileUO: keep old scissor logic or else gumps like world map won't be clipped!
            hueMaterial.SetFloat(Scissor, _useScissor ? 1 : 0);
            if (_useScissor)
            {
                var scissorRect = GraphicsDevice.ScissorRectangle;
                var scissorVector4 = new Vector4(scissorRect.X * scale,
                    scissorRect.Y * scale,
                    scissorRect.X * scale + scissorRect.Width * scale,
                    scissorRect.Y * scale + scissorRect.Height * scale);
                hueMaterial.SetVector(ScissorRect, scissorVector4);
            }

            GraphicsDevice.RasterizerState = _rasterizerState;
            GraphicsDevice.SamplerStates[0] = _sampler;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;

            _projectionMatrix.M11 = (float)(2.0 / GraphicsDevice.Viewport.Width);
            _projectionMatrix.M22 = (float)(-2.0 / GraphicsDevice.Viewport.Height);

            Matrix matrix = _projectionMatrix;
            Matrix.CreateOrthographicOffCenter
            (
                0f,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                0,
                short.MinValue,
                short.MaxValue,
                out matrix
            );
            Matrix.Multiply(ref _transformMatrix, ref matrix, out matrix);

            //Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            //Matrix.Multiply(ref halfPixelOffset, ref matrix, out matrix);

            _basicUOEffect.WorldMatrix.SetValue(Matrix.Identity);
            _basicUOEffect.Viewport.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            _basicUOEffect.MatrixTransform.SetValue(matrix);
            // MobileUO: commented out
            //_basicUOEffect.Pass.Apply();
        }

        private void Flush()
        {
            ApplyStates();

            ++FlushesDone;
        }

        public bool ClipBegin(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            Rectangle scissor = ScissorStack.CalculateScissors
            (
                TransformMatrix,
                x,
                y,
                width,
                height
            );

            Flush();

            if (ScissorStack.PushScissors(GraphicsDevice, scissor))
            {
                EnableScissorTest(true);

                return true;
            }

            return false;
        }

        public void ClipEnd()
        {
            EnableScissorTest(false);
            ScissorStack.PopScissors(GraphicsDevice);

            Flush();
        }

        // MobileUO: keep old Scissor test logic
        public void EnableScissorTest(bool enable)
        {
            if (enable == _useScissor)
                return;

            if (!enable && _useScissor && ScissorStack.HasScissors)
                return;

            _useScissor = enable;
            ApplyStates();
        }

        public void SetBlendState(BlendState blend)
        {
            _blendState = blend ?? BlendState.AlphaBlend;
            ApplyStates();
        }

        public void SetStencil(DepthStencilState stencil)
        {
            _stencil = stencil ?? Stencil;
            ApplyStates();
        }

        public void SetSampler(SamplerState sampler)
        {
            // MobileUO: TODO: add it?
            //Flush();

            _sampler = sampler ?? SamplerState.PointClamp;
        }

        public void Dispose()
        {
            _basicUOEffect?.Dispose();
        }


        // MobileUO: make public
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PositionNormalTextureColor4 : IVertexType
        {
            public Vector3 Position0;
            public Vector3 Normal0;
            public Vector3 TextureCoordinate0;
            public Vector3 Hue0;

            public Vector3 Position1;
            public Vector3 Normal1;
            public Vector3 TextureCoordinate1;
            public Vector3 Hue1;

            public Vector3 Position2;
            public Vector3 Normal2;
            public Vector3 TextureCoordinate2;
            public Vector3 Hue2;

            public Vector3 Position3;
            public Vector3 Normal3;
            public Vector3 TextureCoordinate3;
            public Vector3 Hue3;

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            private static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),                          // position
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),            // normal
                new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0), // tex coord
                new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1)  // hue
            );

            public const int SIZE_IN_BYTES = sizeof(float) * 12 * 4;            
        }
    }
}