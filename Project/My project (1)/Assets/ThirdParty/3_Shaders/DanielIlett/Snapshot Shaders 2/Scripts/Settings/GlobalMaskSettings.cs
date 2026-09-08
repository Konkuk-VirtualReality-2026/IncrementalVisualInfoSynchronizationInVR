using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Global Mask")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/masking-layers/")]
    public sealed class GlobalMaskSettings : SnapshotVolumeComponent
    {
        public GlobalMaskSettings()
        {
            displayName = "Global Mask";
        }

        public override bool IsActive() => active;
    }
}
