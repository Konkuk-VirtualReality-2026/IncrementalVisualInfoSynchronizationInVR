Shader "Hidden/SnapshotShaders2/Pixelate"
{
	SubShader
    {
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

        Pass
        {
            Name "Mask Combine"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord);

                /*
                float strength = 0.0f;

                for(int x = -_MaskExpansion; x <= _MaskExpansion; ++x)
                {
                    for(int y = -_MaskExpansion; y <= _MaskExpansion; ++y)
                    {
                        strength += SampleMask(i.texcoord + _BlitTexture_TexelSize.xy * float2(x, y));
                    }
                }
                */

                float strength = SampleMask(i.texcoord);

                return float4(col.rgb, saturate(strength));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Mask Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            #pragma multi_compile_local_fragment _ _USE_POINT_FILTER

            int _MaskExpansion;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#if _USE_POINT_FILTER
                float4 pixelatedCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord);
#else
				float4 pixelatedCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
#endif

                float strength = 0.0f;

                for(int x = -_MaskExpansion; x <= _MaskExpansion; ++x)
                {
                    for(int y = -_MaskExpansion; y <= _MaskExpansion; ++y)
                    {
#if _USE_POINT_FILTER
                        strength += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord + _BlitTexture_TexelSize.xy * float2(x, y)).a;
#else
				        strength += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + _BlitTexture_TexelSize.xy * float2(x, y)).a;
#endif
                    }
                }

                if(strength < 0.9f)
                {
                    discard;
                }

                return float4(pixelatedCol.rgb, 1.0f);
            }
            ENDHLSL
        }
    }
}
