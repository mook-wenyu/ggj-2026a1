Shader "Custom/WindowSunlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.98, 0.92, 1)
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        
        [Header(Glow)]
        _CenterX ("Center X", Range(0, 1)) = 0.5
        _CenterY ("Center Y", Range(0, 1)) = 0.5
        _Radius ("Radius", Range(0.01, 2)) = 0.5
        _Softness ("Softness", Range(0.01, 2)) = 1.5
        _Intensity ("Intensity", Range(0, 3)) = 1.5
        
        [Header(Animation)]
        _MoveSpeed ("Move Speed", Range(0, 2)) = 0.15
        _MoveRadius ("Move Radius", Range(0, 0.5)) = 0.15
        _RotateSpeed ("Rotate Speed", Range(-2, 2)) = 0.2
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
            float _CenterX;
            float _CenterY;
            float _Radius;
            float _Softness;
            float _Intensity;
            float _MoveSpeed;
            float _MoveRadius;
            float _RotateSpeed;
            
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
                fixed4 tex = tex2D(_MainTex, i.texcoord);
                
                float t = _Time.y;
                float moveX = _MoveRadius * sin(t * _MoveSpeed);
                float moveY = _MoveRadius * cos(t * _MoveSpeed * 0.7);
                float2 center = float2(_CenterX + moveX, _CenterY + moveY);
                
                float2 toCenter = i.texcoord - center;
                float angle = atan2(toCenter.y, toCenter.x) + t * _RotateSpeed;
                float dist = length(toCenter);
                float stretch = 1.0 + 0.15 * sin(angle * 2.0 + t * 0.5);
                dist *= stretch;
                
                float falloff = 1.0 - smoothstep(0, _Radius * _Softness, dist);
                falloff = pow(falloff, 1.2);
                float glow = falloff * _Intensity;
                
                fixed4 col = tex * i.color;
                col.rgb += col.a * glow * _Color.rgb;
                col.a = max(col.a, glow * _Color.a * 0.6);
                col.rgb = saturate(col.rgb);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
