using UnityEngine;

namespace VRAdaptation
{
    /// <summary>
    /// XR Origin에 부착. CharacterController의 중력 적용을 담당한다.
    /// ContinuousMoveProvider(XRI 3.x)는 같은 GameObject에 CharacterController가 있을 때
    /// characterController.Move()로 분기하여 벽 충돌을 자동 처리한다.
    /// 이 스크립트는 중력만 관리하며, 향후 벽 진입 피드백 확장 포인트로 사용한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class VRCharacterCollision : MonoBehaviour
    {
        [SerializeField, Range(0f, 3f)] float m_GravityScale = 1f;

        [Header("벽 진입 피드백 (미구현 — 확장 포인트)")]
        [SerializeField] bool m_EnableWallEntryFeedback = false;

        CharacterController m_CC;
        float m_VerticalVelocity;

        void Awake()
        {
            m_CC = GetComponent<CharacterController>();
        }

        void Update()
        {
            // isGrounded 시 작은 하향력으로 지면 밀착 유지
            if (m_CC.isGrounded)
                m_VerticalVelocity = -0.5f;
            else
                m_VerticalVelocity += Physics.gravity.y * m_GravityScale * Time.deltaTime;

            m_CC.Move(new Vector3(0f, m_VerticalVelocity * Time.deltaTime, 0f));
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!m_EnableWallEntryFeedback) return;
            // TODO: 벽 내부 진입 시 소리/햅틱 피드백 (DEV_PLAN 참고)
        }
    }
}
