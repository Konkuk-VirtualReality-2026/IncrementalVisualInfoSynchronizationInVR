using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [CustomEditor(typeof(SilhouetteSettings))]
    public sealed class SilhouetteEditor : SnapshotVolumeComponentEditor
    {
        SerializedDataParameter enabled;
        SerializedDataParameter useTextureRamp;
        SerializedDataParameter textureRamp;
        SerializedDataParameter nearColor;
        SerializedDataParameter farColor;
        SerializedDataParameter powerRamp;

        public static string gradientName = "SilhouetteGradient";
        public static string textureName = "SilhouetteRampTex";

        protected override string BannerTextureName { get { return "SilhouetteBanner"; } }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SilhouetteSettings>(serializedObject);
            FetchBasicParameters(o);

            enabled = Unpack(o.Find(x => x.enabled));
            useTextureRamp = Unpack(o.Find(x => x.useTextureRamp));
            textureRamp = Unpack(o.Find(x => x.textureRamp));
            nearColor = Unpack(o.Find(x => x.nearColor));
            farColor = Unpack(o.Find(x => x.farColor));
            powerRamp = Unpack(o.Find(x => x.powerRamp));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            VolumeProfile sharedProfile = FindVolumeProfile();
            var silhouetteTarget = target as SilhouetteSettings;

            DrawBanner();

            DrawBasicSettings<SilhouetteFeature>(true);

            DrawDivider();

            EditorGUILayout.LabelField("Silhouette Settings", HeaderStyle);
            PropertyField(enabled);
            PropertyField(useTextureRamp);

            if(silhouetteTarget.useTextureRamp.value)
            {
                var gradientWrapper = FindGradientData(sharedProfile, gradientName, true);
                var textureData = FindTextureData(sharedProfile, textureName, gradientWrapper.gradient, true);

                var gradient = new Gradient();
                gradient.SetKeys(gradientWrapper.gradient.colorKeys, gradientWrapper.gradient.alphaKeys);
                gradient.mode = gradientWrapper.gradient.mode;

                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                gradient = EditorGUILayout.GradientField(new GUIContent("Gradient", ""), gradient);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new UnityEngine.Object[] { gradientWrapper, textureData }, "Modify Gradient");
                    gradientWrapper.gradient = gradient;
                    EditorUtility.SetDirty(gradientWrapper);

                    textureData = GenerateRampTexture(gradient, textureData);
                    EditorUtility.SetDirty(textureData);
                }

                EditorGUI.indentLevel--;

                silhouetteTarget.textureRamp.value = textureData;
                silhouetteTarget.textureRamp.overrideState = true;

                EditorGUI.BeginDisabledGroup(true);
                PropertyField(textureRamp);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                PropertyField(nearColor);
                PropertyField(farColor);
            }
                
            PropertyField(powerRamp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
