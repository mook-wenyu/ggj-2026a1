Shader "Custom/ScreenSnow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        
        [Header(Snow Effect)]
        _SnowIntensity ("雪花强度", Range(0, 1)) = 0.8
        _SnowSpeed ("雪花速度", Range(0.1, 10)) = 2
        _SnowScale ("雪花粒度", Range(10, 500)) = 80
        _SnowBrightness ("亮度", Range(0, 2)) = 1
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _SnowIntensity;
            float _SnowSpeed;
            float _SnowScale;
            float _SnowBrightness;
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p, float t)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i + float2(0, 0) + t);
                float b = hash(i + float2(1, 0) + t);
                float c = hash(i + float2(0, 1) + t);
                float d = hash(i + float2(1, 1) + t);
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float snowNoise(float2 uv, float t)
            {
                float n = 0;
                float f = 1;
                float a = 1;
                for (int i = 0; i < 3; i++)
                {
                    n += a * noise(uv * f, t + float(i) * 17.3);
                    f *= 2;
                    a *= 0.5;
                }
                return saturate(n);
            }
            
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                
                float t = _Time.y * _SnowSpeed;
                float2 uv = i.texcoord * _SnowScale;
                float n = snowNoise(uv, t);
                
                float snow = n * _SnowBrightness;
                col.rgb = lerp(col.rgb, float3(snow, snow, snow), _SnowIntensity);
                col.rgb = saturate(col.rgb);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
