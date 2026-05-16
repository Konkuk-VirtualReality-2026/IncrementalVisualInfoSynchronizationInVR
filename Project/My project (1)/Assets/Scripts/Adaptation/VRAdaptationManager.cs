using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

namespace VRAdaptation
{
    public enum AdaptationPhase
    {
        None,
        Phase1_Static,       // Non-visual cues only (Audio/Haptic) — full blackout
        Phase2_Dynamic,      // Silhouette / outline view
        Phase3_HighFidelity, // Progressive visual quality increase
        AimTrainer_Test,     // Performance measurement
        Complete
    }

    [System.Serializable]
    public class PhaseChangeEvent : UnityEvent<AdaptationPhase> { }

    public class VRAdaptationManager : MonoBehaviour
    {
        public static VRAdaptationManager Instance { get; private set; }

        [Header("Events")]
        public PhaseChangeEvent OnPhaseChanged;

        [Header("Global Effect")]
        [SerializeField] GlobalAdaptationEffect m_GlobalEffect;

        [Header("Aim Trainer")]
        [SerializeField] AimTrainer.AimTargetManager m_AimTrainer;

        [Header("Phase Durations (Seconds)")]
        [SerializeField] float m_Phase1Duration = 30f;
        [SerializeField] float m_Phase2Duration = 30f;
        [SerializeField] float m_Phase3Duration = 30f;
        [SerializeField] float m_AimTrainerDuration = 120f;

        // Phase 1 uses a full-screen black UI panel (CanvasGroup) to produce true
        // darkness — the material-based shader cannot black out the skybox or the
        // space between objects, so a screen-covering overlay is required.
        [Header("Screen Blackout (Phase 1)")]
        [SerializeField] CanvasGroup m_BlackoutPanel;
        [SerializeField, Min(0.1f)] float m_BlackoutFadeDuration = 1.0f;

        [Header("Non-Visual Feedback (Phase 1/2)")]
        [SerializeField] AudioSource m_AmbientAudioSource;
        [SerializeField] AudioClip m_Phase1AmbientClip;
        [SerializeField] bool m_EnableHeartbeatHaptics = true;
        [SerializeField, Range(0f, 1f)] float m_HapticIntensity = 0.1f;

        [Header("Current State")]
        [SerializeField] AdaptationPhase m_CurrentPhase = AdaptationPhase.None;
        [SerializeField, Range(0f, 1f)] float m_CurrentFidelity = 0f;

        private static readonly int GlobalVisualFidelityID = Shader.PropertyToID("_GlobalVisualFidelity");

        // 카메라 배경 원본 복원을 위한 저장 변수
        private Camera        m_XRCamera;
        private CameraClearFlags m_OriginalClearFlags;
        private Color         m_OriginalBackgroundColor;

        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UpdateGlobalFidelity();
            }
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (m_AmbientAudioSource == null)
                m_AmbientAudioSource = GetComponent<AudioSource>();

