using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Pixelate")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/pixelate/")]
    public class PixelateSettings : SnapshotVolumeComponent
    {
        public PixelateSettings()
        {
            displayName = "Pixelate";
        }

        [Tooltip("The size of each pixel in the newly-downsampled image.")]
        public ClampedIntParameter downsampleSize = new ClampedIntParameter(1, 1, 1024);

        [Tooltip("Should the effect use point (nearest neighbor) filtering?" +
            "\nIf unchecked, bilinear filtering is used.")]
        public BoolParameter usePointFiltering = new BoolParameter(true);

        [Tooltip("When a mask is being used, you can expand the mask slightly to prevent " +
            "parts where the original object color is outside the pixelated mask.")]
        public BoolParameter expandMask = new BoolParameter(false);

        public override bool IsActive() => downsampleSize.value > 1 && active;
    }
}
