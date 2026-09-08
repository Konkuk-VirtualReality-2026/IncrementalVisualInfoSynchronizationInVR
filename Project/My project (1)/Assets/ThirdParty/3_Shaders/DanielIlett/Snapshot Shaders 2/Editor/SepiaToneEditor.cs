using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(SepiaToneSettings))]
    public sealed class SepiaToneEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter strength;

        protected override string BannerTextureName { get { return "SepiaToneBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SepiaToneSettings>(serializedObject);
            FetchBasicParameters(o);

            strength = Unpack(o.Find(x => x.strength));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<SepiaToneFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Sepia Tone Settings", HeaderStyle);
            PropertyField(strength);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
