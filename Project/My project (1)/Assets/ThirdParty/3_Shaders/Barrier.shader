Shader "Custom/MagicBarrier_Uniform_URP"
{
    Properties
    {
        [HDR] _BaseColor ("Magic Color (HDR)", Color) = (0.2, 0.6, 1.0, 0.5)
        _MainTex ("Magic Pattern (Noise/Hexagon)", 2D) = "white" {}
        _ScrollX ("Scroll X Speed", Float) = 0.5
        _ScrollY ("Scroll Y Speed", Float) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // 투명도 블렌딩
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                // 프레넬을 안 쓰므로 normal 데이터가 필요 없습니다. (최적화)
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float _ScrollX;
                float _ScrollY;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                // 시간에 따른 UV 스크롤링 계산
                float2 timeScroll = float2(_Time.y * _ScrollX, _Time.y * _ScrollY);
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw + timeScroll;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 마법 패턴 텍스처 샘플링
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // 시야각(Fresnel) 계산을 완전히 제거하고, 
                // 텍스처와 BaseColor의 설정값 그대로 색상과 투명도를 결정합니다.
                half3 finalColor = _BaseColor.rgb * texColor.rgb * 2.0;
                
                // 최종 투명도는 텍스처의 밝기(r)와 컬러 슬롯의 Alpha(a) 값에 비례합니다.
                half alpha = texColor.r * _BaseColor.a;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}