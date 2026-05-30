using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRAdaptation.Experiment
{
    public class LobbyManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Text m_IDDisplayText;
        [SerializeField] GameObject m_NumpadPanel;
        [SerializeField] GameObject m_SelectionPanel;
        [SerializeField] GameObject m_ConfirmPanel;
        [SerializeField] Text m_ConfirmText;
        [SerializeField] Text m_CountdownText;

        [Header("Scene")]
        [SerializeField] string m_ExperimentSceneName = "BasicScene";

        [Header("Settings")]
        [SerializeField] int m_CountdownSeconds = 3;

        string m_CurrentID = "";

        void Start()
        {
            if (m_ConfirmPanel != null) m_ConfirmPanel.SetActive(false);
            if (m_SelectionPanel != null) m_SelectionPanel.SetActive(false); // ID 입력 전 숨김
            RefreshIDDisplay();
        }

        // ── 숫자 패드 ────────────────────────────────────────────────────────

        public void NumpadPress(string digit)
        {
            if (m_CurrentID.Length >= 2) return;
            m_CurrentID += digit;
            RefreshIDDisplay();
        }

        public void NumpadDelete()
        {
            if (m_CurrentID.Length == 0) return;
            m_CurrentID = m_CurrentID.Substring(0, m_CurrentID.Length - 1);
            RefreshIDDisplay();
        }

        // 숫자 패드 확인 버튼 — ID 입력 완료 후 그룹 선택 패널 표시
        public void NumpadConfirm()
        {
            if (m_CurrentID.Length == 0) return;
            if (m_NumpadPanel != null) m_NumpadPanel.SetActive(false);
            if (m_SelectionPanel != null) m_SelectionPanel.SetActive(true);
        }

        void RefreshIDDisplay()
        {
            if (m_IDDisplayText != null)
                m_IDDisplayText.text = m_CurrentID.Length > 0 ? m_CurrentID : "-";
        }

        // ── 그룹 선택 ────────────────────────────────────────────────────────

        public void SelectGroup1()
        {
            ExperimentCondition.SelectedGroup = ExperimentGroup.Control;
            ExperimentCondition.ParticipantID = m_CurrentID;
            ShowConfirmAndCountdown("1군 (대조군) 선택됨\n적응 없이 바로 조준 훈련을 진행합니다.");
        }

        public void SelectGroup2()
        {
            ExperimentCondition.SelectedGroup = ExperimentGroup.Adaptation;
            ExperimentCondition.ParticipantID = m_CurrentID;
            ShowConfirmAndCountdown("2군 (실험군) 선택됨\n3단계 적응 후 조준 훈련을 진행합니다.");
        }

        // ── 확인 + 카운트다운 ────────────────────────────────────────────────

        void ShowConfirmAndCountdown(string message)
        {
            if (m_SelectionPanel != null) m_SelectionPanel.SetActive(false);
            if (m_ConfirmPanel != null) m_ConfirmPanel.SetActive(true);
            if (m_ConfirmText != null) m_ConfirmText.text = message;
            StartCoroutine(CountdownRoutine());
        }

        IEnumerator CountdownRoutine()
        {
            int remaining = m_CountdownSeconds;
            while (remaining > 0)
            {
                if (m_CountdownText != null)
                    m_CountdownText.text = $"{remaining}초 후 시작됩니다...";
                yield return new WaitForSeconds(1f);
                remaining--;
            }

            if (m_CountdownText != null)
                m_CountdownText.text = "시작!";

            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene(m_ExperimentSceneName);
        }
    }
}
