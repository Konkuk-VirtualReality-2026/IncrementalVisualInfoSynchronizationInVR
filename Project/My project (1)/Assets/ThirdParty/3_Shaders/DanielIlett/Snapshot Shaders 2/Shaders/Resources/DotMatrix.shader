Shader "Hidden/SnapshotShaders2/DotMatrix"
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
			uint _GapWidth;
            uint _DotSize;
            float3 _BackgroundColor;

            float4 frag (Varyings i) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                int2 positionSS = i.texcoord * _BlitTexture_TexelSize.zw;

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                int2 pos = positionSS % (_GapWidth + _DotSize);
                int isGap = max(step(_DotSize, pos.x), step(_DotSize, pos.y)) * SampleMask(i.texcoord);

				col.rgb = lerp(col.rgb, _BackgroundColor, isGap);
                return col;
            }
            ENDHLSL
        }
    }
}
