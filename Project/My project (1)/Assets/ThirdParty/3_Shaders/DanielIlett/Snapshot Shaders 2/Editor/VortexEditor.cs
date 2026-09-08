using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(VortexSettings))]
    public class VortexEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter strength;
        SerializedDataParameter center;
        SerializedDataParameter offset;
        SerializedDataParameter rotation;

        protected override string BannerTextureName { get { return "VortexBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VortexSettings>(serializedObject);
            FetchBasicParameters(o);

            strength = Unpack(o.Find(x => x.strength));
            center = Unpack(o.Find(x => x.center));
            offset = Unpack(o.Find(x => x.offset));
            rotation = Unpack(o.Find(x => x.rotation));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawBanner();

            DrawBasicSettings<VortexFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Vortex Settings", HeaderStyle);
            PropertyField(strength);
            PropertyField(center);
            PropertyField(offset);
            PropertyField(rotation);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
