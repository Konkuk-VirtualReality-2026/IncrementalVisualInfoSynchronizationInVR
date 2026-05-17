using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRAdaptation
{
    /// <summary>
    /// URP ScriptableRendererFeature — 화면 공간 깊이·법선 엣지 검출로
    /// Phase 2(fidelity 0.3~0.7) 구간에서만 파란 아웃라인을 그린다.
    ///
    /// 사용법:
    ///  1) 프로젝트의 URP Renderer 에셋(Universal Renderer Data) Inspector 열기
    ///  2) Add Renderer Feature → "Phase Outline Feature" 선택
    ///  3) 또는 에디터 메뉴 VR Adaptation > Setup Phase Outline Feature 실행
    /// </summary>
    public class PhaseOutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [ColorUsage(false, false)]
            public Color   OutlineColor    = new Color(0.2f, 0.6f, 1.0f);
            [Range(0.5f, 4f)]  public float Thickness      = 1.2f;
            [Range(0f,   0.5f)]public float DepthThreshold = 0.03f;
            [Range(0f,   1f)]  public float NormalThreshold= 0.25f;
        }

        public Settings settings = new();

        PhaseOutlinePass m_Pass;
        Material         m_Material;

        static readonly int s_OutlineColor = Shader.PropertyToID("_OutlineColor");
        static readonly int s_Thickness    = Shader.PropertyToID("_Thickness");
        static readonly int s_DepthThresh  = Shader.PropertyToID("_DepthThresh");
        static readonly int s_NormThresh   = Shader.PropertyToID("_NormThresh");
        static readonly int s_GlobalFid    = Shader.PropertyToID("_GlobalVisualFidelity");

        public override void Create()
        {
            var shader = Shader.Find("Hidden/VRAdaptation/PhaseOutline");
            if (shader == null) return;

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass = new PhaseOutlinePass(m_Material, settings);
            m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null || m_Material == null) return;

            float fid = Shader.GetGlobalFloat(s_GlobalFid);
            if (fid < 0.28f || fid >= 0.72f) return;   // Phase 2 구간에서만

            // 설정값 동기화
            m_Material.SetColor(s_OutlineColor, settings.OutlineColor);
            m_Material.SetFloat(s_Thickness,    settings.Thickness);
            m_Material.SetFloat(s_DepthThresh,  settings.DepthThreshold);
            m_Material.SetFloat(s_NormThresh,   settings.NormalThreshold);

            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }

        // ── Inner Pass ───────────────────────────────────────────────────────
        class PhaseOutlinePass : ScriptableRenderPass
        {
            readonly Material m_Mat;
            readonly Settings m_Settings;
            RTHandle          m_TempRT;

            public PhaseOutlinePass(Material mat, Settings settings)
            {
                m_Mat      = mat;
                m_Settings = settings;
                // 깊이·법선 텍스처 필요
                this.requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
            }

#pragma warning disable CS0618 // 레거시 Execute API — Unity 6에서도 동작
            public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
            {
                if (m_Mat == null) return;

                CommandBuffer cmd = CommandBufferPool.Get("PhaseOutline");
                RenderTargetIdentifier src = data.cameraData.renderer.cameraColorTargetHandle;

                // 임시 RT 할당
                RenderTextureDescriptor desc = data.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                cmd.GetTemporaryRT(Shader.PropertyToID("_PhaseOutlineTemp"), desc);

                // Blit: src → temp (엣지 검출)
                Blit(cmd, src, new RenderTargetIdentifier(Shader.PropertyToID("_PhaseOutlineTemp")), m_Mat, 0);
                // Blit: temp → src (결과 복사)
                Blit(cmd, new RenderTargetIdentifier(Shader.PropertyToID("_PhaseOutlineTemp")), src);

                cmd.ReleaseTemporaryRT(Shader.PropertyToID("_PhaseOutlineTemp"));
                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore CS0618
        }
    }
}
