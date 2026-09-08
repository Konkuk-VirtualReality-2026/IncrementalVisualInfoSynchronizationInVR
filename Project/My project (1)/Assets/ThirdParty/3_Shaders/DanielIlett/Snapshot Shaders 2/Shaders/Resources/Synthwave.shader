Shader "Hidden/SnapshotShaders2/Synthwave"
{
	SubShader
    {
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            #pragma multi_compile_local_fragment _ _USE_SCENE_TEXTURE

            // Post process volume settings.
			float4 _BackgroundColor;
			float4 _LineColor1;
			float4 _LineColor2;
			float _LineColorMix;
			float _LineWidth;
			float _LineSoftness;
			float3 _GapWidth;
			float3 _LineOffset;
			float _StartFadeoutDistance;
			float _EndFadeoutDistance;
			float3 _AxisMask;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

#if UNITY_REVERSED_Z
				float rawDepth = SampleSceneDepth(i.texcoord);
				float skyboxCheck = rawDepth;
#else
				float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
				float skyboxCheck = 1.0f - rawDepth;
#endif

				float4 pixelPositionCS = float4(i.texcoord * 2.0f - 1.0f, rawDepth, 1.0f);

#if UNITY_UV_STARTS_AT_TOP
				pixelPositionCS.y = -pixelPositionCS.y;
#endif
				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, rawDepth, UNITY_MATRIX_I_VP) + _LineOffset;

				float3 modWorldPos = fmod(abs(worldPos) + _GapWidth / 2.0f, _GapWidth);

				float3 distWorldPos = abs((_GapWidth / 2.0f) - modWorldPos);

				float3 stepWorldPos = 1.0f - smoothstep(_LineWidth, _LineWidth + _LineSoftness, distWorldPos);
				stepWorldPos *= _AxisMask;

				float lines = saturate(dot(float3(1.0f, 1.0f, 1.0f), stepWorldPos));
				lines *= smoothstep(_EndFadeoutDistance, _StartFadeoutDistance, LinearEyeDepth(rawDepth, _ZBufferParams));

				// Fix for weird issues with the skybox.
				if (skyboxCheck < EPSILON)
				{
					lines = 0.0f;
				}

#if _USE_SCENE_TEXTURE
				float4 background = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
#else
				float4 background = _BackgroundColor;
#endif
				float4 lineColor = lerp(_LineColor1, _LineColor2, pow(i.texcoord.y, _LineColorMix));
				float4 synthwaveCol = lerp(background, lineColor, lines);

                float mask = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, synthwaveCol.rgb, mask);
                return col;
            }
            ENDHLSL
        }
    }
}
