using UnityEngine;
using UnityEngine.UI;

namespace VRAdaptation.Experiment
{
    [RequireComponent(typeof(Button))]
    public class NumpadButton : MonoBehaviour
    {
        [SerializeField] string m_Digit; // 비어있으면 Delete

        LobbyManager m_Lobby;

        void Awake()
        {
            m_Lobby = Object.FindAnyObjectByType<LobbyManager>();
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            if (m_Lobby == null) return;
            if (m_Digit == "CONFIRM")
                m_Lobby.NumpadConfirm();
            else if (string.IsNullOrEmpty(m_Digit))
                m_Lobby.NumpadDelete();
            else
                m_Lobby.NumpadPress(m_Digit);
        }
    }
}
