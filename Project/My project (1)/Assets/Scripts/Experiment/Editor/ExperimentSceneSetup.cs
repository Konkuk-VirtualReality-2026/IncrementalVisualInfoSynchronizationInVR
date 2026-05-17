using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using VRAdaptation.Experiment;

namespace VRAdaptation.Editor
{
    /// <summary>
    /// VR Adaptation / Setup Lobby UI    → LobbyScene에서 실행
    /// VR Adaptation / Setup Instruction UI → BasicScene에서 실행
    /// </summary>
    public static class ExperimentSceneSetup
    {
        // ────────────────────────────────────────────────────────────────────
        // 1. LobbyScene UI
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("VR Adaptation/Setup Lobby UI (LobbyScene)")]
        public static void SetupLobbyUI()
        {
            Camera mainCam = Camera.main;

            // ── LobbyManager 오브젝트 ──────────────────────────────────────
            GameObject managerObj = FindOrCreate("LobbyManager");
            LobbyManager lobbyManager = GetOrAdd<LobbyManager>(managerObj);

            // ── LobbyCanvas (World Space, 1m × 0.8m) ──────────────────────
            GameObject canvasObj = FindOrCreate("LobbyCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create LobbyCanvas");

            Canvas canvas = GetOrAdd<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.WorldSpace;

            // 카메라 정면 1.5m 앞에 배치
            if (mainCam != null)
            {
                canvasObj.transform.position = mainCam.transform.position
                    + mainCam.transform.forward * 1.5f;
                canvasObj.transform.rotation = Quaternion.LookRotation(
                    canvasObj.transform.position - mainCam.transform.position);
            }
            else
            {
                canvasObj.transform.position = new Vector3(0f, 1.4f, 1.5f);
                canvasObj.transform.rotation = Quaternion.identity;
            }
            canvasObj.transform.localScale = Vector3.one * 0.001f;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 800f); // 1m × 0.8m @ scale 0.001

            // GraphicRaycaster (UI 이벤트용)
            GetOrAdd<GraphicRaycaster>(canvasObj);

            // ── 배경 패널 ──────────────────────────────────────────────────
            GameObject bg = FindOrCreateChild(canvasObj, "Background");
            Image bgImage = GetOrAdd<Image>(bg);
            bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            StretchFull(bg.GetComponent<RectTransform>());

            // ── TitleText ──────────────────────────────────────────────────
            GameObject titleObj = FindOrCreateChild(canvasObj, "TitleText");
            TextMeshProUGUI titleText = GetOrAdd<TextMeshProUGUI>(titleObj);
            titleText.text = "VR 실험 참가자 설정";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            SetRect(titleObj.GetComponent<RectTransform>(),
                new Vector2(-450f, 320f), new Vector2(450f, 400f));

            // ── ParticipantIDField ─────────────────────────────────────────
            GameObject idFieldObj = FindOrCreateChild(canvasObj, "ParticipantIDField");
            TMP_InputField idField = CreateTMPInputField(idFieldObj,
                placeholder: "피실험자 번호 입력",
                fontSize: 52);
            SetRect(idFieldObj.GetComponent<RectTransform>(),
                new Vector2(-300f, 200f), new Vector2(300f, 280f));

            // ── SelectionPanel ─────────────────────────────────────────────
            GameObject selPanel = FindOrCreateChild(canvasObj, "SelectionPanel");
            SetRect(selPanel.GetComponent<RectTransform>(),
                new Vector2(-480f, -280f), new Vector2(480f, 180f));

            // Group1Button
            GameObject btn1Obj = FindOrCreateChild(selPanel, "Group1Button");
            Button btn1 = CreateButton(btn1Obj,
                "1군 (대조군)\n적응 없이 바로 조준 훈련",
                new Color(0.2f, 0.4f, 0.8f),
                fontSize: 44);
            SetRect(btn1Obj.GetComponent<RectTransform>(),
                new Vector2(-460f, -200f), new Vector2(-20f, 180f));

            // Group2Button
            GameObject btn2Obj = FindOrCreateChild(selPanel, "Group2Button");
            Button btn2 = CreateButton(btn2Obj,
                "2군 (실험군)\n3단계 적응 후 조준 훈련",
                new Color(0.2f, 0.65f, 0.35f),
                fontSize: 44);
            SetRect(btn2Obj.GetComponent<RectTransform>(),
                new Vector2(20f, -200f), new Vector2(460f, 180f));

