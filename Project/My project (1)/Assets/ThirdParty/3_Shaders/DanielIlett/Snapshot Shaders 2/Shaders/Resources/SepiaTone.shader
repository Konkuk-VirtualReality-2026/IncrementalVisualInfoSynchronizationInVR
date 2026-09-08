Shader "Hidden/SnapshotShaders2/SepiaTone"
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

            // Post process volume settings.
			float _Strength;

            // Color transformation from regular RGB color to sepia-toned color.
            static const float3x3 sepiaTransform = float3x3
			(
				0.393f, 0.769f, 0.189f,	// Weights for red output.
				0.349f, 0.686f, 0.168f,	// Weights for green output.
				0.272f, 0.534f, 0.131f	// Weights for blue output.
			);

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float strength = _Strength * SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, mul(sepiaTransform, col.rgb), strength);
                return col;
            }
            ENDHLSL
        }
    }
}
