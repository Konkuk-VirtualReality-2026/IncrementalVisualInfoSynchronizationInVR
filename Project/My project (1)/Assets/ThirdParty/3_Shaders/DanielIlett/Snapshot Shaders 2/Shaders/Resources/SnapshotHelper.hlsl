#ifndef SNAPSHOT_2_HELPERS
#define SNAPSHOT_2_HELPERS

#pragma multi_compile_local_fragment _ _SS2_USE_MASK

#define EPSILON 1e-06

float _InvertMask;
#ifdef _SS2_USE_MASK
    TEXTURE2D_X(_MaskedObjects);

    float SampleMask(float2 uv)
    {
        float mask = SAMPLE_TEXTURE2D_X(_MaskedObjects, sampler_LinearClamp, uv).r;
        return abs(_InvertMask - mask);
    }
#else
    float SampleMask(float2 uv)
    {
        return 1.0f;
    }
#endif

#endif // SNAPSHOT_2_HELPERS
