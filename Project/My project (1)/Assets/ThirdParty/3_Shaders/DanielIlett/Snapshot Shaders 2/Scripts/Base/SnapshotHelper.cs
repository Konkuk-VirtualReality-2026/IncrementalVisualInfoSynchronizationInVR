using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    public static class SnapshotHelper
    {
        private static GUIStyle _buttonStyle;
        private static GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        wordWrap = true,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _buttonStyle;
            }
        }

        // Get a reference to the default Renderer Data attached to the URP Asset.
        public static ScriptableRendererData GetRendererData()
        {
            var rendererDataList = UniversalRenderPipeline.asset.rendererDataList;

            int index = (int)typeof(UniversalRenderPipelineAsset)
                .GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UniversalRenderPipeline.asset);

            return rendererDataList[index];
        }

        // Check if an effect exists in the Renderer Feature list on the default renderer data.
        public static T CheckEffectExistsOnRenderer<T>() where T : ScriptableRendererFeature
        {
            // Effect class name minus "Feature" on the end.
            var prettyName = typeof(T).Name.Substring(0, typeof(T).Name.Length - 7);

            if (UniversalRenderPipeline.asset == null)
            {
                Debug.LogError($"Failed to search for {prettyName} - cannot find an active URP asset. " + 
                    "Please assign one in Project Settings -> Graphics or Project Settings -> Quality.");
                return null;
            }

            var rendererData = GetRendererData();

            foreach (ScriptableRendererFeature item in rendererData.rendererFeatures)
            {
                if (item?.GetType() == typeof(T))
                {
                    return item as T;
                }
            }

            return null;
        }

        // Check if an effect exists in a given Volume Profile.
        public static T CheckEffectExistsInProfile<T>(VolumeProfile profile) where T : SnapshotVolumeComponent
        {
            foreach(var component in profile.components)
            {
                if(component is T)
                {
                    return component as T;
                }
            }

            return null;
        }

        // Add a missing Renderer Feature to the renderer data.
        public static void AddEffectToRendererDataAsset<T>() where T : ScriptableRendererFeature
        {
#if UNITY_EDITOR
            // Effect class name minus "Feature" on the end.
            var prettyName = typeof(T).Name.Substring(0, typeof(T).Name.Length - 7);

            if (UniversalRenderPipeline.asset == null)
            {
                Debug.LogError($"Failed to add {prettyName} - cannot find an active URP asset. " + 
                    "Please assign one in Project Settings -> Graphics or Project Settings -> Quality.");
                return;
            }

            var rendererData = GetRendererData();
            var effect = ScriptableRendererFeature.CreateInstance<T>();

            AssetDatabase.AddObjectToAsset(effect, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(effect, out var guid, out long localID);

            rendererData.rendererFeatures.Add(effect);
            rendererData.SetDirty();

            Debug.Log($"Added {prettyName} effect to the active renderer data ({rendererData.name}).", rendererData);
#endif
        }

        // Check whether the camera is within a volume collider's blending range.
        private static bool CameraIsInVolume(Volume volume, Vector3 camPos)
        {
            var collider = volume.GetComponent<Collider>();

            if (collider == null || !collider.enabled)
            {
                return false;
            }

            var offset = collider.ClosestPoint(camPos) - camPos;

            return (Vector3.Magnitude(offset) < volume.blendDistance);
        }

        // Perform a manual check to retrieve a kind of volume settings.
        // This is necessary because GetComponent() always returns a volume,
        // even when the camera is not inside a relevant volume.
        public static T GetClosestSettings<T>(Camera camera) where T : SnapshotVolumeComponent
        {
            LayerMask mask = 1 << camera.gameObject.layer;
            Volume[] volumes = VolumeManager.instance.GetVolumes(mask);

            foreach (var volume in volumes)
            {
                if (!volume.enabled || volume.sharedProfile == null)
                {
                    continue;
                }

                if (!volume.sharedProfile.TryGet<T>(out var requiredVolume))
                {
                    continue;
                }

                if (volume.isGlobal || CameraIsInVolume(volume, camera.transform.position))
                {
                    if(requiredVolume.IsActive())
                    {
                        return requiredVolume;
                    }
                }
            }

            return null;
        }

        // Checks for two Renderer Features. If T1 exists before T2, return true.
        public static bool CheckEffectRendersBefore<T1, T2>() 
            where T1 : ScriptableRendererFeature
            where T2 : ScriptableRendererFeature
        {
            var rendererData = GetRendererData();

            foreach (ScriptableRendererFeature item in rendererData.rendererFeatures)
            {
                if (item?.GetType() == typeof(T1))
                {
                    return true;
                }
                else if(item?.GetType() == typeof(T2))
                {
                    return false;
                }
            }

            return false;
        }

        // Checks for two Volume Components. If T1 exists before T2, return true.
        public static bool CheckEffectIsInProfileBefore<T1, T2>(VolumeProfile profile)
            where T1 : SnapshotVolumeComponent
            where T2 : SnapshotVolumeComponent
        {
            foreach(var component in profile.components)
            {
                if(component?.GetType() == typeof(T1))
                {
                    return true;
                }
                else if(component?.GetType() == typeof(T2))
                {
                    return false;
                }
            }

            return false;
        }

        private static void AddGlobalMaskToRendererData<T>() where T : ScriptableRendererFeature
        {
#if UNITY_EDITOR
            var rendererData = GetRendererData();
            var rendererFeatures = rendererData.rendererFeatures;

            int checkFeatureIndex = -1;
            int maskFeatureIndex = -1;

            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                if (rendererFeatures[i] is T)
                {
                    checkFeatureIndex = i;
                }

                if (rendererFeatures[i] is GlobalMaskFeature)
                {
                    maskFeatureIndex = i;
                }
            }

            if (maskFeatureIndex == -1)
            {
                var maskEffect = ScriptableRendererFeature.CreateInstance<GlobalMaskFeature>();

                AssetDatabase.AddObjectToAsset(maskEffect, rendererData);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(maskEffect, out var guid, out long localID);

                rendererFeatures.Insert(checkFeatureIndex, maskEffect);
                rendererData.SetDirty();

                Debug.Log($"Added Global Mask effect to the active renderer data ({rendererData.name}).", rendererData);
            }
            else
            {
                var checkFeature = rendererFeatures[checkFeatureIndex];
                var maskFeature = rendererFeatures[maskFeatureIndex];
                rendererFeatures[checkFeatureIndex] = maskFeature;
                rendererFeatures[maskFeatureIndex] = checkFeature;
                rendererData.SetDirty();
            }
#endif
        }

        // Creates a new Global Mask and inserts it above the specified component with
        // the same RenderPassEvent.
        private static void AddGlobalMaskToVolumeProfile(VolumeProfile profile, SnapshotVolumeComponent volumeComponent)
        {
#if UNITY_EDITOR
            var maskSettings = ScriptableObject.CreateInstance<GlobalMaskSettings>();
            maskSettings.name = "Global Mask";

            AssetDatabase.AddObjectToAsset(maskSettings, profile);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(maskSettings, out var guid, out long localID);

            if (maskSettings.renderPassEvent.value != volumeComponent.renderPassEvent.value)
            {
                maskSettings.renderPassEvent.overrideState = true;
                maskSettings.renderPassEvent.value = volumeComponent.renderPassEvent.value;
            }

            var index = profile.components.IndexOf(volumeComponent);
            profile.components.Insert(index, maskSettings);

            EditorUtility.SetDirty(profile);
#endif
        }

        // Display an error if given feature T doesn't exist in the Renderer Features list.
        public static void DisplayFeatureExistsHelpBox<T>() where T : ScriptableRendererFeature
        {
#if UNITY_EDITOR
            // Effect class name minus "Feature" on the end.
            var prettyName = typeof(T).Name.Substring(0, typeof(T).Name.Length - 7);

            var effect = CheckEffectExistsOnRenderer<T>();
            if (effect == null)
            {

                EditorGUILayout.HelpBox($"The {prettyName} effect must be added to the active renderer's Renderer Features list.", MessageType.Error);
                GUILayout.Space(5);
                if (GUILayout.Button($"Add {prettyName} to the active renderer's Renderer Features list", ButtonStyle))
                {
                    AddEffectToRendererDataAsset<T>();
                }
                GUILayout.Space(10);
            }
            else if(!effect.isActive)
            {
                EditorGUILayout.HelpBox($"The {prettyName} effect is in the active renderer's Renderer Features list, but it must be enabled.", MessageType.Error);
                GUILayout.Space(5);
                if (GUILayout.Button($"Enable {prettyName} on the active renderer's Renderer Features list", ButtonStyle))
                {
                    effect.SetActive(true);
                }
                GUILayout.Space(10);
            }
#endif
        }

        // Display an error if the Global Mask feature renders after feature T.
        public static void DisplayMaskRendererErrorBox<T>() where T : ScriptableRendererFeature
        {
#if UNITY_EDITOR
            // Effect class name minus "Feature" on the end.
            var prettyName = typeof(T).Name.Substring(0, typeof(T).Name.Length - 7);

            bool maskRendersFirst = CheckEffectRendersBefore<GlobalMaskFeature, T>();

            if (!maskRendersFirst)
            {
                EditorGUILayout.HelpBox($"To use a mask, the Global Mask effect must appear before {prettyName} in your renderer's Renderer Features list.", MessageType.Error);
                GUILayout.Space(5);
                if (GUILayout.Button($"Add or move Global Mask above {prettyName}", ButtonStyle))
                {
                    AddGlobalMaskToRendererData<T>();
                }
                GUILayout.Space(10);
            }
#endif
        }

        // Display an error if the Global Mask feature doesn't exist.
        public static void DisplayMaskProfileErrorBox(VolumeProfile profile, SnapshotVolumeComponent volumeComponent)
        {
#if UNITY_EDITOR
            var globalMask = CheckEffectExistsInProfile<GlobalMaskSettings>(profile);

            if(globalMask == null)
            {
                EditorGUILayout.HelpBox($"To use a mask, a Global Mask component must be attached to this volume profile. It will be ignored by this effect.", MessageType.Error);
                GUILayout.Space(5);
                if (GUILayout.Button($"Add Global Mask to this volume profile", ButtonStyle))
                {
                    AddGlobalMaskToVolumeProfile(profile, volumeComponent);
                }
                GUILayout.Space(10);
            }
            else if(!globalMask.IsActive())
            {
                EditorGUILayout.HelpBox($"A Global Mask exists on this profile, but it is inactive. It will be ignored by this effect.", MessageType.Error);
                GUILayout.Space(5);
                if (GUILayout.Button($"Set this profile's Global Mask active.", ButtonStyle))
                {
                    globalMask.active = true;
                }
                GUILayout.Space(10);
            }
            else if(globalMask.renderPassEvent.value > volumeComponent.renderPassEvent.value)
            {
                EditorGUILayout.HelpBox($"The Global Mask on this profile uses a Render Pass Event after this effect. " +
                    "The mask cannot be used for this effect.", MessageType.Error);
                GUILayout.Space(5);
                if (GUILayout.Button($"Change the Global Mask's Render Pass Event.", ButtonStyle))
                {
                    globalMask.renderPassEvent.overrideState = true;
                    globalMask.renderPassEvent.value = volumeComponent.renderPassEvent.value;
                }
                GUILayout.Space(10);
            }
#endif
        }

        public static bool CheckFeatureIsMaskCompatible<T>() where T : ScriptableRendererFeature
        {
#if UNITY_EDITOR
            // Effect class name minus "Feature" on the end.
            var prettyName = typeof(T).Name.Substring(0, typeof(T).Name.Length - 7);

            bool maskRendersFirst = CheckEffectRendersBefore<GlobalMaskFeature, T>();

            if (!maskRendersFirst)
            {
                Debug.LogWarning($"You are probably trying to render an effect using a mask, but the " +
                    $"Global Mask effect must appear before {prettyName} in the " +
                    $"Renderer Features list. Check your Renderer Data asset.\n\n" +
                    $"This effect probably won't run properly in builds.", GetRendererData());
            }

            return maskRendersFirst;
#else
            return true;
#endif
        }
    }
}
