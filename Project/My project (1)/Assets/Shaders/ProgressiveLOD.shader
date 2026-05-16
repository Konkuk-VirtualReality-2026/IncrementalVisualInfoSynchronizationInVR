Shader "VR/AdaptationProgressive"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [Header(Interaction)]
        _RimColor("Rim Color (XRI Support)", Color) = (0.0, 0.5, 1.0, 1)
        _RimPower("Rim Power (XRI Support)", Range(0.5, 8.0)) = 2.0
        
        [Header(Testing)]
        _VisualFidelity("Manual Fidelity Override", Range(0.0, 1.0)) = 0.0
        [Toggle(USE_MANUAL_FIDELITY)] _UseManualFidelity("Use Manual Override", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

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
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _RimColor;
                float _RimPower;
                float _Metallic;
                float _Smoothness;
                float _VisualFidelity;
            CBUFFER_END

            float _GlobalVisualFidelity;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                float3 worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
                float3 worldBitangent = cross(output.normalWS, worldTangent) * input.tangentOS.w;
                output.tangentWS = float4(worldTangent, input.tangentOS.w);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float fidelity = _GlobalVisualFidelity;
                #if defined(USE_MANUAL_FIDELITY)
                    fidelity = _VisualFidelity;
                #endif
                
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                
                // 1. SILHOUETTE (Phase 1 Focus)
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float3 rim = _RimColor.rgb * pow(fresnel, _RimPower);

                // 2. TEXTURE & PBR (Phase 2 & 3)
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = tex.rgb * _BaseColor.rgb;
                
                // Normal Mapping for Phase 3
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                float3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(input.tangentWS.xyz, bitangentWS, normalWS);
                float3 finalNormalWS = lerp(normalWS, normalize(mul(normalTS, tbn)), fidelity);

                // Lighting
                Light mainLight = GetMainLight();
                
                // Diffuse
                float3 diffuse = albedo * saturate(dot(finalNormalWS, mainLight.direction)) * mainLight.color;
                
                // Specular (Roughness based)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float spec = pow(saturate(dot(finalNormalWS, halfDir)), _Smoothness * 128.0);
                float3 specular = mainLight.color * spec * _Metallic;
                
                float3 ambient = SampleSH(finalNormalWS) * albedo;
                float3 fullPBR = diffuse + specular + ambient;

                // --- TRANSITION LOGIC ---
                float3 finalColor;
                
                if (fidelity < 0.3) 
                {
                    // Phase 1 (0.0 -> 0.3): Smooth fading in silhouettes
                    float t = fidelity / 0.3;
                    finalColor = lerp(float3(0.01, 0.01, 0.02), rim, t);
                }
                else if (fidelity < 0.7) 
                {
                    // Phase 2 (0.3 -> 0.7): Fade Silhouettes into Unlit Textures
                    float t = (fidelity - 0.3) / 0.4;
                    finalColor = lerp(rim, albedo, t);
                }
                else 
                {
                    // Phase 3 (0.7 -> 1.0): Fade Unlit into Full PBR (Normal maps, reflections)
                    float t = (fidelity - 0.7) / 0.3;
                    finalColor = lerp(albedo, fullPBR, t);
                }

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "None"
}
