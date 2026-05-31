using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;   // TrackedPoseDriver (런타임 컨트롤러 자동 탐색)

namespace VRAdaptation
{
    /// <summary>
    /// 컨트롤러가 벽에 접촉하면 접촉점을 중심으로 표면을 따라 퍼져나가는 원형 glow 링을
    /// 생성한다. 링 정보는 셰이더 전역(_GlowContacts 등)으로 주입되며,
    /// <c>VR/AdaptationProgressive</c> 셰이더가 Phase 1/2에서 이를 표면에 가산한다.
    ///
    /// 접촉 소스는 <see cref="GlowContactProbe"/>가 자신을 등록하는 방식이라
    /// 특정 XR 리그 구조에 의존하지 않는다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SurfaceGlowManager : MonoBehaviour
    {
        public static SurfaceGlowManager Instance { get; private set; }

        [Header("감지 설정")]
        [Tooltip("벽/표면으로 간주할 레이어")]
        [SerializeField] LayerMask m_WallLayer = ~0;
        [Tooltip("컨트롤러 팁이 표면에서 이 거리(m) 이내면 접촉으로 간주")]
        [SerializeField] float m_ContactRadius = 0.15f;
        [Tooltip("드래그 중 새 링을 찍는 최소 이동 간격(m)")]
        [SerializeField] float m_MinSpawnSpacing = 0.18f;
        [Tooltip("새 링 사이 최소 시간 간격(s). 너무 촘촘한 스폰 방지.")]
        [SerializeField] float m_MinSpawnInterval = 0.10f;
        [Tooltip("같은 자리에 계속 닿아 있어도 이 주기(s)마다 새 링이 퍼져나간다.")]
        [SerializeField] float m_ContinuousSpawnInterval = 0.4f;

        [Header("Glow 외형 (셰이더 전역)")]
        [SerializeField] Color m_GlowColor   = new Color(0.25f, 0.85f, 1f);
        [Tooltip("링 확산 속도 (m/s)")]
        [SerializeField] float m_GlowSpeed    = 1.2f;
        [Tooltip("링 두께 (m)")]
        [SerializeField] float m_GlowRingWidth = 0.25f;
        [Tooltip("한 링의 수명 (s)")]
        [SerializeField] float m_GlowDuration = 1.8f;
        [Tooltip("마스터 강도 (HDR 가산이므로 1 이상이면 강하게 빛남)")]
        [SerializeField, Range(0f, 4f)] float m_GlowIntensity = 1.4f;

        [Header("햅틱 (접촉 시)")]
        [SerializeField] bool m_HapticOnContact = true;
        [SerializeField, Range(0f, 1f)] float m_HapticAmplitude = 0.35f;
        [SerializeField] float m_HapticDuration = 0.05f;

        [Header("자동 연결")]
        [Tooltip("프로브가 하나도 등록되지 않았을 때, 런타임에 컨트롤러(TrackedPoseDriver)를 찾아 프로브를 자동 부착한다.")]
        [SerializeField] bool m_AutoAttachToControllers = true;
        float m_NextAutoAttachTime;

        [Header("디버그")]
        [Tooltip("씬 뷰에 접촉 감지 구체와 활성 링을 그린다.")]
        [SerializeField] bool m_DebugGizmos = true;
        [Tooltip("접촉/스폰 이벤트를 콘솔에 로그로 남긴다.")]
        [SerializeField] bool m_DebugLog = false;

        // 셰이더가 한 번에 처리하는 최대 링 수 (셰이더의 MAX_GLOW_CONTACTS와 일치)
        const int MaxContacts = 16;

        // ── 셰이더 프로퍼티 ID ───────────────────────────────────────────────
        static readonly int s_Contacts     = Shader.PropertyToID("_GlowContacts");
        static readonly int s_ContactCount = Shader.PropertyToID("_GlowContactCount");
        static readonly int s_Time         = Shader.PropertyToID("_GlowTime");
        static readonly int s_Color        = Shader.PropertyToID("_GlowColor");
        static readonly int s_Speed        = Shader.PropertyToID("_GlowSpeed");
        static readonly int s_RingWidth    = Shader.PropertyToID("_GlowRingWidth");
        static readonly int s_Duration     = Shader.PropertyToID("_GlowDuration");
        static readonly int s_Intensity    = Shader.PropertyToID("_GlowIntensity");

        // ── 등록된 접촉 프로브 ───────────────────────────────────────────────
        static readonly List<GlowContactProbe> s_Probes = new();

        public static void Register(GlowContactProbe p)
        {
            if (p != null && !s_Probes.Contains(p)) s_Probes.Add(p);
        }
        public static void Unregister(GlowContactProbe p) => s_Probes.Remove(p);

        // ── 활성 링 버퍼 ─────────────────────────────────────────────────────
        struct Ring { public Vector3 pos; public float startTime; }
        readonly List<Ring> m_Rings = new();
        readonly Vector4[]  m_GpuContacts = new Vector4[MaxContacts];

        // 프로브별 마지막 스폰 추적 (드래그 시 간격 제어)
        readonly Dictionary<GlowContactProbe, Vector3> m_LastSpawnPos  = new();
        readonly Dictionary<GlowContactProbe, float>   m_LastSpawnTime = new();

        readonly Collider[] m_OverlapBuf = new Collider[8];

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            PushStaticParams();
        }

