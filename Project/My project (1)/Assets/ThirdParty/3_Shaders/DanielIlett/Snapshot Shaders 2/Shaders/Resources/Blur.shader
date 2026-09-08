Shader "Hidden/SnapshotShaders2/Blur"
{
	SubShader
    {
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            #define E 2.71828f

            // Post process volume settings.
			uint _KernelSize;
		    float _Spread;
		    uint _BlurStepSize;

            float gaussian(int x) 
		    {
			    float sigmaSqu = _Spread * _Spread;
			    return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
		    }
        ENDHLSL

        Pass
        {
            Name "Gaussian Horizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float kernelSum = 0.0f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                for (int x = lower; x <= upper; x += _BlurStepSize)
				{
                    uv = i.texcoord + float2(_BlitTexture_TexelSize.x * x, 0.0f);
                    float mask = saturate(SampleMask(uv));

					float gauss = gaussian(x);
					col += gauss * float4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb, mask);
                    kernelSum += gauss;
				}

                col /= max(kernelSum, 0.001f);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D(_HorizontalTexture);

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float kernelSum = 0.0f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                float middleMask = SampleMask(i.texcoord);

                for (int y = lower; y <= upper; y += _BlurStepSize)
				{
					uv = i.texcoord + float2(0.0f, _BlitTexture_TexelSize.y * y);

                    float gauss = gaussian(y);
                    col += gauss * SAMPLE_TEXTURE2D_X(_HorizontalTexture, sampler_LinearClamp, uv);
                    kernelSum += gauss;
				}

                col /= max(kernelSum, 0.001f);
                col.a = saturate(col.a / gaussian(0));

                float3 originalCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
                float3 newCol = lerp(col.rgb, originalCol, 1.0f - col.a);

                return float4(newCol.rgb, 1.0f);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Box Horizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float kernelSum = 0.0f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                for (int x = lower; x <= upper; x += _BlurStepSize)
				{
                    uv = i.texcoord + float2(_BlitTexture_TexelSize.x * x, 0.0f);
                    float mask = saturate(SampleMask(uv));

					col += float4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb, mask);
                    kernelSum++;
				}

                col /= max(kernelSum, 0.001f);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Box Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D(_HorizontalTexture);

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float kernelSum = 0.0f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                for (int y = lower; y <= upper; y += _BlurStepSize)
				{
					uv = i.texcoord + float2(0.0f, _BlitTexture_TexelSize.y * y);

                    col += SAMPLE_TEXTURE2D_X(_HorizontalTexture, sampler_LinearClamp, uv);
                    kernelSum++;
				}

                col /= max(kernelSum, 0.001f);
                col.a = saturate(col.a / 0.0625f);

                float3 originalCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
                float3 newCol = lerp(col.rgb, originalCol, 1.0f - col.a);

                return float4(newCol.rgb, 1.0f);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Radial"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 originalCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float3 col = 0.0f;
                float kernelSum = 0.0f;

                float2 offset = i.texcoord - 0.5f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                float middleMask = SampleMask(i.texcoord);

                for (int x = lower; x <= upper; ++x)
				{
					uv = i.texcoord + offset * x * (_BlurStepSize * 0.002f);
                    float mask = saturate(SampleMask(uv) + middleMask);

					float gauss = gaussian(x) * mask;
                    kernelSum += gauss;

					col += gauss * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
				}

                col /= max(kernelSum, 0.001f);
                col = lerp(originalCol.rgb, col, kernelSum);

                return float4(col, 1.0f);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Light Streaks Threshold"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float _LuminanceThreshold;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                col *= step(_LuminanceThreshold, Luminance(col)) * SampleMask(i.texcoord);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Light Streaks Blur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float kernelSum = 0.0f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                for (int x = lower; x <= upper; x += _BlurStepSize)
				{
                    uv = i.texcoord + float2(_BlitTexture_TexelSize.x * x, 0.0f);

					float gauss = gaussian(x);
					col += gauss * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                    kernelSum += gauss;
				}

                col /= max(kernelSum, 0.001f);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Light Streaks Composite"

            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            //TEXTURE2D(_LightStreaksTexture);

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                //col += SAMPLE_TEXTURE2D_X(_LightStreaksTexture, sampler_LinearClamp, i.texcoord);

                return col;
            }
            ENDHLSL
        }
    }
}
