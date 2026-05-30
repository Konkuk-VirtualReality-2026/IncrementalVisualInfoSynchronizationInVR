using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;
using VRAdaptation.Experiment;

namespace VRAdaptation.Editor
{
    public static class ExperimentSceneSetup
    {
        // ────────────────────────────────────────────────────────────────────
        // 0. LobbyScene VR 완전 수정 (원클릭)
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("VR Adaptation/Fix Lobby Scene for VR")]
        public static void FixLobbySceneForVR()
        {
            int fixCount = 0;

            // ── LobbyCanvas: GraphicRaycaster → TrackedDeviceGraphicRaycaster
            GameObject canvasObj = GameObject.Find("LobbyCanvas");
            if (canvasObj != null)
            {
                var oldRC = canvasObj.GetComponent<GraphicRaycaster>();
                if (oldRC != null) { Undo.DestroyObjectImmediate(oldRC); fixCount++; }

                if (canvasObj.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvasObj);
                    fixCount++;
                    Debug.Log("[LobbyFix] TrackedDeviceGraphicRaycaster 추가.");
                }
            }
            else
            {
                Debug.LogWarning("[LobbyFix] LobbyCanvas 없음 — 먼저 'Setup Lobby UI' 실행.");
            }

            // ── EventSystem: XRUIInputModule 추가 (기존 InputModule 제거)
            GameObject esObj = GameObject.Find("EventSystem");
            if (esObj != null)
            {
                // 기존 StandaloneInputModule 제거
                var standalone = esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standalone != null) { Undo.DestroyObjectImmediate(standalone); fixCount++; }