            // ── ConfirmPanel (초기 비활성) ─────────────────────────────────
            GameObject confPanel = FindOrCreateChild(canvasObj, "ConfirmPanel");
            confPanel.SetActive(false);
            SetRect(confPanel.GetComponent<RectTransform>(),
                new Vector2(-480f, -280f), new Vector2(480f, 180f));

            // ConfirmText
            GameObject confTextObj = FindOrCreateChild(confPanel, "ConfirmText");
            TextMeshProUGUI confText = GetOrAdd<TextMeshProUGUI>(confTextObj);
            confText.text = "X군이 선택되었습니다.";
            confText.fontSize = 52;
            confText.alignment = TextAlignmentOptions.Center;
            confText.color = Color.white;
            SetRect(confTextObj.GetComponent<RectTransform>(),
                new Vector2(-460f, 10f), new Vector2(460f, 180f));

            // StartButton
            GameObject startBtnObj = FindOrCreateChild(confPanel, "StartButton");
            Button startBtn = CreateButton(startBtnObj,
                "실험 시작",
                new Color(0.85f, 0.35f, 0.15f),
                fontSize: 60);
            SetRect(startBtnObj.GetComponent<RectTransform>(),
                new Vector2(-200f, -200f), new Vector2(200f, -20f));

            // ── LobbyManager Inspector 연결 ────────────────────────────────
            var managerSO = new SerializedObject(lobbyManager);
            managerSO.FindProperty("m_ParticipantIDField").objectReferenceValue = idField;
            managerSO.FindProperty("m_SelectionPanel").objectReferenceValue     = selPanel;
            managerSO.FindProperty("m_ConfirmPanel").objectReferenceValue       = confPanel;
            managerSO.FindProperty("m_ConfirmText").objectReferenceValue        = confText;
            managerSO.ApplyModifiedProperties();

            // ── 버튼 onClick 연결 ──────────────────────────────────────────
            // Group1Button → LobbyManager.SelectGroup1
            AddButtonListener(btn1, lobbyManager, "SelectGroup1");
            // Group2Button → LobbyManager.SelectGroup2
            AddButtonListener(btn2, lobbyManager, "SelectGroup2");
            // StartButton → LobbyManager.StartExperiment
            AddButtonListener(startBtn, lobbyManager, "StartExperiment");

