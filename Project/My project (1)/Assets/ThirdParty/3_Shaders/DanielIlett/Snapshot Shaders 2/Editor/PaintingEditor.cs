using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(PaintingSettings))]
    public class PaintingEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter drawingMode;
        SerializedDataParameter kernelSize;

        protected override string BannerTextureName { get { return "PaintingBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PaintingSettings>(serializedObject);
            FetchBasicParameters(o);

            drawingMode = Unpack(o.Find(x => x.drawingMode));
            kernelSize = Unpack(o.Find(x => x.kernelSize));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<PaintingFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Painting Settings", HeaderStyle);
            PropertyField(drawingMode);
            PropertyField(kernelSize);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
