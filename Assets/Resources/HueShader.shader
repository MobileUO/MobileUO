Shader "Unlit/HueShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DstBlend", Float) = 10
        _Brightlight ("Brightness", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ MESH_HUE
            #pragma multi_compile _ SCISSOR_ON

            #include "UnityCG.cginc"

            static const int NONE = 0;
            static const int HUED = 1;
            static const int PARTIAL_HUED = 2;
            static const int HUE_TEXT_NO_BLACK = 3;
            static const int HUE_TEXT = 4;
            static const int LAND = 5;
            static const int LAND_COLOR = 6;
            static const int SPECTRAL = 7;
            static const int SHADOW = 8;
            static const int LIGHTS = 9;
            static const int EFFECT_HUED = 10;
            static const int GUMP = 20;

            static const float3 LIGHT_DIRECTION = float3(0.0f, 1.0f, 1.0f);

            static const float HUE_ROWS = 1024;
            static const float HUE_COLUMNS = 16;
            static const float HUE_WIDTH = 32;
            static const float HUES_PER_TEXTURE = HUE_ROWS * HUE_COLUMNS;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 Hue : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 Normal : NORMAL;
                float4 pos : SV_POSITION;
                float4 Hue : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Brightlight;

#if !defined(MESH_HUE)
            float4 _Hue;
            float _uvMirrorX;
#endif

#ifdef SCISSOR_ON
            float4 _ScissorRect;
#endif

            sampler2D _HueTex1;
            sampler2D _HueTex2;

            float3 get_rgb(float gray, float hue)
            {
                float halfPixelX = (1.0f / (HUE_COLUMNS * HUE_WIDTH)) * 0.5f;
                float hueColumnWidth = 1.0f / HUE_COLUMNS;
                float hueStart = frac(hue / HUE_COLUMNS);
                float xPos = hueStart + gray / HUE_COLUMNS;
                xPos = clamp(xPos, hueStart + halfPixelX, hueStart + hueColumnWidth - halfPixelX);
                float yPos = (hue % HUES_PER_TEXTURE) / (HUES_PER_TEXTURE - 1);
                return tex2D(_HueTex1, float2(xPos, yPos)).rgb;
            }

            float get_light(float3 norm)
            {
                float3 light = normalize(LIGHT_DIRECTION);
                float3 normal = normalize(norm);
                float base = (max(dot(normal, light), 0.0f) / 2.0f) + 0.5f;
                return base + ((_Brightlight * (base - 0.85355339f)) - (base - 0.85355339f));
            }

            float3 get_colored_light(float shader, float gray)
            {
                float2 texcoord = float2(gray, (shader - 0.5) / 63);
                return tex2D(_HueTex2, texcoord).rgb;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.Normal = v.normal;
                o.Hue = v.Hue;
                return o;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
#ifdef SCISSOR_ON
                #if UNITY_UV_STARTS_AT_TOP == false
                float scissorY = _ScreenParams.y - IN.pos.y;
                #else
                float scissorY = IN.pos.y;
                #endif
                if (IN.pos.x < _ScissorRect.x || IN.pos.x > _ScissorRect.z ||
                    scissorY  < _ScissorRect.y || scissorY  > _ScissorRect.w)
                    discard;
#endif

#ifdef MESH_HUE
                float4 hueData = IN.Hue;
                if (hueData.w > 0.5)
                    IN.uv.x = 1.0 - IN.uv.x;
#else
                float4 hueData = _Hue;
                if (_uvMirrorX > 0.5)
                    IN.uv.x = 1.0 - IN.uv.x;
#endif

                float4 color = tex2D(_MainTex, IN.uv.xy);

                if (color.a == 0.0f)
                    discard;

                int mode = int(hueData.y);
                float alpha = hueData.z;

                if (mode == NONE)
                    return color * alpha;

                float hue = hueData.x;
                if (mode >= GUMP)
                {
                    mode -= GUMP;
                    if (color.r < 0.02f)
                        hue = 0;
                }

                if (mode == HUED || (mode == PARTIAL_HUED && color.r == color.g && color.r == color.b))
                {
                    color.rgb = get_rgb(color.r, hue);
                }
                else if (mode == HUE_TEXT_NO_BLACK)
                {
                    if (color.r > 0.04f || color.g > 0.04f || color.b > 0.04f)
                        color.rgb = get_rgb(1.0f, hue);
                }
                else if (mode == HUE_TEXT)
                {
                    color.rgb = get_rgb(1.0f, hue);
                }
                else if (mode == LAND)
                {
                    color.rgb *= get_light(IN.Normal);
                }
                else if (mode == LAND_COLOR)
                {
                    color.rgb = get_rgb(color.r, hue) * get_light(IN.Normal);
                }
                else if (mode == SPECTRAL)
                {
                    alpha = 1.0f - (color.r * 1.5f);
                    color.rgb = 0;
                }
                else if (mode == SHADOW)
                {
                    alpha = 0.4f;
                    color.rgb = 0;
                }
                else if (mode == LIGHTS)
                {
                    color.rgb = get_colored_light(hue - 1, color.r);
                }
                else if (mode == EFFECT_HUED)
                {
                    color.rgb = get_rgb(color.g, hue);
                }

                return color * alpha;
            }
            ENDCG
        }
    }
}
