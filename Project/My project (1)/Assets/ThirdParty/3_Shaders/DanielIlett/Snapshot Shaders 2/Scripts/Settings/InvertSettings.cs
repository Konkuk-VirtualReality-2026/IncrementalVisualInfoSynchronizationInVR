using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Invert")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/invert/")]
    public class InvertSettings : SnapshotVolumeComponent
    {
        public InvertSettings()
        {
            displayName = "Invert";
        }

        [Tooltip("How strongly to apply the invert effect. Note that a value of 0.5 will always result in a grey screen, " +
            "although some areas may be brighter if HDR is used and the effect runs before any tonemapping.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public override bool IsActive() => (strength.value > Mathf.Epsilon && active);
    }
}

