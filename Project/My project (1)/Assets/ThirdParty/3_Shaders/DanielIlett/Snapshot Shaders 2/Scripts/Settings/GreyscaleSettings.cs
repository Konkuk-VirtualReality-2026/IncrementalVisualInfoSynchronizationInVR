using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Greyscale")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/greyscale/")]
    public sealed class GreyscaleSettings : SnapshotVolumeComponent
    {
        public GreyscaleSettings()
        {
            displayName = "Greyscale";
        }

        [Tooltip("Greyscale effect intensity.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public override bool IsActive() => (strength.value > Mathf.Epsilon && active);
    }
}
