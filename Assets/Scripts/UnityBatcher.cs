using ClassicUO.Renderer.Effects;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUO.Profiling;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
using Color = UnityEngine.Color;
using CompareFunction = Microsoft.Xna.Framework.Graphics.CompareFunction;
using Quaternion = UnityEngine.Quaternion;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using UnityCamera = UnityEngine.Camera;
using UnityTexture = UnityEngine.Texture;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace ClassicUO.Renderer
{
    internal sealed class UltimaBatcher2D : IDisposable
    {
        private static readonly float[] _cornerOffsetX = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
        private static readonly float[] _cornerOffsetY = new float[] { 0.0f, 0.0f, 1.0f, 1.0f };
        private const int MAX_SPRITES = 0x800;
        //private const int MAX_VERTICES = MAX_SPRITES * 4;
        //private const int MAX_INDICES = MAX_SPRITES * 6;
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
        private Texture2D[] _textureInfo;
        private PositionNormalTextureColor4[] _vertexInfo;
        private Material hueMaterial;
        private Material hueMeshMaterial;
        private Material xbrMaterial;
        private MeshHolder reusedMesh = new MeshHolder(256);
        public float scale = 1;

        public bool UseGraphicsDrawTexture;
        private Mesh draw2DMesh;
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Hue = Shader.PropertyToID("_Hue");
        private static readonly int HueTex1 = Shader.PropertyToID("_HueTex1");
        private static readonly int HueTex2 = Shader.PropertyToID("_HueTex2");
        private static readonly int Brightlight = Shader.PropertyToID("_Brightlight");
        private static readonly int Scissor = Shader.PropertyToID("_Scissor");
        private static readonly int ScissorRect = Shader.PropertyToID("_ScissorRect");
        private static readonly int TextureSize = Shader.PropertyToID("textureSize");
        
        // MobileUO: TODO: flag to use depths while trying to figure out the depth issue
        private bool USE_DEPTH = false;
        private bool LOG_DEPTH = false;
        private bool DIVIDE_DEPTH = false; // if depth values are 100 or lower, they will render. Something clips them at over 100 (100.1 or 101 or higher)
        
        public GraphicsDevice GraphicsDevice { get; }
        public int TextureSwitches, FlushesDone;
        public int TextureSwitchesPerSecond, FlushesPerSecond;
        public int DrawTextures, DrawMeshes;
        public int DrawTexturesPerSecond, DrawMeshesPerSecond;
        
        public UltimaBatcher2D(GraphicsDevice device)
        {
            //if (USE_DEPTH)
            //{
            //    UnityCamera.main.nearClipPlane = 0.01f;
            //    UnityCamera.main.farClipPlane = 10000f;
            //}
            GraphicsDevice = device;
            _textureInfo = new Texture2D[MAX_SPRITES];
            _vertexInfo = new PositionNormalTextureColor4[MAX_SPRITES];
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
            hueMeshMaterial = new Material(hueMaterial);
            hueMeshMaterial.EnableKeyword("MESH_HUE");
            xbrMaterial = new Material(UnityEngine.Resources.Load<Shader>("XbrShader"));
            _batchedVertices.Capacity = 8192; // ~4351
            _batchedMeshVertices.Capacity = 256; // < ~35
            _runQuads.Capacity = 256; // ~ 10
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
        
        public void SetBrightlight(float f)
        {
            // MobileUO: pass Brightlight value to shader
            hueMaterial.SetFloat(Brightlight, f);
            hueMeshMaterial.SetFloat(Brightlight, f);
            _basicUOEffect.Brighlight.SetValue(f);
        }
        
        public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, int x, int y, XnaVector3 color)
            => DrawString(spriteFont, text, new XnaVector2(x, y), color);
        
        public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, XnaVector2 position, XnaVector3 color)
        {
            if (text.IsEmpty)
                return;
            EnsureSize();
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
                var pos = new XnaVector2(offsetX, offsetY);
                Draw
                (
                    textureValue,
                    position + pos,
                    cGlyph,
                    color
                );
                curOffset.X += cKern.Y + cKern.Z;
            }
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
            if (!USE_DEPTH)
                depth = 0;
            if (DIVIDE_DEPTH)
                depth = depth / 1000f;
            if (LOG_DEPTH)
                Log.Info($"Depth: {depth}");
            EnsureSize();
            float sourceX = ((sx + 0.5f) / (float)texture.Width);
            float sourceY = ((sy + 0.5f) / (float)texture.Height);
            float sourcwW = ((swidth - 1f) / (float)texture.Width);
            float sourceH = ((sheight - 1f) / (float)texture.Height);
            //float sourceX = ((sx) / (float)texture.Width);
            //float sourceY = ((sy) / (float)texture.Height);
            //float sourcwW = ((swidth) / (float)texture.Width);
            //float sourceH = ((sheight) / (float)texture.Height);
            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
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
            FlipTextureVertically(ref vertex, texture.IsFromTextureAtlas);
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
            FinalizeVertex(ref vertex, true);
            PushSprite(texture);
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
            // questo qui sotto andrebbe implementato
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
            EnsureSize();
            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
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
            FinalizeVertex(ref vertex);
            PushSprite(texture);
        }
        
        public void DrawShadow(Texture2D texture, XnaVector2 position, Rectangle sourceRect, bool flip, float depth)
        {
            if (texture.UnityTexture == null)
            {
                return;
            }
            // MobileUO: TODO: temp fix to keep things stable - hopefully future commit makes depth work
            if (!USE_DEPTH)
                depth = 0;
            if (DIVIDE_DEPTH)
                depth = depth / 1000f;
            if (LOG_DEPTH)
                Log.Info($"Depth: {depth}");
            float width = sourceRect.Width;
            float height = sourceRect.Height * 0.5f;
            float translatedY = position.Y + height - 10;
            float ratio = height / width;
            EnsureSize();
            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];
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
            FlipTextureVertically(ref vertex, texture.IsFromTextureAtlas);
            vertex.Normal0 = vertex.Normal1 = vertex.Normal2 = vertex.Normal3 = Vector3.forward;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = new Vector4(0f, ShaderHueTranslator.SHADER_SHADOW, 0f, 0f);
            FinalizeVertex(ref vertex, true);
            PushSprite(texture);
        }
        
        private readonly List<VertexData> _batchedVertices = new List<VertexData>();
        private void FinalizeVertex(ref PositionNormalTextureColor4 vertex, bool useMesh = false)
        {
            //vertex.Position0 *= scale;
            //vertex.Position1 *= scale;
            //vertex.Position2 *= scale;
            //vertex.Position3 *= scale;
            vertex.Position0.x *= scale;
            vertex.Position0.y *= scale;
            vertex.Position1.x *= scale;
            vertex.Position1.y *= scale;
            vertex.Position2.x *= scale;
            vertex.Position2.y *= scale;
            vertex.Position3.x *= scale;
            vertex.Position3.y *= scale;
            vertex.UseMesh = useMesh;
        }

        public unsafe void DrawCharacterSitted
        (
            Texture2D texture, XnaVector2 position, Rectangle sourceRect,
            XnaVector3 mod, XnaVector3 hue, bool flip, float depth
        )
        {
            if (texture.UnityTexture == null) return;
            if (!USE_DEPTH) depth = 0;
            if (DIVIDE_DEPTH) depth *= 0.001f;
            float invW = 1.0f / texture.Width;
            float invH = 1.0f / texture.Height;
            bool isAtlas = texture.IsFromTextureAtlas;
            // Source UV X — apply horizontal flip once
            float srcX = sourceRect.X * invW;
            float srcW = sourceRect.Width * invW;
            if (flip) { srcX += srcW; srcW = -srcW; }
            float sittingOffset = flip ? -8.0f : 8.0f;
            float width = sourceRect.Width;
            int totalHeight = sourceRect.Height;
            // Atlas: per-sprite Y-flip maps source row r → atlas UV (spriteEnd - r/texH).
            // spriteEndY is constant for all segments.
            float spriteEndY = (sourceRect.Y + sourceRect.Height) * invH;
            int prevPixel = 0;
            float prevMod = 0f;
            float* mods = stackalloc float[3] { mod.X, mod.Y, mod.Z };
            for (int i = 0; i < 3; i++)
            {
                float currentMod = mods[i];
                if (currentMod <= prevMod) continue;
                int endPixel = (int)MathF.Round(totalHeight * currentMod);
                int segH = endPixel - prevPixel;
                if (segH <= 0) continue;
                float drawY = position.Y + prevPixel;
                float segUVH = segH * invH;
                float segSrcY = (sourceRect.Y + prevPixel) * invH;
                // UV Y: atlas = per-sprite flip → head of segment at spriteEnd - prevPixel/texH;
                //        non-atlas = whole-texture flip → head of segment at 1 - prevPixel/texH
                float uvTop = isAtlas ? (spriteEndY - prevPixel * invH) : (1f - segSrcY);
                float uvBot = uvTop - segUVH;
                // Position X: bust(0) = all shifted; thighs(1) = top shifted, bottom not (trapezoid); legs(2) = not shifted
                float topX = position.X + ((i < 2) ? sittingOffset : 0f);
                float botX = position.X + ((i < 1) ? sittingOffset : 0f);
                EnsureSize();
                ref PositionNormalTextureColor4 v = ref _vertexInfo[_numSprites];
                v.Position0 = new Vector3(topX,         drawY,        depth);
                v.Position1 = new Vector3(topX + width, drawY,        depth);
                v.Position2 = new Vector3(botX,         drawY + segH, depth);
                v.Position3 = new Vector3(botX + width, drawY + segH, depth);
                v.Normal0 = v.Normal1 = v.Normal2 = v.Normal3 = Vector3.forward;
                v.TextureCoordinate0 = new Vector3(srcX,        uvTop, 0f);
                v.TextureCoordinate1 = new Vector3(srcX + srcW, uvTop, 0f);
                v.TextureCoordinate2 = new Vector3(srcX,        uvBot, 0f);
                v.TextureCoordinate3 = new Vector3(srcX + srcW, uvBot, 0f);
                v.Hue0 = v.Hue1 = v.Hue2 = v.Hue3 = hue;
                FinalizeVertex(ref v, true);
                PushSprite(texture);
                prevPixel = endPixel;
                prevMod = currentMod;
            }
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
            DrawFast(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);
            rect.X += width;
            rect.Width = 1;
            rect.Height += height;
            DrawFast(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);
            rect.X = x;
            rect.Y = y + height;
            rect.Width = width;
            rect.Height = 1;
            DrawFast(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);
            rect.X = x;
            rect.Y = y;
            rect.Width = 1;
            rect.Height = height;
            DrawFast(texture, rect, null, hue, 0f, XnaVector2.Zero, SpriteEffects.None, depth);
            return true;
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
        
        public void Draw(Texture2D texture, XnaVector2 position, Rectangle? sourceRectangle, XnaVector3 color)
        {
            float sX = 0, sY = 0, sW = 1, sH = 1;
            float dW = texture.Width, dH = texture.Height;
            if (sourceRectangle.HasValue)
            {
                var src = sourceRectangle.Value;
                float invW = 1.0f / dW; // dW is texture.Width
                float invH = 1.0f / dH;
                sX = src.X * invW;
                sY = src.Y * invH;
                sW = src.Width * invW;
                sH = src.Height * invH;
                dW = src.Width;
                dH = src.Height;
            }
            AddSprite(texture, sX, sY, sW, sH, position.X, position.Y, dW, dH, color, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0, false);
        }
        
        public void Draw(Texture2D texture, XnaVector2 position, Rectangle? sourceRectangle, XnaVector3 color, float rotation, XnaVector2 origin, float scale, SpriteEffects effects, float layerDepth)
        {
            float sX = 0, sY = 0, sW = 1, sH = 1;
            float dW, dH, oX, oY;
            if (sourceRectangle.HasValue)
            {
                var src = sourceRectangle.Value;
                float invW = 1.0f / texture.Width;
                float invH = 1.0f / texture.Height;
                sX = src.X * invW;
                sY = src.Y * invH;
                sW = src.Width * invW;
                sH = src.Height * invH;
                dW = scale * src.Width;
                dH = scale * src.Height;
                oX = origin.X / src.Width;
                oY = origin.Y / src.Height;
            }
            else
            {
                dW = scale * texture.Width;
                dH = scale * texture.Height;
                oX = origin.X / texture.Width;
                oY = origin.Y / texture.Height;
            }
            float sin = 0, cos = 1;
            if (rotation != 0)
            {
                sin = MathF.Sin(rotation);
                cos = MathF.Cos(rotation);
            }
            AddSprite(texture, sX, sY, sW, sH, position.X, position.Y, dW, dH, color, oX, oY, sin, cos, layerDepth, (byte)effects, rotation != 0);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(Texture2D texture, XnaVector2 pos, Rectangle? source, XnaVector3 color, float rotation, XnaVector2 origin, XnaVector2 scale, SpriteEffects effects, float depth)
        {
            float sX = 0, sY = 0, sW = 1, sH = 1;
            float destW, destH;
            float oX, oY;
            if (source.HasValue)
            {
                Rectangle src = source.Value;
                float invW = 1.0f / texture.Width;
                float invH = 1.0f / texture.Height;
                sX = src.X * invW;
                sY = src.Y * invH;
                sW = src.Width * invW;
                sH = src.Height * invH;
                destW = scale.X * src.Width;
                destH = scale.Y * src.Height;
                // Simplified algebra: no texture.Width inside
                oX = origin.X / src.Width;
                oY = origin.Y / src.Height;
            }
            else
            {
                destW = scale.X * texture.Width;
                destH = scale.Y * texture.Height;
                oX = origin.X / texture.Width;
                oY = origin.Y / texture.Height;
            }
            float sin = 0, cos = 1;
            if (rotation != 0)
            {
                sin = MathF.Sin(rotation);
                cos = MathF.Cos(rotation);
            }
            AddSprite(texture, sX, sY, sW, sH, pos.X, pos.Y, destW, destH, color, oX, oY, sin, cos, depth, (byte)effects, rotation != 0);
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
                (byte)(effects & (SpriteEffects)0x03),
                rotation != 0
            );
        }
        
        public void DrawFast(Texture2D texture, Rectangle dest, Rectangle? source, XnaVector3 color, float rotation, XnaVector2 origin, SpriteEffects effects, float depth)
        {
            float sX = 0, sY = 0, sW = 1, sH = 1;
            float oX = 0, oY = 0;
            if (source.HasValue)
            {
                Rectangle src = source.Value;
                // only one division per axis, end of the paranoia
                float invW = 1.0f / texture.Width;
                float invH = 1.0f / texture.Height;
                sX = src.X * invW;
                sY = src.Y * invH;
                sW = src.Width * invW;
                sH = src.Height * invH;
                // origin normalized without passing on the textureWidth (basic calculations)
                oX = origin.X / src.Width;
                oY = origin.Y / src.Height;
            }
            float sin = 0, cos = 1;
            if (rotation != 0) // Il salvavita della CPU
            {
                sin = MathF.Sin(rotation);
                cos = MathF.Cos(rotation);
            }
            AddSprite(texture, sX, sY, sW, sH, dest.X, dest.Y, dest.Width, dest.Height, color, oX, oY, sin, cos, depth, (byte)effects, rotation != 0);
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
            byte effects,
            bool useMesh = false
        )
        {
            EnsureSize();
            SetVertexFast
            (
                ref _vertexInfo[_numSprites],
                sourceX, sourceY, sourceW, sourceH,
                destinationX, destinationY, destinationW, destinationH,
                color,
                originX, originY,
                rotationSin, rotationCos,
                depth, effects,
                texture.IsFromTextureAtlas
            );
            FinalizeVertex(ref _vertexInfo[_numSprites], useMesh);
            PushSprite(texture);
        }
        
        public void Begin()
        {
            var hueTex1 = GraphicsDevice.Textures[1].UnityTexture;
            var hueTex2 = GraphicsDevice.Textures[2].UnityTexture;
            hueMaterial.SetTexture(HueTex1, hueTex1);
            hueMaterial.SetTexture(HueTex2, hueTex2);
            hueMeshMaterial.SetTexture(HueTex1, hueTex1);
            hueMeshMaterial.SetTexture(HueTex2, hueTex2);
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
            //TextureSwitches = 0;
            //FlushesDone = 0;
            CustomEffect = customEffect;
            _transformMatrix = transform_matrix;
        }
        
        public void End()
        {
            Flush();
            CustomEffect = null;
        }
        
        private void FlipTextureVertically(ref PositionNormalTextureColor4 sprite, bool isFromTextureAtlas)
        {
            // MobileUO: we must flip the sprite vertically for rendering
            if (isFromTextureAtlas)
            {
                // flip vertically relative to the sprite sheet
                var oldTextureCoordinate0 = sprite.TextureCoordinate0;
                var oldTextureCoordinate1 = sprite.TextureCoordinate1;
                sprite.TextureCoordinate0 = sprite.TextureCoordinate2;   // BL → TL
                sprite.TextureCoordinate1 = sprite.TextureCoordinate3;   // BR → TR
                sprite.TextureCoordinate2 = oldTextureCoordinate0;       // TL → BL
                sprite.TextureCoordinate3 = oldTextureCoordinate1;       // TR → BR
            }
            else
            {
                // flip entire texture vertically
                sprite.TextureCoordinate0.y = 1f - sprite.TextureCoordinate0.y;
                sprite.TextureCoordinate1.y = 1f - sprite.TextureCoordinate1.y;
                sprite.TextureCoordinate2.y = 1f - sprite.TextureCoordinate2.y;
                sprite.TextureCoordinate3.y = 1f - sprite.TextureCoordinate3.y;
            }
        }
        
        private unsafe void SetVertexFast
        (
            ref PositionNormalTextureColor4 sprite,
            float sourceX, float sourceY, float sourceW, float sourceH,
            float destinationX, float destinationY, float destinationW, float destinationH,
            XnaVector3 color,
            float originX, float originY,
            float rotationSin, float rotationCos,
            float depth,
            byte effects,
            bool isFromTextureAtlas
        )
        {
            if (DIVIDE_DEPTH) depth *= 0.001f;
            // pre-invert Y: atlas textures flip within the sprite rect (SetDataPointerEXT),
            // non-atlas textures flip the entire texture (SetDataBytes), so sub-rect UVs differ.
            if (isFromTextureAtlas)
                sourceY += sourceH;   // atlas: top of sprite = sY + sH after per-sprite flip
            else
                sourceY = 1f - sourceY; // non-atlas: top of sprite = 1 - sY after whole-texture flip
            sourceH = -sourceH;
            // Both X and Y flip resolved on CPU via corner XOR (correct for sub-texture UVs)
            int eff = effects & 0x03;
            fixed (PositionNormalTextureColor4* pSprite = &sprite)
            {
                float* f = (float*)pSprite;
                for (int i = 0; i < 4; i++)
                {
                    // POSITION
                    float vX = (i & 1) - originX;
                    float vY = (i >> 1) - originY;
                    float cornerX = vX * destinationW;
                    float cornerY = vY * destinationH;
                    f[0] = (-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX;
                    f[1] = (rotationCos * cornerY) + (rotationSin * cornerX) + destinationY;
                    f[2] = depth;
                    // NORMAL (0,0,1) constant
                    *((Vector3*)(f + 3)) = Vector3.forward;
                    // UV — full XOR flip on CPU (correct for sub-texture atlas UVs)
                    int cornerIdx = i ^ eff;
                    f[6] = (_cornerOffsetX[cornerIdx] * sourceW) + sourceX;
                    f[7] = (_cornerOffsetY[cornerIdx] * sourceH) + sourceY;
                    f[8] = 0;
                    // HUE (xyz = color, w = 0; hue.w reserved for future per-vertex X-flip)
                    *(XnaVector3*)(f + 9) = color;
                    f[12] = 0f;
                    f += 13;
                }
            }
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
            var src = BlendModesMatchingXna[(int)blendState.ColorSourceBlend];
            var dst = BlendModesMatchingXna[(int)blendState.ColorDestinationBlend];
            SetMaterialBlendState(mat, src, dst);
        }
        
        private static void SetMaterialBlendState(Material mat, BlendMode src, BlendMode dst)
        {
            mat.SetFloat(SrcBlend, (float)src);
            mat.SetFloat(DstBlend, (float)dst);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSize()
        {
            //EnsureStarted();
            //if (_numSprites >= MAX_SPRITES)
            //{
            //    Flush();
            //}
            if (_numSprites >= _vertexInfo.Length)
            {
                //Flush();
                int newMax = _vertexInfo.Length + MAX_SPRITES;
                Array.Resize(ref _vertexInfo, newMax);
                Array.Resize(ref _textureInfo, newMax);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PushSprite(Texture2D texture)
        {
            if (texture == null || texture.IsDisposed)
            {
                return false;
            }
            EnsureSize();
            _textureInfo[_numSprites++] = texture;
            return true;
        }
        
        private void ApplyStates()
        {
            // GraphicsDevice.BlendState = _blendState;
            SetMaterialBlendState(hueMaterial, _blendState);
            SetMaterialBlendState(hueMeshMaterial, _blendState);
            SetMaterialBlendState(xbrMaterial, _blendState);
            GraphicsDevice.DepthStencilState = _stencil;
            // GraphicsDevice.RasterizerState = _useScissor ? _rasterizerState : RasterizerState.CullNone;
            // GraphicsDevice.SamplerStates[0] = _sampler;
            // MobileUO: keep old scissor logic or else gumps like world map won't be clipped!
            // hueMaterial and hueMeshMaterial use shader keyword for branch-free scissor
            xbrMaterial.SetFloat(Scissor, _useScissor ? 1 : 0);
            if (_useScissor)
            {
                var scissorRect = GraphicsDevice.ScissorRectangle;
                var scissorVector4 = new Vector4(scissorRect.X * scale,
                    scissorRect.Y * scale,
                    scissorRect.X * scale + scissorRect.Width * scale,
                    scissorRect.Y * scale + scissorRect.Height * scale);
                hueMaterial.EnableKeyword("SCISSOR_ON");
                hueMaterial.SetVector(ScissorRect, scissorVector4);
                hueMeshMaterial.EnableKeyword("SCISSOR_ON");
                hueMeshMaterial.SetVector(ScissorRect, scissorVector4);
                xbrMaterial.SetVector(ScissorRect, scissorVector4);
            }
            else
            {
                hueMaterial.DisableKeyword("SCISSOR_ON");
                hueMeshMaterial.DisableKeyword("SCISSOR_ON");
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
        
        private readonly List<PositionNormalTextureColor4> _runQuads = new List<PositionNormalTextureColor4>();
        private readonly List<VertexData> _batchedMeshVertices = new List<VertexData>();
        private float _lastSampleTime = UnityEngine.Time.unscaledTime;
        private PositionNormalTextureColor4[] _meshRun = new PositionNormalTextureColor4[MAX_SPRITES];
        private int _meshRunCount;
        private Texture2D _meshRunTex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureMeshRunCapacity(int needed)
        {
            if (needed <= _meshRun.Length)
                return;
            Array.Resize(ref _meshRun, Mathf.NextPowerOfTwo(needed));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AccumulateMeshQuad(Texture2D tex, in PositionNormalTextureColor4 quad)
        {
            if (_meshRunCount == 0)
            {
                _meshRunTex = tex;
            }
            else if (tex != _meshRunTex)
            {
                ++TextureSwitches;
                FlushMeshBatch();
                _meshRunTex = tex;
            }
            EnsureMeshRunCapacity(_meshRunCount + 1);
            _meshRun[_meshRunCount++] = quad;
        }

        public void FlushMeshBatch()
        {
            if (_meshRunCount == 0)
                return;
            reusedMesh.Populate(_meshRun, 0, _meshRunCount);
            var mat = hueMeshMaterial;
            mat.mainTexture = _meshRunTex.UnityTexture;
            mat.SetPass(0);
            Graphics.DrawMeshNow(reusedMesh.Mesh, Vector3.zero, Quaternion.identity);
            DrawMeshes++;
            _meshRunCount = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Min4(float a, float b, float c, float d)
            => Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Max4(float a, float b, float c, float d)
            => Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
        
        public void Flush()
        {
            if (_numSprites == 0)
            {
                return;
            }
            using (UnityProfiler.Auto(UnityProfiler.Mk_Flush))
            {
                Texture2D.FlushPendingApplies();
                using (UnityProfiler.Auto(UnityProfiler.Mk_ApplyStates))
                {
                    ApplyStates();
                }
                int arrayOffset = 0;
                nextbatch:
                ++FlushesDone;
                int batchSize = Math.Min(_numSprites, MAX_SPRITES);
                //int baseOff = UpdateVertexBuffer(arrayOffset, batchSize);
                //int offset = 0;
                Texture2D curTexture = _textureInfo[arrayOffset];
                for (int i = 0; i < batchSize; ++i)
                {
                    Texture2D texture = _textureInfo[arrayOffset + i];
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[arrayOffset + i];
                    // draw with mesh if UseDrawTexture is off or if flagged to use mesh (draw stretched land or shadows)
                    if (UserPreferences.UseDrawTexture.CurrentValue == (int)PreferenceEnums.UseDrawTexture.Off
                        || (UserPreferences.UseDrawTexture.CurrentValue == (int)PreferenceEnums.UseDrawTexture.On && vertex.UseMesh))
                    {
                        // accumulate for a mesh-batch (hue is per-vertex, no hue grouping needed)
                        using (UnityProfiler.Auto(UnityProfiler.Mk_CollectMesh))
                        {
                            AccumulateMeshQuad(texture, vertex);
                        }
                    }
                    // else use draw texture
                    else
                    {
                        using (UnityProfiler.Auto(UnityProfiler.Mk_DrawTexture))
                        {
                            using (UnityProfiler.Auto(UnityProfiler.Mk_FlushMesh))
                            {
                                FlushMeshBatch();
                            }
                            Rect src, dst;
                            using (UnityProfiler.Auto(UnityProfiler.Mk_ComputeRects))
                            {
                                float xMin = Min4(vertex.Position0.x, vertex.Position1.x, vertex.Position2.x, vertex.Position3.x);
                                float xMax = Max4(vertex.Position0.x, vertex.Position1.x, vertex.Position2.x, vertex.Position3.x);
                                float yMin = Min4(vertex.Position0.y, vertex.Position1.y, vertex.Position2.y, vertex.Position3.y);
                                float yMax = Max4(vertex.Position0.y, vertex.Position1.y, vertex.Position2.y, vertex.Position3.y);
                                int ix0 = Mathf.RoundToInt(xMin);
                                int iy0 = Mathf.RoundToInt(yMin);
                                int ix1 = Mathf.RoundToInt(xMax);
                                int iy1 = Mathf.RoundToInt(yMax);
                                dst = Rect.MinMaxRect(ix0, iy0, ix1, iy1);
                                // flip vertically
                                vertex.TextureCoordinate0.y = 1f - vertex.TextureCoordinate0.y;
                                vertex.TextureCoordinate1.y = 1f - vertex.TextureCoordinate1.y;
                                vertex.TextureCoordinate2.y = 1f - vertex.TextureCoordinate2.y;
                                vertex.TextureCoordinate3.y = 1f - vertex.TextureCoordinate3.y;
                                // compute uv src rect
                                float u0 = vertex.TextureCoordinate0.x;
                                float v0 = 1 - vertex.TextureCoordinate3.y;
                                float u1 = vertex.TextureCoordinate1.x - u0;
                                float v1 = vertex.TextureCoordinate2.y - vertex.TextureCoordinate0.y;
                                // apply X flip from hue.w if needed (SetVertexFast removed CPU X-flip)
                                if (vertex.Hue0.w > 0.5f)
                                {
                                    u0 += u1;
                                    u1 = -u1;
                                }
                                src = new Rect(u0, v0, u1, v1);
                            }
                            if (CustomEffect is XBREffect xbrEffect)
                            {
                                int w = Mathf.Max(1, Mathf.RoundToInt(src.width * texture.UnityTexture.width));
                                int h = Mathf.Max(1, Mathf.RoundToInt(src.height * texture.UnityTexture.height));
                                xbrMaterial.SetVector(TextureSize, new Vector4(w, h, 1f / w, 1f / h));
                                xbrMaterial.mainTexture = texture.UnityTexture;
                                Graphics.DrawTexture(dst, texture.UnityTexture, src, 0, 0, 0, 0, xbrMaterial);
                            }
                            else
                            {
                                Vector4 hue = vertex.Hue0;
                                hueMaterial.SetColor(Hue, new Color(hue.x, hue.y, hue.z));
                                Graphics.DrawTexture(dst, texture.UnityTexture, src, 0, 0, 0, 0, hueMaterial);
                            }
                            DrawTextures++;
                        }
                    }
                }
                using (UnityProfiler.Auto(UnityProfiler.Mk_FlushMesh))
                {
                    FlushMeshBatch();
                }
                if (_numSprites > MAX_SPRITES)
                {
                    Debug.Log($"{_numSprites} more than {MAX_SPRITES} sprites in batch, flushing in chunks");
                    _numSprites -= MAX_SPRITES;
                    arrayOffset += MAX_SPRITES;
                    goto nextbatch;
                }
                _numSprites = 0;
            }
        }
        
        // Calculate flushes and texture switches per second
        public void TickStats(float now)
        {
            if (now - _lastSampleTime < 1f)
                return;
            FlushesPerSecond = FlushesDone;
            TextureSwitchesPerSecond = TextureSwitches;
            DrawTexturesPerSecond = DrawTextures;
            DrawMeshesPerSecond = DrawMeshes;
            FlushesDone = 0;
            TextureSwitches = 0;
            DrawTextures = 0;
            DrawMeshes = 0;
            _lastSampleTime = now;
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
                // MobileUO: Re-apply the new scissor to the material
                ApplyStates();
                return true;
            }
            return false;
        }
        
        public void ClipEnd()
        {
            // MobileUO: Draw whatever was accumulated under the current scissor
            Flush();
            EnableScissorTest(false);
            ScissorStack.PopScissors(GraphicsDevice);
            // MobileUO: Push scissor change into the material
            ApplyStates();
        }
        
        // MobileUO: keep old Scissor test logic
        public void EnableScissorTest(bool enable)
        {
            if (enable == _useScissor)
                return;
            if (!enable && _useScissor && ScissorStack.HasScissors)
                return;
            _useScissor = enable;
            //ApplyStates();
            Flush();
        }
        
        public void SetBlendState(BlendState blend)
        {
            Flush();
            _blendState = blend ?? BlendState.AlphaBlend;
            //ApplyStates();
        }
        
        public void SetStencil(DepthStencilState stencil)
        {
            Flush();
            _stencil = stencil ?? Stencil;
            //ApplyStates();
        }
        
        public void SetSampler(SamplerState sampler)
        {
            Flush();
            _sampler = sampler ?? SamplerState.PointClamp;
        }
        
        public void Dispose()
        {
            _vertexInfo = null;
            _basicUOEffect?.Dispose();
        }
        // MobileUO: make public
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PositionNormalTextureColor4 : IVertexType
        {
            public Vector3 Position0;
            public Vector3 Normal0;
            public Vector3 TextureCoordinate0;
            public Vector4 Hue0;
            public Vector3 Position1;
            public Vector3 Normal1;
            public Vector3 TextureCoordinate1;
            public Vector4 Hue1;
            public Vector3 Position2;
            public Vector3 Normal2;
            public Vector3 TextureCoordinate2;
            public Vector4 Hue2;
            public Vector3 Position3;
            public Vector3 Normal3;
            public Vector3 TextureCoordinate3;
            public Vector4 Hue3;
            public bool UseMesh;
            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
            private static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),                          // position
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),            // normal
                new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0), // tex coord
                new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1)  // hue (xyz=color, w=xFlip)
            );
            public const int SIZE_IN_BYTES = sizeof(float) * 13 * 4;
        }
        
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct InvertedCoords
        {
        }
        
        public struct VertexData
        {
            public PositionNormalTextureColor4 Vertex;
            public Texture2D Texture;
            public Vector4 Hue;
            public bool UseMesh;
            public VertexData(PositionNormalTextureColor4 vertex, Texture2D texture, Vector4 hue, bool useMesh = false)
            {
                this.Vertex = vertex;
                this.Texture = texture;
                this.Hue = hue;
                this.UseMesh = useMesh;
            }
        }
    }
}