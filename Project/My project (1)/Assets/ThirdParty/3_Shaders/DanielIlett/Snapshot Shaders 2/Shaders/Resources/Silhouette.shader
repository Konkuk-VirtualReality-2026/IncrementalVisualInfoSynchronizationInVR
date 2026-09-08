Shader "Hidden/SnapshotShaders2/Silhouette"
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
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            #pragma multi_compile_fragment _ _USE_TEXTURE_RAMP

            // Post process volume settings.
            TEXTURE2D(_RampTexture);
			float3 _NearColor;
			float3 _FarColor;
			float _PowerRamp;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(i.texcoord);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
#endif
				depth = pow(abs(Linear01Depth(depth, _ZBufferParams)), _PowerRamp);

#if _USE_TEXTURE_RAMP
                float3 silhouetteColor = SAMPLE_TEXTURE2D(_RampTexture, sampler_LinearClamp, float2(depth, 0.5f)).rgb;
#else
                float3 silhouetteColor = lerp(_NearColor, _FarColor, depth);
#endif

                float strength = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, silhouetteColor, strength);
                return col;
            }
            ENDHLSL
        }
    }
}
