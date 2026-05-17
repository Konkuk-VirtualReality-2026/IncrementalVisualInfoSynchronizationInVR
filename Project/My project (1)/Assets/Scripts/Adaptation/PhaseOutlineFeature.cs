using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRAdaptation
{
    /// <summary>
    /// URP ScriptableRendererFeature — 화면 공간 깊이+법선 엣지 검출로
    /// Phase 2(fidelity 0.3~0.7) 구간에서만 아웃라인을 그린다.
    ///
    /// 사용법:
    ///  에디터 메뉴 VR Adaptation > Setup Phase Outline Feature (DepthNormals) 실행
    /// </summary>
    public class PhaseOutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("체크하면 Fidelity 구간 무시하고 항상 실행 (테스트용)")]
            public bool    ForceEnable     = false;
            [ColorUsage(false, false)]
            public Color   OutlineColor    = new Color(0.7f, 0.9f, 1.0f);
            [Range(0.5f, 4f)]   public float Thickness      = 1.5f;
            [Range(0f,   0.5f)] public float DepthThreshold = 0.015f;
            [Range(0f,   1f)]   public float NormalThreshold= 0.25f;
        }

        public Settings settings = new();

        PhaseOutlinePass m_Pass;
        Material         m_Material;

        static readonly int s_OutlineColor  = Shader.PropertyToID("_OutlineColor");
        static readonly int s_Thickness     = Shader.PropertyToID("_Thickness");
        static readonly int s_DepthThresh   = Shader.PropertyToID("_DepthThresh");
        static readonly int s_NormalThresh  = Shader.PropertyToID("_NormalThresh");
        static readonly int s_GlobalFid     = Shader.PropertyToID("_GlobalVisualFidelity");

        public override void Create()
        {
            var shader = Shader.Find("Hidden/VRAdaptation/PhaseOutline");
            if (shader == null)
            {
                Debug.LogWarning("[VRAdaptation] PhaseOutline 셰이더를 찾지 못했습니다.");
                return;
            }

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass = new PhaseOutlinePass(m_Material);
            m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null || m_Material == null) return;
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;

            float fid = Shader.GetGlobalFloat(s_GlobalFid);
            if (!settings.ForceEnable && (fid < 0.28f || fid >= 0.72f)) return;

            m_Material.SetColor(s_OutlineColor,  settings.OutlineColor);
            m_Material.SetFloat(s_Thickness,     settings.Thickness);
            m_Material.SetFloat(s_DepthThresh,   settings.DepthThreshold);
            m_Material.SetFloat(s_NormalThresh,  settings.NormalThreshold);

            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            CoreUtils.Destroy(m_Material);
        }

        class PhaseOutlinePass : ScriptableRenderPass
        {
            readonly Material m_Mat;
            RTHandle          m_TempRT;

            public PhaseOutlinePass(Material mat)
            {
                m_Mat = mat;
                // 법선+깊이 프리패스 요청 — Normal 이 있어야 법선 엣지 검출 가능
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

                CommandBuffer cmd = CommandBufferPool.Get("PhaseOutline");

                RenderTextureDescriptor desc = data.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, desc, name: "_PhaseOutlineTemp");

                RTHandle colorTarget = data.cameraData.renderer.cameraColorTargetHandle;

                // Blitter 는 _BlitTexture + _BlitTexture_TexelSize 를 올바르게 바인딩
                Blitter.BlitCameraTexture(cmd, colorTarget, m_TempRT,   m_Mat, 0);
                Blitter.BlitCameraTexture(cmd, m_TempRT,   colorTarget);

                ctx.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
