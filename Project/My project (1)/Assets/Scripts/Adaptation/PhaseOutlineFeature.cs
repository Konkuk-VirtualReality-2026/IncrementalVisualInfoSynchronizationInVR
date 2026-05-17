using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace VRAdaptation
{
    /// <summary>
    /// Phase 2 화면 공간 아웃라인 Renderer Feature.
    /// Unity 6 RenderGraph API(RecordRenderGraph) + 구 호환 API(Execute) 모두 구현.
    /// </summary>
    public class PhaseOutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Fidelity 구간 무시하고 항상 실행 (테스트용)")]
            public bool  ForceEnable     = false;
            [ColorUsage(false, false)]
            public Color OutlineColor    = new Color(1f, 1f, 1f);
            [Range(0.5f, 4f)]   public float Thickness      = 1.5f;
            [Range(0f, 0.1f)]   public float DepthThreshold = 0.005f;
            [Range(0f, 1f)]     public float NormalThreshold= 0.5f;
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
            RTHandle          m_TempRT;   // Compatibility Mode 용
            bool              m_Logged;

            // RenderGraph 패스 데이터
            class PassData
            {
                internal Material      mat;
                internal TextureHandle src;
            }

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

            // ── Unity 6 RenderGraph API ───────────────────────────────────
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (m_Mat == null) return;
                if (!m_Logged)
                {
                    Debug.Log("[PhaseOutline] RecordRenderGraph 호출됨 — RenderGraph 모드");
                    m_Logged = true;
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData   = frameData.Get<UniversalCameraData>();

                TextureHandle colorHandle   = resourceData.activeColorTexture;
                TextureHandle depthHandle   = resourceData.cameraDepthTexture;
                TextureHandle normalsHandle = resourceData.cameraNormalsTexture;

                // 핑퐁 블릿용 중간 버퍼
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                TextureHandle tempHandle = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, desc, "_PhaseOutlineTemp", false);

                // ─ Pass 1: color → temp  (아웃라인 셰이더) ─────────────
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           "PhaseOutline_Apply", out var pd))
                {
                    pd.mat = m_Mat;
                    pd.src = colorHandle;

                    builder.UseTexture(colorHandle);           // Read color
                    // depth/normals 는 셰이더가 전역 텍스처로 접근하지만,
                    // RenderGraph 가 프리패스를 cull 하지 않도록 명시적으로 선언한다.
                    if (depthHandle.IsValid())   builder.UseTexture(depthHandle);
                    if (normalsHandle.IsValid()) builder.UseTexture(normalsHandle);
                    builder.SetRenderAttachment(tempHandle, 0); // Write
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc<PassData>(static (data, ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.src,
                            new Vector4(1, 1, 0, 0), data.mat, 0);
                    });
                }

                // ─ Pass 2: temp → color  (결과 복사) ───────────────────
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           "PhaseOutline_CopyBack", out var pd))
                {
                    pd.src = tempHandle;

                    builder.UseTexture(tempHandle);             // Read
                    builder.SetRenderAttachment(colorHandle, 0); // Write
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc<PassData>(static (data, ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.src,
                            new Vector4(1, 1, 0, 0), 0, false);
                    });
                }
            }

            // ── Compatibility Mode (RenderGraph 비활성화 시) ──────────────
#pragma warning disable CS0618
            public override void Execute(ScriptableRenderContext ctx, ref RenderingData data)
            {
                if (m_Mat == null) return;
                if (!m_Logged)
                {
                    Debug.Log("[PhaseOutline] Execute 호출됨 — Compatibility Mode");
                    m_Logged = true;
                }

                CommandBuffer cmd = CommandBufferPool.Get("PhaseOutline");

                RTHandle colorTarget = data.cameraData.renderer.cameraColorTargetHandle;
                RenderTextureDescriptor desc = data.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples     = 1;

                RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, name: "_PhaseOutlineTemp");

                cmd.SetGlobalTexture(Shader.PropertyToID("_MainTex"), colorTarget);
                cmd.SetRenderTarget(m_TempRT.nameID,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, m_Mat, 0, MeshTopology.Triangles, 3);
                cmd.Blit(m_TempRT.nameID, colorTarget);

                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore CS0618
        }
    }
}
