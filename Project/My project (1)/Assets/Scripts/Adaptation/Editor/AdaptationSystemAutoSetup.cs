using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;   // ContinuousMoveProvider
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;    // SnapTurnProvider
using VRAdaptation;
using VRAdaptation.AimTrainer;

namespace VRAdaptation.Editor
{
    public class AdaptationSystemAutoSetup : EditorWindow
    {
        [MenuItem("VR Adaptation/Auto Setup Adaptation System")]
        public static void SetupSystem()
        {
            // 1. Create or Find Manager Object
            GameObject managerObj = GameObject.Find("VR_Adaptation_Manager");
            if (managerObj == null)
            {
                managerObj = new GameObject("VR_Adaptation_Manager");
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Adaptation Manager");
            }

            // 2. Add and Configure GlobalAdaptationEffect
            GlobalAdaptationEffect globalEffect = managerObj.GetComponent<GlobalAdaptationEffect>();
            if (globalEffect == null)
            {
                globalEffect = managerObj.AddComponent<GlobalAdaptationEffect>();
            }

            // Automatically find the shader
            Shader adaptationShader = Shader.Find("VR/AdaptationProgressive");
            if (adaptationShader != null)
            {
                var so = new SerializedObject(globalEffect);
                so.FindProperty("m_AdaptationShader").objectReferenceValue = adaptationShader;
                
                // Set default target layers (Default, Environment)
                so.FindProperty("m_TargetLayers").intValue = (1 << 0) | (1 << 4); 
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[VRAdaptation] Shader 'VR/AdaptationProgressive' not found. Please assign it manually.");
            }

            // 3. Add and Configure AudioSource
            AudioSource audioSource = managerObj.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = managerObj.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D ambient for initial adaptation
            }

            // 4. Add and Configure VRAdaptationManager
            VRAdaptationManager manager = managerObj.GetComponent<VRAdaptationManager>();
            if (manager == null)
            {
                manager = managerObj.AddComponent<VRAdaptationManager>();
            }

            // 5. Setup Aim Trainer
            GameObject aimTrainerObj = GameObject.Find("Aim_Trainer_Manager");
            if (aimTrainerObj == null)
            {
                aimTrainerObj = new GameObject("Aim_Trainer_Manager");
                Undo.RegisterCreatedObjectUndo(aimTrainerObj, "Create Aim Trainer Manager");
            }
            
            var targetManager = aimTrainerObj.GetComponent<AimTargetManager>();
            if (targetManager == null) targetManager = aimTrainerObj.AddComponent<AimTargetManager>();
            
            var dataLogger = aimTrainerObj.GetComponent<ExperimentDataLogger>();
            if (dataLogger == null) dataLogger = aimTrainerObj.AddComponent<ExperimentDataLogger>();

            // Try to find main camera for tracking
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var targetSO = new SerializedObject(targetManager);
                targetSO.FindProperty("m_PlayerHead").objectReferenceValue = mainCam.transform;
                targetSO.ApplyModifiedProperties();
            }

            // 6. Blackout Canvas (Phase 1 완전 암흑용)
            // 오브젝트 셰이더는 스카이박스·오브젝트 사이 공간을 가릴 수 없으므로
            // XR 카메라 바로 앞에 전체 화면을 덮는 검정 UI 패널을 배치한다.
            GameObject canvasObj = GameObject.Find("BlackoutCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("BlackoutCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Blackout Canvas");
            }

            // World Space 캔버스 — VR 스테레오에서 Screen Space Overlay는
            // 한쪽 눈에만 렌더링되는 문제가 있어 World Space를 사용한다.
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasGroup canvasGroup = canvasObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha          = 1f;

