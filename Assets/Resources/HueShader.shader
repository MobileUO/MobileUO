Shader "Unlit/HueShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DstBlend", Float) = 10
        _Brightlight ("Brightness", Float) = 0.0
        _Viewport ("Viewport", Vector) = (1, 1, 0, 0)
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
            
            static const float HuesPerTexture = 2048;

            static const float3 LIGHT_DIRECTION = float3(0.0f, 1.0f, 1.0f);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
                float3 Hue : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 Normal : NORMAL;
                float4 pos : SV_POSITION;
                float3 Hue : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Hue;
            float _uvMirrorX;
            float _Debug;
            float _Scissor;
            float4 _ScissorRect;
            float4 _Viewport;
            float _Brightlight;

            sampler2D _HueTex1;
            sampler2D _HueTex2;
            sampler2D _HueTex3;

            float3 get_rgb(float gray, float hue)
            {
                if (hue <= HuesPerTexture)
                {
                    float2 texcoord = float2(gray, hue / HuesPerTexture);
                    return tex2D(_HueTex1, texcoord).rgb;
                }
                else
                {
                    float2 texcoord = float2(gray, (hue - HuesPerTexture) / HuesPerTexture);
                    return tex2D(_HueTex2, texcoord).rgb;
                }
            }

            float get_light(float3 norm)
            {
                float3 light = normalize(LIGHT_DIRECTION);
                float3 normal = normalize(norm);
                float base = (max(dot(normal, light), 0.0f) / 2.0f) + 0.5f;

	            // At 45 degrees (the angle the flat tiles are lit at) it must come out
	            // to (cos(45) / 2) + 0.5 or 0.85355339...
	            return base + ((_Brightlight * (base - 0.85355339f)) - (base - 0.85355339f));
            }

            float3 get_colored_light(float shader, float gray)
            {
	            float2 texcoord = float2(gray, (shader - 0.5) / 63);

	            return tex2D(_HueTex3, texcoord).rgb;
            }

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);
                
                // Adjust the position based on the viewport dimensions
                // MobileUO: TODO: this does "work" but it moves the rendering way off. I'm not sure why it is needed or if it is even needed for Unity ~mandlar
                // pos.x -= 0.5 / _Viewport.x;
                // pos.y += 0.5 / _Viewport.y;
                
                o.pos = pos;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.Normal = v.normal;
                o.Hue = v.Hue;
                return o;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                if(_Scissor == 1)
                {
                    #if UNITY_UV_STARTS_AT_TOP == false
                    IN.pos.y = _ScreenParams.y - IN.pos.y;
                    #endif
                    if(IN.pos.x < _ScissorRect.x || IN.pos.x > _ScissorRect.z || IN.pos.y < _ScissorRect.y || IN.pos.y > _ScissorRect.w)
                    {
                        discard;
                    }
                }

                if(_uvMirrorX == 1)
                {
                    IN.uv.x = 1 - IN.uv.x;
                }

                float4 color = tex2D(_MainTex, IN.uv.xy);

                if (color.a == 0.0f)
                    discard;

                int mode = int(_Hue.y);
                float alpha = _Hue.z;

                if (mode == NONE)
                {
                    return color * alpha;
                }

                float hue = _Hue.x;
                if (mode >= GUMP)
                {
                    mode -= GUMP;

                    if (color.r < 0.02f)
                    {
                        hue = 0;
                    }
                }

                if (mode == HUED || (mode == PARTIAL_HUED && color.r == color.g && color.r == color.b))
                {
                    color.rgb = get_rgb(color.r, hue);
                }
                else if (mode == HUE_TEXT_NO_BLACK)
	            {
		            if (color.r > 0.04f || color.g > 0.04f || color.b > 0.04f)
		            {
			            color.rgb = get_rgb(1.0f, hue);
		            }
	            }
	            else if (mode == HUE_TEXT)
	            {
		            // 31 is max red, so this is just selecting the color of the darkest pixel in the hue
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
		            color.r = 0;
		            color.g = 0;
		            color.b = 0;
	            }
	            else if (mode == SHADOW)
	            {
		            alpha = 0.4f;
		            color.r = 0;
		            color.g = 0;
		            color.b = 0;
	            }
	            else if (mode == LIGHTS)
	            {
			        color.rgb = get_colored_light(IN.Hue.x - 1, color.r);
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
