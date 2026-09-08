using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Vortex")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/vortex/")]
    public class VortexSettings : SnapshotVolumeComponent
    {
        public VortexSettings()
        {
            displayName = "Vortex";
        }

        [Tooltip("How strongly the effect will twirl pixels around the center.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);

        [Tooltip("The vortex will swirl around this normalized position.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("How far the image is offset before twirling.")]
        public Vector2Parameter offset = new Vector2Parameter(Vector2.zero);

        [Tooltip("An extra rotation applied to the entire image (normalized between 0-360 degrees).")]
        public ClampedFloatParameter rotation = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public override bool IsActive() => 
            (strength.value > Mathf.Epsilon || rotation.value > Mathf.Epsilon
            || offset.value.x > Mathf.Epsilon || offset.value.y > Mathf.Epsilon) && active;
    }
}

