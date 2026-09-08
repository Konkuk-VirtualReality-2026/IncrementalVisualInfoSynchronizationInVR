using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(InvertSettings))]
    public class InvertEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter strength;

        protected override string BannerTextureName { get { return "InvertBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<InvertSettings>(serializedObject);
            FetchBasicParameters(o);

            strength = Unpack(o.Find(x => x.strength));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<InvertFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Invert Settings", HeaderStyle);
            PropertyField(strength);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
