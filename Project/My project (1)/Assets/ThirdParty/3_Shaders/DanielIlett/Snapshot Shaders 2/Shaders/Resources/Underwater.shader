Shader "Hidden/SnapshotShaders2/Underwater"
{
	SubShader
    {
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE
			#pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            #include_with_pragmas "SnapshotHelper.hlsl"

            // Post process volume settings.
            float _WaveStrength;
            TEXTURE2D(_WaveFlowMap);
            float _WaveFlowTiling;
            float _WaveFlowSpeed;
		ENDHLSL

		Pass
        {
			Name "No Caustics"

            HLSLPROGRAM

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float2 timeUV = ((i.texcoord * _WaveFlowTiling) + (_Time.y * _WaveFlowSpeed)) % 1.0f;
				float4 flowMap = SAMPLE_TEXTURE2D(_WaveFlowMap, sampler_LinearRepeat, timeUV);
				float2 flowUV = flowMap.rg * 2.0f - 1.0f;

				float2 waveUV = i.texcoord + (1.0f / _ScreenParams.xy) * flowUV * _WaveStrength;
				float4 underwaterCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, waveUV);

                float mask = SampleMask(waveUV);

				return lerp(baseCol, underwaterCol, mask);
            }
            ENDHLSL
        }

        Pass
        {
			Name "Triplanar Mapped"

            HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

			#pragma multi_compile_local_fragment _ _USE_CAUSTICS_COLOR_SEPARATION

			TEXTURE2D(_CausticsTexture);
            float4 _CausticsTint;
            float2 _CausticsTiling;
            float3 _CausticsScrollVelocity1;
            float3 _CausticsScrollVelocity2;
            float _CausticsStartFade;
            float _CausticsEndFade;
            float _CausticsColorSeparation;

            // Based on https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/:
			float3 triplanarSample(Texture2D tex, SamplerState texSampler, float3 uv, float3 normals, float blend)
			{
				float2 uvX = uv.zy;
				float2 uvY = uv.xz;
				float2 uvZ = uv.xy;

				if (normals.x < 0)
				{
					uvX.x = -uvX.x;
				}

				if (normals.y < 0)
				{
					uvY.x = -uvY.x;
				}

				if (normals.z >= 0)
				{
					uvZ.x = -uvZ.x;
				}

				float3 colX = SAMPLE_TEXTURE2D(tex, texSampler, uvX).rgb;
				float3 colY = SAMPLE_TEXTURE2D(tex, texSampler, uvY).rgb;
				float3 colZ = SAMPLE_TEXTURE2D(tex, texSampler, uvZ).rgb;

				float3 blending = pow(abs(normals), blend);
				blending /= dot(blending, 1.0f);

				return (colX * blending.x + colY * blending.y + colZ * blending.z);
			}

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float2 timeUV = ((i.texcoord * _WaveFlowTiling) + (_Time.y * _WaveFlowSpeed)) % 1.0f;
				float4 flowMap = SAMPLE_TEXTURE2D(_WaveFlowMap, sampler_LinearRepeat, timeUV);
				float2 flowUV = flowMap.rg * 2.0f - 1.0f;

				float2 waveUV = i.texcoord + (1.0f / _ScreenParams.xy) * flowUV * _WaveStrength;
				float4 underwaterCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, waveUV);

#if UNITY_REVERSED_Z
				float rawDepth = SampleSceneDepth(waveUV);
				float skyboxCheck = rawDepth;
#else
				float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(waveUV));
				float skyboxCheck = 1.0f - rawDepth;
#endif
                
				float3 worldPos = ComputeWorldSpacePosition(waveUV, rawDepth, UNITY_MATRIX_I_VP);// + _Offset;
				float3 worldNormal = normalize(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_LinearClamp, waveUV).rgb);

#if _USE_CAUSTICS_COLOR_SEPARATION
				float caustics1R = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y - _CausticsColorSeparation) * _CausticsTiling.x, worldNormal, 1.0f).r;
				float caustics1G = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y) * _CausticsTiling.x, worldNormal, 1.0f).g;
				float caustics1B = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y + _CausticsColorSeparation) * _CausticsTiling.x, worldNormal, 1.0f).b;

				float caustics2R = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y - _CausticsColorSeparation) * _CausticsTiling.y, worldNormal, 1.0f).r;
				float caustics2G = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y) * _CausticsTiling.y, worldNormal, 1.0f).g;
				float caustics2B = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y + _CausticsColorSeparation) * _CausticsTiling.y, worldNormal, 1.0f).b;

				float3 caustics1 = float3(caustics1R, caustics1G, caustics1B);
				float3 caustics2 = float3(caustics2R, caustics2G, caustics2B);