        void OnValidate()
        {
            // 에디터에서 값 조정 시 즉시 반영 (Play 중 튜닝용)
            if (Application.isPlaying) PushStaticParams();
        }

        void PushStaticParams()
        {
            Shader.SetGlobalColor(s_Color,     m_GlowColor);
            Shader.SetGlobalFloat(s_Speed,     m_GlowSpeed);
            Shader.SetGlobalFloat(s_RingWidth, m_GlowRingWidth);
            Shader.SetGlobalFloat(s_Duration,  m_GlowDuration);
            Shader.SetGlobalFloat(s_Intensity, m_GlowIntensity);
        }

        void Update()
        {
            if (m_AutoAttachToControllers && s_Probes.Count == 0 && Time.time >= m_NextAutoAttachTime)
            {
                TryAutoAttachProbes();
                m_NextAutoAttachTime = Time.time + 0.5f; // 컨트롤러가 늦게 생성될 수 있으므로 주기적 재시도
            }

            DetectContacts();
            ExpireRings();
            PushDynamicParams();
        }

        // 씬의 TrackedPoseDriver(HMD/컨트롤러) 중 카메라가 아닌 것 = 컨트롤러/손.
        // 여기에 GlowContactProbe를 붙이면 OnEnable에서 자동 등록된다.
        void TryAutoAttachProbes()
        {
            var drivers = FindObjectsByType<TrackedPoseDriver>(FindObjectsSortMode.None);
            foreach (var d in drivers)
            {
                if (d == null) continue;
                // HMD(카메라가 달린 드라이버)는 제외
                if (d.GetComponent<Camera>() != null) continue;
                if (d.GetComponent<GlowContactProbe>() == null)
                    d.gameObject.AddComponent<GlowContactProbe>();
            }
        }

        // ── 접촉 감지 ────────────────────────────────────────────────────────
        void DetectContacts()
        {
            for (int i = 0; i < s_Probes.Count; i++)
            {
                var probe = s_Probes[i];
                if (probe == null) continue;

                Vector3 p = probe.TipPosition;   // 컨트롤러 팁 기준
                float radius = probe.RadiusOverride > 0f ? probe.RadiusOverride : m_ContactRadius;

                int n = Physics.OverlapSphereNonAlloc(
                    p, radius, m_OverlapBuf, m_WallLayer, QueryTriggerInteraction.Ignore);
                if (n == 0)
                {
                    // 접촉 해제 → 다음 접촉이 새 링을 즉시 찍도록 위치·시간 추적 모두 리셋.
                    // (시간까지 리셋해야 떼었다 곧바로 다시 댈 때 즉시 새 링이 생성된다.)
                    m_LastSpawnPos.Remove(probe);
                    m_LastSpawnTime.Remove(probe);
                    continue;
                }

                // 가장 가까운 표면점 찾기
                Vector3 contact = p;
                float best = float.MaxValue;
                for (int c = 0; c < n; c++)
                {
                    Vector3 cp = ClosestPointSafe(m_OverlapBuf[c], p);
                    float d = (cp - p).sqrMagnitude;
                    if (d < best) { best = d; contact = cp; }
                }

                TrySpawnRing(probe, contact);
            }
        }

