using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(DebugMaskSettings))]
    public class DebugMaskEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter enabled;

        protected override string BannerTextureName { get { return "DebugMaskBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DebugMaskSettings>(serializedObject);
            FetchBasicParameters(o);

            enabled = Unpack(o.Find(x => x.enabled));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<DebugMaskFeature>(true);

            var maskModeValue = maskMode.value.GetEnumValue<MaskMode>();
            if (maskModeValue == MaskMode.None)
            {
                EditorGUILayout.HelpBox($"This effect does nothing when no mask is set.", MessageType.Info);
            }
            else
            {
                DrawDivider();

                EditorGUILayout.LabelField("Debug Mask Settings", HeaderStyle);
                PropertyField(enabled);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
