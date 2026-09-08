using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(GlobalMaskSettings))]
    public class GlobalMaskEditor : SnapshotVolumeComponentEditor
    {
        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalMaskSettings>(serializedObject);
            FetchBasicParameters(o);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SnapshotHelper.DisplayFeatureExistsHelpBox<GlobalMaskFeature>();

            EditorGUILayout.LabelField("Basic Settings", HeaderStyle);
            PropertyField(renderPassEvent);

            DrawDivider();

            DrawMaskSettings();
        }
    }
}