            // XR 카메라 원본 설정 저장
            m_XRCamera = Camera.main;
            if (m_XRCamera != null)
            {
                m_OriginalClearFlags       = m_XRCamera.clearFlags;
                m_OriginalBackgroundColor  = m_XRCamera.backgroundColor;
            }
        }

        void Start()
        {
            StartCoroutine(AdaptationSequence());
            if (m_EnableHeartbeatHaptics)
                StartCoroutine(HeartbeatHapticsRoutine());
        }

        // Phase 1·2 동안 스카이박스 대신 순수 검정을 배경으로 설정한다.
        // 셰이더는 오브젝트 표면만 제어하므로 배경(스카이박스)을 별도로 차단하지 않으면
        // 오브젝트 사이 빈 공간으로 하늘이 그대로 보여 실루엣 효과가 깨진다.
        void SetCameraBackground(bool blackout)
        {
            if (m_XRCamera == null) return;
            if (blackout)
            {
                m_XRCamera.clearFlags       = CameraClearFlags.SolidColor;
                m_XRCamera.backgroundColor  = Color.black;
            }
            else
            {
                m_XRCamera.clearFlags       = m_OriginalClearFlags;
                m_XRCamera.backgroundColor  = m_OriginalBackgroundColor;
            }
        }

        IEnumerator AdaptationSequence()
        {
            // --- Phase 1: Full Blackout + Audio/Haptic cues only ---
            // The blackout panel covers the entire screen so the user truly cannot
            // see anything — the object-level shader alone cannot achieve this because
            // the skybox and inter-object space would remain visible.
            m_CurrentPhase = AdaptationPhase.Phase1_Static;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            m_CurrentFidelity = 0f;
            UpdateGlobalFidelity();

            SetCameraBackground(blackout: true);   // 스카이박스 → 검정 배경
            if (m_BlackoutPanel != null)
                m_BlackoutPanel.alpha = 1f;

            if (m_AmbientAudioSource != null && m_Phase1AmbientClip != null)
            {
                m_AmbientAudioSource.clip = m_Phase1AmbientClip;
                m_AmbientAudioSource.loop = true;
                m_AmbientAudioSource.Play();
            }

            Debug.Log("[VRAdaptation] Starting Phase 1: Full blackout (audio/haptic only)");
            yield return new WaitForSeconds(m_Phase1Duration);

            // --- Phase 2: Silhouette / Outline view ---
            // Snap fidelity to 0.3 (peak rim/silhouette) immediately, then fade the
            // blackout panel out so the silhouette becomes visible right away.
            m_CurrentPhase = AdaptationPhase.Phase2_Dynamic;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            m_CurrentFidelity = 0.3f;
            UpdateGlobalFidelity();
            Debug.Log("[VRAdaptation] Starting Phase 2: Silhouette/Outline view");

            // Fade out the blackout while the silhouette shader is already active
            if (m_BlackoutPanel != null)
                yield return StartCoroutine(FadeBlackout(1f, 0f, m_BlackoutFadeDuration));

            yield return new WaitForSeconds(m_Phase2Duration);

            // --- Phase 3: Progressive quality (Outline → Full PBR) ---
            // 이 시점에서 카메라 배경을 원본(스카이박스)으로 복원한다.
            // Phase 3는 텍스처·조명이 점점 살아나는 구간이므로 배경도 자연스럽게 등장해야 한다.
            SetCameraBackground(blackout: false);
            m_CurrentPhase = AdaptationPhase.Phase3_HighFidelity;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            Debug.Log("[VRAdaptation] Starting Phase 3: Progressive quality increase");
            yield return StartCoroutine(SmoothFidelityTransition(0.3f, 1.0f, m_Phase3Duration));

            // --- Aim Trainer Test Phase ---
            m_CurrentPhase = AdaptationPhase.AimTrainer_Test;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            if (m_AimTrainer != null)
                m_AimTrainer.StartAimTrainer("PostAdaptation");
            
            Debug.Log("[VRAdaptation] Starting Aim Trainer Test Phase");
            yield return new WaitForSeconds(m_AimTrainerDuration);
            
            if (m_AimTrainer != null)
                m_AimTrainer.StopAimTrainer();

            m_CurrentPhase = AdaptationPhase.Complete;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            m_CurrentFidelity = 1.0f;
            UpdateGlobalFidelity();

            if (m_GlobalEffect != null)
                m_GlobalEffect.RestoreEffect();

            Debug.Log("[VRAdaptation] Adaptation Complete. Restored original materials.");
        }

        IEnumerator HeartbeatHapticsRoutine()
        {
            while (m_CurrentPhase != AdaptationPhase.Complete && m_CurrentPhase != AdaptationPhase.None)
            {
                // Simple rhythmic pulse to provide a 'Rest Frame' or sense of presence
                // Only active during Phase 1 and 2
                if (m_CurrentPhase == AdaptationPhase.Phase1_Static || m_CurrentPhase == AdaptationPhase.Phase2_Dynamic)
                {
                    TriggerHapticPulse(m_HapticIntensity, 0.1f);
                    yield return new WaitForSeconds(1.5f); // Pulse every 1.5 seconds
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        void TriggerHapticPulse(float intensity, float duration)
        {
            // Implementation depends on XRI version, typically via XRController or ActionBasedController
            // For now, we log it. In a real VR build, this would use OpenXR/XRI Haptic APIs.
            // Debug.Log($"[VRAdaptation] Haptic Pulse: {intensity}");
        }

        IEnumerator FadeBlackout(float fromAlpha, float toAlpha, float duration)
        {
            if (m_BlackoutPanel == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_BlackoutPanel.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                yield return null;
            }
            m_BlackoutPanel.alpha = toAlpha;
        }

        IEnumerator SmoothFidelityTransition(float start, float end, float duration)
        {
            float elapsed = 0f;
            float lastLogTime = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_CurrentFidelity = Mathf.Lerp(start, end, elapsed / duration);
                UpdateGlobalFidelity();

                // Log every 1 second
                if (Time.time - lastLogTime > 1f)
                {
                    Debug.Log($"[VRAdaptation] {m_CurrentPhase} - Fidelity: {m_CurrentFidelity:F2}");
                    lastLogTime = Time.time;
                }
                yield return null;
            }
            m_CurrentFidelity = end;
            UpdateGlobalFidelity();
        }

        void UpdateGlobalFidelity()
        {
            Shader.SetGlobalFloat(GlobalVisualFidelityID, m_CurrentFidelity);
        }

        // Methods to be called by other systems (Audio/Haptics)
        public AdaptationPhase GetCurrentPhase() => m_CurrentPhase;
        public float GetCurrentFidelity() => m_CurrentFidelity;
    }
}
