using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Colorize")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/colorize/")]
    public class ColorizeSettings : SnapshotVolumeComponent
    {
        public ColorizeSettings()
        {
            displayName = "Colorize";
        }

        [ColorUsage(true, true), Tooltip("Tint color to use. Alpha channel controls effect strength.")]
        public ColorParameter tintColor = new ColorParameter(new Color(1.0f, 1.0f, 1.0f, 0.0f), true, true, true);

        public override bool IsActive() => (tintColor.value.a > Mathf.Epsilon && active);
    }
}

