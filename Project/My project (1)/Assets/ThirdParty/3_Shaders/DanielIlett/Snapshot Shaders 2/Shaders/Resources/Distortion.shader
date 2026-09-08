Shader "Hidden/SnapshotShaders2/Distortion"
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
			float4 _BackgroundColor;
			float _Strength;
            float _Smoothing;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float2 UVs = i.texcoord - 0.5f;
				UVs = UVs * (1 + _Strength * length(UVs) * length(UVs)) + 0.5f;

                float4 distortedCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UVs);
                //distortedCol = (UVs.x >= 0.0f && UVs.x <= 1.0f && UVs.y >= 0.0f && UVs.y <= 1.0f) ? distortedCol : _BackgroundColor.rgb;

                float2 smoothedEdges = smoothstep(0.0f, _Smoothing, UVs.xy);
				smoothedEdges *= (1.0f - smoothstep(1.0f - _Smoothing, 1.0f, UVs.xy));

                distortedCol.rgb = lerp(_BackgroundColor.rgb, distortedCol.rgb, min(smoothedEdges.x, smoothedEdges.y) + (1.0f - _BackgroundColor.a));

                float strength = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, distortedCol.rgb, strength);
                return col;
            }
            ENDHLSL
        }
    }
}
