Shader "Hidden/SnapshotShaders2/Painting"
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

		ENDHLSL

		Pass
        {
			Name "Kuwahara Pre Pass"

            HLSLPROGRAM

            // Post process volume settings.
			uint _KernelSize;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 originalCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

				int upper = (_KernelSize - 1) / 2;
				int samples = (upper + 1) * (upper + 1);

				float3 sum = 0.0;
				float3 squareSum = 0.0;

				[loop]
				for (int x = 0; x <= upper; ++x)
				{
					[loop]
					for (int y = 0; y <= upper; ++y)
					{
						float2 offset = float2(_BlitTexture_TexelSize.x * x, _BlitTexture_TexelSize.y * y);
						float3 tex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + offset).rgb;
						sum += tex;
						squareSum += tex * tex;
					}
				}

				float3 mean = sum / samples;
				float3 variance = abs((squareSum / samples) - (mean * mean));

				return float4(mean, length(variance));
				
            }
            ENDHLSL
        }

        Pass
        {
			Name "Kuwahara Main Pass"

            HLSLPROGRAM

            // Post process volume settings.
			TEXTURE2D_X(_PrePassData);
			float4 _PrePassData_TexelSize;
			uint _KernelSize;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				float4 originalCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

				float strength = SampleMask(i.texcoord);

				if(strength < 0.5f)
				{
					discard;
				}

				int upper = (_KernelSize - 1) / 2;
				int lower = -upper;

				float2 offset = float2(_PrePassData_TexelSize.x * lower, _PrePassData_TexelSize.y * lower);
				float4 regionA = SAMPLE_TEXTURE2D_X(_PrePassData, sampler_LinearClamp, i.texcoord + offset);

				offset = float2(0, _PrePassData_TexelSize.y * lower);
				float4 regionB = SAMPLE_TEXTURE2D_X(_PrePassData, sampler_LinearClamp, i.texcoord + offset);

				offset = float2(_PrePassData_TexelSize.x * lower, 0);
				float4 regionC = SAMPLE_TEXTURE2D_X(_PrePassData, sampler_LinearClamp, i.texcoord + offset);

				offset = float2(0, 0);
				float4 regionD = SAMPLE_TEXTURE2D_X(_PrePassData, sampler_LinearClamp, i.texcoord + offset);

				float3 col = regionA.rgb;
				float minVar = regionA.a;

				if(regionB.a < minVar)
				{
					col = regionB.rgb;
					minVar = regionB.a;
				}

				if(regionC.a < minVar)
				{
					col = regionC.rgb;
					minVar = regionC.a;
				}

				if(regionD.a < minVar)
				{
					col = regionD.rgb;
				}
				
				return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
