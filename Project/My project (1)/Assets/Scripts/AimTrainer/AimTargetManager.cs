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
        // Battle Phase (통합: Moving + Static 타겟 전부 동시 활성화)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 이동 타겟(PatrolA/B 있음)과 고정 타겟(PatrolA/B 없음)을 동시에 활성화.
        /// Enemy 전부 처치 시 OnAllEnemiesKilled 이벤트 발생.
        /// </summary>
        public void StartBattlePhase(string condition)
        {
            if (m_IsRunning) return;
            m_IsRunning          = true;
            m_Condition          = condition;
            m_EnemiesKilled      = 0;
            m_EnemiesMissed      = 0;
            m_EnemiesActivated   = 0;
            m_TotalEnemiesTarget = 0;

            var allTargets = new List<AimTarget>();
            allTargets.AddRange(m_MovingTargets);
            allTargets.AddRange(m_StaticTargets);

            foreach (var t in allTargets)
                if (t != null && t.Type == TargetType.Enemy) m_TotalEnemiesTarget++;

            m_EnemiesActivated = m_TotalEnemiesTarget;

            ExperimentDataLogger.Instance.StartLogging(condition);

            foreach (var t in allTargets)
            {
                if (t == null) continue;
                t.OnHit          = (tgt) => HandleBattleEnemyKill(tgt);
                t.OnFriendlyFire = (tgt) => HandleFriendlyFire(tgt);
                t.Activate(persistAfterHit: false);
                if (t.Type == TargetType.Enemy) LogSpawn(t);
            }

            AimTrainerHUD.Instance?.UpdateRemainingEnemies(m_TotalEnemiesTarget);
            Debug.Log($"[AimTrainer] Battle Phase 시작 — 타겟 {allTargets.Count}개 (적 {m_TotalEnemiesTarget}개)");
        }

        public void StopBattlePhase()
        {
            m_IsRunning = false;
            StopAllCoroutines();
            var allTargets = new List<AimTarget>();
            allTargets.AddRange(m_MovingTargets);
            allTargets.AddRange(m_StaticTargets);
            foreach (var t in allTargets)
                if (t != null) t.gameObject.SetActive(false);
            ExperimentDataLogger.Instance.StopLogging();
            Debug.Log("[AimTrainer] Battle Phase 강제 종료");
        }

        void HandleBattleEnemyKill(AimTarget t)
        {
            float reaction = (Time.time - t.GetSpawnTime()) * 1000f;
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "Hit", reaction, t.transform.position, GetHeadRotation());
            AimTrainerHUD.Instance?.RegisterHit();
            m_EnemiesKilled++;
            AimTrainerHUD.Instance?.UpdateRemainingEnemies(m_TotalEnemiesTarget - m_EnemiesKilled);

            if (m_EnemiesKilled >= m_TotalEnemiesTarget)
            {
                m_IsRunning = false;
                ExperimentDataLogger.Instance.StopLogging();
                Debug.Log("[AimTrainer] Battle Phase 완료 — 전체 처치");
                OnAllEnemiesKilled?.Invoke();
            }
        }

        public int GetBattleEnemyCount()
        {
            int count = 0;
            foreach (var t in m_MovingTargets) if (t != null && t.Type == TargetType.Enemy) count++;
            foreach (var t in m_StaticTargets) if (t != null && t.Type == TargetType.Enemy) count++;
            return count;
        }

        // ═══════════════════════════════════════════════════════════════
        // Infinite Respawn Phase (80% 상시 활성화 유지)
        // ═══════════════════════════════════════════════════════════════

        List<AimTarget> m_AllPool = new();
        List<AimTarget> m_InactivePool = new();

        /// <summary>
        /// 씬에 배치된 모든 타겟 중 일정 비율(ratio)만 활성화한다.
        /// 적을 처치하면 즉시 비활성 풀에서 하나를 골라 리스폰시킨다.
        /// </summary>
        public void StartInfiniteRespawnPhase(string condition, float ratio = 0.8f)
        {
            if (m_IsRunning) return;
            m_IsRunning = true;
            m_Condition = condition;

            m_AllPool.Clear();
            m_AllPool.AddRange(m_MovingTargets);
            m_AllPool.AddRange(m_StaticTargets);
            m_InactivePool.Clear();
            m_InactivePool.AddRange(m_AllPool);

            // 전체 중 80% 개수 계산
            int targetActiveCount = Mathf.RoundToInt(m_AllPool.Count * ratio);
            targetActiveCount = Mathf.Clamp(targetActiveCount, 1, m_AllPool.Count);

            ExperimentDataLogger.Instance.StartLogging(condition);

            // 셔플 후 초기 80% 활성화
            ShuffleList(m_InactivePool);
            for (int i = 0; i < targetActiveCount; i++)
            {
                ActivateFromPool();
            }

            Debug.Log($"[AimTrainer] 무한 리스폰 시작 (총 {m_AllPool.Count}개 중 {targetActiveCount}개 상시 활성)");
        }

        void ActivateFromPool()
        {
            if (m_InactivePool.Count == 0) return;

            int randomIndex = Random.Range(0, m_InactivePool.Count);
            AimTarget t = m_InactivePool[randomIndex];
            m_InactivePool.RemoveAt(randomIndex);

            if (t != null)
            {
                t.OnHit = (tgt) => HandleRespawnHit(tgt);
                t.OnFriendlyFire = (tgt) => HandleFriendlyFire(tgt);
                
                // 타겟 타입에 따른 활성화 방식 결정
                if (t.GetComponentInChildren<AimTarget>().Type == TargetType.Enemy)
                {
                    // Moving 타겟이면 순찰 모드로, 아니면 그냥 활성화
                    t.Activate(persistAfterHit: false);
                    LogSpawn(t);
                }
                else
                {
                    // Friendly는 리스폰 로직에서 제외하거나 별도 관리 가능 (일단 활성화)
                    t.Activate(persistAfterHit: false);
                }
            }
        }

        void HandleRespawnHit(AimTarget t)
        {
            // 1. 데이터 기록
            float reaction = (Time.time - t.GetSpawnTime()) * 1000f;
            ExperimentDataLogger.Instance.LogEvent(m_Condition, "Hit", reaction, t.transform.position, GetHeadRotation());
            AimTrainerHUD.Instance?.RegisterHit();

            // 2. 처치된 타겟을 풀로 반환
            m_InactivePool.Add(t);

            // 3. 즉시 새로운 타겟 리스폰
            ActivateFromPool();
        }

        void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
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

        // ─── 레거시 호환 (외부 호출용) ─────────────────────────────────
        public void StartAimTrainer(string condition) => StartBattlePhase(condition);
        public void StopAimTrainer() { if (m_IsRunning) StopBattlePhase(); }
    }
}
