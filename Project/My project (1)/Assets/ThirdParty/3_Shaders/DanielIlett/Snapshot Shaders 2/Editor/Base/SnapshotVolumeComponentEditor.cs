using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    public class SnapshotVolumeComponentEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter renderPassEvent;
        protected SerializedDataParameter maskMode;
        protected SerializedDataParameter maskSettings;
        protected SerializedDataParameter invertMask;

        protected Texture2D bannerTexture;
        protected virtual string BannerTextureName { get; }


        protected static GUIStyle _headerStyle;
        protected static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true,
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _headerStyle;
            }
        }

        protected static GUIStyle _italicStyle;
        protected static GUIStyle ItalicStyle
        {
            get
            {
                if(_italicStyle == null)
                {
                    _italicStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Italic
                    };
                }

                return _italicStyle;
            }
        }

        protected static GUIStyle _descriptionStyle;
        protected static GUIStyle DescriptionStyle
        {
            get
            {
                if (_descriptionStyle == null)
                {
                    _descriptionStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontStyle = FontStyle.Bold,
                        fontSize = 12
                    };
                }

                return _descriptionStyle;
            }
        }

        // Fetches properties which are common to all SnapshotVolumeComponents.
        protected void FetchBasicParameters<T>(PropertyFetcher<T> o) where T : SnapshotVolumeComponent
        {
            renderPassEvent = Unpack(o.Find(x => x.renderPassEvent));
            maskMode = Unpack(o.Find(x => x.maskMode));
            maskSettings = Unpack(o.Find(x => x.maskSettings));
            invertMask = Unpack(o.Find(x => x.invertMask));
        }

        // Draws a thin dividing line between sections.
        protected void DrawDivider()
        {
            EditorGUILayout.Space();

            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = new Color(0.35f, 0.35f, 0.35f);
            Handles.DrawLine(new Vector2(rect.x + 5.0f, rect.y), new Vector2(rect.width + 10.0f, rect.y), 1.0f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        // Get the volume profile that this component is attached to. Used for error-checking.
        protected VolumeProfile FindVolumeProfile()
        {
#if UNITY_EDITOR
            if (volume != null)
            {
                return volume.sharedProfile;
            }

            var component = target as VolumeComponent;

            if (target == null)
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:VolumeProfile");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

                if (profile != null && profile.components.Contains(component))
                {
                    return profile;
                }
            }
#endif

            return null;
        }

        protected Texture2D GenerateRampTexture(Gradient gradient, Texture2D rampTexture)
        {
            int size = 1024;

            for (int x = 0; x < size; ++x)
            {
                float t = x / (float)size;
                Color color = gradient.Evaluate(t);
                rampTexture.SetPixel(x, 0, color);
            }

            rampTexture.Apply();

            return rampTexture;
        }

        protected SnapshotGradientData FindGradientData(VolumeProfile profile, string name, bool createIfMissing)
        {
            var path = AssetDatabase.GetAssetPath(profile);
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            // Try to find an existing gradient data asset.
            foreach (var asset in subAssets)
            {
                if (asset is SnapshotGradientData gradientData)
                {
                    if(asset.name == name)
                    {
                        return gradientData;
                    }
                }
            }

            if (createIfMissing)
            {
                var gradientData = ScriptableObject.CreateInstance<SnapshotGradientData>();
                gradientData.name = name;

                AssetDatabase.AddObjectToAsset(gradientData, profile);
                AssetDatabase.SaveAssets();

                return gradientData;
            }

            return null;
        }

        protected Texture2D FindTextureData(VolumeProfile profile, string name, Gradient gradient, bool createIfMissing)
        {
            var path = AssetDatabase.GetAssetPath(profile);
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            // Try to find an existing gradient data asset.
            foreach (var asset in subAssets)
            {
                if (asset is Texture2D textureData)
                {
                    if (asset.name == name)
                    {
                        return textureData;
                    }
                }
            }

            if (createIfMissing)
            {
                var textureData = new Texture2D(512, 1);
                textureData.wrapMode = TextureWrapMode.Clamp;
                textureData = GenerateRampTexture(gradient, textureData);
                textureData.name = name;

                AssetDatabase.AddObjectToAsset(textureData, profile);
                AssetDatabase.SaveAssets();

                return textureData;
            }

            return null;
        }

        protected void DeleteGradientData(VolumeProfile profile, string gradientName, string textureName)
        {
            var path = AssetDatabase.GetAssetPath(profile);
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

            for (int i = 0; i < subAssets.Length; ++i)
            {
                if (subAssets[i].name == gradientName || subAssets[i].name == textureName)
                {
                    DestroyImmediate(subAssets[i], true);
                }
            }

            AssetDatabase.SaveAssets();
        }

        protected void CheckSubAssetsAreValid(VolumeProfile sharedProfile, VolumeComponent component)
        {
            var silhouetteExists = false;

            // Only run the sub-asset check once per draw.
            if (sharedProfile.components[0] == component)
            {
                for(int i = 0; i < sharedProfile.components.Count; ++i)
                {
                    if(sharedProfile.components[i] is SilhouetteSettings)
                    {
                        silhouetteExists = true;
                    }
                }

                if (!silhouetteExists)
                {
                    DeleteGradientData(sharedProfile, SilhouetteEditor.gradientName, SilhouetteEditor.textureName);
                }
            }
        }

        // Draw a nice banner for each effect.
        protected void DrawBanner()
        {
            // Try and retrieve the banner texture if we don't have it yet.
            if (bannerTexture == null)
            {
                bannerTexture = Resources.Load<Texture2D>(BannerTextureName);
            }

            var tint = (target as SnapshotVolumeComponent).IsActive() ? Color.white : Color.grey;

            if (bannerTexture != null)
            {
                float baseWidth = bannerTexture.width * 0.65f;
                float baseHeight = bannerTexture.height * 0.65f;

                float width = baseWidth;// Mathf.Max(EditorGUIUtility.currentViewWidth * 0.75f, baseWidth);
                float height = baseHeight;// Mathf.Min(baseHeight * (width / baseWidth), baseHeight);
                var bannerRect = EditorGUILayout.GetControlRect();
                bannerRect.width = width;
                bannerRect.height = height;
                bannerRect.y -= 5.0f;
                GUI.DrawTexture(bannerRect, bannerTexture, ScaleMode.ScaleToFit, true, 0.0f, tint, 0.0f, 0.0f);
                
                GUILayout.Space(height * 0.4f);
            }
        }

        // Draw mask settings if a Local mask mode was chosen.
        protected void DrawMaskSettings()
        {
            var prop = maskSettings.value;

            var layerMask = prop.FindPropertyRelative("layerMask");
            var renderingLayerMask = prop.FindPropertyRelative("renderingLayerMask");
            var lightModes = prop.FindPropertyRelative("lightModes");
            var renderQueue = prop.FindPropertyRelative("renderQueue");
            var drawSkyboxToMask = prop.FindPropertyRelative("drawSkyboxToMask");

            EditorGUILayout.LabelField(new GUIContent("Mask Settings"), HeaderStyle);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(layerMask);
            EditorGUILayout.PropertyField(renderingLayerMask);
            EditorGUILayout.PropertyField(lightModes);
            EditorGUILayout.PropertyField(renderQueue);
            EditorGUILayout.PropertyField(drawSkyboxToMask);

            EditorGUI.indentLevel--;
        }

        // Draw settings related to common features of all SnapshotVolumeComponents.
        private void DrawBasicSettingsBlock(bool drawMask, MaskMode maskModeValue)
        {
            EditorGUILayout.LabelField("Basic Settings", HeaderStyle);
            PropertyField(renderPassEvent);

            if(!drawMask)
            {
                return;
            }

            PropertyField(maskMode);

            if (maskModeValue == MaskMode.Local)
            {
                DrawDivider();
                DrawMaskSettings();
            }

            if(maskModeValue != MaskMode.None)
            {
                PropertyField(invertMask);
            }
        }

        // Draw basic settings for the component and perform error-checking.
        protected void DrawBasicSettings<T>(bool drawMask) where T : ScriptableRendererFeature
        {
            var sharedProfile = FindVolumeProfile();

            SnapshotHelper.DisplayFeatureExistsHelpBox<T>();

            var maskModeValue = maskMode.value.GetEnumValue<MaskMode>();

            if (maskModeValue == MaskMode.Global)
            {
                SnapshotHelper.DisplayMaskRendererErrorBox<T>();

                if (sharedProfile != null)
                {
                    SnapshotHelper.DisplayMaskProfileErrorBox(sharedProfile, volumeComponent as SnapshotVolumeComponent);
                }
            }

            DrawBasicSettingsBlock(drawMask, maskModeValue);
        }

        public override void OnInspectorGUI()
        {
            var sharedProfile = FindVolumeProfile();
            CheckSubAssetsAreValid(sharedProfile, target as VolumeComponent);
        }
    }
}
