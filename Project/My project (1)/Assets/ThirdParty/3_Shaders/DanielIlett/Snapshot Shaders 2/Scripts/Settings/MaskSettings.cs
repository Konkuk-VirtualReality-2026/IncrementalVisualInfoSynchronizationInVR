using System.Collections.Generic;
using UnityEngine;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable]
    public struct MaskSettings
    {
        [Tooltip("Restrict the effect to only the following regular layers.")]
        public LayerMask layerMask;

        [Tooltip("Additionally restrict the effect to only these Rendering Layers.")]
        public RenderingLayerMask renderingLayerMask;

        [Tooltip("Which LightMode tags should be included in the mask?\n" +
            "\n<b>UniversalForwardOnly</b> includes the base Toon shader." +
            "\n<b>UniversalForward</b> includes most lit shaders, including Shader Graphs." +
            "\n<b>SRPDefaultUnlit</b> includes most unlit shaders, including Shader Graphs." +
            "\nMost other settings will capture almost all shaders.\n" +
            "\n<b>Warning</b>: Duplicated entries may increase resource usage with no benefit.")]
        public List<LightModeType> lightModes;

        [Tooltip("Should outlines be applied to opaque or transparent objects?" +
            "\nCurrently, the outlines only function with opaque objects.")]
        public RenderQueueType renderQueue;

        [Tooltip("Should the skybox be drawn into the mask texture?")]
        public bool drawSkyboxToMask;

        public MaskSettings(bool enabled)
        {
            layerMask = ~0;
            renderingLayerMask = ~0;
            lightModes = new List<LightModeType>() { LightModeType.UniversalForward, LightModeType.SRPDefaultUnlit };
            renderQueue = RenderQueueType.Opaque;
            drawSkyboxToMask = false;
        }
    }
}
