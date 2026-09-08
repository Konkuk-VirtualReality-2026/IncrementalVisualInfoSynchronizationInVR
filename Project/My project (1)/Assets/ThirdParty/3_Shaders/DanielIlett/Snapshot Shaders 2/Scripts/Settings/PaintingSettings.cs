using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Painting")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/painting/")]
    public class PaintingSettings : SnapshotVolumeComponent
    {
        public PaintingSettings()
        {
            displayName = "Painting";
        }

        [Tooltip("Which painting algorithm to use.")]
        public PaintingDrawingModeParameter drawingMode = new PaintingDrawingModeParameter(PaintingDrawingMode.Oil);

        [Tooltip("Radius of the painting effect.")]
        public ClampedIntParameter kernelSize = new ClampedIntParameter(1, 1, 100);

        public override bool IsActive() => (kernelSize.value > 1 && active);
    }
}
