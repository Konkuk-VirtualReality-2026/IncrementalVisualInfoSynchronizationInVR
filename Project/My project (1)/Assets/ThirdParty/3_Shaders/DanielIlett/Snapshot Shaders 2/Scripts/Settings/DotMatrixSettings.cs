using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Dot Matrix")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/dot-matrix/")]
    public class DotMatrixSettings : SnapshotVolumeComponent
    {
        public DotMatrixSettings()
        {
            displayName = "Dot Matrix";
        }

        [Tooltip("How wide should the pixel gaps between dots be?")]
        public ClampedIntParameter gapWidth = new ClampedIntParameter(0, 0, 20);

        [Tooltip("How many pixels wide should each dot be?")]
        public ClampedIntParameter dotSize = new ClampedIntParameter(1, 1, 20);

        [Tooltip("The background color seen in the gaps between dots.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);

        public override bool IsActive() => (gapWidth.value > 0 && active);
    }
}

