using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace VRAdaptation.Experiment
{
    public class LobbyManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] TMP_InputField m_ParticipantIDField;
        [SerializeField] GameObject m_SelectionPanel;
        [SerializeField] GameObject m_ConfirmPanel;
        [SerializeField] TMP_Text m_ConfirmText;

        [Header("Scene")]
        [SerializeField] string m_ExperimentSceneName = "BasicScene";

        void Start()
        {
            if (m_ConfirmPanel != null)
                m_ConfirmPanel.SetActive(false);
            if (m_SelectionPanel != null)
                m_SelectionPanel.SetActive(true);
        }

        public void SelectGroup1()
        {
            ExperimentCondition.SelectedGroup = ExperimentGroup.Control;
            SaveParticipantID();
            ShowConfirmPanel("1군 (대조군)이 선택되었습니다.\n적응 없이 바로 조준 훈련을 진행합니다.");
        }

        public void SelectGroup2()
        {
            ExperimentCondition.SelectedGroup = ExperimentGroup.Adaptation;
            SaveParticipantID();
            ShowConfirmPanel("2군 (실험군)이 선택되었습니다.\n3단계 적응 후 조준 훈련을 진행합니다.");
        }

        public void StartExperiment()
        {
            if (ExperimentCondition.SelectedGroup == ExperimentGroup.NotSelected)
            {
                Debug.LogWarning("[LobbyManager] 실험 그룹이 선택되지 않았습니다.");
                return;
            }
            SceneManager.LoadScene(m_ExperimentSceneName);
        }

        void SaveParticipantID()
        {
            if (m_ParticipantIDField != null)
                ExperimentCondition.ParticipantID = m_ParticipantIDField.text;
        }

        void ShowConfirmPanel(string message)
        {
            if (m_SelectionPanel != null)
                m_SelectionPanel.SetActive(false);
            if (m_ConfirmPanel != null)
                m_ConfirmPanel.SetActive(true);
            if (m_ConfirmText != null)
                m_ConfirmText.text = message;
        }
    }
}
