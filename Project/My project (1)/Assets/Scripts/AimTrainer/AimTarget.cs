using UnityEngine;
using UnityEngine.Events;

namespace VRAdaptation.AimTrainer
{
    public enum TargetType { Enemy, Friendly }

    /// <summary>
    /// COD 훈련장 스타일 타겟.
    ///
    /// ▸ MovingPhase (Patrol):
    ///   Inspector에서 PatrolA/B를 두 자식 Transform으로 연결한다.
    ///   맞아도 소멸하지 않고 계속 왕복한다. 점수만 누적.
    ///
    /// ▸ StaticPhase (OneWay):
    ///   AimTargetManager가 런타임에 InitOneWay()로 시작/도착 좌표를 주입한다.
    ///   도착 지점에 닿으면 OnReachedCenter 이벤트 발생 후 소멸.
    ///   맞으면 즉시 소멸 (Enemy) 또는 소멸 없이 패널티 (Friendly).
    /// </summary>
    public class AimTarget : MonoBehaviour
    {
        // ── 타입 ──────────────────────────────────────────────────────────
        [Header("타겟 종류")]
        public TargetType Type = TargetType.Enemy;

        // ── 순찰 (MovingPhase용 — Inspector에서 자식 Transform 연결) ──────
        [Header("Patrol (Moving Phase)")]
        [SerializeField] Transform m_PatrolA;
        [SerializeField] Transform m_PatrolB;
        [SerializeField] float     m_PatrolSpeed = 2f;

        // ── 비주얼 ────────────────────────────────────────────────────────
        [Header("Visual")]
        [SerializeField] Color m_EnemyColor    = new Color(1f, 0.15f, 0.15f);
        [SerializeField] Color m_FriendlyColor = new Color(0.2f, 0.9f, 0.25f);

        // ── 이벤트 ────────────────────────────────────────────────────────
        public UnityAction<AimTarget> OnHit;           // Enemy 명중
        public UnityAction<AimTarget> OnFriendlyFire;  // Friendly 명중
        public UnityAction<AimTarget> OnReachedCenter; // OneWay: 센터 도달 (미스)

        // ── 내부 상태 ─────────────────────────────────────────────────────
        enum MoveMode { Idle, Patrol, OneWay }

        float    m_SpawnTime;
        bool     m_IsActive;
        bool     m_PersistAfterHit;  // true = 맞아도 소멸 안 함 (MovingPhase)

        MoveMode m_MoveMode   = MoveMode.Idle;
        int      m_PatrolDir  = 1;   // 1 = A→B, -1 = B→A

        Vector3  m_WaypointA;
        Vector3  m_WaypointB;
        float    m_ActiveSpeed;

        Renderer m_Renderer;
        Material m_Mat;

        // ─────────────────────────────────────────────────────────────────
        public float GetSpawnTime() => m_SpawnTime;

        void Awake()
        {
            m_Renderer = GetComponentInChildren<Renderer>();
        }

        // ── MovingPhase: Inspector에 PatrolA/B가 연결된 상태로 사용 ────────
        /// <summary>PatrolA/B가 Inspector에서 연결된 경우 호출 (MovingPhase).</summary>
        public void Activate(bool persistAfterHit = true)
        {
            if (m_PatrolA == null || m_PatrolB == null)
            {
                Debug.LogWarning($"[AimTarget] {name}: PatrolA 또는 PatrolB 미연결 — 이동 없이 활성화.");
                m_MoveMode = MoveMode.Idle;
            }
            else
            {
                m_WaypointA = m_PatrolA.position;
                m_WaypointB = m_PatrolB.position;
                m_ActiveSpeed = m_PatrolSpeed;
                m_MoveMode    = MoveMode.Patrol;
                m_PatrolDir   = 1;
                transform.position = m_WaypointA;
            }

            m_PersistAfterHit = persistAfterHit;
            m_SpawnTime       = Time.time;
            m_IsActive        = true;
            gameObject.SetActive(true);
            ApplyVisual();
        }

        // ── StaticPhase: 런타임에 시작/도착 좌표를 주입 ──────────────────
        /// <summary>센터 방향으로 이동하는 OneWay 타겟으로 초기화 (StaticPhase).</summary>
        public void InitOneWay(Vector3 spawnPos, Vector3 centerPos, float speed)
        {
            transform.position = spawnPos;
            m_WaypointA        = spawnPos;
            m_WaypointB        = centerPos;
            m_ActiveSpeed      = speed;
            m_MoveMode         = MoveMode.OneWay;
            m_PersistAfterHit  = false; // Static: 맞으면 즉시 소멸
            m_SpawnTime        = Time.time;
            m_IsActive         = true;
            gameObject.SetActive(true);
            ApplyVisual();
        }

        void Update()
        {
            if (!m_IsActive) return;
            MoveUpdate();
        }

        void MoveUpdate()
        {
            if (m_MoveMode == MoveMode.Idle) return;

            Vector3 target = (m_PatrolDir > 0) ? m_WaypointB : m_WaypointA;
            transform.position = Vector3.MoveTowards(transform.position, target, m_ActiveSpeed * Time.deltaTime);

            // 이동 방향을 바라봄
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                if (m_MoveMode == MoveMode.Patrol)
                {
                    m_PatrolDir = -m_PatrolDir; // 반환
                }
                else // OneWay
                {
                    m_IsActive = false;
                    OnReachedCenter?.Invoke(this);
                    gameObject.SetActive(false);
                }
            }
        }

        /// <summary>발사 충돌 시 호출. 타입에 따라 이벤트 분기.</summary>
        public void Hit()
        {
            if (!m_IsActive) return;

            if (Type == TargetType.Enemy)
            {
                OnHit?.Invoke(this);
                if (!m_PersistAfterHit)
                {
                    m_IsActive = false;
                    gameObject.SetActive(false);
                }
                // PersistAfterHit = true 면 계속 이동 (MovingPhase)
            }
            else // Friendly
            {
                OnFriendlyFire?.Invoke(this);
                // Friendly 는 어느 Phase에서도 소멸하지 않음 (패널티만)
            }
        }

        void ApplyVisual()
        {
            if (m_Renderer == null) m_Renderer = GetComponentInChildren<Renderer>();
            if (m_Renderer == null) return;

            if (m_Mat == null)
                m_Mat = new Material(Shader.Find("Unlit/Color"));

            m_Mat.color             = (Type == TargetType.Enemy) ? m_EnemyColor : m_FriendlyColor;
            m_Renderer.sharedMaterial = m_Mat;
        }

        void OnDestroy()
        {
            if (m_Mat != null) Destroy(m_Mat);
        }
    }
}