#else
				float3 caustics1 = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y) * _CausticsTiling.x, worldNormal, 1.0f).rgb;
				float3 caustics2 = triplanarSample(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y) * _CausticsTiling.y, worldNormal, 1.0f).rgb;
#endif

				float3 caustics = min(caustics1, caustics2) * _CausticsTint.rgb * _CausticsTint.a;

                // Fix for weird issues with the skybox.
				if (skyboxCheck < EPSILON)
				{
					caustics = 0.0f;
				}

				float causticStrength = smoothstep(_CausticsStartFade, _CausticsEndFade, LinearEyeDepth(rawDepth, _ZBufferParams));
				underwaterCol.rgb += lerp(caustics, 0.0f, causticStrength);

                float mask = SampleMask(waveUV);

				return lerp(baseCol, underwaterCol, mask);
            }
            ENDHLSL
        }

		Pass
        {
			Name "Light Aligned"

            HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			#pragma multi_compile_local_fragment _ _USE_CAUSTICS_COLOR_SEPARATION

			half4x4 _MainLightMatrix;
			TEXTURE2D(_CausticsTexture);
            float4 _CausticsTint;
            float2 _CausticsTiling;
            float3 _CausticsScrollVelocity1;
            float3 _CausticsScrollVelocity2;
            float _CausticsStartFade;
            float _CausticsEndFade;
            float _CausticsColorSeparation;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float2 timeUV = ((i.texcoord * _WaveFlowTiling) + (_Time.y * _WaveFlowSpeed)) % 1.0f;
				float4 flowMap = SAMPLE_TEXTURE2D(_WaveFlowMap, sampler_LinearRepeat, timeUV);
				float2 flowUV = flowMap.rg * 2.0f - 1.0f;

				float2 waveUV = i.texcoord + (1.0f / _ScreenParams.xy) * flowUV * _WaveStrength;
				float4 underwaterCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, waveUV);

#if UNITY_REVERSED_Z
				float rawDepth = SampleSceneDepth(waveUV);
				float skyboxCheck = rawDepth;
#else
				float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(waveUV));
				float skyboxCheck = 1.0f - rawDepth;
#endif
                
				float3 worldPos = ComputeWorldSpacePosition(waveUV, rawDepth, UNITY_MATRIX_I_VP);// + _Offset;
				worldPos = mul((half3x3)_MainLightMatrix, worldPos).xyz;

#if _USE_CAUSTICS_COLOR_SEPARATION
				float caustics1R = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y - _CausticsColorSeparation).xy * _CausticsTiling.x).r;
				float caustics1G = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y).xy * _CausticsTiling.x).g;
				float caustics1B = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y + _CausticsColorSeparation).xy * _CausticsTiling.x).b;

				float caustics2R = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y - _CausticsColorSeparation).xy * _CausticsTiling.y).r;
				float caustics2G = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y).xy * _CausticsTiling.y).g;
				float caustics2B = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y + _CausticsColorSeparation).xy * _CausticsTiling.y).b;

				float3 caustics1 = float3(caustics1R, caustics1G, caustics1B);
				float3 caustics2 = float3(caustics2R, caustics2G, caustics2B);
#else
				float3 caustics1 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity1 * _Time.y).xy * _CausticsTiling.x).rgb;
				float3 caustics2 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_LinearRepeat, (worldPos + _CausticsScrollVelocity2 * _Time.y).xy * _CausticsTiling.y).rgb;
#endif

				float3 caustics = min(caustics1, caustics2) * _CausticsTint.rgb * _CausticsTint.a;

                // Fix for weird issues with the skybox.
				if (skyboxCheck < EPSILON)
				{
					caustics = 0.0f;
				}

				float causticStrength = smoothstep(_CausticsStartFade, _CausticsEndFade, LinearEyeDepth(rawDepth, _ZBufferParams));
				underwaterCol.rgb += lerp(caustics, 0.0f, causticStrength);

                float mask = SampleMask(waveUV);

				return lerp(baseCol, underwaterCol, mask);
            }
            ENDHLSL
        }
    }
}
