using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using System.IO;

namespace VRAdaptation.Editor
{
    /// <summary>
    /// 마우스/키보드로 VR 컨트롤러를 시뮬레이션할 수 있도록 XR Device Simulator를 씬에 추가한다.
    /// </summary>
    public static class SimulatorSetup
    {
        const string SimulatorObjName = "XR Device Simulator";
        const string SampleDstRoot    = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator";

        const string SimulatorActionsPath   = SampleDstRoot + "/XR Device Simulator Controls.inputactions";
        const string ControllerActionsPath  = SampleDstRoot + "/XR Device Controller Controls.inputactions";
        const string HandActionsPath        = SampleDstRoot + "/XR Device Hand Controls.inputactions";
        const string SimulatorPrefabPath    = SampleDstRoot + "/XR Device Simulator.prefab";

        // ── 메뉴: 씬에 시뮬레이터 오브젝트 추가 ──────────────────────────────
        [MenuItem("VR Adaptation/Add XR Device Simulator (Mouse Test)")]
        public static void AddSimulatorToScene()
        {
            // 이미 씬에 있으면 선택만 한다.
            GameObject existing = GameObject.Find(SimulatorObjName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.Log("[SimulatorSetup] XR Device Simulator가 이미 씬에 있습니다.");
                return;
            }

            // Samples 폴더에 프리팹이 있으면 그것을 인스턴스화
            AssetDatabase.Refresh();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Add XR Device Simulator");
                // 프리팹에 이미 액션이 연결돼 있지 않으면 수동으로 연결한다.
                var sim = instance.GetComponent<XRDeviceSimulator>();
                if (sim != null) AssignActionAssets(sim);
                Selection.activeGameObject = instance;
                Debug.Log("[SimulatorSetup] XR Device Simulator 프리팹을 씬에 추가했습니다.");
                ShowControls();
                return;
            }

            // 프리팹이 없으면 빈 오브젝트에 컴포넌트 직접 추가하고 액션 에셋을 연결한다.
            GameObject simObj = new GameObject(SimulatorObjName);
            Undo.RegisterCreatedObjectUndo(simObj, "Add XR Device Simulator");
            var sim = simObj.AddComponent<XRDeviceSimulator>();
            AssignActionAssets(sim);
            Selection.activeGameObject = simObj;

            Debug.Log("[SimulatorSetup] XR Device Simulator 컴포넌트를 씬에 추가했습니다.");
            ShowControls();
        }

        // ── 메뉴: 씬에서 시뮬레이터 제거 (빌드/실기기 테스트 전) ───────────
        [MenuItem("VR Adaptation/Remove XR Device Simulator")]
        public static void RemoveSimulatorFromScene()
        {
            GameObject sim = GameObject.Find(SimulatorObjName);
            if (sim != null)
            {
                Undo.DestroyObjectImmediate(sim);
                Debug.Log("[SimulatorSetup] XR Device Simulator를 씬에서 제거했습니다.");
            }
            else
            {
                Debug.Log("[SimulatorSetup] 씬에 XR Device Simulator가 없습니다.");
            }
        }

        static void AssignActionAssets(XRDeviceSimulator sim)
        {
            AssetDatabase.Refresh();

            var simActions  = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(SimulatorActionsPath);
            var ctrlActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(ControllerActionsPath);
            var handActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(HandActionsPath);

            var so = new SerializedObject(sim);
            if (simActions  != null) so.FindProperty("m_DeviceSimulatorActionAsset").objectReferenceValue = simActions;
            if (ctrlActions != null) so.FindProperty("m_ControllerActionAsset").objectReferenceValue      = ctrlActions;
            if (handActions != null) so.FindProperty("m_HandActionAsset").objectReferenceValue            = handActions;
            so.ApplyModifiedProperties();

            if (simActions == null || ctrlActions == null || handActions == null)
                Debug.LogWarning("[SimulatorSetup] 일부 Action Asset을 찾지 못했습니다. Inspector에서 수동으로 연결하세요.");
            else
                Debug.Log("[SimulatorSetup] Device Simulator / Controller / Hand Action Asset 연결 완료.");
        }

        static void ShowControls()
        {
            Debug.Log(
                "[SimulatorSetup] ── 마우스/키보드 조작법 ──\n" +
                "  마우스 오른쪽 드래그  : HMD 시선 회전\n" +
                "  WASD / 화살표키      : 이동\n" +
                "  Q / E               : 위/아래 이동\n" +
                "  Left Shift (hold)   : 왼쪽 컨트롤러 조작 모드\n" +
                "  Left Shift + Space  : 오른쪽 컨트롤러 조작 모드\n" +
                "  G                   : 왼쪽 컨트롤러 Grip\n" +
                "  T                   : 왼쪽 컨트롤러 Trigger (발사)\n" +
                "  마우스 왼쪽 클릭    : 활성 컨트롤러 Trigger (발사)\n" +
                "  ────────────────────────────────────────"
            );
        }
    }
}