            Selection.activeGameObject = canvasObj;
            Debug.Log("[ExperimentSceneSetup] LobbyUI 생성 완료. " +
                      "TrackedDeviceGraphicRaycaster를 LobbyCanvas에 수동으로 추가하세요 (XRI 컨트롤러 레이캐스트용).");
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. BasicScene InstructionUI
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("VR Adaptation/Setup Instruction UI (BasicScene)")]
        public static void SetupInstructionUI()
        {
            Camera mainCam = Camera.main;

            // ── InstructionUI 루트 오브젝트 ───────────────────────────────
            GameObject rootObj = FindOrCreate("InstructionUI");
            ExperimentInstructionUI uiComp = GetOrAdd<ExperimentInstructionUI>(rootObj);

            // ── InstructionCanvas (World Space, 0.9m × 0.35m) ─────────────
            GameObject canvasObj = FindOrCreateChild(rootObj, "InstructionCanvas");
            Canvas canvas = GetOrAdd<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.WorldSpace;

            canvasObj.transform.localScale = Vector3.one * 0.001f;
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 350f); // 0.9m × 0.35m @ scale 0.001

            // 초기 위치: 카메라 전방 1.8m, 높이 -0.15m
            if (mainCam != null)
            {
                canvasObj.transform.position = mainCam.transform.position
                    + mainCam.transform.forward * 1.8f
                    + Vector3.up * -0.15f;
                canvasObj.transform.rotation = Quaternion.LookRotation(
                    canvasObj.transform.position - mainCam.transform.position);
            }
            else
            {
                canvasObj.transform.position = new Vector3(0f, 1.25f, 1.8f);
                canvasObj.transform.rotation = Quaternion.identity;
            }

            // CanvasGroup (페이드용)
            CanvasGroup cg = GetOrAdd<CanvasGroup>(canvasObj);
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // ── Panel (반투명 배경) ────────────────────────────────────────
            GameObject panelObj = FindOrCreateChild(canvasObj, "Panel");
            Image panelImage = GetOrAdd<Image>(panelObj);
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);
            StretchFull(panelObj.GetComponent<RectTransform>());

            // 둥근 모서리 (Rounded Rectangle) — 기본 Sprite 사용
            // 원하면 sprite를 둥근 사각형으로 교체 가능

            // ── InstructionText ───────────────────────────────────────────
            GameObject textObj = FindOrCreateChild(panelObj, "InstructionText");
            TextMeshProUGUI instrText = GetOrAdd<TextMeshProUGUI>(textObj);
            instrText.text = "";
            instrText.fontSize = 54;
            instrText.alignment = TextAlignmentOptions.Center;
            instrText.color = Color.white;
            instrText.enableWordWrapping = true;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 16f);
            textRect.offsetMax = new Vector2(-24f, -16f);

            // ── ExperimentInstructionUI Inspector 연결 ─────────────────────
            var uiSO = new SerializedObject(uiComp);
            uiSO.FindProperty("m_InstructionText").objectReferenceValue = instrText;
            uiSO.FindProperty("m_CanvasGroup").objectReferenceValue     = cg;
            if (mainCam != null)
                uiSO.FindProperty("m_FollowTarget").objectReferenceValue = mainCam.transform;
            uiSO.ApplyModifiedProperties();

            Selection.activeGameObject = rootObj;
            Debug.Log("[ExperimentSceneSetup] InstructionUI 생성 완료. " +
                      "m_FollowTarget을 XR Camera Transform으로 연결하세요.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 헬퍼 메서드
        // ────────────────────────────────────────────────────────────────────

        static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            }
            return go;
        }

        static GameObject FindOrCreateChild(GameObject parent, string childName)
        {
            Transform existing = parent.transform.Find(childName);
            if (existing != null) return existing.gameObject;

            var child = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
            child.transform.SetParent(parent.transform, false);

            // RectTransform이 없으면 추가
            if (child.GetComponent<RectTransform>() == null)
                child.AddComponent<RectTransform>();

            return child;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>앵커를 0~1 스트레치로, offset을 0으로 설정</summary>
        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>절대 좌표(min/max)로 RectTransform 설정</summary>
        static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = (min + max) * 0.5f;
            rt.sizeDelta = max - min;
        }

        static Button CreateButton(GameObject go, string label, Color bgColor, int fontSize)
        {
            // 배경 Image
            Image img = GetOrAdd<Image>(go);
            img.color = bgColor;

            // 버튼 컴포넌트
            Button btn = GetOrAdd<Button>(go);

            // 텍스트 자식
            GameObject labelObj = FindOrCreateChild(go, "Label");
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(labelObj);
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);

            return btn;
        }

        static TMP_InputField CreateTMPInputField(GameObject go, string placeholder, int fontSize)
        {
            // 배경
            Image bg = GetOrAdd<Image>(go);
            bg.color = new Color(1f, 1f, 1f, 0.15f);

            TMP_InputField field = GetOrAdd<TMP_InputField>(go);
            field.characterLimit = 20;

            // Placeholder 텍스트
            GameObject phObj = FindOrCreateChild(go, "Placeholder");
            TextMeshProUGUI phText = GetOrAdd<TextMeshProUGUI>(phObj);
            phText.text = placeholder;
            phText.fontSize = fontSize;
            phText.color = new Color(1f, 1f, 1f, 0.45f);
            phText.fontStyle = FontStyles.Italic;
            phText.enableWordWrapping = false;
            StretchFull(phObj.GetComponent<RectTransform>());

            // 입력 텍스트
            GameObject inputTextObj = FindOrCreateChild(go, "Text");
            TextMeshProUGUI inputText = GetOrAdd<TextMeshProUGUI>(inputTextObj);
            inputText.fontSize = fontSize;
            inputText.color = Color.white;
            inputText.enableWordWrapping = false;
            StretchFull(inputTextObj.GetComponent<RectTransform>());

            // TMP_InputField 참조 연결
            var so = new SerializedObject(field);
            so.FindProperty("m_TextComponent").objectReferenceValue    = inputText;
            so.FindProperty("m_Placeholder").objectReferenceValue      = phText;
            so.ApplyModifiedProperties();

            return field;
        }

        /// <summary>
        /// Button.onClick에 UnityEvent persistent 리스너를 추가한다.
        /// </summary>
        static void AddButtonListener(Button button, Object target, string methodName)
        {
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                button.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction),
                    target,
                    target.GetType().GetMethod(methodName)));
        }
    }
}
