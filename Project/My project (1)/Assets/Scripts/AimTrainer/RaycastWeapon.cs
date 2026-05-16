using UnityEngine;
using UnityEngine.InputSystem;

namespace VRAdaptation.AimTrainer
{
    public class RaycastWeapon : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] float m_MaxDistance = 50f;
        [SerializeField] LayerMask m_TargetLayer;
        [SerializeField] Transform m_MuzzlePoint;
        
        [Header("Effects")]
        [SerializeField] LineRenderer m_LaserPrefab;
        [SerializeField] float m_LaserDuration = 0.05f;
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_FireClip;
        [SerializeField] AudioClip m_HitClip;

        public void Fire()
        {
            if (m_MuzzlePoint == null) m_MuzzlePoint = transform;

            if (m_AudioSource && m_FireClip)
                m_AudioSource.PlayOneShot(m_FireClip);

            Ray ray = new Ray(m_MuzzlePoint.position, m_MuzzlePoint.forward);
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, m_MaxDistance, m_TargetLayer);

            ShowLaser(m_MuzzlePoint.position, hitSomething ? hit.point : m_MuzzlePoint.position + m_MuzzlePoint.forward * m_MaxDistance);

            if (hitSomething)
            {
                if (hit.collider.TryGetComponent(out AimTarget target))
                {
                    target.Hit();
                    if (m_AudioSource && m_HitClip)
                        m_AudioSource.PlayOneShot(m_HitClip);
                    
                    Debug.Log("[AimTrainer] Target Hit!");
                }
            }
        }

        private void ShowLaser(Vector3 start, Vector3 end)
        {
            if (m_LaserPrefab == null) return;
            
            LineRenderer laser = Instantiate(m_LaserPrefab, transform.position, Quaternion.identity);
            laser.SetPosition(0, start);
            laser.SetPosition(1, end);
            Destroy(laser.gameObject, m_LaserDuration);
        }
    }
}
