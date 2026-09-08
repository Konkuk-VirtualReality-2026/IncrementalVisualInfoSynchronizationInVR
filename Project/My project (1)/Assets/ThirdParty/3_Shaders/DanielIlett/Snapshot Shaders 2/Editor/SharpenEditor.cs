using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(SharpenSettings))]
    public class SharpenEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter strength;

        protected override string BannerTextureName { get { return "SharpenBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SharpenSettings>(serializedObject);
            FetchBasicParameters(o);

            strength = Unpack(o.Find(x => x.strength));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<SharpenFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Sharpen Settings", HeaderStyle);
            PropertyField(strength);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
