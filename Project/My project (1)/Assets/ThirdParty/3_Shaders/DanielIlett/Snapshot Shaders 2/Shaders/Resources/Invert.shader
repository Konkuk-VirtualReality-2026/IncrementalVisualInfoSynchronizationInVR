Shader "Hidden/SnapshotShaders2/Invert"
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

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;

                float strength = SampleMask(i.texcoord) * _Strength;

                float channelMax = max(max(col.r, max(col.g, col.b)), 1.0f);
                float3 inverted = channelMax - col;

				col = lerp(col, inverted, strength * 0.99f);
                return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
