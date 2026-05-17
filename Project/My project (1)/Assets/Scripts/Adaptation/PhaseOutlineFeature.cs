using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRAdaptation
{
    /// <summary>
    /// URP ScriptableRendererFeature — 화면 공간 깊이 엣지 검출로
    /// Phase 2(fidelity 0.3~0.7) 구간에서만 파란 아웃라인을 그린다.
    ///
    /// 사용법:
    ///  1) 에디터 메뉴 VR Adaptation > Setup Phase Outline Feature (DepthNormals) 실행
    ///  2) 또는 URP Renderer 에셋 Inspector > Add Renderer Feature > Phase Outline Feature
    /// </summary>
    public class PhaseOutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [ColorUsage(false, false)]
            public Color   OutlineColor    = new Color(0.2f, 0.6f, 1.0f);
            [Range(0.5f, 4f)]  public float Thickness      = 1.2f;
            [Range(0f,   0.5f)]public float DepthThreshold = 0.015f;
        }

        public Settings settings = new();

        PhaseOutlinePass m_Pass;
        Material         m_Material;

        static readonly int s_OutlineColor = Shader.PropertyToID("_OutlineColor");
        static readonly int s_Thickness    = Shader.PropertyToID("_Thickness");
        static readonly int s_DepthThresh  = Shader.PropertyToID("_DepthThresh");
        static readonly int s_GlobalFid    = Shader.PropertyToID("_GlobalVisualFidelity");

        public override void Create()
        {
            var shader = Shader.Find("Hidden/VRAdaptation/PhaseOutline");
            if (shader == null)
            {
                Debug.LogWarning("[VRAdaptation] PhaseOutline 셰이더를 찾지 못했습니다. " +
                                 "Assets/Shaders/PhaseOutline.shader 가 프로젝트에 있는지 확인하세요.");
                return;
            }

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass = new PhaseOutlinePass(m_Material);
            m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null || m_Material == null) return;

            // 편집기 미리보기 카메라 제외
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;

            float fid = Shader.GetGlobalFloat(s_GlobalFid);
            if (fid < 0.28f || fid >= 0.72f) return;   // Phase 2 구간에서만

            // 설정값 동기화
            m_Material.SetColor(s_OutlineColor, settings.OutlineColor);
            m_Material.SetFloat(s_Thickness,    settings.Thickness);
            m_Material.SetFloat(s_DepthThresh,  settings.DepthThreshold);

            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            CoreUtils.Destroy(m_Material);
        }

        // ── Inner Pass ───────────────────────────────────────────────────────
        class PhaseOutlinePass : ScriptableRenderPass
        {
            readonly Material m_Mat;
            RTHandle          m_TempRT;

            public PhaseOutlinePass(Material mat)
            {
                m_Mat = mat;
                // 깊이 텍스처만 필요 (법선 불필요)
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Dispose()
            {
                m_TempRT?.Release();
                m_TempRT = null;
            }

            public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
            {
                if (m_Mat == null) return;

                CommandBuffer cmd = CommandBufferPool.Get("PhaseOutline");

                RenderTextureDescriptor desc = data.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                // RTHandle 재사용 (매 프레임 Alloc 방지)
                RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, name: "_PhaseOutlineTemp");

                RTHandle colorTarget = data.cameraData.renderer.cameraColorTargetHandle;

                // Blitter.BlitCameraTexture 는 _BlitTexture + _BlitTexture_TexelSize 를 올바르게 바인딩.
                // 스테레오(싱글패스 인스턴싱) 환경에서도 정상 작동.
                Blitter.BlitCameraTexture(cmd, colorTarget, m_TempRT,   m_Mat, 0);
                Blitter.BlitCameraTexture(cmd, m_TempRT,   colorTarget);

                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
