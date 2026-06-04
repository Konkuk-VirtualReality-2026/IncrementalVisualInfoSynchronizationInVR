using UnityEngine;

namespace VRAdaptation
{
    /// <summary>
    /// 암흑/Edge 단계에서 플레이어를 안내하는 발광 구체.
    /// GlobalAdaptationEffect의 셰이더 교체에서 제외되며, Unlit 셰이더로 항상 발광.
    /// NavigationMission이 순서대로 활성화한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GlowGuidePoint : MonoBehaviour
    {
        [SerializeField] Color  m_GlowColor  = new Color(0.3f, 1f, 0.5f);
        [SerializeField] float  m_PulseSpeed = 2.5f;
        [SerializeField] float  m_MinBright  = 0.35f;
        [SerializeField] float  m_MaxBright  = 1.4f;

        Renderer m_Renderer;
        Material m_Mat;
        bool     m_Showing;

        void Awake()
        {
            m_Renderer = GetComponentInChildren<Renderer>();

            // MeshRenderer/SphereCollider 가 없으면 구체를 자동 생성한다
            if (m_Renderer == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "GlowSphere";
                sphere.transform.SetParent(transform, false);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale    = Vector3.one * 0.18f;
                Destroy(sphere.GetComponent<Collider>());
                m_Renderer = sphere.GetComponent<Renderer>();
            }

            m_Mat = new Material(Shader.Find("Unlit/Color"));
            m_Renderer.sharedMaterial = m_Mat;

            // 기본 비활성
            m_Renderer.enabled = false;
            m_Showing          = false;
        }

        void Update()
        {
            if (!m_Showing || m_Mat == null) return;

            // 밝기 펄스
            float t         = (Mathf.Sin(Time.time * m_PulseSpeed) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(m_MinBright, m_MaxBright, t);
            m_Mat.color     = m_GlowColor * intensity;
        }

        /// <summary>발광 구체 표시/숨김</summary>
        public void SetVisible(bool show)
        {
            m_Showing = show;
            if (m_Renderer != null) m_Renderer.enabled = show;
        }
    }
}
