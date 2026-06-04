using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRAdaptation
{
    /// <summary>
    /// 플레이어가 이 Collider(Trigger)에 진입하면 OnPlayerEntered 이벤트를 발생시킨다.
    ///
    /// 사용법:
    ///   1. 빈 GameObject에 이 컴포넌트를 추가한다.
    ///   2. 같은 오브젝트에 Box/Sphere/Capsule Collider를 추가하고 Is Trigger = true 로 설정.
    ///   3. m_LinkedGlows 에 이 구간에서 보여줄 GlowGuidePoint 를 원하는 만큼 추가한다 (선택).
    ///   4. NavigationMission.m_Zones 리스트에 등록한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class PhaseCheckpointZone : MonoBehaviour
    {
        [Tooltip("이 구간 활성화 시 켜질 GlowGuidePoint 목록 — 여러 개 등록 가능")]
        [SerializeField] List<GlowGuidePoint> m_LinkedGlows = new();

        [Tooltip("활성화 여부 — NavigationMission이 순서대로 켜고 끔")]
        [SerializeField] bool m_StartActive = false;

        public UnityAction OnPlayerEntered;

        bool m_Listening = false;

        void Awake()
        {
            // Collider가 없으면 자동으로 Sphere Collider 추가
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.radius    = 1.0f;
                col.isTrigger = true;
            }
            else
            {
                // 기존 Collider의 isTrigger 강제 설정
                foreach (var col in GetComponents<Collider>())
                    col.isTrigger = true;
            }

            SetActive(m_StartActive);
        }

        // ── 외부 제어 ─────────────────────────────────────────────────────
        /// <summary>이 체크포인트를 활성화 (Glow 표시 + 트리거 수신 시작)</summary>
        public void SetActive(bool active)
        {
            m_Listening = active;
            foreach (var glow in m_LinkedGlows)
                if (glow != null) glow.SetVisible(active);
        }

        // ── 트리거 검출 ───────────────────────────────────────────────────
        void OnTriggerEnter(Collider other)
        {
            if (!m_Listening) return;
            if (!IsPlayer(other)) return;

            m_Listening = false; // 중복 발동 방지
            OnPlayerEntered?.Invoke();
        }

        static bool IsPlayer(Collider other)
        {
            // CharacterController, VRCharacterCollision, 또는 "Player" 태그로 판별
            if (other.CompareTag("Player")) return true;
            if (other.GetComponentInParent<VRCharacterCollision>() != null) return true;
            if (other.GetComponent<CharacterController>() != null) return true;
            return false;
        }

        // ── 에디터 시각화 ─────────────────────────────────────────────────
        void OnDrawGizmos()
        {
            Gizmos.color = m_Listening ? new Color(0.2f, 1f, 0.4f, 0.35f) : new Color(1f, 1f, 0f, 0.15f);
            var col = GetComponent<SphereCollider>();
            if (col != null)
                Gizmos.DrawSphere(transform.position, col.radius);
            else
                Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
