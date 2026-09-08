Shader "Hidden/SnapshotShaders2/Sharpen"
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

                float mask = SampleMask(i.texcoord);

                if(mask > 0.5f)
                {
                    col += 4.0f * col * _Strength;

                    float2 s = _BlitTexture_TexelSize.xy;
				    col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(0,	   -s.y)).rgb * _Strength;
				    col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(-s.x,    0)).rgb * _Strength;
				    col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(s.x,     0)).rgb * _Strength;
				    col -= SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord + float2(0,     s.y)).rgb * _Strength;
                }

                return float4(col, 1.0f);
            }
            ENDHLSL
        }
    }
}
