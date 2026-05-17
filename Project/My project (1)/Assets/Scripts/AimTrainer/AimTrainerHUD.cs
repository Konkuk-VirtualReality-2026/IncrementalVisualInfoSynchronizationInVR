using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRAdaptation.AimTrainer
{
    /// <summary>
    /// AimTrainer 단계에서 플레이어 시야 앞에 표시되는 점수 HUD.
    /// VRAdaptationManager.AimTrainer_Test 페이즈에 자동으로 표시/숨김.
    /// </summary>
    public class AimTrainerHUD : MonoBehaviour
    {
        public static AimTrainerHUD Instance { get; private set; }
        [Header("UI References")]
        [SerializeField] Text m_ScoreText;
        [SerializeField] Text m_AccuracyText;
        [SerializeField] Text m_TimerText;

        [Header("Tracking")]
        [SerializeField] Transform m_FollowTarget; // XR Camera

        [Header("Position")]
        [SerializeField] float m_Distance   = 2.0f;
        [SerializeField] float m_HeightOffset = -0.3f;

        int   m_Hits;
        int   m_Shots;
        float m_TimeRemaining;
        bool  m_Active;

        Canvas m_Canvas;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            m_Canvas = GetComponent<Canvas>();
            SetVisible(false);
        }

        void Update()
        {
            if (!m_Active) return;

            // HUD를 항상 카메라 정면 약간 아래에 위치
            if (m_FollowTarget != null)
            {
                Vector3 forward = m_FollowTarget.forward;
                forward.y = 0f;
                if (forward == Vector3.zero) forward = m_FollowTarget.forward;
                forward.Normalize();

                Vector3 target = m_FollowTarget.position
                               + forward * m_Distance
                               + Vector3.up * m_HeightOffset;
                transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5f);
                transform.LookAt(m_FollowTarget.position);
                transform.Rotate(0f, 180f, 0f);
            }

            // 타이머 감소
            if (m_TimeRemaining > 0f)
            {
                m_TimeRemaining -= Time.deltaTime;
                RefreshUI();
            }
        }

        public void StartHUD(float duration, Transform followTarget)
        {
            m_Hits          = 0;
            m_Shots         = 0;
            m_TimeRemaining = duration;
            m_FollowTarget  = followTarget;
            m_Active        = true;
            SetVisible(true);
            RefreshUI();
        }

        public void StopHUD()
        {
            m_Active = false;
            SetVisible(false);
        }

        public void RegisterShot()
        {
            m_Shots++;
            RefreshUI();
        }

        public void RegisterHit()
        {
            m_Hits++;
            RefreshUI();
        }

        void RefreshUI()
        {
            if (m_ScoreText    != null) m_ScoreText.text    = $"SCORE\n{m_Hits}";
            if (m_AccuracyText != null)
            {
                float acc = m_Shots > 0 ? (float)m_Hits / m_Shots * 100f : 0f;
                m_AccuracyText.text = $"ACC\n{acc:F0}%";
            }
            if (m_TimerText != null)
            {
                int sec = Mathf.CeilToInt(Mathf.Max(0f, m_TimeRemaining));
                m_TimerText.text = $"TIME\n{sec}s";
            }
        }

        void SetVisible(bool show)
        {
            if (m_Canvas != null) m_Canvas.enabled = show;
        }
    }
}
