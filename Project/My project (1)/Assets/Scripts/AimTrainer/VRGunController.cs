using UnityEngine;
using UnityEngine.InputSystem;

namespace VRAdaptation.AimTrainer
{
    /// <summary>
    /// XRI 컨트롤러 트리거 입력 → RaycastWeapon.Fire() 연결.
    /// 반동 햅틱은 RaycastWeapon 내부에서 처리.
    /// </summary>
    [RequireComponent(typeof(RaycastWeapon))]
    public class VRGunController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("XRI Right Hand Interaction/Activate 액션을 연결한다.")]
        [SerializeField] InputActionReference m_FireAction;

        [Header("발사 설정")]
        [SerializeField, Min(0f)] float m_FireCooldown = 0.15f;

        RaycastWeapon m_Weapon;
        float         m_LastFireTime = -999f;

        void Awake()  => m_Weapon = GetComponent<RaycastWeapon>();

        void OnEnable()
        {
            if (m_FireAction == null) return;
            m_FireAction.action.Enable();
            m_FireAction.action.performed += OnFirePerformed;
        }

        void OnDisable()
        {
            if (m_FireAction == null) return;
            m_FireAction.action.performed -= OnFirePerformed;
        }

        void OnFirePerformed(InputAction.CallbackContext ctx)
        {
            if (Time.time - m_LastFireTime < m_FireCooldown) return;
            m_LastFireTime = Time.time;

            bool isHit = m_Weapon.Fire();

            // HUD: 샷은 항상, 히트는 AimTargetManager의 OnHit 콜백에서 처리
            AimTrainerHUD.Instance?.RegisterShot();
        }
    }
}
