Shader "Hidden/SnapshotShaders2/Glitch"
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
            #include_with_pragmas "SnapshotHelper.hlsl"

            #pragma multi_compile_local_fragment _ _USE_SLICE_BANDS
            #pragma multi_compile_local_fragment _ _USE_BLOCK_ARTIFACTS
            #pragma multi_compile_local_fragment _ _USE_POINT_FILTER

            // Post process volume settings.
			float4 _TintColor;

            // Basic offset glitches.
            float _OffsetStrength;
            float _OffsetSpeed;
            float _OffsetTiling;

            // Slice band glitches.
            float _SliceBandSpeed;
            float _SliceBandJitter;
            float _SliceBandWidth;
            float _SliceBandMinDuration;
            float _SliceBandMaxDuration;
            float _SliceBandFrequency;
            float _SliceBandStrength;

            // Random block glitches.
            float2 _BlockArtifactTiling;
            float _BlockArtifactChance;
            float _BlockArtifactStrengthMin;
            float _BlockArtifactStrengthMax;
            float _BlockArtifactSpeed;

            TEXTURE2D(_OffsetTexture);

            float rand(float2 pos)
			{
				return frac(sin(dot(pos, float2(12.9898f, 78.233f))) * 43758.5453123f);
			}

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float mask = SampleMask(i.texcoord);

                if(mask < 0.5f)
                {
                    discard;
                }

                float2 uv = i.texcoord;
                
                float2 offsetUV = uv;

#if _USE_POINT_FILTER
                float offsetAmount = SAMPLE_TEXTURE2D(_OffsetTexture, sampler_PointRepeat, uv * float2(1.0f, _OffsetTiling) + float2(0.0f, (_Time.y * _OffsetSpeed) % 1.0f)).r;
#else
                float offsetAmount = SAMPLE_TEXTURE2D(_OffsetTexture, sampler_LinearRepeat, uv * float2(1.0f, _OffsetTiling) + float2(0.0f, (_Time.y * _OffsetSpeed) % 1.0f)).r;
#endif
                
                offsetUV.x += (offsetAmount - 0.5f) * _OffsetStrength;

#if _USE_SLICE_BANDS
                float bandStartPos = _Time.y * _SliceBandSpeed;
                float randomJitter = rand(float2(bandStartPos, 0.5f)) * _SliceBandJitter;

                float bandScroll = abs(sin((bandStartPos + uv.y + randomJitter) * PI));

                float band = (1.0f - step(_SliceBandWidth, bandScroll)) * (step(0.0f, bandScroll));

                float sliceID = floor(_Time.y * _SliceBandFrequency);

                float randomDuration = rand(sliceID) * (_SliceBandMaxDuration - _SliceBandMinDuration) + _SliceBandMinDuration;

                float bandTimer = step(1.0f - randomDuration * _SliceBandFrequency, (_Time.y * _SliceBandFrequency) % 1.0f);

                float maskedSliceBand = band * bandTimer;
                offsetUV.x += maskedSliceBand * _SliceBandStrength;
#endif

#if _USE_BLOCK_ARTIFACTS
                float2 uvBlockID = floor(offsetUV * _BlockArtifactTiling);
                float randBlockID = rand(uvBlockID);

                float randomBlock = (randBlockID + _Time.y * _BlockArtifactSpeed) % 1.0f;
                float maskedBlocks = step((1.0f - _BlockArtifactChance), randomBlock);

                offsetUV.x += maskedBlocks * lerp(_BlockArtifactStrengthMin, _BlockArtifactStrengthMax, randBlockID);
#endif

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearRepeat, offsetUV);

                float strength = SampleMask(i.texcoord);

                return col;
            }
            ENDHLSL
        }
    }
}
