using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(GreyscaleSettings))]
    public sealed class GreyscaleEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter strength;

        protected override string BannerTextureName { get { return "GreyscaleBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GreyscaleSettings>(serializedObject);
            FetchBasicParameters(o);

            strength = Unpack(o.Find(x => x.strength));
        }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DrawBanner();

        DrawBasicSettings<GreyscaleFeature>(true);

        DrawDivider();

        EditorGUILayout.LabelField("Greyscale Settings", HeaderStyle);
        PropertyField(strength);

        serializedObject.ApplyModifiedProperties();
    }
    }
}