                if (esObj.GetComponent<XRUIInputModule>() == null)
                {
                    Undo.AddComponent<XRUIInputModule>(esObj);
                    fixCount++;
                    Debug.Log("[LobbyFix] XRUIInputModule 추가.");
                }
            }

            // ── XR Origin 관련 처리
            var xrOrigin = Object.FindAnyObjectByType<XROrigin>();

            // Main Camera AudioListener 제거 (XR Origin 카메라와 중복)
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                bool isXRCam = xrOrigin != null && cam.transform.IsChildOf(xrOrigin.transform);
                if (!isXRCam)
                {
                    var listener = cam.GetComponent<AudioListener>();
                    if (listener != null)
                    {
                        Undo.DestroyObjectImmediate(listener);
                        fixCount++;
                        Debug.Log($"[LobbyFix] AudioListener 제거: {cam.gameObject.name}");
                    }
                }
            }

            if (xrOrigin != null)
            {
                // CharacterController 비활성화 (로비에서 중력 낙하 방지)
                var cc = xrOrigin.GetComponent<CharacterController>();
                if (cc != null && cc.enabled)
                {
                    cc.enabled = false;
                    EditorUtility.SetDirty(xrOrigin.gameObject);
                    fixCount++;
                    Debug.Log("[LobbyFix] CharacterController 비활성화.");
                }

                // XR Ray Interactor: UI Interaction ON
                foreach (var ray in xrOrigin.GetComponentsInChildren<XRRayInteractor>())
                {
                    if (!ray.enableUIInteraction)
                    {
                        ray.enableUIInteraction = true;
                        EditorUtility.SetDirty(ray);
                        fixCount++;
                        Debug.Log($"[LobbyFix] UI Interaction ON: {ray.gameObject.name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[LobbyFix] XR Origin 없음 — GameObject → XR → XR Origin (VR) 로 추가 후 다시 실행.");
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[LobbyFix] 완료. 총 {fixCount}개 항목 수정. 씬 저장 후 빌드하세요.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 1. LobbyScene UI
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("VR Adaptation/Setup Lobby UI (LobbyScene)")]
        public static void SetupLobbyUI()
        {
            Camera mainCam = Camera.main;

            GameObject managerObj = FindOrCreate("LobbyManager");
            LobbyManager lobbyManager = GetOrAdd<LobbyManager>(managerObj);

            // ── LobbyCanvas (World Space, 1m × 0.8m) ──────────────────────
            GameObject canvasObj = FindOrCreate("LobbyCanvas");
            Canvas canvas = GetOrAdd<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.WorldSpace;

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
            canvasRect.sizeDelta = new Vector2(1000f, 800f);

            GetOrAdd<GraphicRaycaster>(canvasObj);

            // ── 배경 ──────────────────────────────────────────────────────
            GameObject bg = FindOrCreateChild(canvasObj, "Background");
            Image bgImage = GetOrAdd<Image>(bg);
            bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            StretchFull(bg.GetComponent<RectTransform>());

            // ── TitleText ──────────────────────────────────────────────────
            GameObject titleObj = FindOrCreateChild(canvasObj, "TitleText");
            Text titleText = GetOrAdd<Text>(titleObj);
            titleText.text = "VR 실험 참가자 설정";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(titleObj.GetComponent<RectTransform>(),
                new Vector2(-450f, 320f), new Vector2(450f, 400f));

            // ── ParticipantIDField ─────────────────────────────────────────
            GameObject idFieldObj = FindOrCreateChild(canvasObj, "ParticipantIDField");
            InputField idField = CreateInputField(idFieldObj,
                placeholder: "피실험자 번호 입력", fontSize: 52);
            SetRect(idFieldObj.GetComponent<RectTransform>(),
                new Vector2(-300f, 200f), new Vector2(300f, 280f));

            // ── SelectionPanel ─────────────────────────────────────────────
            GameObject selPanel = FindOrCreateChild(canvasObj, "SelectionPanel");
            SetRect(selPanel.GetComponent<RectTransform>(),
                new Vector2(-480f, -280f), new Vector2(480f, 180f));

            GameObject btn1Obj = FindOrCreateChild(selPanel, "Group1Button");
            Button btn1 = CreateButton(btn1Obj,
                "1군 (대조군)\n적응 없이 바로 조준 훈련",
                new Color(0.2f, 0.4f, 0.8f), fontSize: 44);
            SetRect(btn1Obj.GetComponent<RectTransform>(),
                new Vector2(-460f, -200f), new Vector2(-20f, 180f));

            GameObject btn2Obj = FindOrCreateChild(selPanel, "Group2Button");
            Button btn2 = CreateButton(btn2Obj,
                "2군 (실험군)\n3단계 적응 후 조준 훈련",
                new Color(0.2f, 0.65f, 0.35f), fontSize: 44);
            SetRect(btn2Obj.GetComponent<RectTransform>(),
                new Vector2(20f, -200f), new Vector2(460f, 180f));

            // ── ConfirmPanel (초기 비활성) ─────────────────────────────────
            GameObject confPanel = FindOrCreateChild(canvasObj, "ConfirmPanel");
            confPanel.SetActive(false);
            SetRect(confPanel.GetComponent<RectTransform>(),
                new Vector2(-480f, -280f), new Vector2(480f, 180f));

            GameObject confTextObj = FindOrCreateChild(confPanel, "ConfirmText");
            Text confText = GetOrAdd<Text>(confTextObj);
            confText.text = "X군이 선택되었습니다.";
            confText.fontSize = 52;
            confText.alignment = TextAnchor.MiddleCenter;
            confText.color = Color.white;
            confText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(confTextObj.GetComponent<RectTransform>(),
                new Vector2(-460f, 10f), new Vector2(460f, 180f));

            GameObject startBtnObj = FindOrCreateChild(confPanel, "StartButton");
            Button startBtn = CreateButton(startBtnObj,
                "실험 시작", new Color(0.85f, 0.35f, 0.15f), fontSize: 60);
            SetRect(startBtnObj.GetComponent<RectTransform>(),
                new Vector2(-200f, -200f), new Vector2(200f, -20f));

            // ── LobbyManager 참조 연결 ─────────────────────────────────────
            var managerSO = new SerializedObject(lobbyManager);
            managerSO.FindProperty("m_ParticipantIDField").objectReferenceValue = idField;
            managerSO.FindProperty("m_SelectionPanel").objectReferenceValue     = selPanel;
            managerSO.FindProperty("m_ConfirmPanel").objectReferenceValue       = confPanel;
            managerSO.FindProperty("m_ConfirmText").objectReferenceValue        = confText;
            managerSO.ApplyModifiedProperties();

            AddButtonListener(btn1, lobbyManager, "SelectGroup1");
            AddButtonListener(btn2, lobbyManager, "SelectGroup2");
            AddButtonListener(startBtn, lobbyManager, "StartExperiment");

            Selection.activeGameObject = canvasObj;
            Debug.Log("[ExperimentSceneSetup] LobbyUI 생성 완료.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. BasicScene InstructionUI
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("VR Adaptation/Setup Instruction UI (BasicScene)")]
        public static void SetupInstructionUI()
        {
            Camera mainCam = Camera.main;

            GameObject rootObj = FindOrCreate("InstructionUI");
            ExperimentInstructionUI uiComp = GetOrAdd<ExperimentInstructionUI>(rootObj);

            GameObject canvasObj = FindOrCreateChild(rootObj, "InstructionCanvas");
            Canvas canvas = GetOrAdd<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.WorldSpace;

            canvasObj.transform.localScale = Vector3.one * 0.001f;
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 350f);

            if (mainCam != null)
            {
                canvasObj.transform.position = mainCam.transform.position
                    + mainCam.transform.forward * 1.8f + Vector3.up * -0.15f;
                canvasObj.transform.rotation = Quaternion.LookRotation(
                    canvasObj.transform.position - mainCam.transform.position);
            }
            else
            {
                canvasObj.transform.position = new Vector3(0f, 1.25f, 1.8f);
                canvasObj.transform.rotation = Quaternion.identity;
            }

            CanvasGroup cg = GetOrAdd<CanvasGroup>(canvasObj);
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            GameObject panelObj = FindOrCreateChild(canvasObj, "Panel");
            GetOrAdd<Image>(panelObj).color = new Color(0f, 0f, 0f, 0.72f);
            StretchFull(panelObj.GetComponent<RectTransform>());

            GameObject textObj = FindOrCreateChild(panelObj, "InstructionText");
            Text instrText = GetOrAdd<Text>(textObj);
            instrText.text = "";
            instrText.fontSize = 54;
            instrText.alignment = TextAnchor.MiddleCenter;
            instrText.color = Color.white;
            instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instrText.resizeTextForBestFit = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 16f);
            textRect.offsetMax = new Vector2(-24f, -16f);

            var uiSO = new SerializedObject(uiComp);
            uiSO.FindProperty("m_InstructionText").objectReferenceValue = instrText;
            uiSO.FindProperty("m_CanvasGroup").objectReferenceValue     = cg;
            if (mainCam != null)
                uiSO.FindProperty("m_FollowTarget").objectReferenceValue = mainCam.transform;
            uiSO.ApplyModifiedProperties();

            Selection.activeGameObject = rootObj;
            Debug.Log("[ExperimentSceneSetup] InstructionUI 생성 완료.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 헬퍼
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

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

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
            GetOrAdd<Image>(go).color = bgColor;
            Button btn = GetOrAdd<Button>(go);

            GameObject labelObj = FindOrCreateChild(go, "Label");
            Text t = GetOrAdd<Text>(labelObj);
            t.text = label;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform lr = labelObj.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(16f, 8f);
            lr.offsetMax = new Vector2(-16f, -8f);

            return btn;
        }

        static InputField CreateInputField(GameObject go, string placeholder, int fontSize)
        {
            GetOrAdd<Image>(go).color = new Color(1f, 1f, 1f, 0.15f);
            InputField field = GetOrAdd<InputField>(go);
            field.characterLimit = 20;

            GameObject phObj = FindOrCreateChild(go, "Placeholder");
            Text phText = GetOrAdd<Text>(phObj);
            phText.text = placeholder;
            phText.fontSize = fontSize;
            phText.color = new Color(1f, 1f, 1f, 0.45f);
            phText.fontStyle = FontStyle.Italic;
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StretchFull(phObj.GetComponent<RectTransform>());

            GameObject inputTextObj = FindOrCreateChild(go, "Text");
            Text inputText = GetOrAdd<Text>(inputTextObj);
            inputText.fontSize = fontSize;
            inputText.color = Color.white;
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            StretchFull(inputTextObj.GetComponent<RectTransform>());

            var so = new SerializedObject(field);
            so.FindProperty("m_TextComponent").objectReferenceValue = inputText;
            so.FindProperty("m_Placeholder").objectReferenceValue   = phText;
            so.ApplyModifiedProperties();

            return field;
        }

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
