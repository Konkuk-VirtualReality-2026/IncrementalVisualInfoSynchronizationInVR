using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(UnderwaterSettings))]
    public class UnderwaterEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter waveFlowMap;
        SerializedDataParameter waveStrength;
        SerializedDataParameter waveFlowTiling;
        SerializedDataParameter waveFlowSpeed;
        SerializedDataParameter causticsMode;
        SerializedDataParameter causticsTexture;
        SerializedDataParameter causticsTint;
        SerializedDataParameter causticsTiling1;
        SerializedDataParameter causticsTiling2;
        SerializedDataParameter causticsScrollVelocity1;
        SerializedDataParameter causticsScrollVelocity2;
        SerializedDataParameter causticsColorSeparation;
        SerializedDataParameter causticsStartFade;
        SerializedDataParameter causticsFadeFalloff;

        protected override string BannerTextureName { get { return "UnderwaterBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<UnderwaterSettings>(serializedObject);
            FetchBasicParameters(o);

            waveFlowMap = Unpack(o.Find(x => x.waveFlowMap));
            waveStrength = Unpack(o.Find(x => x.waveStrength));
            waveFlowTiling = Unpack(o.Find(x => x.waveFlowTiling));
            waveFlowSpeed = Unpack(o.Find(x => x.waveFlowSpeed));
            causticsMode = Unpack(o.Find(x => x.causticsMode));
            causticsTexture = Unpack(o.Find(x => x.causticsTexture));
            causticsTint = Unpack(o.Find(x => x.causticsTint));
            causticsTiling1 = Unpack(o.Find(x => x.causticsTiling1));
            causticsTiling2 = Unpack(o.Find(x => x.causticsTiling2));
            causticsScrollVelocity1 = Unpack(o.Find(x => x.causticsScrollVelocity1));
            causticsScrollVelocity2 = Unpack(o.Find(x => x.causticsScrollVelocity2));
            causticsColorSeparation = Unpack(o.Find(x => x.causticsColorSeparation));
            causticsStartFade = Unpack(o.Find(x => x.causticsStartFade));
            causticsFadeFalloff = Unpack(o.Find(x => x.causticsFadeFalloff));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<UnderwaterFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Underwater Settings", HeaderStyle);
            PropertyField(waveFlowMap);
            PropertyField(waveStrength);
            PropertyField(waveFlowTiling);
            PropertyField(waveFlowSpeed);

            EditorGUILayout.LabelField("Caustics Settings", HeaderStyle);

            PropertyField(causticsMode);

            if(causticsMode.value.GetEnumValue<CausticsMode>() != CausticsMode.Off)
            {
                PropertyField(causticsTexture);
                PropertyField(causticsTint);
                PropertyField(causticsTiling1);
                PropertyField(causticsTiling2);
                PropertyField(causticsScrollVelocity1);
                PropertyField(causticsScrollVelocity2);
                PropertyField(causticsColorSeparation);
                PropertyField(causticsStartFade);
                PropertyField(causticsFadeFalloff);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
