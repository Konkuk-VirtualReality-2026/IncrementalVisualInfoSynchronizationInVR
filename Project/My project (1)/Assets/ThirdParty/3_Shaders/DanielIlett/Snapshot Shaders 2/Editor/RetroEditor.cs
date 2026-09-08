using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(RetroSettings))]
    public class RetroEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter enabled;
        SerializedDataParameter drawingMode;
        SerializedDataParameter redLevels;
        SerializedDataParameter greenLevels;
        SerializedDataParameter blueLevels;
        SerializedDataParameter darkestColor;
        SerializedDataParameter darkColor;
        SerializedDataParameter lightColor;
        SerializedDataParameter lightestColor;
        SerializedDataParameter powerRamp;

        protected override string BannerTextureName { get { return "RetroBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<RetroSettings>(serializedObject);
            FetchBasicParameters(o);

            enabled = Unpack(o.Find(x => x.enabled));
            drawingMode = Unpack(o.Find(x => x.drawingMode));
            redLevels = Unpack(o.Find(x => x.redLevels));
            greenLevels = Unpack(o.Find(x => x.greenLevels));
            blueLevels = Unpack(o.Find(x => x.blueLevels));
            darkestColor = Unpack(o.Find(x => x.darkestColor));
            darkColor = Unpack(o.Find(x => x.darkColor));
            lightColor = Unpack(o.Find(x => x.lightColor));
            lightestColor = Unpack(o.Find(x => x.lightestColor));
            powerRamp = Unpack(o.Find(x => x.powerRamp));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<RetroFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Retro Settings", HeaderStyle);
            PropertyField(enabled);
            PropertyField(drawingMode);

            if(drawingMode.value.intValue == 0)
            {
                PropertyField(darkestColor);
                PropertyField(darkColor);
                PropertyField(lightColor);
                PropertyField(lightestColor);
            }
            else
            {
                PropertyField(redLevels);
                PropertyField(greenLevels);
                PropertyField(blueLevels);
            }
            
            PropertyField(powerRamp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

