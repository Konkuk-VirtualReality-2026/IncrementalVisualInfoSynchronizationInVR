using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Silhouette")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/silhouette/")]
    public class SilhouetteSettings : SnapshotVolumeComponent
    {
        public SilhouetteSettings()
        {
            displayName = "Silhouette";
        }

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("")]
        public BoolParameter useTextureRamp = new BoolParameter(false);

        [Tooltip("")]
        public Texture2DParameter textureRamp = new Texture2DParameter(null);

        [Tooltip("Color at the camera's near clip plane.")]
        public ColorParameter nearColor = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f));

        [Tooltip("Color at the camera's far clip plane.")]
        public ColorParameter farColor = new ColorParameter(new Color(1.0f, 1.0f, 1.0f, 1.0f));

        [Tooltip("Modify the input colors via a power ramp. 1 = original mapping, " +
            "higher = favors near color, lower = favors far color.")]
        public ClampedFloatParameter powerRamp = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);

        public override bool IsActive() => (enabled.value && active);
    }
}
