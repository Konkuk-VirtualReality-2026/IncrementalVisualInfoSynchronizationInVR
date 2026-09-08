using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Debug Mask")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/debug-mask/")]
    public class DebugMaskSettings : SnapshotVolumeComponent
    {
        public DebugMaskSettings()
        {
            displayName = "Debug Mask";
        }

        [Tooltip("Should the debug view be rendered?")]
        public BoolParameter enabled = new BoolParameter(false);

        public override bool IsActive() => (enabled.value && active);
    }
}

