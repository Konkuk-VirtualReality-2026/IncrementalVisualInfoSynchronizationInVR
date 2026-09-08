Shader "Hidden/SnapshotShaders2/Thermal"
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
            Name "Draw Thermals"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #pragma multi_compile_local_fragment _ _RENDER_ONLY_MASK

            // Post process volume settings.
            float3 _Color0;
            float _Threshold0;
            float3 _Color1;
            float _Threshold1;
            float3 _Color2;
            float _Threshold2;
            float3 _Color3;
            float _Threshold3;
            float3 _Color4;
            float _Threshold4;
            float3 _Color5;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float4 outputCol = baseCol;

                float brightness = Luminance(outputCol.rgb);

                if(brightness < _Threshold0)
                {
                    outputCol = float4(_Color0, 1.0f);
                }
                else if(brightness < _Threshold1)
                {
                    outputCol = float4(_Color1, 1.0f);
                }
                else if(brightness < _Threshold2)
                {
                    outputCol = float4(_Color2, 1.0f);
                }
                else if(brightness < _Threshold3)
                {
                    outputCol = float4(_Color3, 1.0f);
                }
                else if(brightness < _Threshold4)
                {
                    outputCol = float4(_Color4, 1.0f);
                }
                else
                {
                    outputCol = float4(_Color5, 1.0f);
                }

#if _RENDER_ONLY_MASK
                outputCol = lerp(baseCol, outputCol, baseCol.a);
#endif

                return outputCol;
            }
            ENDHLSL
        }

        Pass
        {
            // Do the horizontal mask blur and save to a temp mask texture.
            Name "Mask Blur Horizontal"

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
            // Do the vertical blur pass and composite the blurred mask onto the color copy.
            Name "Mask Blur Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            // Post process volume settings.
            float _MaskBrightnessBoost;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float col = 0.0f;
                float kernelSum = 0.0f;

                int upper = ((_KernelSize - 1) / 2);
                int lower = -upper;

                float2 uv;

                for (int y = lower; y <= upper; y += _BlurStepSize)
				{
                    uv = i.texcoord + float2(0.0f, _BlitTexture_TexelSize.y * y);

					float gauss = gaussian(y);
					col += gauss * SampleMask(uv);
                    kernelSum += gauss;
				}

                col /= max(kernelSum, 0.001f);

                float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);
                float3 maskCol = float3(col, col, col) * _MaskBrightnessBoost;
                return float4(baseCol.rgb + maskCol, col);
            }
            ENDHLSL
        }
    }
}
