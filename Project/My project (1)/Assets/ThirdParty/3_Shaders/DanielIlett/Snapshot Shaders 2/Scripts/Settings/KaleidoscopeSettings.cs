using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Kaleidoscope")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/kaleidoscope/")]
    public class KaleidoscopeSettings : SnapshotVolumeComponent
    {
        public KaleidoscopeSettings()
        {
            displayName = "Kaleidoscope";
        }

        [Tooltip("The number of radial segments.")]
        public ClampedFloatParameter segments = new ClampedFloatParameter(1.0f, 1.0f, 64.0f);

        public override bool IsActive() => (segments.value > 1.0f + Mathf.Epsilon && active);
    }
}

