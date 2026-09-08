Shader "Hidden/SnapshotShaders2/Dither"
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

            #pragma multi_compile_local_fragment _ _USE_SCENE_TEXTURE

            // Post process volume settings.
			TEXTURE2D(_NoiseTexture);
			float4 _NoiseTexture_TexelSize;
			float _NoiseSize;
			float _LuminanceThreshold;
			float4 _LightColor;
			float4 _DarkColor;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float luminance = Luminance(col);

                float2 noiseUV = i.texcoord * _NoiseTexture_TexelSize.xy * _ScreenParams.xy * 2.0f / _NoiseSize;
                float3 noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_LinearRepeat, noiseUV).rgb;
                float threshold = Luminance(noise) + _LuminanceThreshold;

#if _USE_SCENE_TEXTURE
				float3 ditherCol = luminance < threshold ? _DarkColor.rgb : col;
#else
				float3 ditherCol = luminance < threshold ? _DarkColor.rgb : _LightColor.rgb;
#endif
                float mask = SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, ditherCol, mask);
                return col;
            }
            ENDHLSL
        }
    }
}
