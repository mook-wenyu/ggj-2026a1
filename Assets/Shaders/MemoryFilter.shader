Shader "Custom/MemoryFilter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.95, 0.88, 0.35)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        
        [Header(Nostalgia)]
        _Sepia ("Sepia Strength", Range(0, 1)) = 0.6
        _Warmth ("Warmth", Range(0, 1)) = 0.7
        _Fade ("Fade / Bleach", Range(0, 1)) = 0.25
        _Vignette ("Vignette", Range(0, 1)) = 0.4
        _VignetteSoft ("Vignette Softness", Range(0.1, 2)) = 1.2
        
        [Header(Film)]
        _Grain ("Grain Amount", Range(0, 0.15)) = 0.04
        _GrainSpeed ("Grain Speed", Range(0, 5)) = 1.5
        _Scanline ("Scanline", Range(0, 0.2)) = 0.03
        _ScanSpeed ("Scanline Speed", Range(0, 3)) = 0.5
        
        [Header(Motion)]
        _DriftSpeed ("Color Drift Speed", Range(0, 1)) = 0.1
        _PulseSpeed ("Vignette Pulse Speed", Range(0, 2)) = 0.3
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
            float _Sepia;
            float _Warmth;
            float _Fade;
            float _Vignette;
            float _VignetteSoft;
            float _Grain;
            float _GrainSpeed;
            float _Scanline;
            float _ScanSpeed;
            float _DriftSpeed;
            float _PulseSpeed;
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 uv, float t)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i + t);
                float b = hash(i + float2(1, 0) + t);
                float c = hash(i + float2(0, 1) + t);
                float d = hash(i + float2(1, 1) + t);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
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
                fixed4 tex = tex2D(_MainTex, i.texcoord) * i.color;
                
                float t = _Time.y;
                float2 uv = i.texcoord;
                
                float vignette = length(uv - 0.5) * 2.0;
                float pulse = 0.5 + 0.5 * sin(t * _PulseSpeed);
                vignette = 1.0 - _Vignette * (1.0 + pulse * 0.2) * smoothstep(_VignetteSoft, 0.3, vignette);
                
                float drift = 0.5 + 0.5 * sin(t * _DriftSpeed) * 0.05;
                float3 sepia = float3(0.92 + _Warmth * 0.08 + drift, 0.85 + _Warmth * 0.1, 0.75 + _Warmth * 0.15);
                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float3 nostalgic = lerp(tex.rgb, lum * sepia, _Sepia);
                nostalgic = lerp(nostalgic, float3(1, 1, 1), _Fade * (1.0 - lum));
                
                float grain = (noise(uv * 200.0, t * _GrainSpeed) - 0.5) * _Grain;
                nostalgic += grain;
                
                float scan = sin(uv.y * 400.0 + t * _ScanSpeed * 100.0) * 0.5 + 0.5;
                nostalgic -= scan * _Scanline;
                
                tex.rgb = saturate(nostalgic);
                tex.rgb *= vignette;
                
                return tex;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
