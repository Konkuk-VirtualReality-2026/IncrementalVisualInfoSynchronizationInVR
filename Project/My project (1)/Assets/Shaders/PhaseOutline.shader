// 화면 공간 깊이+법선 불연속 기반 엣지 검출 셰이더
// Phase 2: 비엣지 = 완전 검정, 엣지 = 아웃라인 색상
// 법선 차이 검출 덕분에 카메라 이동 방향에 무관하게 모든 기하 경계를 검출한다.
Shader "Hidden/VRAdaptation/PhaseOutline"
{
    Properties
    {
        _BlitTexture  ("Source",           2D)               = "white" {}
        _OutlineColor ("Outline Color",    Color)            = (0.7, 0.9, 1.0, 1.0)
        _DepthThresh  ("Depth Threshold",  Range(0.001, 0.1)) = 0.015
        _NormalThresh ("Normal Threshold", Range(0.01,  1.0)) = 0.25
        _Thickness    ("Thickness (px)",   Range(0.5,   4.0)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off  ZTest Always  Cull Off

        Pass
        {
            Name "PhaseOutlineEdge"

            HLSLPROGRAM
            #pragma vertex   vert_fs
            #pragma fragment frag_edge
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _DepthThresh;
                float  _NormalThresh;
                float  _Thickness;
            CBUFFER_END

            struct Attr { uint vertID : SV_VertexID; };
            struct Vary { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

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

            float CalcEdge(float2 uv)
            {
                // _BlitTexture_TexelSize 는 MaterialPropertyBlock 공간에 있어 0 이 됨.
                // Unity 빌트인 _ScreenParams 로 픽셀 크기 계산.
                float2 px = _Thickness * rcp(_ScreenParams.xy);

                // ── 깊이 엣지: 오브젝트 경계, 실루엣 ──────────────────────
                float dc = LinearEyeDepth(SampleSceneDepth(uv),                    _ZBufferParams);
                float d0 = LinearEyeDepth(SampleSceneDepth(uv + float2( px.x, 0)), _ZBufferParams);
                float d1 = LinearEyeDepth(SampleSceneDepth(uv + float2(-px.x, 0)), _ZBufferParams);
                float d2 = LinearEyeDepth(SampleSceneDepth(uv + float2(0,  px.y)), _ZBufferParams);
                float d3 = LinearEyeDepth(SampleSceneDepth(uv + float2(0, -px.y)), _ZBufferParams);

                float maxDepthDiff = max(max(abs(dc-d0), abs(dc-d1)), max(abs(dc-d2), abs(dc-d3)));
                float depthEdge    = step(_DepthThresh, maxDepthDiff / max(dc, 0.01));

                // ── 법선 엣지: 인접 픽셀 법선 방향 차이 → 기하 경계 ──────
                // 카메라/이동 방향과 완전히 무관 — 모든 각도에서 동일한 경계를 검출
                float3 nc = SampleSceneNormals(uv);
                float3 n0 = SampleSceneNormals(uv + float2( px.x, 0));
                float3 n1 = SampleSceneNormals(uv + float2(-px.x, 0));
                float3 n2 = SampleSceneNormals(uv + float2(0,  px.y));
                float3 n3 = SampleSceneNormals(uv + float2(0, -px.y));

                // dot 이 1 에 가까울수록 같은 법선(비엣지), 낮을수록 다른 법선(엣지)
                float nd = max(max(1.0 - saturate(dot(nc, n0)),
                                   1.0 - saturate(dot(nc, n1))),
                               max(1.0 - saturate(dot(nc, n2)),
                                   1.0 - saturate(dot(nc, n3))));
                float normalEdge = step(_NormalThresh, nd);

                return saturate(depthEdge + normalEdge);
            }

            half4 frag_edge(Vary IN) : SV_Target
            {
                // Phase 2: 비엣지 = 완전 검정, 엣지 = 아웃라인 색상
                // src (_BlitTexture) 는 ProgressiveLOD Phase2가 이미 검게 만들었지만
                // 다른 셰이더 오브젝트를 위해 src 에서 lerp 해서 덮어씌운다.
                float edge = CalcEdge(IN.uv);
                return half4(lerp(float3(0,0,0), _OutlineColor.rgb, edge), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
