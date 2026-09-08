using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Blur")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/blur/")]
    public class BlurSettings : SnapshotVolumeComponent
    {
        public BlurSettings()
        {
            displayName = "Blur";
        }

        public BlurModeParameter blurMode = new BlurModeParameter(BlurMode.Gaussian);

        public ClampedIntParameter strength = new ClampedIntParameter(1, 1, 501);

        public ClampedIntParameter blurStepSize = new ClampedIntParameter(1, 1, 11);

        public ClampedFloatParameter luminanceThreshold = new ClampedFloatParameter(1.0f, 0.0f, 20.0f);

        public override bool IsActive() => (strength.value > 1 && active);
    }
}
