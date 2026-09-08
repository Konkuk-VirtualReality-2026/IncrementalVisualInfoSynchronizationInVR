using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(DistortionSettings))]
    public class DistortionEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter strength;
        SerializedDataParameter smoothing;
        SerializedDataParameter backgroundColor;

        protected override string BannerTextureName { get { return "DistortionBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DistortionSettings>(serializedObject);
            FetchBasicParameters(o);

            strength = Unpack(o.Find(x => x.strength));
            smoothing = Unpack(o.Find(x => x.smoothing));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<DistortionFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Distortion Settings", HeaderStyle);
            PropertyField(strength);
            PropertyField(smoothing);
            PropertyField(backgroundColor);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
