// 화면 공간 깊이+법선 엣지 검출
// cmd.Blit 은 소스를 _MainTex 에 바인딩한다 → _MainTex 선언 필요
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // cmd.Blit 이 소스를 _MainTex 로 바인딩
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float  _DepthThresh;
                float  _Thickness;
            CBUFFER_END

            struct Attr { uint vertID : SV_VertexID; };
            struct Vary  { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

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
                float2 px = _Thickness * rcp(_ScreenParams.xy);

                // ── 깊이 엣지만 사용 ──────────────────────────────────
                // 법선 엣지는 URP Lit 노멀맵 오브젝트에서 오탐(표면 전체가 엣지)을
                // 유발하므로 깊이 불연속성만으로 실루엣을 검출한다.
                float dc = LinearEyeDepth(SampleSceneDepth(uv),                     _ZBufferParams);
                float d0 = LinearEyeDepth(SampleSceneDepth(uv + float2( px.x, 0)), _ZBufferParams);
                float d1 = LinearEyeDepth(SampleSceneDepth(uv + float2(-px.x, 0)), _ZBufferParams);
                float d2 = LinearEyeDepth(SampleSceneDepth(uv + float2(0,  px.y)), _ZBufferParams);
                float d3 = LinearEyeDepth(SampleSceneDepth(uv + float2(0, -px.y)), _ZBufferParams);

                float maxDiff = max(max(abs(dc-d0), abs(dc-d1)), max(abs(dc-d2), abs(dc-d3)));
                return step(_DepthThresh, maxDiff / max(dc, 0.01));
            }

            half4 frag_edge(Vary IN) : SV_Target
            {
                float edge = CalcEdge(IN.uv);
                return half4(lerp(float3(0, 0, 0), _OutlineColor.rgb, edge), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
