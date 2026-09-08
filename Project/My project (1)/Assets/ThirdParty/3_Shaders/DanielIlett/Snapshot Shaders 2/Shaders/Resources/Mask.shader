Shader "Hidden/SnapshotShaders2/Mask"
{
	SubShader
    {
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE

			#pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			float GetSceneDepth(float2 uv)
			{
#if UNITY_REVERSED_Z
				return SampleSceneDepth(uv);
#else
				return lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
#endif
			}

		ENDHLSL

        Pass
        {
            Name "Binary Mask"

            HLSLPROGRAM

			struct appdata
            {
                float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			float _MaskColor;

			v2f vert (appdata v, out float4 positionCS : SV_Position)
            {
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            // Thanks to this for the note on using VPOS: https://gamedev.stackexchange.com/questions/157922/depth-intersection-shader
            float frag (v2f i, float4 positionSS : VPOS) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 screenUV = positionSS.xy / _ScreenSize.xy;
				float screenDepth = Linear01Depth(GetSceneDepth(screenUV), _ZBufferParams);
				float objectDepth = Linear01Depth(positionSS.z, _ZBufferParams);

				// Compare the two pixels - if depthMask is 1, the object is visible in the final image.
				float depthMask = step(objectDepth, screenDepth);

				if(depthMask < 0.5f)
				{
					discard;
				}

				return _MaskColor;
            }
            ENDHLSL
        }
    }
}
