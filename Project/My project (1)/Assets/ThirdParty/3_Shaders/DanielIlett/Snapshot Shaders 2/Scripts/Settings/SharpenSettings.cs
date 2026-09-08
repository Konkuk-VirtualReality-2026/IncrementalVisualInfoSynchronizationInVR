using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Sharpen")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/sharpen/")]
    public class SharpenSettings : SnapshotVolumeComponent
    {
        public SharpenSettings()
        {
            displayName = "Sharpen";
        }

        [Tooltip("How strongly to apply the sharpen filter to the image.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public override bool IsActive() => (strength.value > Mathf.Epsilon && active);
    }
}
