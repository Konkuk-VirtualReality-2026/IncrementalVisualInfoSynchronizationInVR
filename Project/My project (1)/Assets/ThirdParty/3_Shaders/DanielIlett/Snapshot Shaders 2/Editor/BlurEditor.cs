using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(BlurSettings))]
    public class BlurEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter blurMode;
        SerializedDataParameter strength;
        SerializedDataParameter blurStepSize;
        SerializedDataParameter luminanceThreshold;

        protected override string BannerTextureName { get { return "BlurBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<BlurSettings>(serializedObject);
            FetchBasicParameters(o);

            blurMode = Unpack(o.Find(x => x.blurMode));
            strength = Unpack(o.Find(x => x.strength));
            blurStepSize = Unpack(o.Find(x => x.blurStepSize));
            luminanceThreshold = Unpack(o.Find(x => x.luminanceThreshold));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<BlurFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Blur Settings", HeaderStyle);
            PropertyField(blurMode);
            PropertyField(strength);
            PropertyField(blurStepSize);

            if(blurMode.value.GetEnumValue<BlurMode>() == BlurMode.LightStreaks)
            {
                PropertyField(luminanceThreshold);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
