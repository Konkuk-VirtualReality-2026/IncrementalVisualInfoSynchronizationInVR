using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace VRAdaptation.AimTrainer
{
    /// <summary>
    /// AimTrainer 단계에서 플레이어 시야 앞에 표시되는 점수 HUD.
    ///
    /// Moving Phase: 점수(히트 수) + 정확도 + 남은 시간 표시.
    /// Static Phase: 남은 적 수 + 패널티(오발) + 타이머 숨김.
    /// </summary>
    public class AimTrainerHUD : MonoBehaviour
    {
        public static AimTrainerHUD Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] Text m_ScoreText;
        [SerializeField] Text m_AccuracyText;
        [SerializeField] Text m_TimerText;
        [SerializeField] Text m_RemainingText;  // 남은 적 (Static Phase)
        [SerializeField] Text m_PenaltyText;    // 오발 횟수

        [Header("Tracking")]
        [SerializeField] Transform m_FollowTarget;

        [Header("Position")]
        [SerializeField] float m_Distance    = 2.0f;
        [SerializeField] float m_HeightOffset = -0.3f;

        int   m_Hits;
        int   m_Shots;
        int   m_Penalties;
        float m_TimeRemaining;
        bool  m_Active;
        bool  m_IsStaticMode;

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

            if (!m_IsStaticMode && m_TimeRemaining > 0f)
            {
                m_TimeRemaining -= Time.deltaTime;
                RefreshUI();
            }
        }

        // ── Moving Phase HUD ─────────────────────────────────────────────
        public void StartHUD(float duration, Transform followTarget)
        {
            m_Hits          = 0;
            m_Shots         = 0;
            m_Penalties     = 0;
            m_TimeRemaining = duration;
            m_FollowTarget  = followTarget;
            m_Active        = true;
            m_IsStaticMode  = false;
            SetVisible(true);

            if (m_RemainingText != null) m_RemainingText.gameObject.SetActive(false);
            if (m_TimerText     != null) m_TimerText.gameObject.SetActive(true);
            RefreshUI();
        }

        // ── Static Phase HUD ─────────────────────────────────────────────
        public void StartStaticHUD(int totalEnemies, Transform followTarget)
        {
            m_Hits         = 0;
            m_Shots        = 0;
            m_Penalties    = 0;
            m_FollowTarget = followTarget;
            m_Active       = true;
            m_IsStaticMode = true;
            SetVisible(true);

            if (m_TimerText     != null) m_TimerText.gameObject.SetActive(false);
            if (m_RemainingText != null) m_RemainingText.gameObject.SetActive(true);

            UpdateRemainingEnemies(totalEnemies);
            RefreshUI();
        }

        public void StartTimerHUD(float duration, Transform followTarget)
        {
            m_Hits          = 0;
            m_Shots         = 0;
            m_Penalties     = 0;
            m_TimeRemaining = duration;
            m_FollowTarget  = followTarget;
            m_Active        = true;
            m_IsStaticMode  = false;
            SetVisible(true);

            if (m_RemainingText != null) m_RemainingText.gameObject.SetActive(false);
            if (m_TimerText     != null) m_TimerText.gameObject.SetActive(true);
            RefreshUI();
        }

        public void StopHUD()
        {
            m_Active = false;
            SetVisible(false);
        }

        // ── 외부 호출 ────────────────────────────────────────────────────
        public void RegisterShot()
        {
            m_Shots++;
            RefreshUI();
        }

        public void RegisterHit()
        {
            m_Hits++;
            m_Shots++;
            RefreshUI();
        }

        public void RegisterPenalty()
        {
            m_Penalties++;
            m_Shots++;
            RefreshUI();
        }

        public void UpdateRemainingEnemies(int remaining)
        {
            if (m_RemainingText != null)
                m_RemainingText.text = $"남은 적\n{remaining}";
        }

        // ─────────────────────────────────────────────────────────────────
        void RefreshUI()
        {
            if (m_ScoreText != null)
                m_ScoreText.text = $"SCORE\n{m_Hits}";

            if (m_AccuracyText != null)
            {
                float acc = m_Shots > 0 ? (float)m_Hits / m_Shots * 100f : 0f;
                m_AccuracyText.text = $"ACC\n{acc:F0}%";
            }

            if (!m_IsStaticMode && m_TimerText != null)
            {
                int sec = Mathf.CeilToInt(Mathf.Max(0f, m_TimeRemaining));
                m_TimerText.text = $"TIME\n{sec}s";
            }

            if (m_PenaltyText != null)
                m_PenaltyText.text = m_Penalties > 0 ? $"오발\n-{m_Penalties}" : "";
        }

        void SetVisible(bool show)
        {
            if (m_Canvas != null) m_Canvas.enabled = show;
        }
    }
}
