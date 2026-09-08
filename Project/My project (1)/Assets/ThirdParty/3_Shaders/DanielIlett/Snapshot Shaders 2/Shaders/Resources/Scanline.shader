Shader "Hidden/SnapshotShaders2/Scanline"
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

            #pragma multi_compile_local_fragment _ _USE_POINT_FILTERING

            // Post process volume settings.
            TEXTURE2D(_ScanlineTexture);
			float _ScanlineStrength;
            float _ScanlineSize;
            float _ScanlineScrollSpeed;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float2 scanlineUV = i.texcoord * _ScreenParams.xy / _ScanlineSize;
                scanlineUV.y += _Time.y * _ScanlineScrollSpeed;

#if _USE_POINT_FILTERING
                float3 scanline = SAMPLE_TEXTURE2D(_ScanlineTexture, sampler_PointRepeat, scanlineUV).rgb;
#else
                float3 scanline = SAMPLE_TEXTURE2D(_ScanlineTexture, sampler_LinearRepeat, scanlineUV).rgb;
#endif

                float strength = SampleMask(i.texcoord) * _ScanlineStrength;

				col.rgb = lerp(col.rgb, col.rgb * scanline, strength);
                return col;
            }
            ENDHLSL
        }
    }
}
