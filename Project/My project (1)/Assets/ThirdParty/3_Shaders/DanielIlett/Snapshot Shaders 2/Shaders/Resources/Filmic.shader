Shader "Hidden/SnapshotShaders2/Filmic"
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

			#pragma multi_compile_local_fragment _ _USE_QUINTIC_INTERP
			#pragma multi_compile_local_fragment _ _USE_FILM_BARS

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include_with_pragmas "SnapshotHelper.hlsl"

            // Post process volume settings.
			float _AspectRatio;
			float3 _FilmBarColor;
            float _NoiseStrength;
			float _Speed;
			float _NoiseSize;

            // Generate time-sensitive random numbers between 0 and 1.
			float rand(float2 pos)
			{
				return frac(sin(dot(pos + (_Time.y % 1.0f) * _Speed, float2(12.9898f, 78.233f))) * 43758.5453123f);
			}

			// Generate a random vector on the unit circle.
			float2 randUnitCircle(float2 pos)
			{
				float randVal = rand(pos);
				float theta = 2.0f * PI * randVal;

				return float2(cos(theta), sin(theta));
			}

			// Quintic interpolation curve.
			float quinterp(float f)
			{
				return f*f*f * (f * (f * 6.0f - 15.0f) + 10.0f);
			}

			// Hermite interpolation curve.
			float hermite(float f)
			{
				return f*f * (3.0f - f * 2.0f);
			}

			// Perlin gradient noise generator.
			float perlin2D(float2 positionSS)
			{
				float2 pos00 = floor(positionSS);
				float2 pos10 = pos00 + float2(1.0f, 0.0f);
				float2 pos01 = pos00 + float2(0.0f, 1.0f);
				float2 pos11 = pos00 + float2(1.0f, 1.0f);

				float2 rand00 = randUnitCircle(pos00);
				float2 rand10 = randUnitCircle(pos10);
				float2 rand01 = randUnitCircle(pos01);
				float2 rand11 = randUnitCircle(pos11);

				float dot00 = dot(rand00, pos00 - positionSS);
				float dot10 = dot(rand10, pos10 - positionSS);
				float dot01 = dot(rand01, pos01 - positionSS);
				float dot11 = dot(rand11, pos11 - positionSS);

				float2 d = frac(positionSS);
#if USE_QUINTIC_INTERP
				float x1 = lerp(dot00, dot10, quinterp(d.x));
				float x2 = lerp(dot01, dot11, quinterp(d.x));
				float y = lerp(x1, x2, quinterp(d.y));
#else
				float x1 = lerp(dot00, dot10, hermite(d.x));
				float x2 = lerp(dot01, dot11, hermite(d.x));
				float y = lerp(x1, x2, hermite(d.y));
#endif

				return y;
			}

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                float strength = SampleMask(i.texcoord);

				float2 pos = i.texcoord * _ScreenParams.xy / _NoiseSize;
				float n = perlin2D(pos);

				col.rgb = lerp(col.rgb, col.rgb - _NoiseStrength * n, strength);

#if _USE_FILM_BARS
				float actualAspectRatio = _ScreenParams.x / _ScreenParams.y;
				float targetAspectRatio = _AspectRatio;
				float filmBars = 1.0f;

				if(targetAspectRatio > actualAspectRatio)
				{
					filmBars = step(abs(0.5f - i.texcoord.y) * 2.0f, actualAspectRatio / _AspectRatio);
				}
				else
				{
					filmBars = step(abs(0.5f - i.texcoord.x) * 2.0f, _AspectRatio / actualAspectRatio);
				}

				col.rgb = lerp(_FilmBarColor, col.rgb, filmBars);
#endif

                return col;
            }
            ENDHLSL
        }
    }
}
