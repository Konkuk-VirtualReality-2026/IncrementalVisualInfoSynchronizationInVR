using UnityEngine;
using UnityEngine.Events;

namespace VRAdaptation.AimTrainer
{
    public class AimTarget : MonoBehaviour
    {
        public UnityAction<AimTarget> OnHit;
        public UnityAction<AimTarget> OnExpired;

        [SerializeField] float m_Lifetime = 2.0f;
        private float m_SpawnTime;
        private bool m_IsActive = false;

        public float GetSpawnTime() => m_SpawnTime;

        public void Activate(float lifetime)
        {
            m_Lifetime = lifetime;
            m_SpawnTime = Time.time;
            m_IsActive = true;
            gameObject.SetActive(true);
            
            // Visual feedback could be added here (e.g., popping effect)
        }

        void Update()
        {
            if (!m_IsActive) return;

            if (Time.time - m_SpawnTime >= m_Lifetime)
            {
                Expire();
            }
        }

        public void Hit()
        {
            if (!m_IsActive) return;
            m_IsActive = false;
            OnHit?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void Expire()
        {
            if (!m_IsActive) return;
            m_IsActive = false;
            OnExpired?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
