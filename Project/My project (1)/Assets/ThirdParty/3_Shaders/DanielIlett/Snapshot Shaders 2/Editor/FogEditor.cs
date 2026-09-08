using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(FogSettings))]
    public class FogEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter distanceStrength;
        SerializedDataParameter startDistance;
        SerializedDataParameter startDistanceColor;
        SerializedDataParameter fullStrengthDistance;
        SerializedDataParameter fullStrengthDistanceColor;
        SerializedDataParameter distanceFalloff;

        SerializedDataParameter heightStrength;
        SerializedDataParameter startHeight;
        SerializedDataParameter startHeightColor;
        SerializedDataParameter fullStrengthHeight;
        SerializedDataParameter fullStrengthHeightColor;
        SerializedDataParameter useFogAtSkybox;
        SerializedDataParameter skyboxHeight;
        SerializedDataParameter heightFalloff;

        protected override string BannerTextureName { get { return "FogBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FogSettings>(serializedObject);
            FetchBasicParameters(o);

            distanceStrength = Unpack(o.Find(x => x.distanceStrength));
            startDistance = Unpack(o.Find(x => x.startDistance));
            startDistanceColor = Unpack(o.Find(x => x.startDistanceColor));
            fullStrengthDistance = Unpack(o.Find(x => x.fullStrengthDistance));
            fullStrengthDistanceColor = Unpack(o.Find(x => x.fullStrengthDistanceColor));
            distanceFalloff = Unpack(o.Find(x => x.distanceFalloff));

            heightStrength = Unpack(o.Find(x => x.heightStrength));
            startHeight = Unpack(o.Find(x => x.startHeight));
            startHeightColor = Unpack(o.Find(x => x.startHeightColor));
            fullStrengthHeight = Unpack(o.Find(x => x.fullStrengthHeight));
            fullStrengthHeightColor = Unpack(o.Find(x => x.fullStrengthHeightColor));
            useFogAtSkybox = Unpack(o.Find(x => x.useFogAtSkybox));
            skyboxHeight = Unpack(o.Find(x => x.skyboxHeight));
            heightFalloff = Unpack(o.Find(x => x.heightFalloff));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<FogFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Distance Fog Settings", HeaderStyle);
            PropertyField(distanceStrength);
            PropertyField(startDistance);
            PropertyField(startDistanceColor);
            PropertyField(fullStrengthDistance);
            PropertyField(fullStrengthDistanceColor);
            PropertyField(distanceFalloff);

            EditorGUILayout.LabelField("Height Fog Settings", HeaderStyle);
            PropertyField(heightStrength);
            PropertyField(startHeight);
            PropertyField(startHeightColor);
            PropertyField(fullStrengthHeight);
            PropertyField(fullStrengthHeightColor);
            PropertyField(useFogAtSkybox);

            if(useFogAtSkybox.value.boolValue)
            {
                PropertyField(skyboxHeight);
            }
            
            PropertyField(heightFalloff);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
