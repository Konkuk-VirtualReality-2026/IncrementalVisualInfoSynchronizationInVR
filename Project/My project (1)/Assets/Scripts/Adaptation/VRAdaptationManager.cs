using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using VRAdaptation.Experiment;

namespace VRAdaptation
{
    public enum AdaptationPhase
    {
        None,
        Phase1_Blackout,      // 암흑 + 네비게이션 미션 (2군, 최대 2분)
        Phase2_Edge,          // 엣지/실루엣 + 네비게이션 미션 (2군, 최대 2분)
        AimTrainer_Moving,    // 이동식 타겟 순찰 (양군, 2분)
        AimTrainer_Static,    // 사방 타겟 전체 처치 (양군, 무제한)
        Complete
    }

    [System.Serializable]
    public class PhaseChangeEvent : UnityEvent<AdaptationPhase> { }

    // GlobalAdaptationEffect.Awake() 보다 먼저 실행되도록 우선순위 설정
    [DefaultExecutionOrder(-100)]
    public class VRAdaptationManager : MonoBehaviour
    {
        public static VRAdaptationManager Instance { get; private set; }

        [Header("Events")]
        public PhaseChangeEvent OnPhaseChanged;

        [Header("Global Effect")]
        [SerializeField] GlobalAdaptationEffect m_GlobalEffect;

        [Header("Aim Trainer")]
        [SerializeField] AimTrainer.AimTargetManager m_AimTrainer;
        [SerializeField] AimTrainer.AimTrainerHUD    m_AimTrainerHUD;

        [Header("Navigation Missions (2군 전용)")]
        [SerializeField] NavigationMission m_Phase1NavMission;
        [SerializeField] NavigationMission m_Phase2NavMission;

        [Header("플레이어 스폰 위치")]
        [Tooltip("각 Phase 시작 시 플레이어를 이 위치로 리셋한다")]
        [SerializeField] Transform m_PlayerSpawnPoint;

        [Header("Phase Durations (초) — NavMission은 도달 즉시 종료, 여기서는 Moving만 사용")]
        [Tooltip("Moving Phase 시간 제한")]
        [SerializeField] float m_MovingDuration   = 120f; // 2분
        // Static Phase 는 전체 처치 시 자동 종료 — 시간 제한 없음

        [Header("Experiment Condition")]
        [Tooltip("대조군(1군)으로 강제 설정 (Inspector 디버그용)")]
        [SerializeField] bool m_ForceControlGroup = false;

        [Header("Debug")]
        [Tooltip("true 시 모든 Phase 시간을 1/10 로 단축")]
        [SerializeField] bool m_DebugFastMode = false;

        [Header("Screen Blackout (legacy — kept transparent)")]
        [SerializeField] CanvasGroup m_BlackoutPanel;

        [Header("World Lighting (Phase 1/2 암흑)")]
        [SerializeField] List<Light> m_WorldLights = new();
        [SerializeField] bool        m_DisableAmbientDuringDark = true;

        [Header("Non-Visual Feedback (Phase 1/2)")]
        [SerializeField] AudioSource m_AmbientAudioSource;
        [SerializeField] AudioClip   m_Phase1AmbientClip;
        [SerializeField, Range(0f, 1f)] float m_HapticIntensity = 0.1f;

        [Header("Current State (읽기 전용)")]
        [SerializeField] AdaptationPhase m_CurrentPhase    = AdaptationPhase.None;
        [SerializeField, Range(0f, 1f)] float m_CurrentFidelity = 0f;

        // ── 내부 저장 변수 ────────────────────────────────────────────────
        static readonly int GlobalVisualFidelityID = Shader.PropertyToID("_GlobalVisualFidelity");

        Camera           m_XRCamera;
        CameraClearFlags m_OriginalClearFlags;
        Color            m_OriginalBackgroundColor;

        Color m_OrigAmbientLight;
        float m_OrigAmbientIntensity;
        bool  m_LightingCaptured;

