Shader "Hidden/SnapshotShaders2/Retro"
{
	SubShader
    {
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

        Pass
        {
            Name "Game Boy"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            // Post process volume settings.
			float3 _GBDarkest;
			float3 _GBDark;
			float3 _GBLight;
			float3 _GBLightest;
			float _PowerRamp;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float luminance = pow(abs(Luminance(col.rgb)), _PowerRamp);

                float3 gbColor = _GBLightest;

                if(luminance < 0.25f)
                {
                    gbColor = _GBDarkest;
                }
                else if(luminance < 0.5f)
                {
                    gbColor = _GBDark;
                }
                else if(luminance < 0.75f)
                {
                    gbColor = _GBLight;
                }

                float strength = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, gbColor, strength);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "SNES"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            // Post process volume settings.
			int _RedLevels;
			int _GreenLevels;
			int _BlueLevels;
			float _PowerRamp;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float3 posterizedCol = pow(abs(col.rgb), _PowerRamp);

                float r = floor(lerp(0.0f, 1.0f - EPSILON, posterizedCol.r) * _RedLevels);
                float g = floor(lerp(0.0f, 1.0f - EPSILON, posterizedCol.g) * _GreenLevels);
                float b = floor(lerp(0.0f, 1.0f - EPSILON, posterizedCol.b) * _BlueLevels);
                posterizedCol = float3(r / _RedLevels, g / _GreenLevels, b / _BlueLevels);

                float strength = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, posterizedCol, strength);
                return col;
            }
            ENDHLSL
        }
    }
}
