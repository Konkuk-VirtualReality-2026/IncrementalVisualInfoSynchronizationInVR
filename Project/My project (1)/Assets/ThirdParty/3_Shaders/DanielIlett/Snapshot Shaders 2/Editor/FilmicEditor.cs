using UnityEditor;
using UnityEditor.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(FilmicSettings))]
    public sealed class FilmicEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter useFilmBars;
        SerializedDataParameter aspectRatio;
        SerializedDataParameter filmBarColor;
        SerializedDataParameter noiseStrength;
        SerializedDataParameter noiseSpeed;
        SerializedDataParameter noiseSize;
        SerializedDataParameter noiseInterpolation;

        protected override string BannerTextureName { get { return "FilmicBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FilmicSettings>(serializedObject);
            FetchBasicParameters(o);

            useFilmBars = Unpack(o.Find(x => x.useFilmBars));
            aspectRatio = Unpack(o.Find(x => x.aspectRatio));
            filmBarColor = Unpack(o.Find(x => x.filmBarColor));
            noiseStrength = Unpack(o.Find(x => x.noiseStrength));
            noiseSpeed = Unpack(o.Find(x => x.noiseSpeed));
            noiseSize = Unpack(o.Find(x => x.noiseSize));
            noiseInterpolation = Unpack(o.Find(x => x.noiseInterpolation));
        }

        public override void OnInspectorGUI()
        {
            DrawBanner();

            DrawBasicSettings<FilmicFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Filmic Settings", HeaderStyle);
            PropertyField(useFilmBars);
            if(useFilmBars.value.boolValue)
            {
                var maskModeValue = maskMode.value.GetEnumValue<MaskMode>();
                if (maskModeValue != MaskMode.None)
                {
                    EditorGUILayout.HelpBox($"Note that the film bars will ignore the mask and always be applied.", MessageType.Info);
                }

                PropertyField(aspectRatio);
                PropertyField(filmBarColor);
            }
            
            PropertyField(noiseStrength);
            PropertyField(noiseSpeed);
            PropertyField(noiseSize);
            PropertyField(noiseInterpolation);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