            // XR 카메라 자식으로 부착 — Near Clip Plane(기본 0.3) 바로 앞 0.31에 위치
            // 캔버스 1000×1000px, scale 0.002 → 실제 2m×2m
            // Meta Quest 3 FOV ~100°, z=0.31 기준 필요 너비 ≈ 0.74m → 2m으로 충분히 커버
            if (mainCam != null)
            {
                canvasObj.transform.SetParent(mainCam.transform, false);
                canvasObj.transform.localPosition = new Vector3(0f, 0f, 0.31f);
                canvasObj.transform.localRotation = Quaternion.identity;
                canvasObj.transform.localScale    = Vector3.one * 0.002f;
            }
            else
            {
                Debug.LogWarning("[VRAdaptation] Main Camera를 찾지 못했습니다. BlackoutCanvas를 XR Camera 하위에 수동으로 배치하세요.");
            }

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 1000f);

            // 검정 Image 자식 생성 (없을 때만)
            Transform existingImage = canvasObj.transform.Find("BlackImage");
            GameObject imageObj;
            if (existingImage == null)
            {
                imageObj = new GameObject("BlackImage");
                Undo.RegisterCreatedObjectUndo(imageObj, "Create Black Image");
                imageObj.transform.SetParent(canvasObj.transform, false);
            }
            else
            {
                imageObj = existingImage.gameObject;
            }

            Image blackImage = imageObj.GetComponent<Image>();
            if (blackImage == null) blackImage = imageObj.AddComponent<Image>();
            blackImage.color = Color.black;

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            // 7. Manager에 모든 레퍼런스 연결
            var managerSO = new SerializedObject(manager);
            managerSO.FindProperty("m_GlobalEffect").objectReferenceValue       = globalEffect;
            managerSO.FindProperty("m_AmbientAudioSource").objectReferenceValue = audioSource;
            managerSO.FindProperty("m_AimTrainer").objectReferenceValue         = targetManager;
            managerSO.FindProperty("m_BlackoutPanel").objectReferenceValue      = canvasGroup;
            managerSO.ApplyModifiedProperties();

            // ── 8. VR 총 (RaycastWeapon + VRGunController) ──────────────────
            // Right Hand Controller를 찾아 총 컴포넌트를 부착한다.
            GameObject rightController = FindRightController();
            if (rightController != null)
            {
                // 총구 위치용 자식 오브젝트
                Transform muzzle = rightController.transform.Find("Muzzle");
                if (muzzle == null)
                {
                    GameObject muzzleObj = new GameObject("Muzzle");
                    Undo.RegisterCreatedObjectUndo(muzzleObj, "Create Muzzle");
                    muzzleObj.transform.SetParent(rightController.transform, false);
                    muzzleObj.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                    muzzle = muzzleObj.transform;
                }

                RaycastWeapon weapon = rightController.GetComponent<RaycastWeapon>();
                if (weapon == null) weapon = rightController.AddComponent<RaycastWeapon>();

                var weaponSO = new SerializedObject(weapon);
                weaponSO.FindProperty("m_MuzzlePoint").objectReferenceValue = muzzle;
                // AimTrainer 타겟 레이어만 맞힘 (Default 레이어 = 0)
                weaponSO.FindProperty("m_TargetLayer").intValue = 1 << 0;
                weaponSO.ApplyModifiedProperties();

                VRGunController gunCtrl = rightController.GetComponent<VRGunController>();
                if (gunCtrl == null) gunCtrl = rightController.AddComponent<VRGunController>();

                // XRI Default Input Actions에서 Activate 액션을 자동 탐색해 연결한다.
                var fireActionRef = FindInputActionReference("XRI Right Hand Interaction/Activate");
                if (fireActionRef != null)
                {
                    var gunSO = new SerializedObject(gunCtrl);
                    gunSO.FindProperty("m_FireAction").objectReferenceValue = fireActionRef;
                    gunSO.ApplyModifiedProperties();
                    Debug.Log("[VRAdaptation] Gun: FireAction 자동 연결 완료.");
                }
                else
                {
                    Debug.LogWarning("[VRAdaptation] 'XRI Right Hand Interaction/Activate' 액션을 찾지 못했습니다. VRGunController의 Fire Action을 수동으로 연결하세요.");
                }

                Debug.Log($"[VRAdaptation] Gun: RaycastWeapon + VRGunController → {rightController.name}");
            }
            else
            {
                Debug.LogWarning("[VRAdaptation] Right Hand Controller를 찾지 못했습니다. XR Origin 하위에 'Right'가 포함된 오브젝트가 있어야 합니다.");
            }

            // ── 9. 이동/회전 (Locomotion) ────────────────────────────────────
            // XR Origin에 Continuous Move + Snap Turn 프로바이더를 추가한다.
            // 이미 존재하는 경우 중복 추가하지 않는다.
            var xrOrigin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                GameObject xrOriginObj = xrOrigin.gameObject;

                // Continuous Move (왼쪽 스틱) — XRI 3.x: ContinuousMoveProvider
                if (xrOriginObj.GetComponent<ContinuousMoveProvider>() == null)
                {
                    var moveProvider = xrOriginObj.AddComponent<ContinuousMoveProvider>();
                    moveProvider.moveSpeed = 1.5f;

                    // XRI 3.x XRInputValueReader: m_LeftHandMoveInput.m_InputActionReference
                    var leftMoveRef  = FindInputActionReference("XRI Left Hand Locomotion/Move");
                    var rightMoveRef = FindInputActionReference("XRI Right Hand Locomotion/Move");
                    if (leftMoveRef != null || rightMoveRef != null)
                    {
                        var moveSO = new SerializedObject(moveProvider);
                        if (leftMoveRef  != null) moveSO.FindProperty("m_LeftHandMoveInput").FindPropertyRelative("m_InputActionReference").objectReferenceValue  = leftMoveRef;
                        if (rightMoveRef != null) moveSO.FindProperty("m_RightHandMoveInput").FindPropertyRelative("m_InputActionReference").objectReferenceValue = rightMoveRef;
                        moveSO.ApplyModifiedProperties();
                        Debug.Log("[VRAdaptation] Locomotion: MoveProvider 액션 연결 완료.");
                    }
                    else
                    {
                        Debug.LogWarning("[VRAdaptation] Move 액션을 찾지 못했습니다. ContinuousMoveProvider의 입력을 수동으로 연결하세요.");
                    }
                }

                // Snap Turn (오른쪽 스틱) — XRI 3.x: SnapTurnProvider
                if (xrOriginObj.GetComponent<SnapTurnProvider>() == null)
                {
                    var turnProvider = xrOriginObj.AddComponent<SnapTurnProvider>();
                    turnProvider.turnAmount = 45f;

                    // XRI 3.x: m_LeftHandTurnInput / m_RightHandTurnInput
                    var leftTurnRef  = FindInputActionReference("XRI Left Hand Locomotion/Snap Turn");
                    var rightTurnRef = FindInputActionReference("XRI Right Hand Locomotion/Snap Turn");
                    if (leftTurnRef != null || rightTurnRef != null)
                    {
                        var turnSO = new SerializedObject(turnProvider);
                        if (leftTurnRef  != null) turnSO.FindProperty("m_LeftHandTurnInput").FindPropertyRelative("m_InputActionReference").objectReferenceValue  = leftTurnRef;
                        if (rightTurnRef != null) turnSO.FindProperty("m_RightHandTurnInput").FindPropertyRelative("m_InputActionReference").objectReferenceValue = rightTurnRef;
                        turnSO.ApplyModifiedProperties();
                        Debug.Log("[VRAdaptation] Locomotion: SnapTurnProvider 액션 연결 완료.");
                    }
                    else
                    {
                        Debug.LogWarning("[VRAdaptation] Snap Turn 액션을 찾지 못했습니다. SnapTurnProvider의 입력을 수동으로 연결하세요.");
                    }
                }

                Debug.Log($"[VRAdaptation] Locomotion: Move + SnapTurn → {xrOriginObj.name}");
            }
            else
            {
                Debug.LogWarning("[VRAdaptation] XROrigin을 씬에서 찾지 못했습니다. 이동 기능을 수동으로 설정하세요.");
            }

            Selection.activeGameObject = managerObj;
            Debug.Log("[VRAdaptation] Auto setup complete: Manager + AimTrainer + BlackoutCanvas + Gun + Locomotion 완료.");
        }

        // XR Origin 하위에서 Right Hand Controller 오브젝트를 탐색한다.
        static GameObject FindRightController()
        {
            // 이름에 "Right"가 들어간 XRBaseController 또는 ActionBasedController를 찾는다.
            var controllers = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(
                UnityEngine.FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                if (c.gameObject.name.ToLower().Contains("right"))
                    return c.gameObject;
            }
            // 폴백: 이름 기반 탐색
            string[] candidates = { "Right Controller", "Right Hand Controller", "RightHand", "Right" };
            foreach (var name in candidates)
            {
                GameObject go = GameObject.Find(name);
                if (go != null) return go;
            }
            return null;
        }

        // 프로젝트 내 모든 InputActionAsset에서 actionPath("Map/Action")로 InputActionReference를 탐색한다.
        static InputActionReference FindInputActionReference(string actionPath)
        {
            string[] parts = actionPath.Split('/');
            if (parts.Length < 2) return null;
            string mapName    = parts[0];
            string actionName = parts[1];

            string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
            foreach (string guid in guids)
            {
                string path  = AssetDatabase.GUIDToAssetPath(guid);
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object obj in all)
                {
                    if (obj is InputActionReference actionRef)
                    {
                        if (actionRef.action?.actionMap?.name == mapName &&
                            actionRef.action?.name            == actionName)
                            return actionRef;
                    }
                }
            }
            return null;
        }
    }
}
