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
            RTHandle          m_TempRT;
            bool              m_Logged;

            public PhaseOutlinePass(Material mat)
            {
                m_Mat = mat;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }

            public void Dispose()
            {
                m_TempRT?.Release();
                m_TempRT = null;
            }

            public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
            {
                if (m_Mat == null) return;

                if (!m_Logged)
                {
                    Debug.Log("[PhaseOutline] Execute() 호출됨 — 셰이더 실행 중");
                    m_Logged = true;
                }

                CommandBuffer cmd = CommandBufferPool.Get("PhaseOutline");

                RTHandle colorTarget = data.cameraData.renderer.cameraColorTargetHandle;
                RenderTextureDescriptor desc = data.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples     = 1;

                RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, name: "_PhaseOutlineTemp");

                // cmd.Blit 은 내부에서 quad(4 vertex) 를 쓰기 때문에
                // SV_VertexID 기반 fullscreen-triangle 셰이더와 UV 가 맞지 않는다.
                // DrawProcedural(Triangles, 3) 으로 정확히 3개 꼭짓점만 넘겨야 한다.
                cmd.SetGlobalTexture(Shader.PropertyToID("_MainTex"), colorTarget);
                cmd.SetRenderTarget(m_TempRT.nameID,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, m_Mat, 0, MeshTopology.Triangles, 3);

                // 결과를 color target 으로 복사 (단순 픽셀 복사 — UV 무관)
#pragma warning disable CS0618
                cmd.Blit(m_TempRT.nameID, colorTarget);
#pragma warning restore CS0618

                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
