using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Distortion")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/distortion/")]
    public class DistortionSettings : SnapshotVolumeComponent
    {
        public DistortionSettings()
        {
            displayName = "Distortion";
        }

        [Tooltip("Strength of the distortion. Values above zero cause CRT screen-like distortion; values below zero bulge outwards.")]
        public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, -5.0f, 5.0f);

        [Tooltip("Amount of smoothing applied to edges of the distorted screen.")]
        public ClampedFloatParameter smoothing = new ClampedFloatParameter(0.01f, 0.0f, 0.1f);

        [Tooltip("Color of the background around the 'screen'.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);

        public override bool IsActive() => (Mathf.Abs(strength.value) > Mathf.Epsilon && active);
    }
}

