Shader "Hidden/SnapshotShaders2/Fog"
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

            #pragma multi_compile_local_fragment _ _USE_SKYBOX_FOG

            // Post process volume settings.
			float _DistanceStrength;
            float _StartDistance;
            float4 _StartDistanceColor;
            float _FullStrengthDistance;
            float4 _FullStrengthDistanceColor;
            float _DistanceFalloff;

            float _HeightStrength;
            float _StartHeight;
            float4 _StartHeightColor;
            float _FullStrengthHeight;
            float4 _FullStrengthHeightColor;
            float _FullStrengthHeightAtSkybox;
            float _HeightFalloff;

            float LinearEyeDepthToOutputDepth(float z)
            {
                return (1 - _ZBufferParams.w * z) / (_ZBufferParams.z * z);
            }

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

#if UNITY_REVERSED_Z
				float rawDepth = SampleSceneDepth(i.texcoord);
                float maxDepth = 0.0f;
#else
				float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(i.texcoord));
                float maxDepth = 1.0f;
#endif

				float3 worldPos = ComputeWorldSpacePosition(i.texcoord, rawDepth, UNITY_MATRIX_I_VP);

                float worldDistance = distance(worldPos, _WorldSpaceCameraPos);

                float distanceMask = saturate((worldDistance - _StartDistance) / (_FullStrengthDistance - _StartDistance));
				distanceMask = pow(distanceMask, _DistanceFalloff) * _DistanceStrength;

                // Modify the full strength height based on distance from full-strength distance and far depth.
                // This lets us blend between the two kinds of fog more pleasingly.
                float minDepth = LinearEyeDepthToOutputDepth(_FullStrengthDistance);
                float heightDepthModifier = saturate((rawDepth - minDepth) / (maxDepth - minDepth));
                float fullStrengthHeight = lerp(_FullStrengthHeight, _FullStrengthHeightAtSkybox, heightDepthModifier);
                float heightMask = saturate((worldPos.y - _StartHeight) / (fullStrengthHeight - _StartHeight));
				heightMask = pow(heightMask, _HeightFalloff) * _HeightStrength;

                float4 fogDistanceColor = lerp(_StartDistanceColor, _FullStrengthDistanceColor, distanceMask);
				float4 fogHeightColor = lerp(_StartHeightColor, _FullStrengthHeightColor, heightMask);

#if !_USE_SKYBOX_FOG
                if (rawDepth < EPSILON)
				{
					distanceMask = 0.0f;
					heightMask = 0.0f;
				}
#endif

                float3 fogColor = lerp(col.rgb, fogDistanceColor, distanceMask * fogDistanceColor.a);
				fogColor = lerp(fogColor, fogHeightColor, heightMask * fogHeightColor.a);

                float mask = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, fogColor, mask);
                return col;
            }
            ENDHLSL
        }
    }
}
