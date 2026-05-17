// 화면 공간 깊이 차이 기반 엣지 검출 셰이더
// PhaseOutlineFeature 또는 FullScreenPassRendererFeature 에서 사용
Shader "Hidden/VRAdaptation/PhaseOutline"
{
    Properties
    {
        // FullScreenPassRendererFeature 호환용 (_BlitTexture)
        _BlitTexture ("Source",          2D)              = "white" {}
        _OutlineColor("Outline Color",   Color)           = (0.2, 0.6, 1.0, 1.0)
        _DepthThresh ("Depth Threshold", Range(0.001, 0.1)) = 0.015
        _Thickness   ("Thickness (px)",  Range(0.5, 5.0)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off  ZTest Always  Cull Off

        Pass
        {
            Name "PhaseOutlineEdge"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert_fs
            #pragma fragment frag_edge
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // FullScreenPassRendererFeature 가 바인딩하는 소스 텍스처
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BlitTexture_TexelSize;
                float4 _OutlineColor;
                float  _DepthThresh;
                float  _Thickness;
            CBUFFER_END

            struct Attr { uint vertID : SV_VertexID; };
            struct Vary { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // 인덱스 0,1,2 → 전체 화면 삼각형
            Vary vert_fs(Attr IN)
            {
                Vary OUT;
                OUT.uv    = float2((IN.vertID << 1) & 2, IN.vertID & 2);
                OUT.posCS = float4(OUT.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    OUT.uv.y = 1.0 - OUT.uv.y;
                #endif
                return OUT;
            }

            // 선형 깊이(Eye depth) 기반 엣지 검출
            // → 거리 변화에 강인하고 모든 각도에서 물체 경계를 검출
            float CalcEdge(float2 uv)
            {
                float2 step = _Thickness * _BlitTexture_TexelSize.xy;

                // 5점 샘플 (중앙 + 상하좌우)
                float dc = LinearEyeDepth(SampleSceneDepth(uv),              _ZBufferParams);
                float d0 = LinearEyeDepth(SampleSceneDepth(uv + float2( step.x, 0)),  _ZBufferParams);
                float d1 = LinearEyeDepth(SampleSceneDepth(uv + float2(-step.x, 0)),  _ZBufferParams);
                float d2 = LinearEyeDepth(SampleSceneDepth(uv + float2(0,  step.y)),  _ZBufferParams);
                float d3 = LinearEyeDepth(SampleSceneDepth(uv + float2(0, -step.y)),  _ZBufferParams);

                // 중앙과의 최대 차이 → 엣지 강도
                float maxDiff = max(max(abs(dc - d0), abs(dc - d1)),
                                    max(abs(dc - d2), abs(dc - d3)));

                // 거리 보정: 멀리 있어도 일정한 선 폭
                float normDiff = maxDiff / max(dc, 0.01);
                return step(_DepthThresh, normDiff);
            }

            half4 frag_edge(Vary IN) : SV_Target
            {
                half4 src  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, IN.uv);
                float edge = CalcEdge(IN.uv);
                return lerp(src, half4(_OutlineColor.rgb, 1.0), edge);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
