using UnityEngine;
using UnityEngine.InputSystem;

namespace VRAdaptation.AimTrainer
{
    /// <summary>
    /// XRI 컨트롤러 트리거 입력을 RaycastWeapon.Fire()로 연결한다.
    /// Right Hand Controller 오브젝트에 RaycastWeapon과 함께 부착한다.
    /// </summary>
    [RequireComponent(typeof(RaycastWeapon))]
    public class VRGunController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("XRI Right Hand Interaction/Activate 액션을 연결한다.")]
        [SerializeField] InputActionReference m_FireAction;

        [Header("Cooldown")]
        [SerializeField, Min(0f)] float m_FireCooldown = 0.15f;

        RaycastWeapon m_Weapon;
        float m_LastFireTime = -999f;

        void Awake()
        {
            m_Weapon = GetComponent<RaycastWeapon>();
        }

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
            m_Weapon.Fire();
            AimTrainerHUD.Instance?.RegisterShot();
        }
    }
}
