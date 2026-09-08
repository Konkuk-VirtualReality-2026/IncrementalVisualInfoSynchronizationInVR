using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(GlitchSettings))]
    public class GlitchEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter offsetStrength;
        SerializedDataParameter offsetTexture;
        SerializedDataParameter usePointFilter;
        SerializedDataParameter offsetSpeed;
        SerializedDataParameter offsetTiling;

        SerializedDataParameter useSliceBandGlitches;
        SerializedDataParameter sliceBandSpeed;
        SerializedDataParameter sliceBandJitter;
        SerializedDataParameter sliceBandWidth;
        SerializedDataParameter sliceBandMinDuration;
        SerializedDataParameter sliceBandMaxDuration;
        SerializedDataParameter sliceBandFrequency;
        SerializedDataParameter sliceBandStrength;

        SerializedDataParameter useBlockArtifacts;
        SerializedDataParameter blockArtifactTiling;
        SerializedDataParameter blockArtifactChance;
        SerializedDataParameter blockArtifactStrengthMin;
        SerializedDataParameter blockArtifactStrengthMax;
        SerializedDataParameter blockArtifactSpeed;

        protected override string BannerTextureName { get { return "GlitchBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlitchSettings>(serializedObject);
            FetchBasicParameters(o);

            offsetStrength = Unpack(o.Find(x => x.offsetStrength));
            offsetTexture = Unpack(o.Find(x => x.offsetTexture));
            usePointFilter = Unpack(o.Find(x => x.usePointFilter));
            offsetSpeed = Unpack(o.Find(x => x.offsetSpeed));
            offsetTiling = Unpack(o.Find(x => x.offsetTiling));

            useSliceBandGlitches = Unpack(o.Find(x => x.useSliceBandGlitches));
            sliceBandStrength = Unpack(o.Find(x => x.sliceBandStrength));
            sliceBandSpeed = Unpack(o.Find(x => x.sliceBandSpeed));
            sliceBandJitter = Unpack(o.Find(x => x.sliceBandJitter));
            sliceBandWidth = Unpack(o.Find(x => x.sliceBandWidth));
            sliceBandMinDuration = Unpack(o.Find(x => x.sliceBandMinDuration));
            sliceBandMaxDuration = Unpack(o.Find(x => x.sliceBandMaxDuration));
            sliceBandFrequency = Unpack(o.Find(x => x.sliceBandFrequency));

            useBlockArtifacts = Unpack(o.Find(x => x.useBlockArtifacts));
            blockArtifactTiling = Unpack(o.Find(x => x.blockArtifactTiling));
            blockArtifactChance = Unpack(o.Find(x => x.blockArtifactChance));
            blockArtifactStrengthMin = Unpack(o.Find(x => x.blockArtifactStrengthMin));
            blockArtifactStrengthMax = Unpack(o.Find(x => x.blockArtifactStrengthMax));
            blockArtifactSpeed = Unpack(o.Find(x => x.blockArtifactSpeed));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<GlitchFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Offset Glitch Settings", HeaderStyle);
            PropertyField(offsetStrength);
            PropertyField(offsetTexture);
            PropertyField(usePointFilter);
            PropertyField(offsetSpeed);
            PropertyField(offsetTiling);

            EditorGUILayout.LabelField("Slice Band Settings", HeaderStyle);
            PropertyField(useSliceBandGlitches);

            if(useSliceBandGlitches.value.boolValue)
            {
                PropertyField(sliceBandStrength);
                PropertyField(sliceBandSpeed);
                PropertyField(sliceBandJitter);
                PropertyField(sliceBandWidth);
                PropertyField(sliceBandMinDuration);
                PropertyField(sliceBandMaxDuration);
                PropertyField(sliceBandFrequency);
            }

            EditorGUILayout.LabelField("Block Artifact Settings", HeaderStyle);
            PropertyField(useBlockArtifacts);

            if(useBlockArtifacts.value.boolValue)
            {
                PropertyField(blockArtifactTiling);
                PropertyField(blockArtifactChance);
                PropertyField(blockArtifactStrengthMin);
                PropertyField(blockArtifactStrengthMax);
                PropertyField(blockArtifactSpeed);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
