using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(ColorizeSettings))]
    public class ColorizeEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter tintColor;

        protected override string BannerTextureName { get { return "ColorizeBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ColorizeSettings>(serializedObject);
            FetchBasicParameters(o);

            tintColor = Unpack(o.Find(x => x.tintColor));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<ColorizeFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Colorize Settings", HeaderStyle);
            PropertyField(tintColor);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
