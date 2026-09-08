using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(ScanlineSettings))]
    public class ScanlineEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter scanlineTexture;
        SerializedDataParameter scanlineStrength;
        SerializedDataParameter scanlineSize;
        SerializedDataParameter scanlineScrollSpeed;
        SerializedDataParameter usePointFiltering;

        protected override string BannerTextureName { get { return "ScanlineBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScanlineSettings>(serializedObject);
            FetchBasicParameters(o);

            scanlineTexture = Unpack(o.Find(x => x.scanlineTexture));
            scanlineStrength = Unpack(o.Find(x => x.scanlineStrength));
            scanlineSize = Unpack(o.Find(x => x.scanlineSize));
            scanlineScrollSpeed = Unpack(o.Find(x => x.scanlineScrollSpeed));
            usePointFiltering = Unpack(o.Find(x => x.usePointFiltering));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<ScanlineFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Scanline Settings", HeaderStyle);
            PropertyField(scanlineTexture);
            PropertyField(scanlineStrength);
            PropertyField(scanlineSize);
            PropertyField(scanlineScrollSpeed);
            PropertyField(usePointFiltering);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
