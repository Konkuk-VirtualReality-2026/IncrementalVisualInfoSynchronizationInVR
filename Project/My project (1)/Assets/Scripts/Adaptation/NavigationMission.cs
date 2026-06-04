using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRAdaptation
{
    /// <summary>
    /// 암흑/Edge 단계 맵 탐색 미션.
    ///
    /// m_Zones 리스트의 PhaseCheckpointZone 을 순서대로 활성화한다.
    /// 플레이어가 각 Zone에 진입하면 다음 Zone이 켜진다.
    /// 마지막 Zone에 진입하면 OnCompleted 이벤트 발생 → 다음 Phase로 전환.
    ///
    /// 사용법 (Inspector):
    ///   1. 빈 오브젝트에 PhaseCheckpointZone 컴포넌트 + Trigger Collider 추가.
    ///   2. 각 Zone의 m_LinkedGlow 에 GlowGuidePoint 연결 (선택).
    ///   3. m_Zones 리스트에 순서대로 등록 (마지막 = 최종 목적지).
    ///   4. VRAdaptationManager.m_Phase1NavMission / m_Phase2NavMission 에 연결.
    /// </summary>
    public class NavigationMission : MonoBehaviour
    {
        [Header("체크포인트 Zone 목록 (순서대로 — 마지막이 최종 목적지)")]
        [SerializeField] List<PhaseCheckpointZone> m_Zones = new();

        public UnityAction OnCompleted;

        int  m_CurrentIndex = -1;
        bool m_Active       = false;

        // ── 외부 API ─────────────────────────────────────────────────────

        /// <summary>미션 시작 — 첫 번째 Zone 활성화.</summary>
        public void Activate()
        {
            DeactivateAll();
            m_CurrentIndex = 0;
            m_Active       = true;
            ActivateCurrent();
            Debug.Log($"[NavMission] 시작 — Zone {m_Zones.Count}개");
        }

        /// <summary>미션 중단 — 모든 Zone 비활성화.</summary>
        public void Deactivate()
        {
            m_Active = false;
            DeactivateAll();
            Debug.Log("[NavMission] 비활성화");
        }

        // ── 내부 ────────────────────────────────────────────────────────

        void ActivateCurrent()
        {
            if (m_CurrentIndex < 0 || m_CurrentIndex >= m_Zones.Count) return;
            var zone = m_Zones[m_CurrentIndex];
            if (zone == null) { Advance(); return; }

            zone.SetActive(true);
            zone.OnPlayerEntered = OnZoneEntered;
        }

        void OnZoneEntered()
        {
            if (!m_Active) return;

            var current = m_Zones[m_CurrentIndex];
            if (current != null) current.SetActive(false);

            bool isLast = (m_CurrentIndex == m_Zones.Count - 1);
            if (isLast)
            {
                Complete();
            }
            else
            {
                Advance();
            }
        }

        void Advance()
        {
            m_CurrentIndex++;
            ActivateCurrent();
        }

        void Complete()
        {
            m_Active = false;
            DeactivateAll();
            Debug.Log("[NavMission] 최종 목적지 도달 — 완료!");
            OnCompleted?.Invoke();
        }

        void DeactivateAll()
        {
            foreach (var zone in m_Zones)
                if (zone != null) zone.SetActive(false);
        }
    }
}
