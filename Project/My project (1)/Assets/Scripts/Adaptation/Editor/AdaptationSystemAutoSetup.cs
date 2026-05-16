using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
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

            Selection.activeGameObject = managerObj;
            Debug.Log("[VRAdaptation] Auto setup complete: Manager + AimTrainer + BlackoutCanvas 생성 및 연결 완료.");
        }
    }
}
