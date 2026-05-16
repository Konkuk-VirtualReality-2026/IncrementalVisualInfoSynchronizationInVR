using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VRAdaptation.AimTrainer
{
    public class AimTargetManager : MonoBehaviour
    {
        public static AimTargetManager Instance { get; private set; }

        [Header("Spawn Configuration")]
        [SerializeField] AimTarget m_TargetPrefab;
        [SerializeField] float m_SpawnDistance = 3f;
        [SerializeField] Vector2 m_HorizontalAngleRange = new Vector2(-45f, 45f);
        [SerializeField] Vector2 m_VerticalAngleRange = new Vector2(-20f, 40f);
        [SerializeField] float m_TargetLifetime = 2.0f;
        [SerializeField] float m_SpawnInterval = 1.5f;

        [Header("Experiment Info")]
        [SerializeField] Transform m_PlayerHead; // Reference to Main Camera / HMD
        
        private bool m_IsRunning = false;
        private List<AimTarget> m_ActiveTargets = new List<AimTarget>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartAimTrainer(string condition)
        {
            if (m_IsRunning) return;
            m_IsRunning = true;
            
            ExperimentDataLogger.Instance.StartLogging(condition);
            StartCoroutine(SpawnRoutine(condition));
            Debug.Log($"[AimTrainer] Started for condition: {condition}");
        }

        public void StopAimTrainer()
        {
            m_IsRunning = false;
            StopAllCoroutines();
            ExperimentDataLogger.Instance.StopLogging();
            
            foreach (var target in m_ActiveTargets)
            {
                if (target != null) target.gameObject.SetActive(false);
            }
            m_ActiveTargets.Clear();
            Debug.Log("[AimTrainer] Stopped.");
        }

        IEnumerator SpawnRoutine(string condition)
        {
            while (m_IsRunning)
            {
                SpawnTarget(condition);
                yield return new WaitForSeconds(m_SpawnInterval);
            }
        }

        private void SpawnTarget(string condition)
        {
            if (m_TargetPrefab == null) return;

            // Calculate random position in front of player
            float hAngle = Random.Range(m_HorizontalAngleRange.x, m_HorizontalAngleRange.y);
            float vAngle = Random.Range(m_VerticalAngleRange.x, m_VerticalAngleRange.y);
            
            Quaternion rotation = Quaternion.Euler(-vAngle, hAngle, 0);
            Vector3 position = (m_PlayerHead != null ? m_PlayerHead.position : transform.position) + (rotation * Vector3.forward * m_SpawnDistance);

            AimTarget target = Instantiate(m_TargetPrefab, position, Quaternion.LookRotation(position - m_PlayerHead.position));
            target.Activate(m_TargetLifetime);
            
            target.OnHit = (t) => HandleTargetHit(t, condition);
            target.OnExpired = (t) => HandleTargetMiss(t, condition);
            
            m_ActiveTargets.Add(target);
            
            ExperimentDataLogger.Instance.LogEvent(condition, "Spawn", 0, position, GetHeadRotation());
        }

        private void HandleTargetHit(AimTarget target, string condition)
        {
            float reactionTime = (Time.time - target.GetSpawnTime()) * 1000f; // ms
            ExperimentDataLogger.Instance.LogEvent(condition, "Hit", reactionTime, target.transform.position, GetHeadRotation());
            m_ActiveTargets.Remove(target);
            Destroy(target.gameObject);
        }

        private void HandleTargetMiss(AimTarget target, string condition)
        {
            ExperimentDataLogger.Instance.LogEvent(condition, "Miss", m_TargetLifetime * 1000f, target.transform.position, GetHeadRotation());
            m_ActiveTargets.Remove(target);
            Destroy(target.gameObject);
        }

        private Vector3 GetHeadRotation()
        {
            return m_PlayerHead != null ? m_PlayerHead.eulerAngles : Vector3.zero;
        }
    }
}
