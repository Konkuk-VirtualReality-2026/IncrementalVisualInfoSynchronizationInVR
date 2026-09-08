using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(SynthwaveSettings))]
    public class SynthwaveEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter useSceneColor;
        SerializedDataParameter backgroundColor;
        SerializedDataParameter lineColor1;
        SerializedDataParameter lineColor2;
        SerializedDataParameter lineColorMix;
        SerializedDataParameter lineWidth;
        SerializedDataParameter lineSoftness;
        SerializedDataParameter gapWidth;
        SerializedDataParameter lineOffset;
        SerializedDataParameter startFadeoutDistance;
        SerializedDataParameter endFadeoutDistance;
        SerializedDataParameter axisMask;

        protected override string BannerTextureName { get { return "SynthwaveBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SynthwaveSettings>(serializedObject);
            FetchBasicParameters(o);

            useSceneColor = Unpack(o.Find(x => x.useSceneColor));
            backgroundColor = Unpack(o.Find(x => x.backgroundColor));
            lineColor1 = Unpack(o.Find(x => x.lineColor1));
            lineColor2 = Unpack(o.Find(x => x.lineColor2));
            lineColorMix = Unpack(o.Find(x => x.lineColorMix));
            lineWidth = Unpack(o.Find(x => x.lineWidth));
            lineSoftness = Unpack(o.Find(x => x.lineSoftness));
            gapWidth = Unpack(o.Find(x => x.gapWidth));
            lineOffset = Unpack(o.Find(x => x.lineOffset));
            startFadeoutDistance = Unpack(o.Find(x => x.startFadeoutDistance));
            endFadeoutDistance = Unpack(o.Find(x => x.endFadeoutDistance));
            axisMask = Unpack(o.Find(x => x.axisMask));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<SynthwaveFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Synthwave Settings", HeaderStyle);
            PropertyField(useSceneColor);

            if(useSceneColor.value.boolValue == false)
            {
                PropertyField(backgroundColor);
            }
            
            PropertyField(lineColor1);
            PropertyField(lineColor2);
            PropertyField(lineColorMix);
            PropertyField(lineWidth);
            PropertyField(lineSoftness);
            PropertyField(gapWidth);
            PropertyField(lineOffset);
            PropertyField(startFadeoutDistance);
            PropertyField(endFadeoutDistance);
            PropertyField(axisMask);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
