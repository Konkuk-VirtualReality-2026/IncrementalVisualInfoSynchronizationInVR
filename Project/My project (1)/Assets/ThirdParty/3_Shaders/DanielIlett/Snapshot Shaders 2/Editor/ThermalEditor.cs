using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(ThermalSettings))]
    public class ThermalEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter enabled;
        SerializedDataParameter thermalMaskMode;
        SerializedDataParameter maskBlur;
        SerializedDataParameter maskBlurStepSize;
        SerializedDataParameter addBrightnessToMask;
        SerializedDataParameter color0;
        SerializedDataParameter threshold0;
        SerializedDataParameter color1;
        SerializedDataParameter threshold1;
        SerializedDataParameter color2;
        SerializedDataParameter threshold2;
        SerializedDataParameter color3;
        SerializedDataParameter threshold3;
        SerializedDataParameter color4;
        SerializedDataParameter threshold4;
        SerializedDataParameter color5;

        protected override string BannerTextureName { get { return "ThermalBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ThermalSettings>(serializedObject);
            FetchBasicParameters(o);

            enabled = Unpack(o.Find(x => x.enabled));
            thermalMaskMode = Unpack(o.Find(x => x.thermalMaskMode));
            maskBlur = Unpack(o.Find(x => x.maskBlur));
            maskBlurStepSize = Unpack(o.Find(x => x.maskBlurStepSize));
            addBrightnessToMask = Unpack(o.Find(x => x.addBrightnessToMask));
            color0 = Unpack(o.Find(x => x.color0));
            threshold0 = Unpack(o.Find(x => x.threshold0));
            color1 = Unpack(o.Find(x => x.color1));
            threshold1 = Unpack(o.Find(x => x.threshold1));
            color2 = Unpack(o.Find(x => x.color2));
            threshold2 = Unpack(o.Find(x => x.threshold2));
            color3 = Unpack(o.Find(x => x.color3));
            threshold3 = Unpack(o.Find(x => x.threshold3));
            color4 = Unpack(o.Find(x => x.color4));
            threshold4 = Unpack(o.Find(x => x.threshold4));
            color5 = Unpack(o.Find(x => x.color5));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<ThermalFeature>(true);

            var maskModeValue = maskMode.value.GetEnumValue<MaskMode>();
            if (maskModeValue == MaskMode.None)
            {
                EditorGUILayout.HelpBox($"This effect can use a mask to artifically set some objects to max brightness.", MessageType.Info);
            }

            DrawDivider();

            EditorGUILayout.LabelField("Thermal Settings", HeaderStyle);
            PropertyField(enabled);

            if(maskModeValue != MaskMode.None)
            {
                PropertyField(thermalMaskMode);
                PropertyField(maskBlur);
                PropertyField(maskBlurStepSize);
                PropertyField(addBrightnessToMask);
            }

            EditorGUILayout.LabelField("Colors and Thresholds", ItalicStyle);
            PropertyField(color0);
            PropertyField(threshold0);
            PropertyField(color1);
            PropertyField(threshold1);
            PropertyField(color2);
            PropertyField(threshold2);
            PropertyField(color3);
            PropertyField(threshold3);
            PropertyField(color4);
            PropertyField(threshold4);
            PropertyField(color5);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
