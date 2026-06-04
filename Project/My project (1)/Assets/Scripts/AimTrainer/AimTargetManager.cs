using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace VRAdaptation.AimTrainer
{
    /// <summary>
    /// AimTrainer 두 단계를 관리한다.
    ///
    /// ▸ Moving Phase (StartMovingPhase):
    ///   씬에 미리 배치된 m_MovingTargets 를 모두 활성화해 순찰 이동시킨다.
    ///   시간 제한(2분) 종료 시 StopMovingPhase() → 다음 Phase로.
    ///
    /// ▸ Static Phase (StartStaticPhase):
    ///   m_StaticSpawnCenter 주변 360° 에서 주기적으로 적/우군을 스폰,
    ///   센터로 이동시킨다. 모든 적이 처치되면 OnAllEnemiesKilled 이벤트 발생.
    ///
    /// Inspector 연결:
    ///   - m_MovingTargets : 씬에 배치된 AimTarget(비활성) 리스트
    ///   - m_StaticSpawnCenter : 플레이어 고정 위치 Transform
    ///   - m_StaticTargetPrefab : StaticPhase 스폰용 AimTarget 프리팹
    ///   - m_PlayerHead : XR Camera
    /// </summary>
    public class AimTargetManager : MonoBehaviour
    {
        public static AimTargetManager Instance { get; private set; }

        // ── Moving Phase ──────────────────────────────────────────────────
        [Header("Moving Phase")]
        [Tooltip("씬에 미리 배치한 순찰 타겟들 (비활성 상태로 배치)")]
        [SerializeField] List<AimTarget> m_MovingTargets = new();

        // ── Static Phase ──────────────────────────────────────────────────
        [Header("Static Phase")]
        [Tooltip("씬에 미리 배치한 Static Phase 타겟들 (비활성 상태로 배치)")]
        [SerializeField] List<AimTarget> m_StaticTargets = new();
        [Tooltip("타겟이 이동할 목표 지점 (플레이어 고정 위치)")]
        [SerializeField] Transform m_StaticCenter;
        [Tooltip("센터로 이동하는 속도")]
        [SerializeField] float m_ApproachSpeed  = 1.5f;
        [Tooltip("타겟 활성화 간격 (초) — 한 번에 하나씩 등장")]
        [SerializeField] float m_SpawnInterval  = 2.5f;

        [Header("공통")]
        [SerializeField] Transform m_PlayerHead;

        // ── 이벤트 ────────────────────────────────────────────────────────
        /// <summary>Static Phase: 모든 Enemy가 처치되면 발생</summary>
        public UnityAction OnAllEnemiesKilled;

        // ── 내부 상태 ─────────────────────────────────────────────────────
        bool m_IsRunning;
        string m_Condition;

        // Static Phase 카운터
        int m_EnemiesActivated;
        int m_EnemiesKilled;
        int m_EnemiesMissed;
        int m_TotalEnemiesTarget;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // ═══════════════════════════════════════════════════════════════
        // Moving Phase
        // ═══════════════════════════════════════════════════════════════

        /// <summary>씬 배치 타겟을 모두 활성화해 순찰 시작. 로깅 시작.</summary>
        public void StartMovingPhase(string condition)
        {
            if (m_IsRunning) return;
            m_IsRunning = true;
            m_Condition = condition;

            ExperimentDataLogger.Instance.StartLogging(condition);

            foreach (var t in m_MovingTargets)
            {
                if (t == null) continue;
                t.OnHit          = (tgt) => HandleEnemyHit(tgt);
                t.OnFriendlyFire = (tgt) => HandleFriendlyFire(tgt);
                t.Activate(persistAfterHit: true); // 맞아도 소멸 안 함
                LogSpawn(t);
            }

            Debug.Log($"[AimTrainer] Moving Phase 시작 — 타겟 {m_MovingTargets.Count}개");
        }

        /// <summary>Moving Phase 종료 — 모든 순찰 타겟 비활성화.</summary>
        public void StopMovingPhase()
        {
            m_IsRunning = false;
            foreach (var t in m_MovingTargets)
                if (t != null) t.gameObject.SetActive(false);

            ExperimentDataLogger.Instance.StopLogging();
            Debug.Log("[AimTrainer] Moving Phase 종료");
        }

        // ═══════════════════════════════════════════════════════════════
        // Static Phase
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Static Phase 시작.
        /// 씬에 배치된 m_StaticTargets 를 m_SpawnInterval 간격으로 하나씩 활성화.
        /// 모두 처치(또는 센터 도달)되면 OnAllEnemiesKilled 이벤트 발생.
        /// </summary>
        public void StartStaticPhase(string condition)
        {
            if (m_IsRunning) return;
            m_IsRunning          = true;
            m_Condition          = condition;
            m_EnemiesActivated   = 0;
            m_EnemiesKilled      = 0;
            m_EnemiesMissed      = 0;
            m_TotalEnemiesTarget = m_StaticTargets.Count;

            if (m_TotalEnemiesTarget == 0)
            {
                Debug.LogWarning("[AimTrainer] m_StaticTargets 가 비어있습니다.");
                OnAllEnemiesKilled?.Invoke();
                return;
            }

            ExperimentDataLogger.Instance.StartLogging(condition);
            StartCoroutine(StaticActivateRoutine());
            Debug.Log($"[AimTrainer] Static Phase 시작 — 씬 배치 타겟 {m_TotalEnemiesTarget}개");
        }

        IEnumerator StaticActivateRoutine()
        {
            foreach (var t in m_StaticTargets)
            {
                if (!m_IsRunning) yield break;
                if (t == null) { m_EnemiesActivated++; continue; }

                Vector3 startPos  = t.transform.position;
                Vector3 centerPos = m_StaticCenter != null
                    ? m_StaticCenter.position
                    : Vector3.zero;

                t.OnHit           = (tgt) => HandleStaticEnemyKill(tgt);
                t.OnFriendlyFire  = (tgt) => HandleFriendlyFire(tgt);
                t.OnReachedCenter = (tgt) => HandleStaticMiss(tgt);

                t.InitOneWay(startPos, centerPos, m_ApproachSpeed);
                m_EnemiesActivated++;

                ExperimentDataLogger.Instance.LogEvent(m_Condition, "Spawn", 0f, startPos, GetHeadRotation());

                yield return new WaitForSeconds(m_SpawnInterval);
            }
        }

        void HandleStaticEnemyKill(AimTarget t)
        {
            float reaction = (Time.time - t.GetSpawnTime()) * 1000f;
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "Hit", reaction, t.transform.position, GetHeadRotation());
            AimTrainerHUD.Instance?.RegisterHit();
            m_EnemiesKilled++;
            CheckStaticComplete();
        }

        void HandleStaticMiss(AimTarget t)
        {
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "Miss", 0f, t.transform.position, GetHeadRotation());
            m_EnemiesMissed++;
            CheckStaticComplete();
        }

        void CheckStaticComplete()
        {
            AimTrainerHUD.Instance?.UpdateRemainingEnemies(
                Mathf.Max(0, m_TotalEnemiesTarget - m_EnemiesKilled - m_EnemiesMissed));

            bool allActivated = (m_EnemiesActivated >= m_TotalEnemiesTarget);
            bool allResolved  = (m_EnemiesKilled + m_EnemiesMissed >= m_EnemiesActivated);

            if (allActivated && allResolved)
            {
                Debug.Log($"[AimTrainer] Static Phase 완료 — 처치 {m_EnemiesKilled} / 센터 도달 {m_EnemiesMissed}");
                OnAllEnemiesKilled?.Invoke();
            }
        }

        public void StopStaticPhase()
        {
            m_IsRunning = false;
            StopAllCoroutines();
            foreach (var t in m_StaticTargets)
                if (t != null) t.gameObject.SetActive(false);
            ExperimentDataLogger.Instance.StopLogging();
            Debug.Log("[AimTrainer] Static Phase 강제 종료");
        }

        // ═══════════════════════════════════════════════════════════════
        // 공통 핸들러
        // ═══════════════════════════════════════════════════════════════

        void HandleEnemyHit(AimTarget t)
        {
            float reaction = (Time.time - t.GetSpawnTime()) * 1000f;
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "Hit", reaction, t.transform.position, GetHeadRotation());
            AimTrainerHUD.Instance?.RegisterHit();
        }

        public void HandleFriendlyFire(AimTarget t)
        {
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "FriendlyFire", 0f, t.transform.position, GetHeadRotation());
            AimTrainerHUD.Instance?.RegisterPenalty();
        }

        void LogSpawn(AimTarget t)
        {
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "Spawn", 0f, t.transform.position, GetHeadRotation());
        }

        Vector3 GetHeadRotation() =>
            m_PlayerHead != null ? m_PlayerHead.eulerAngles : Vector3.zero;

        /// <summary>Static Phase 씬 배치 타겟 총 수 (HUD 초기값용)</summary>
        public int GetStaticTargetCount() => m_StaticTargets.Count;

        // ─── 레거시 호환 (VRAdaptationManager 가 직접 호출하는 인터페이스) ──
        /// <summary>Moving Phase 시작 (VRAdaptationManager 호출용)</summary>
        public void StartAimTrainer(string condition) => StartMovingPhase(condition);
        /// <summary>Moving Phase 정지 (VRAdaptationManager 호출용)</summary>
        public void StopAimTrainer()
        {
            if (m_IsRunning)
            {
                StopMovingPhase();
                StopStaticPhase();
            }
        }
    }
}