        // ── 생명주기 ─────────────────────────────────────────────────────
        void OnValidate()
        {
            if (!Application.isPlaying) UpdateGlobalFidelity();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (m_AmbientAudioSource == null)
                m_AmbientAudioSource = GetComponent<AudioSource>();

            m_XRCamera = Camera.main;
            if (m_XRCamera != null)
            {
                m_OriginalClearFlags      = m_XRCamera.clearFlags;
                m_OriginalBackgroundColor = m_XRCamera.backgroundColor;
            }

            // 대조군은 Awake 단계에서 fidelity=1.0 으로 초기화
            bool isControlEarly = m_ForceControlGroup
                || ExperimentCondition.SelectedGroup == ExperimentGroup.Control;
            if (isControlEarly)
            {
                m_CurrentFidelity = 1.0f;
                UpdateGlobalFidelity();
                if (m_BlackoutPanel != null) m_BlackoutPanel.alpha = 0f;
            }
        }

        void Start()
        {
            if (ExperimentCondition.SelectedGroup == ExperimentGroup.NotSelected && !m_ForceControlGroup)
                Debug.LogWarning("[VRAdaptation] 실험 그룹 미선택 — 기본값(실험군) 진행");

            CaptureLighting();

            if (m_BlackoutPanel != null)
            {
                m_BlackoutPanel.alpha          = 0f;
                m_BlackoutPanel.blocksRaycasts = false;
                m_BlackoutPanel.interactable   = false;
            }

            bool isControl = m_ForceControlGroup
                || ExperimentCondition.SelectedGroup == ExperimentGroup.Control;

            if (isControl)
            {
                m_CurrentFidelity = 1.0f;
                UpdateGlobalFidelity();
                SetCameraBackground(blackout: false);
                if (m_GlobalEffect != null) m_GlobalEffect.RestoreEffect();
                StartCoroutine(ControlSequence());
            }
            else
            {
                StartCoroutine(AdaptationSequence());
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 1군 (Control) 시퀀스
        // ═══════════════════════════════════════════════════════════════
        IEnumerator ControlSequence()
        {
            // 처음부터 완전 시각
            SetCameraBackground(blackout: false);
            SetWorldLighting(true);
            m_CurrentFidelity = 1.0f;
            UpdateGlobalFidelity();
            if (m_GlobalEffect != null) m_GlobalEffect.RestoreEffect();

            yield return null; // 렌더링 반영 대기

            // ── Moving Phase (2분) ──────────────────────────────────────
            m_CurrentPhase = AdaptationPhase.AimTrainer_Moving;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            Debug.Log("[VRAdaptation] 1군 Moving Phase 시작");

            if (m_AimTrainer != null)
                m_AimTrainer.StartMovingPhase(ExperimentCondition.GetConditionName());
            if (m_AimTrainerHUD != null)
                m_AimTrainerHUD.StartHUD(D(m_MovingDuration), m_XRCamera?.transform);

            yield return new WaitForSeconds(D(m_MovingDuration));

            if (m_AimTrainer != null) m_AimTrainer.StopMovingPhase();
            if (m_AimTrainerHUD != null) m_AimTrainerHUD.StopHUD();

            // Moving → Static 전환 시 스폰 위치로 리셋
            ResetPlayerToSpawn();
            yield return null;

            // ── Static Phase (전체 처치) ────────────────────────────────
            yield return StartCoroutine(RunStaticPhase());

            // ── 완료 ───────────────────────────────────────────────────
            FinishExperiment();
        }

        // ═══════════════════════════════════════════════════════════════
        // 2군 (Adaptation) 시퀀스
        // ═══════════════════════════════════════════════════════════════
        IEnumerator AdaptationSequence()
        {
            // ── Phase 1: 암흑 + 네비게이션 미션 ────────────────────────
            m_CurrentPhase    = AdaptationPhase.Phase1_Blackout;
            m_CurrentFidelity = 0f;
            UpdateGlobalFidelity();
            OnPhaseChanged.Invoke(m_CurrentPhase);

            SetCameraBackground(blackout: true);
            SetWorldLighting(false);

            if (m_AmbientAudioSource != null && m_Phase1AmbientClip != null)
            {
                m_AmbientAudioSource.clip = m_Phase1AmbientClip;
                m_AmbientAudioSource.loop = true;
                m_AmbientAudioSource.Play();
            }

            Debug.Log("[VRAdaptation] Phase1 Blackout 시작");
            yield return RunNavMission(m_Phase1NavMission);

            // Phase1 완료 → 스폰 위치로 리셋
            ResetPlayerToSpawn();
            yield return null;

            // ── Phase 2: Edge + 네비게이션 미션 ────────────────────────
            m_CurrentPhase    = AdaptationPhase.Phase2_Edge;
            m_CurrentFidelity = 0.3f;
            UpdateGlobalFidelity();
            OnPhaseChanged.Invoke(m_CurrentPhase);

            Debug.Log("[VRAdaptation] Phase2 Edge 시작");
            yield return RunNavMission(m_Phase2NavMission);

            // Phase2 완료 → 스폰 위치로 리셋
            ResetPlayerToSpawn();
            yield return null;

            // 조명/배경 복원 (AimTrainer 단계는 완전 시각)
            SetCameraBackground(blackout: false);
            SetWorldLighting(true);
            m_CurrentFidelity = 1.0f;
            UpdateGlobalFidelity();
            if (m_GlobalEffect != null) m_GlobalEffect.RestoreEffect();

            if (m_AmbientAudioSource != null) m_AmbientAudioSource.Stop();

            yield return null;

            // ── Moving Phase (2분) ──────────────────────────────────────
            m_CurrentPhase = AdaptationPhase.AimTrainer_Moving;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            Debug.Log("[VRAdaptation] 2군 Moving Phase 시작");

            if (m_AimTrainer != null)
                m_AimTrainer.StartMovingPhase(ExperimentCondition.GetConditionName());
            if (m_AimTrainerHUD != null)
                m_AimTrainerHUD.StartHUD(D(m_MovingDuration), m_XRCamera?.transform);

            yield return new WaitForSeconds(D(m_MovingDuration));

            if (m_AimTrainer != null) m_AimTrainer.StopMovingPhase();
            if (m_AimTrainerHUD != null) m_AimTrainerHUD.StopHUD();

            // Moving → Static 전환 시 스폰 위치로 리셋
            ResetPlayerToSpawn();
            yield return null;

            // ── Static Phase (전체 처치) ────────────────────────────────
            yield return StartCoroutine(RunStaticPhase());

            // ── 완료 ───────────────────────────────────────────────────
            FinishExperiment();
        }

        // ═══════════════════════════════════════════════════════════════
        // 공통 Static Phase 코루틴
        // ═══════════════════════════════════════════════════════════════
        IEnumerator RunStaticPhase()
        {
            m_CurrentPhase = AdaptationPhase.AimTrainer_Static;
            OnPhaseChanged.Invoke(m_CurrentPhase);
            Debug.Log("[VRAdaptation] Static Phase 시작 — 전체 처치까지 대기");

            bool allKilled = false;

            if (m_AimTrainer != null)
            {
                m_AimTrainer.OnAllEnemiesKilled = () => allKilled = true;
                m_AimTrainer.StartStaticPhase(ExperimentCondition.GetConditionName() + "_Static");
            }

            if (m_AimTrainerHUD != null)
                m_AimTrainerHUD.StartStaticHUD(
                    m_AimTrainer != null ? GetTotalEnemies() : 0,
                    m_XRCamera?.transform);

            // 전체 처치까지 대기 (시간 제한 없음)
            while (!allKilled) yield return null;

            if (m_AimTrainer != null) m_AimTrainer.StopStaticPhase();
            if (m_AimTrainerHUD != null) m_AimTrainerHUD.StopHUD();

            Debug.Log("[VRAdaptation] Static Phase 완료");
        }

        // ═══════════════════════════════════════════════════════════════
        // 네비게이션 미션 코루틴 (체크포인트 도달 시 즉시 종료)
        // ═══════════════════════════════════════════════════════════════
        IEnumerator RunNavMission(NavigationMission mission)
        {
            bool completed = false;

            if (mission != null)
            {
                mission.OnCompleted = () => completed = true;
                mission.Activate();
            }
            else
            {
                // 미션 미설정 시 즉시 통과
                Debug.LogWarning("[VRAdaptation] NavMission 미설정 — 즉시 다음 Phase로 진행");
                yield break;
            }

            // 최종 Zone 진입까지 무한 대기
            while (!completed) yield return null;

            mission.Deactivate();
            Debug.Log("[VRAdaptation] 네비게이션 미션 완료 (체크포인트 도달)");
        }

        // ═══════════════════════════════════════════════════════════════
        // 플레이어 리셋 (XR Origin 을 스폰 위치로 순간이동)
        // ═══════════════════════════════════════════════════════════════
        void ResetPlayerToSpawn()
        {
            if (m_PlayerSpawnPoint == null) return;

            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                // CharacterController 가 있으면 잠깐 끄고 이동 (이동 중 충돌 방지)
                var cc = xrOrigin.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                xrOrigin.transform.SetPositionAndRotation(
                    m_PlayerSpawnPoint.position,
                    m_PlayerSpawnPoint.rotation);

                if (cc != null) cc.enabled = true;

                Debug.Log("[VRAdaptation] 플레이어 스폰 위치로 리셋");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 종료
        // ═══════════════════════════════════════════════════════════════
        void FinishExperiment()
        {
            m_CurrentPhase    = AdaptationPhase.Complete;
            m_CurrentFidelity = 1.0f;
            UpdateGlobalFidelity();
            OnPhaseChanged.Invoke(m_CurrentPhase);
            if (m_GlobalEffect != null) m_GlobalEffect.RestoreEffect();
            Debug.Log("[VRAdaptation] 실험 완료.");
        }

        // ═══════════════════════════════════════════════════════════════
        // 조명 / 배경 유틸리티
        // ═══════════════════════════════════════════════════════════════
        void SetCameraBackground(bool blackout)
        {
            if (m_XRCamera == null) return;
            if (blackout)
            {
                m_XRCamera.clearFlags      = CameraClearFlags.SolidColor;
                m_XRCamera.backgroundColor = Color.black;
            }
            else
            {
                m_XRCamera.clearFlags      = m_OriginalClearFlags;
                m_XRCamera.backgroundColor = m_OriginalBackgroundColor;
            }
        }

        void CaptureLighting()
        {
            if (m_LightingCaptured) return;
            if (m_WorldLights == null || m_WorldLights.Count == 0)
                m_WorldLights = new List<Light>(FindObjectsByType<Light>(FindObjectsSortMode.None));

            m_OrigAmbientLight     = RenderSettings.ambientLight;
            m_OrigAmbientIntensity = RenderSettings.ambientIntensity;
            m_LightingCaptured     = true;
        }

        void SetWorldLighting(bool on)
        {
            if (m_WorldLights != null)
                foreach (var l in m_WorldLights)
                    if (l != null) l.enabled = on;

            if (m_DisableAmbientDuringDark && m_LightingCaptured)
            {
                if (on)
                {
                    RenderSettings.ambientLight     = m_OrigAmbientLight;
                    RenderSettings.ambientIntensity = m_OrigAmbientIntensity;
                }
                else
                {
                    RenderSettings.ambientLight     = Color.black;
                    RenderSettings.ambientIntensity = 0f;
                }
            }
        }

        float D(float seconds) => m_DebugFastMode ? seconds * 0.1f : seconds;

        int GetTotalEnemies() =>
            m_AimTrainer != null ? m_AimTrainer.GetStaticTargetCount() : 0;

        IEnumerator SmoothFidelityTransition(float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_CurrentFidelity = Mathf.Lerp(start, end, elapsed / duration);
                UpdateGlobalFidelity();
                yield return null;
            }
            m_CurrentFidelity = end;
            UpdateGlobalFidelity();
        }

        void UpdateGlobalFidelity()
        {
            Shader.SetGlobalFloat(GlobalVisualFidelityID, m_CurrentFidelity);
        }

        // ── 공개 API ────────────────────────────────────────────────────
        public AdaptationPhase GetCurrentPhase()    => m_CurrentPhase;
        public float           GetCurrentFidelity() => m_CurrentFidelity;
    }
}
