// 화면 공간 깊이·법선 차이 기반 엣지 검출 셰이더
// PhaseOutlineFeature(ScriptableRendererFeature)가 Full-Screen Blit으로 구동한다.
Shader "Hidden/VRAdaptation/PhaseOutline"
{
    Properties
    {
        _MainTex     ("Source",          2D)              = "white" {}
        _OutlineColor("Outline Color",   Color)           = (0.2, 0.6, 1.0, 1.0)
        _DepthThresh ("Depth Threshold", Range(0.0, 0.5)) = 0.03
        _NormThresh  ("Normal Threshold",Range(0.0, 1.0)) = 0.25
        _Thickness   ("Thickness (px)",  Range(0.5, 4.0)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "PhaseOutlineEdge"

            HLSLPROGRAM
            #pragma vertex   vert_blit
            #pragma fragment frag_edge
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _DepthThresh;
                float  _NormThresh;
                float  _Thickness;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert_blit(Attributes IN)
            {
                Varyings OUT;
                // Full-screen triangle
                OUT.posCS = float4(IN.posOS.xy, 0.0, 1.0);
                OUT.uv    = IN.uv;
                // Flip UV on non-metal platforms
                #if UNITY_UV_STARTS_AT_TOP
                    OUT.uv.y = 1.0 - OUT.uv.y;
                #endif
                return OUT;
            }

            // 이웃 픽셀 깊이·법선 샘플 → 차이가 크면 엣지
            float CalcEdge(float2 uv)
            {
                float2 texel = _Thickness / _ScreenParams.xy;

                // 4방향 오프셋
                float2 o0 = float2( texel.x,  0);
                float2 o1 = float2(-texel.x,  0);
                float2 o2 = float2( 0,  texel.y);
                float2 o3 = float2( 0, -texel.y);

                // 깊이 차이
                float d  = SampleSceneDepth(uv);
                float d0 = SampleSceneDepth(uv + o0);
                float d1 = SampleSceneDepth(uv + o1);
                float d2 = SampleSceneDepth(uv + o2);
                float d3 = SampleSceneDepth(uv + o3);

                float depthEdge = max(max(abs(d - d0), abs(d - d1)),
                                      max(abs(d - d2), abs(d - d3)));

                // 법선 차이
                float3 n  = SampleSceneNormals(uv);
                float3 n0 = SampleSceneNormals(uv + o0);
                float3 n1 = SampleSceneNormals(uv + o1);
                float3 n2 = SampleSceneNormals(uv + o2);
                float3 n3 = SampleSceneNormals(uv + o3);

                float normEdge = max(max(distance(n, n0), distance(n, n1)),
                                     max(distance(n, n2), distance(n, n3)));

                float edge = step(_DepthThresh, depthEdge)
                           + step(_NormThresh,  normEdge);
                return saturate(edge);
            }

            half4 frag_edge(Varyings IN) : SV_Target
            {
                half4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float edge = CalcEdge(IN.uv);
                // 엣지 위에 아웃라인 색상을 알파 블렌드
                return lerp(src, half4(_OutlineColor.rgb, 1.0), edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
