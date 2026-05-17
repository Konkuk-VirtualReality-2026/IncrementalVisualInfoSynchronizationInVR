using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRAdaptation
{
    public class PhaseOutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Fidelity 구간 무시하고 항상 실행 (테스트용)")]
            public bool  ForceEnable     = false;
            [ColorUsage(false, false)]
            public Color OutlineColor    = new Color(1f, 1f, 1f);   // 흰색: 잘 보이게
            [Range(0.5f, 4f)]   public float Thickness      = 1.5f;
            [Range(0f, 0.1f)]   public float DepthThreshold = 0.005f;
            [Range(0f, 1f)]     public float NormalThreshold= 0.15f;
        }

        public Settings settings = new();

        PhaseOutlinePass m_Pass;
        Material         m_Material;

        static readonly int s_OutlineColor = Shader.PropertyToID("_OutlineColor");
        static readonly int s_Thickness    = Shader.PropertyToID("_Thickness");
        static readonly int s_DepthThresh  = Shader.PropertyToID("_DepthThresh");
        static readonly int s_NormalThresh = Shader.PropertyToID("_NormalThresh");
        static readonly int s_GlobalFid    = Shader.PropertyToID("_GlobalVisualFidelity");

        public override void Create()
        {
            var shader = Shader.Find("Hidden/VRAdaptation/PhaseOutline");
            if (shader == null)
            {
                Debug.LogError("[PhaseOutline] 셰이더를 찾지 못했습니다: Hidden/VRAdaptation/PhaseOutline");
                return;
            }
            Debug.Log("[PhaseOutline] Create() — 셰이더 발견, Material 생성");
            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass     = new PhaseOutlinePass(m_Material);
            m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
        {
            if (m_Pass == null || m_Material == null) return;
            if (data.cameraData.cameraType == CameraType.Preview) return;

            float fid = Shader.GetGlobalFloat(s_GlobalFid);
            bool inPhase2 = fid >= 0.28f && fid < 0.72f;

            if (!settings.ForceEnable && !inPhase2) return;

            m_Material.SetColor(s_OutlineColor, settings.OutlineColor);
            m_Material.SetFloat(s_Thickness,    settings.Thickness);
            m_Material.SetFloat(s_DepthThresh,  settings.DepthThreshold);
            m_Material.SetFloat(s_NormalThresh, settings.NormalThreshold);

            Debug.Log($"[PhaseOutline] 패스 등록 — fid={fid:F2} forceEnable={settings.ForceEnable}");
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            CoreUtils.Destroy(m_Material);
        }

        // ── Inner Pass ────────────────────────────────────────────────────
        class PhaseOutlinePass : ScriptableRenderPass
        {
            readonly Material m_Mat;
            bool m_Logged;

            public PhaseOutlinePass(Material mat)
            {
                m_Mat = mat;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }

            public void Dispose() { }

#pragma warning disable CS0618
            public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
            {
                if (m_Mat == null) return;

                if (!m_Logged)
                {
                    Debug.Log("[PhaseOutline] Execute() 호출됨 — 셰이더 실행 중");
                    m_Logged = true;
                }

                CommandBuffer cmd = CommandBufferPool.Get("PhaseOutline");

                RenderTargetIdentifier src = data.cameraData.renderer.cameraColorTargetHandle;
                RenderTextureDescriptor desc = data.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples     = 1;

                int tempID = Shader.PropertyToID("_PhaseOutlineTemp");
                cmd.GetTemporaryRT(tempID, desc);

                // src → temp (아웃라인 셰이더 적용)
                cmd.Blit(src, tempID, m_Mat, 0);
                // temp → src (결과 복사)
                cmd.Blit(tempID, src);

                cmd.ReleaseTemporaryRT(tempID);
                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore CS0618
        }
    }
}
