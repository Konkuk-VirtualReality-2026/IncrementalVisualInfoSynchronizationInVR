// 화면 공간 깊이 엣지 검출 (XR Single-Pass Instanced / Multiview 지원)
Shader "Hidden/VRAdaptation/PhaseOutline"
{
    Properties
    {
        _MainTex      ("Source",           2D)                = "white" {}
        _OutlineColor ("Outline Color",    Color)             = (1, 1, 1, 1)
        _DepthThresh  ("Depth Threshold",  Range(0.0001, 0.1)) = 0.005
        _NormalThresh ("Normal Threshold", Range(0.01, 1.0))   = 0.5
        _Thickness    ("Thickness (px)",   Range(0.5, 4.0))    = 1.5
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

            // Quest 3 Single-Pass Instanced / Multiview 지원에 필수.
            // STEREO_MULTIVIEW_ON 이 없으면 Quest(Vulkan multiview)에서 깊이 텍스처가
            // Texture2DArray로 잡히는데도 eye index가 0으로 고정돼 엣지 검출이 실패한다.
            // (PC standalone은 multi-pass라 이 키워드 없이도 동작했음)
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            // Blitter.BlitTexture 가 소스 화면을 _BlitTexture 에 바인딩한다.
            // 원본 색(표면 검정 + 접촉 glow 링)을 읽어 보존하려고 직접 선언한다.
            // (sampler_PointClamp 는 Core.hlsl 이 이미 제공하므로 재선언하지 않는다.)
            TEXTURE2D_X(_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float  _DepthThresh;
                float  _NormalThresh;
                float  _Thickness;
            CBUFFER_END

            struct Attr
            {
                uint vertID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Vary
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Vary vert_fs(Attr IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Vary OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.uv    = float2((IN.vertID << 1) & 2, IN.vertID & 2);
                OUT.posCS = float4(OUT.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    OUT.uv.y = 1.0 - OUT.uv.y;
                #endif
                return OUT;
            }

            float CalcEdge(float2 uv)
            {
                float2 px = _Thickness * rcp(_ScreenParams.xy);

                // 깊이 차분 (오브젝트 경계, 실루엣)
                float dc = LinearEyeDepth(SampleSceneDepth(uv),                      _ZBufferParams);
                float d0 = LinearEyeDepth(SampleSceneDepth(uv + float2( px.x, 0.0)), _ZBufferParams);
                float d1 = LinearEyeDepth(SampleSceneDepth(uv + float2(-px.x, 0.0)), _ZBufferParams);
                float d2 = LinearEyeDepth(SampleSceneDepth(uv + float2(0.0,  px.y)), _ZBufferParams);
                float d3 = LinearEyeDepth(SampleSceneDepth(uv + float2(0.0, -px.y)), _ZBufferParams);

                float maxDiff = max(max(abs(dc - d0), abs(dc - d1)), max(abs(dc - d2), abs(dc - d3)));
                float depthEdge = step(_DepthThresh, maxDiff / max(dc, 0.01));

                // 법선 차분 (오목 경계, 내부 꺾임 — 깊이만으로 검출 불가한 엣지)
                // DepthNormals Pass가 _CameraNormalsTexture에 법선을 기록하므로 바로 샘플링 가능
                float3 nc = SampleSceneNormals(uv);
                float3 n0 = SampleSceneNormals(uv + float2( px.x, 0.0));
                float3 n1 = SampleSceneNormals(uv + float2(-px.x, 0.0));
                float3 n2 = SampleSceneNormals(uv + float2(0.0,  px.y));
                float3 n3 = SampleSceneNormals(uv + float2(0.0, -px.y));

                float maxNDiff = max(
                    max(distance(nc, n0), distance(nc, n1)),
                    max(distance(nc, n2), distance(nc, n3))
                );
                float normalEdge = step(_NormalThresh, maxNDiff);

                return saturate(depthEdge + normalEdge);
            }

            half4 frag_edge(Vary IN) : SV_Target
            {
                // Stereo eye index 설정 — SampleSceneDepth / _BlitTexture 가 올바른 eye를 샘플링하도록
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // 원본 화면 색을 보존한다(표면 검정 + 접촉 glow 링이 여기 들어있음).
                // 과거에는 검정에서 시작해 비-엣지 픽셀을 모두 덮어써서 glow가 사라졌다.
                half3 src  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, IN.uv).rgb;

                float edge = CalcEdge(IN.uv);
                // 엣지 픽셀은 윤곽선 색, 그 외는 원본(glow 포함) 유지.
                half3 col  = lerp(src, _OutlineColor.rgb, edge);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
