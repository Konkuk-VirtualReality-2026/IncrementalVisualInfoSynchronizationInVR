Shader "VR/AdaptationProgressive"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        [Header(Silhouette Outline Phase 2)]
        _OutlineColor("Outline Color", Color) = (0.2, 0.6, 1.0, 1)
        _OutlineWidth("Outline Width (m)", Range(0.0, 0.05)) = 0.012

        [Header(Testing)]
        _VisualFidelity("Manual Fidelity Override", Range(0.0, 1.0)) = 0.0
        [Toggle(USE_MANUAL_FIDELITY)] _UseManualFidelity("Use Manual Override", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        // ────────────────────────────────────────────────────────────────────
        // Pass 1 : Outline  (뒷면 법선 방향으로 확장 → 파란 테두리)
        // Phase 2(0.3~0.7)에서만 두께가 생기고, 그 외에는 두께=0(보이지 않음)
        // ────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert_ol
            #pragma fragment frag_ol
            #pragma shader_feature USE_MANUAL_FIDELITY
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _Metallic;
                float  _Smoothness;
                float  _VisualFidelity;
            CBUFFER_END

            float _GlobalVisualFidelity;

            struct Attr_OL { float4 posOS : POSITION; float3 normOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vary_OL { float4 posCS : SV_POSITION; float fidelity : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

            Vary_OL vert_ol(Attr_OL IN)
            {
                Vary_OL OUT = (Vary_OL)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float fid = _GlobalVisualFidelity;
                #if defined(USE_MANUAL_FIDELITY)
                    fid = _VisualFidelity;
                #endif
                OUT.fidelity = fid;

                // 인플레이션 아웃라인은 PhaseOutlineFeature(화면 공간 법선+깊이)로 대체.
                // back-face 팽창 방식은 특정 각도에만 보이는 문제가 있으므로 비활성화.
                float scale = 0.0;

                float3 normWS = normalize(TransformObjectToWorldNormal(IN.normOS));
                float3 posWS  = TransformObjectToWorld(IN.posOS.xyz);
                float4 clipPos = TransformWorldToHClip(posWS);

                // ── 클립 공간 법선 확장 ─────────────────────────────────────
                // 뷰 공간 법선의 XY를 화면 공간 방향으로 사용하면
                // 각도·거리에 무관하게 균일한 두께의 테두리를 얻을 수 있다.
                float3 normVS  = normalize(mul((float3x3)UNITY_MATRIX_V, normWS));
                float2 normDir = normalize(normVS.xy + float2(0.0001, 0.0001));
                // 화면 비율 보정 (가로로 눌리지 않도록)
                normDir.x *= _ScreenParams.y / _ScreenParams.x;
                normDir    = normalize(normDir);
                // clipPos.w 를 곱해 원근 보정 → NDC 상 고정 두께
                clipPos.xy += normDir * (_OutlineWidth * scale) * clipPos.w;

                OUT.posCS = clipPos;
                return OUT;
            }

            half4 frag_ol(Vary_OL IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        // ────────────────────────────────────────────────────────────────────
        // Pass 2 : Main Surface
        // Phase 1 → 검정
        // Phase 2 → 어두운 단색 (표면은 검정, 테두리는 Pass1이 담당)
        // Phase 3 → 알베도 → 풀 PBR
        // ────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "AdaptationForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_MANUAL_FIDELITY
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON STEREO_CUBEMAP_RENDER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _Metallic;
                float  _Smoothness;
                float  _VisualFidelity;
            CBUFFER_END

            float _GlobalVisualFidelity;

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                float3 worldTan = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.tangentWS   = float4(worldTan, IN.tangentOS.w);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float fidelity = _GlobalVisualFidelity;
                #if defined(USE_MANUAL_FIDELITY)
                    fidelity = _VisualFidelity;
                #endif

                // ── 텍스처 & PBR 계산 (Phase 3용) ───────────────────────────
                half4 tex    = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = tex.rgb * _BaseColor.rgb;

                float3 normalWS   = normalize(IN.normalWS);
                float3 normalTS   = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv));
                float3 bitangentWS = cross(normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 tbn      = float3x3(IN.tangentWS.xyz, bitangentWS, normalWS);
                float3 finalNorm  = lerp(normalWS, normalize(mul(normalTS, tbn)), saturate((fidelity - 0.7) / 0.3));

                float3 viewDir  = normalize(GetCameraPositionWS() - IN.positionWS);
                Light  light    = GetMainLight();

                float3 diffuse  = albedo * saturate(dot(finalNorm, light.direction)) * light.color;
                float3 halfDir  = normalize(light.direction + viewDir);
                float  spec     = pow(saturate(dot(finalNorm, halfDir)), _Smoothness * 128.0);
                float3 specular = light.color * spec * _Metallic;
                float3 ambient  = SampleSH(finalNorm) * albedo;
                float3 fullPBR  = diffuse + specular + ambient;

                // ── 페이즈 분기 ───────────────────────────────────────────────
                float3 finalColor;

                if (fidelity < 0.3)
                {
                    // Phase 1 : 완전 암흑 (블랙아웃 캔버스가 덮지만, 셰이더도 검정 유지)
                    finalColor = float3(0.0, 0.0, 0.0);
                }
                else if (fidelity < 0.7)
                {
                    // Phase 2 : 완전 검정 — 엣지는 PhaseOutlineFeature(화면 공간)가 담당
                    finalColor = float3(0.0, 0.0, 0.0);
                }
                else
                {
                    // Phase 3 : 알베도 → 풀 PBR
                    float t = (fidelity - 0.7) / 0.3;
                    finalColor = lerp(albedo, fullPBR, t);
                }

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ────────────────────────────────────────────────────────────────────
        // Pass 3 : DepthNormals
        // PhaseOutlineFeature 의 법선+깊이 엣지 검출에 필요.
        // 이 패스가 없으면 _CameraNormalsTexture 에 법선이 기록되지 않아
        // 평면 오브젝트의 엣지가 전혀 검출되지 않는다.
        // ────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert_dn
            #pragma fragment frag_dn
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _Metallic;
                float  _Smoothness;
                float  _VisualFidelity;
            CBUFFER_END

            struct Attr_DN
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Vary_DN
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Vary_DN vert_dn(Attr_DN IN)
            {
                Vary_DN OUT = (Vary_DN)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 frag_dn(Vary_DN IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return float4(normalize(IN.normalWS) * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "None"
}
