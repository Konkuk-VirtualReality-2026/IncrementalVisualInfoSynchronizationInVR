Shader "Hidden/SnapshotShaders2/Kaleidoscope"
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
			float _Segments;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 originalCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float2 shiftUV = _ScreenParams.xy * (i.texcoord - 0.5f);

				float radius = sqrt(dot(shiftUV, shiftUV));
				float angle = atan2(shiftUV.y, shiftUV.x);

				float segmentAngle = PI * 2.0f / _Segments;
				angle -= segmentAngle * floor(angle / segmentAngle);
				angle = min(angle, segmentAngle - angle);

				float2 uv = float2(cos(angle), sin(angle)) * radius + _ScreenParams.xy / 2.0f;
				uv = max(min(uv, _ScreenParams.xy * 2.0f - uv), -uv) / _ScreenParams.xy;

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float strength = SampleMask(i.texcoord);

                return lerp(originalCol, col, strength);
            }
            ENDHLSL
        }
    }
}
