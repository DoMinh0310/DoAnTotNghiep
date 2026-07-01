Shader "ProjectM/UI/DynamicShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0,0,0, 0.6)
        
        _ShadowDepth ("Độ nén trục Y (Âm để lật ngược xuống)", Float) = -0.5
        _ShadowDistance ("Lực đẩy Skew X", Float) = 0.5
        _BottomY ("Toạ độ gót chân (Thường là -150 hoặc 0)", Float) = -150

        // Bắt buộc phải có cho UI Canvas
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        // Khối Stencil để không bị lỗi tàng hình trong Canvas
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off 
        Lighting Off 
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _ShadowColor;
            float _ShadowDepth;
            float _ShadowDistance;
            float _BottomY;
            
            float4 _GlobalLightPos; 

            v2f vert(appdata_t v)
            {
                v2f OUT;
                
                // Lấy toạ độ tâm thẻ
                float4 objectWorldPos = mul(unity_ObjectToWorld, float4(0,0,0,1));
                
                // Vector từ mặt trăng đâm tới thẻ 
                float2 lightDir = objectWorldPos.xy - _GlobalLightPos.xy;
                
                // CHUẨN HOÁ HƯỚNG SÁNG: Chỉ lấy hướng, không lấy khoảng cách
                // Tránh việc tính toán toạ độ pixel bị nổ tung (giúp bóng không biến thành đường thẳng)
                float2 normalizedLightDir = normalize(lightDir);

                // Ép dẹt và lật ngược (yêu cầu Pivot Y = 0)
                v.vertex.y = v.vertex.y * _ShadowDepth;
                
                // Bóng ngả tạt ngang dựa trên toạ độ UV (v.texcoord.y từ 0 đến 1)
                // Đỉnh thẻ (UV=1) sẽ ngả nhiều nhất, đáy thẻ (UV=0) sẽ đứng im
                v.vertex.x += normalizedLightDir.x * v.texcoord.y * _ShadowDistance * 100.0;
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = _ShadowColor;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half alpha = tex2D(_MainTex, IN.texcoord).a;
                return fixed4(IN.color.rgb, IN.color.a * alpha);
            }
            ENDCG
        }
    }
}
