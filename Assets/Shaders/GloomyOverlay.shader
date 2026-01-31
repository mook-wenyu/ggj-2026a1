Shader "Custom/GloomyOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.15, 0.12, 0.1, 0.7)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        
        [Header(Vignette)]
        _VignetteStrength ("Edge Darkness", Range(0.5, 1)) = 0.95
        _CenterRadius ("Center Radius", Range(0.1, 0.8)) = 0.35
        _VignetteFalloff ("Falloff", Range(1, 4)) = 2.5
        _CenterAlpha ("Center Alpha", Range(0, 0.5)) = 0.08
        _EdgeAlpha ("Edge Alpha", Range(0.5, 1)) = 0.92
        
        [Header(Grain)]
        _GrainAmount ("Grain Amount", Range(0, 0.3)) = 0.12
        _GrainSpeed ("Grain Scroll Speed", Range(0, 10)) = 3
        _GrainScale ("Grain Size", Range(50, 300)) = 120
        
        [Header(Shadows)]
        _ShadowAmount ("Shadow Intensity", Range(0, 0.5)) = 0.15
        _ShadowSpeed ("Shadow Sway Speed", Range(0, 2)) = 0.4
        _ShadowScale ("Shadow Scale", Range(2, 15)) = 6
        _ShadowSoftness ("Shadow Softness", Range(0.5, 3)) = 1.5
        
        [Header(Motion)]
        _VignettePulse ("Vignette Pulse", Range(0, 0.2)) = 0.05
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.25
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
            float _VignetteStrength;
            float _CenterRadius;
            float _VignetteFalloff;
            float _CenterAlpha;
            float _EdgeAlpha;
            float _GrainAmount;
            float _GrainSpeed;
            float _GrainScale;
            float _ShadowAmount;
            float _ShadowSpeed;
            float _ShadowScale;
            float _ShadowSoftness;
            float _VignettePulse;
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
            
            float fbm(float2 uv, float t)
            {
                float v = 0;
                float f = 1;
                float a = 1;
                for (int i = 0; i < 3; i++)
                {
                    v += a * noise(uv * f, t + float(i) * 13.7);
                    f *= 2;
                    a *= 0.5;
                }
                return v;
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
                
                float2 toCenter = uv - 0.5;
                float dist = length(toCenter) * 2.0;
                float pulse = 1.0 + _VignettePulse * sin(t * _PulseSpeed);
                float centerMask = 1.0 - smoothstep(_CenterRadius, _CenterRadius + 0.5, dist);
                centerMask = pow(centerMask, 1.0 / _VignetteFalloff);
                float edgeMask = 1.0 - centerMask;
                
                float vignette = 1.0 - _VignetteStrength * pulse * pow(dist, _VignetteFalloff);
                vignette = saturate(vignette);
                
                tex.a *= lerp(_EdgeAlpha, _CenterAlpha, centerMask);
                tex.rgb *= lerp(0.15, 1.0, centerMask);
                tex.rgb *= vignette;
                
                float2 grainUV = uv * _GrainScale;
                grainUV.y += t * _GrainSpeed * 50.0;
                grainUV.x += t * _GrainSpeed * 30.0;
                float grain = (noise(grainUV, t * 10.0) - 0.5) * _GrainAmount;
                
                float2 shadowUV = uv * _ShadowScale;
                shadowUV += float2(sin(t * _ShadowSpeed) * 2.0, cos(t * _ShadowSpeed * 0.7) * 1.5);
                float shadow1 = fbm(shadowUV, t * _ShadowSpeed);
                float2 shadowUV2 = uv * _ShadowScale * 1.3 + float2(3.1, 2.7);
                shadowUV2 += float2(cos(t * _ShadowSpeed * 0.8) * 1.5, sin(t * _ShadowSpeed * 0.6) * 2.0);
                float shadow2 = fbm(shadowUV2, t * _ShadowSpeed + 5.0);
                float shadow = (shadow1 * 0.6 + shadow2 * 0.4);
                shadow = pow(saturate(shadow), _ShadowSoftness);
                shadow = shadow * _ShadowAmount * edgeMask;
                
                tex.rgb -= shadow;
                tex.rgb += grain;
                tex.rgb = saturate(tex.rgb);
                
                return tex;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
