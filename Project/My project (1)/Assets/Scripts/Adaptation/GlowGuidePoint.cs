using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRAdaptation
{
    /// <summary>
    /// 암흑/Edge 단계에서 플레이어를 안내하는 발광 구체.
    /// 플레이어가 구체에 진입하면 자동으로 숨김.
    /// SetVisible(true)는 NavigationMission이 외부에서 호출.
    /// GlobalAdaptationEffect의 셰이더 교체에서 제외됨.
    /// </summary>
    [DisallowMultipleComponent]
    public class GlowGuidePoint : MonoBehaviour
    {
        [SerializeField] Color     m_GlowColor      = new Color(0.3f, 1f, 0.5f);
        [SerializeField] float     m_PulseSpeed     = 2.5f;
        [SerializeField] float     m_MinBright      = 0.35f;
        [SerializeField] float     m_MaxBright      = 1.4f;
        [SerializeField] float     m_TriggerRadius  = 0.5f;

        [Header("수집 피드백")]
        [SerializeField] AudioClip m_CollectClip;
        [SerializeField, Range(0f, 1f)] float m_HapticAmplitude = 0.5f;
        [SerializeField] float m_HapticDuration = 0.15f;

        Renderer    m_Renderer;
        Material    m_Mat;
        AudioSource m_AudioSource;
        bool        m_Showing;

        static readonly List<InputDevice> s_Devices = new();

        void Awake()
        {
            // 구체 자동 생성
            if (GetComponentInChildren<Renderer>() == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "GlowSphere";
                sphere.transform.SetParent(transform, false);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale    = Vector3.one * 0.18f;
                Destroy(sphere.GetComponent<Collider>());
            }

            m_Renderer = GetComponentInChildren<Renderer>();
            m_Mat      = new Material(Shader.Find("Unlit/Color"));
            if (m_Renderer != null)
                m_Renderer.sharedMaterial = m_Mat;

            // 플레이어 진입 감지용 Trigger Collider
            var col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.radius    = m_TriggerRadius;
            col.isTrigger = true;

            m_AudioSource           = gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake  = false;
            m_AudioSource.spatialBlend = 1f;

            m_Renderer.enabled = false;
            m_Showing          = false;
        }

        void Update()
        {
            if (!m_Showing || m_Mat == null) return;

            float t         = (Mathf.Sin(Time.time * m_PulseSpeed) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(m_MinBright, m_MaxBright, t);
            m_Mat.color     = m_GlowColor * intensity;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!m_Showing) return;
            if (!IsPlayer(other)) return;
            SetVisible(false);
            PlayCollectFeedback();
        }

        void PlayCollectFeedback()
        {
            if (m_CollectClip != null)
                m_AudioSource.PlayOneShot(m_CollectClip);

            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, s_Devices);
            foreach (var dev in s_Devices)
                dev.SendHapticImpulse(0, m_HapticAmplitude, m_HapticDuration);
        }

        public void SetVisible(bool show)
        {
            m_Showing = show;
            if (m_Renderer != null) m_Renderer.enabled = show;
        }

        static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            if (other.GetComponentInParent<VRCharacterCollision>() != null) return true;
            if (other.GetComponent<CharacterController>() != null) return true;
            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = m_Showing
                ? new Color(0.3f, 1f, 0.5f, 0.3f)
                : new Color(0.3f, 1f, 0.5f, 0.08f);
            Gizmos.DrawWireSphere(transform.position, m_TriggerRadius);
        }
    }
}
