Shader "Hidden/SnapshotShaders2/Greyscale"
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
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            // Post process volume settings.
			float _Strength;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float strength = _Strength * SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, Luminance(col.rgb), strength);
                return col;
            }
            ENDHLSL
        }
    }
}
