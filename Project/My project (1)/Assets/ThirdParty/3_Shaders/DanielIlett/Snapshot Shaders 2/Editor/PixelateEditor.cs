using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(PixelateSettings))]
    public class PixelateEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter downsampleSize;
        SerializedDataParameter usePointFiltering;
        SerializedDataParameter expandMask;

        protected override string BannerTextureName { get { return "PixelateBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PixelateSettings>(serializedObject);
            FetchBasicParameters(o);

            downsampleSize = Unpack(o.Find(x => x.downsampleSize));
            usePointFiltering = Unpack(o.Find(x => x.usePointFiltering));
            expandMask = Unpack(o.Find(x => x.expandMask));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<PixelateFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Pixelate Settings", HeaderStyle);
            PropertyField(downsampleSize);
            PropertyField(usePointFiltering);

            var maskModeValue = maskMode.value.GetEnumValue<MaskMode>();
            if (maskModeValue != MaskMode.None)
            {
                PropertyField(expandMask);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
