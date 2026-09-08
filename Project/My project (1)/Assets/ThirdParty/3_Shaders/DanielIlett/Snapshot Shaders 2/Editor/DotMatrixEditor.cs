using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(DotMatrixSettings))]
    public class DotMatrixEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter gapWidth;
        SerializedDataParameter dotSize;
        SerializedDataParameter backgroundColor;

        protected override string BannerTextureName { get { return "DotMatrixBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DotMatrixSettings>(serializedObject);
            FetchBasicParameters(o);

            gapWidth = Unpack(o.Find(x => x.gapWidth));
            dotSize = Unpack(o.Find(x => x.dotSize));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<DotMatrixFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Dot Matrix Settings", HeaderStyle);
            PropertyField(gapWidth);
            PropertyField(dotSize);
            PropertyField(backgroundColor);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
