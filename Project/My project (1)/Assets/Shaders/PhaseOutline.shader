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

            // Quest 3 Single-Pass Instanced / Multiview 지원에 필수
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float  _DepthThresh;
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

                float dc = LinearEyeDepth(SampleSceneDepth(uv),                      _ZBufferParams);
                float d0 = LinearEyeDepth(SampleSceneDepth(uv + float2( px.x, 0.0)), _ZBufferParams);
                float d1 = LinearEyeDepth(SampleSceneDepth(uv + float2(-px.x, 0.0)), _ZBufferParams);
                float d2 = LinearEyeDepth(SampleSceneDepth(uv + float2(0.0,  px.y)), _ZBufferParams);
                float d3 = LinearEyeDepth(SampleSceneDepth(uv + float2(0.0, -px.y)), _ZBufferParams);

                float maxDiff = max(max(abs(dc - d0), abs(dc - d1)), max(abs(dc - d2), abs(dc - d3)));
                return step(_DepthThresh, maxDiff / max(dc, 0.01));
            }

            half4 frag_edge(Vary IN) : SV_Target
            {
                // Stereo eye index 설정 — SampleSceneDepth 가 올바른 eye를 샘플링하도록
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float edge = CalcEdge(IN.uv);
                return half4(lerp(float3(0, 0, 0), _OutlineColor.rgb, edge), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
