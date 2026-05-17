using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRAdaptation.AimTrainer
{
    public class RaycastWeapon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float     m_MaxDistance = 50f;
        [SerializeField] LayerMask m_TargetLayer;
        [SerializeField] Transform m_MuzzlePoint;

        [Header("레이저 조준선 (항상 표시)")]
        [SerializeField] bool  m_ShowLaserSight  = true;
        [SerializeField] Color m_LaserSightColor = new Color(1f, 0.08f, 0.08f, 0.9f);
        [SerializeField] float m_LaserSightWidth = 0.0018f;

        [Header("발사 이펙트")]
        [SerializeField] float m_MuzzleFlashDuration = 0.055f;
        [SerializeField] float m_TracerDuration      = 0.06f;

        [Header("오디오")]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip   m_FireClip;
        [SerializeField] AudioClip   m_HitClip;

        [Header("햅틱 반동")]
        [SerializeField, Range(0f, 1f)] float m_RecoilAmplitude    = 0.7f;
        [SerializeField]                float m_RecoilDuration      = 0.08f;
        [SerializeField, Range(0f, 1f)] float m_HitBonusAmplitude  = 0.35f;

        // ── 자동 생성 컴포넌트 ────────────────────────────────────────────
        LineRenderer m_SightLine;
        LineRenderer m_TracerLine;
        GameObject   m_FlashObj;
        Light        m_FlashLight;

        static readonly List<InputDevice> s_Controllers = new();

        // ── 생명주기 ──────────────────────────────────────────────────────
        void Awake()
        {
            if (m_MuzzlePoint == null) m_MuzzlePoint = transform;
            if (m_AudioSource  == null) m_AudioSource = GetComponent<AudioSource>();

            BuildLaserSight();
            BuildMuzzleFlash();
            BuildTracer();
        }

        void Update()
        {
            UpdateLaserSight();
        }

        // ── 공개 API ──────────────────────────────────────────────────────

        /// <summary>발사. 반환값: true = 타겟 적중</summary>
        public bool Fire()
        {
            // 발사음
            if (m_AudioSource != null && m_FireClip != null)
                m_AudioSource.PlayOneShot(m_FireClip);

            // 뮤즐 플래시
            StartCoroutine(DoMuzzleFlash());

            // 레이캐스트
            Ray ray    = new Ray(m_MuzzlePoint.position, m_MuzzlePoint.forward);
            bool isHit = Physics.Raycast(ray, out RaycastHit hit, m_MaxDistance, m_TargetLayer);
            Vector3 endPt = isHit
                ? hit.point
                : m_MuzzlePoint.position + m_MuzzlePoint.forward * m_MaxDistance;

            // 트레이서
            StartCoroutine(ShowTracer(m_MuzzlePoint.position, endPt));

            // 반동 햅틱
            float hapticAmp = m_RecoilAmplitude;

            if (isHit)
            {
                StartCoroutine(ShowImpact(hit.point, hit.normal));

                if (hit.collider.TryGetComponent(out AimTarget target))
                {
                    target.Hit();
                    hapticAmp = Mathf.Clamp01(hapticAmp + m_HitBonusAmplitude);
                    if (m_AudioSource != null && m_HitClip != null)
                        m_AudioSource.PlayOneShot(m_HitClip);
                }
            }

            SendRecoilHaptic(hapticAmp);
            return isHit;
        }

        // ── 이펙트 구성 ───────────────────────────────────────────────────

        void BuildLaserSight()
        {
            if (!m_ShowLaserSight) return;

            m_SightLine = gameObject.AddComponent<LineRenderer>();
            m_SightLine.positionCount  = 2;
            m_SightLine.startWidth     = m_LaserSightWidth;
            m_SightLine.endWidth       = m_LaserSightWidth * 0.3f;
            m_SightLine.useWorldSpace  = true;
            m_SightLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_SightLine.receiveShadows = false;
            var mat = new Material(Shader.Find("Unlit/Color")) { color = m_LaserSightColor };
            m_SightLine.material = mat;
        }

        void BuildMuzzleFlash()
        {
            m_FlashObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_FlashObj.name = "MuzzleFlash";
            Destroy(m_FlashObj.GetComponent<Collider>());
            m_FlashObj.transform.SetParent(m_MuzzlePoint, false);
            m_FlashObj.transform.localPosition = Vector3.zero;
            m_FlashObj.transform.localScale    = Vector3.one * 0.09f;

            var mat = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.85f, 0.25f) };
            m_FlashObj.GetComponent<Renderer>().sharedMaterial = mat;

            m_FlashLight = m_FlashObj.AddComponent<Light>();
            m_FlashLight.type      = LightType.Point;
            m_FlashLight.range     = 3.5f;
            m_FlashLight.intensity = 6f;
            m_FlashLight.color     = new Color(1f, 0.75f, 0.2f);

            m_FlashObj.SetActive(false);
        }

        void BuildTracer()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);

            m_TracerLine = go.AddComponent<LineRenderer>();
            m_TracerLine.positionCount  = 2;
            m_TracerLine.startWidth     = 0.012f;
            m_TracerLine.endWidth       = 0.003f;
            m_TracerLine.useWorldSpace  = true;
            m_TracerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_TracerLine.receiveShadows = false;
            var mat = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.92f, 0.35f) };
            m_TracerLine.sharedMaterial = mat;
            m_TracerLine.enabled = false;
        }

        // ── 매 프레임 조준선 갱신 ─────────────────────────────────────────
        void UpdateLaserSight()
        {
            if (m_SightLine == null || m_MuzzlePoint == null) return;

            Ray ray   = new Ray(m_MuzzlePoint.position, m_MuzzlePoint.forward);
            Vector3 end = Physics.Raycast(ray, out RaycastHit hit, m_MaxDistance, m_TargetLayer)
                ? hit.point
                : m_MuzzlePoint.position + m_MuzzlePoint.forward * m_MaxDistance;

            m_SightLine.SetPosition(0, m_MuzzlePoint.position);
            m_SightLine.SetPosition(1, end);
        }

        // ── 코루틴 이펙트 ─────────────────────────────────────────────────
        IEnumerator DoMuzzleFlash()
        {
            m_FlashObj.SetActive(true);
            yield return new WaitForSeconds(m_MuzzleFlashDuration);
            m_FlashObj.SetActive(false);
        }

        IEnumerator ShowTracer(Vector3 from, Vector3 to)
        {
            m_TracerLine.SetPosition(0, from);
            m_TracerLine.SetPosition(1, to);
            m_TracerLine.enabled = true;
            yield return new WaitForSeconds(m_TracerDuration);
            m_TracerLine.enabled = false;
        }

        IEnumerator ShowImpact(Vector3 pos, Vector3 normal)
        {
            GameObject imp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            imp.name = "Impact";
            Destroy(imp.GetComponent<Collider>());
            imp.transform.position = pos + normal * 0.015f;

            var mat = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.45f, 0.05f) };
            imp.GetComponent<Renderer>().sharedMaterial = mat;

            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(0.13f, 0f, elapsed / duration);
                imp.transform.localScale = Vector3.one * s;
                yield return null;
            }
            Destroy(imp);
        }

        // ── 햅틱 ─────────────────────────────────────────────────────────
        void SendRecoilHaptic(float amplitude)
        {
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right,
                s_Controllers);
            foreach (var dev in s_Controllers)
                dev.SendHapticImpulse(0, Mathf.Clamp01(amplitude), m_RecoilDuration);
        }
    }
}
