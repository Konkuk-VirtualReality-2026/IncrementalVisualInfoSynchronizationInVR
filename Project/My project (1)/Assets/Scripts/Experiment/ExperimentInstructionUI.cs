using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VRAdaptation.Experiment;

namespace VRAdaptation
{
    public class ExperimentInstructionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Text m_InstructionText;
        [SerializeField] CanvasGroup m_CanvasGroup;

        [Header("Camera Anchor Settings")]
        [Tooltip("미지정 시 Camera.main 자동 탐색")]
        [SerializeField] Transform m_FollowTarget;
        [SerializeField] float m_FollowDistance = 0.25f;
        [SerializeField] float m_HeightOffset = -0.1f;

        [Header("Timing")]
        [SerializeField] float m_FadeDuration = 0.5f;
        [SerializeField] float m_AimTrainerTextDuration = 5f;

        Coroutine m_HideCoroutine;
        bool m_IsVisible;

        // Phase별 안내 텍스트
        static readonly string TXT_CONTROL_AIMTRAINER =
            "조준 훈련을 시작합니다.\n\n빨간 구체 타겟이 나타나면\n컨트롤러를 조준해 트리거를 당기세요.\n타겟이 사라지기 전에 빠르게 맞추세요.";

        static readonly string TXT_ADAPTATION_AIMTRAINER =
            "적응 과정이 완료되었습니다.\n\n조준 훈련을 시작합니다.\n빨간 타겟이 나타나면 조준 후 트리거를 당기세요.";

        static readonly string TXT_PHASE1 =
            "잠시 화면이 어두워집니다.\n\n편안히 서서 정면을 바라봐 주세요.\n소리와 진동을 느끼며 공간에 집중하세요.";

        static readonly string TXT_PHASE2 =
            "서서히 주변 윤곽이 보이기 시작합니다.\n\n천천히 고개를 돌려\n주변 공간을 살펴보세요.";

        static readonly string TXT_PHASE3 =
            "시각이 점점 선명해집니다.\n\n주변을 자연스럽게 둘러보세요.\n잠시 후 조준 훈련이 시작됩니다.";

        static readonly string TXT_COMPLETE =
            "실험이 종료되었습니다.\n실험자의 안내에 따라 주세요.";

        void Awake()
        {
            if (m_CanvasGroup != null)
                m_CanvasGroup.alpha = 0f;
            m_IsVisible = false;

            // 카메라를 못 찾으면 Camera.main 사용
            if (m_FollowTarget == null && Camera.main != null)
                m_FollowTarget = Camera.main.transform;

            // 카메라 자식으로 붙여서 벽에 묻히지 않게 고정
            if (m_FollowTarget != null)
            {
                transform.SetParent(m_FollowTarget, false);
                transform.localPosition = new Vector3(0f, m_HeightOffset, m_FollowDistance);
                transform.localRotation = Quaternion.identity;
            }

            // BlackoutCanvas(z=0.31)보다 앞에 그려지도록 sortingOrder 높임
            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 10;
        }

        void OnEnable()
        {
            if (VRAdaptationManager.Instance != null)
                VRAdaptationManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
        }

        void OnDisable()
        {
            if (VRAdaptationManager.Instance != null)
                VRAdaptationManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }

        void OnPhaseChanged(AdaptationPhase phase)
        {
            bool isControl = ExperimentCondition.SelectedGroup == ExperimentGroup.Control;

            switch (phase)
            {
                case AdaptationPhase.Phase1_Static:
                    if (!isControl)
                        ShowInstruction(TXT_PHASE1, -1f); // 페이드아웃 없이 유지
                    break;

                case AdaptationPhase.Phase2_Dynamic:
                    if (!isControl)
                        ShowInstruction(TXT_PHASE2, -1f);
                    break;

                case AdaptationPhase.Phase3_HighFidelity:
                    if (!isControl)
                        ShowInstruction(TXT_PHASE3, -1f);
                    break;

                case AdaptationPhase.AimTrainer_Test:
                    string aimText = isControl ? TXT_CONTROL_AIMTRAINER : TXT_ADAPTATION_AIMTRAINER;
                    ShowInstruction(aimText, m_AimTrainerTextDuration);
                    break;

                case AdaptationPhase.Complete:
                    ShowInstruction(TXT_COMPLETE, -1f);
                    break;
            }
        }

        /// <summary>
        /// 안내 텍스트를 표시합니다.
        /// </summary>
        /// <param name="text">표시할 텍스트</param>
        /// <param name="duration">표시 시간(초). -1이면 자동 숨김 없음.</param>
        public void ShowInstruction(string text, float duration)
        {
            if (m_HideCoroutine != null)
            {
                StopCoroutine(m_HideCoroutine);
                m_HideCoroutine = null;
            }

            if (m_InstructionText != null)
                m_InstructionText.text = text;

            StartCoroutine(FadeCanvasGroup(0f, 1f, m_FadeDuration));
            m_IsVisible = true;

            if (duration > 0f)
                m_HideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return FadeCanvasGroup(1f, 0f, m_FadeDuration);
            m_IsVisible = false;
        }

        IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            if (m_CanvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                m_CanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            m_CanvasGroup.alpha = to;
        }

        // 카메라 자식으로 고정되므로 Update에서 위치 추적 불필요
    }
}
