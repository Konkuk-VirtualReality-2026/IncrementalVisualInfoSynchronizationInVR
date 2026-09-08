using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(DitherSettings))]
    public class DitherEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter noiseTexture;
        SerializedDataParameter noiseSize;
        SerializedDataParameter luminanceThreshold;
        SerializedDataParameter darkColor;
        SerializedDataParameter useSceneColor;
        SerializedDataParameter lightColor;

        protected override string BannerTextureName { get { return "DitherBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DitherSettings>(serializedObject);
            FetchBasicParameters(o);

            noiseTexture = Unpack(o.Find(x => x.noiseTexture));
            noiseSize = Unpack(o.Find(x => x.noiseSize));
            luminanceThreshold = Unpack(o.Find(x => x.luminanceThreshold));
            darkColor = Unpack(o.Find(x => x.darkColor));
            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
            lightColor = Unpack(o.Find(x => x.lightColor));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<DitherFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Dither Settings", HeaderStyle);
            PropertyField(noiseTexture);
            PropertyField(noiseSize);
            PropertyField(luminanceThreshold);
            PropertyField(darkColor);
            PropertyField(useSceneColor);

            if(!useSceneColor.value.boolValue)
            {
                PropertyField(lightColor);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
