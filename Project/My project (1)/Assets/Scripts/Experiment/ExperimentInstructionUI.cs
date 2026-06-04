using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VRAdaptation.Experiment;

namespace VRAdaptation
{
    public class ExperimentInstructionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Text        m_InstructionText;
        [SerializeField] CanvasGroup m_CanvasGroup;

        [Header("Camera Anchor Settings")]
        [Tooltip("미지정 시 Camera.main 자동 탐색")]
        [SerializeField] Transform m_FollowTarget;
        [SerializeField] float     m_FollowDistance = 0.25f;
        [SerializeField] float     m_HeightOffset   = -0.1f;

        [Header("Timing")]
        [SerializeField] float m_FadeDuration          = 0.5f;
        [SerializeField] float m_AimTrainerTextDuration = 5f;

        Coroutine m_HideCoroutine;

        // ── Phase별 안내 텍스트 ──────────────────────────────────────────
        static readonly string TXT_BLACKOUT =
            "당신은 어둠속에서 이 VR 공간에 대해 인지하며, 적응해야합니다.\n초록색 점을 따라 이동해보세요.";

        static readonly string TXT_EDGE =
            "윤곽이 보이기 시작합니다.\n\n빛나는 점을 따라 지정 위치로 이동하세요.\n주변 공간의 형태를 파악하세요.";

        static readonly string TXT_AIMTRAINER =
            "훈련을 시작합니다.\n\n빨간 타겟이 적입니다. 조준 후 트리거를 당겨 모든 적을 처치하세요.\n모든 적을 처치하면 훈련이 종료됩니다.\n\n초록 타겟은 아군 — 절대 쏘지 마세요!";

        static readonly string TXT_COMPLETE =
            "훈련이 종료되었습니다.\n실험자의 안내에 따라 주세요.";

        // ── 생명주기 ─────────────────────────────────────────────────────
        void Awake()
        {
            if (m_CanvasGroup != null) m_CanvasGroup.alpha = 0f;

            if (m_FollowTarget == null && Camera.main != null)
                m_FollowTarget = Camera.main.transform;

            if (m_FollowTarget != null)
            {
                transform.SetParent(m_FollowTarget, false);
                transform.localPosition = new Vector3(0f, m_HeightOffset, m_FollowDistance);
                transform.localRotation = Quaternion.identity;
            }

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

        // ── Phase 변경 콜백 ──────────────────────────────────────────────
        void OnPhaseChanged(AdaptationPhase phase)
        {
            bool isControl = ExperimentCondition.SelectedGroup == ExperimentGroup.Control;

            switch (phase)
            {
                case AdaptationPhase.Phase1_Blackout:
                    if (!isControl) ShowInstruction(TXT_BLACKOUT, -1f);
                    break;

                case AdaptationPhase.Phase2_Edge:
                    if (!isControl) ShowInstruction(TXT_EDGE, -1f);
                    break;

                case AdaptationPhase.AimTrainer_Moving:
                case AdaptationPhase.AimTrainer_Static:
                    ShowInstruction(TXT_AIMTRAINER, m_AimTrainerTextDuration);
                    break;

                case AdaptationPhase.Complete:
                    ShowInstruction(TXT_COMPLETE, -1f);
                    break;
            }
        }

        // ── 공개 API ─────────────────────────────────────────────────────
        /// <summary>안내 텍스트 표시. duration=-1이면 자동 숨김 없음.</summary>
        public void ShowInstruction(string text, float duration)
        {
            if (m_HideCoroutine != null)
            {
                StopCoroutine(m_HideCoroutine);
                m_HideCoroutine = null;
            }

            if (m_InstructionText != null) m_InstructionText.text = text;

            StartCoroutine(FadeCanvasGroup(0f, 1f, m_FadeDuration));

            if (duration > 0f)
                m_HideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        // ── 코루틴 ───────────────────────────────────────────────────────
        IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return FadeCanvasGroup(1f, 0f, m_FadeDuration);
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
    }
}
