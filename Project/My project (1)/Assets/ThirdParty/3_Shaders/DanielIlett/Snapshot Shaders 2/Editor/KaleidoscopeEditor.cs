using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(KaleidoscopeSettings))]
    public class KaleidoscopeEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter segments;

        protected override string BannerTextureName { get { return "KaleidoscopeBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<KaleidoscopeSettings>(serializedObject);
            FetchBasicParameters(o);

            segments = Unpack(o.Find(x => x.segments));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<KaleidoscopeFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Kaleidoscope Settings", HeaderStyle);
            PropertyField(segments);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