        void TrySpawnRing(GlowContactProbe probe, Vector3 contact)
        {
            bool hasLast = m_LastSpawnPos.TryGetValue(probe, out Vector3 lastPos);
            float lastTime = m_LastSpawnTime.TryGetValue(probe, out float t) ? t : -999f;

            float sinceLast = Time.time - lastTime;

            // 두 가지 경로 중 하나면 새 링을 찍는다:
            //  (1) 표면을 따라 충분히 이동(드래그)했고 최소 간격이 지남
            //  (2) 같은 자리에 계속 닿아 있어도 연속 스폰 주기가 지남
            bool moved      = !hasLast || (contact - lastPos).magnitude >= m_MinSpawnSpacing;
            bool oldEnough  = sinceLast >= m_MinSpawnInterval;
            bool continuous = sinceLast >= m_ContinuousSpawnInterval;

            if (!((moved && oldEnough) || continuous)) return;

            SpawnRing(contact);
            m_LastSpawnPos[probe]  = contact;
            m_LastSpawnTime[probe] = Time.time;

            if (m_DebugLog)
                Debug.Log($"[SurfaceGlow] Ring @ {contact} (probe={probe.name}, rings={m_Rings.Count})");

            if (m_HapticOnContact)
                HapticPulse();
        }

        void SpawnRing(Vector3 pos)
        {
            m_Rings.Add(new Ring { pos = pos, startTime = Time.time });
            // 가장 오래된 것부터 잘라 최대 개수 유지
            if (m_Rings.Count > MaxContacts)
                m_Rings.RemoveRange(0, m_Rings.Count - MaxContacts);
        }

        void ExpireRings()
        {
            float now = Time.time;
            for (int i = m_Rings.Count - 1; i >= 0; i--)
            {
                if (now - m_Rings[i].startTime > m_GlowDuration)
                    m_Rings.RemoveAt(i);
            }
        }

        // ── 셰이더 전역 주입 ─────────────────────────────────────────────────
        void PushDynamicParams()
        {
            int count = Mathf.Min(m_Rings.Count, MaxContacts);
            for (int i = 0; i < MaxContacts; i++)
            {
                if (i < count)
                {
                    Ring r = m_Rings[i];
                    m_GpuContacts[i] = new Vector4(r.pos.x, r.pos.y, r.pos.z, r.startTime);
                }
                else
                {
                    m_GpuContacts[i] = Vector4.zero;
                }
            }

            Shader.SetGlobalVectorArray(s_Contacts, m_GpuContacts);
            Shader.SetGlobalFloat(s_ContactCount, count);
            Shader.SetGlobalFloat(s_Time, Time.time);
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────────

        // 비볼록 MeshCollider 등 ClosestPoint가 부정확/예외인 경우를 방어한다.
        static Vector3 ClosestPointSafe(Collider col, Vector3 point)
        {
            if (col == null) return point;
            // 프리미티브/볼록 콜라이더는 정확한 표면점을 반환한다.
            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
                return col.ClosestPoint(point);
            if (col is MeshCollider mc && mc.convex)
                return col.ClosestPoint(point);
            // 비볼록 메시: 표면점 계산이 보장되지 않으므로 접촉 지점(컨트롤러 위치)을 사용
            return point;
        }

        void HapticPulse()
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
            foreach (var d in devices)
                d.SendHapticImpulse(0, m_HapticAmplitude, m_HapticDuration);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── 디버그 기즈모 ─────────────────────────────────────────────────────
        void OnDrawGizmos()
        {
            if (!m_DebugGizmos || !Application.isPlaying) return;

            // 각 프로브의 접촉 감지 구체 (팁 기준)
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            for (int i = 0; i < s_Probes.Count; i++)
            {
                var probe = s_Probes[i];
                if (probe == null) continue;
                float radius = probe.RadiusOverride > 0f ? probe.RadiusOverride : m_ContactRadius;
                Gizmos.DrawWireSphere(probe.TipPosition, radius);
            }

            // 활성 링: 현재 확산 반경을 구체로 표시
            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.8f);
            float now = Application.isPlaying ? Time.time : 0f;
            foreach (var r in m_Rings)
            {
                float age = now - r.startTime;
                if (age < 0f || age > m_GlowDuration) continue;
                Gizmos.DrawWireSphere(r.pos, age * m_GlowSpeed);
            }
        }
    }
}
