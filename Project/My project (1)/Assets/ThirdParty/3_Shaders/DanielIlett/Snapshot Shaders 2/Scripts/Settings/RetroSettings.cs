using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Retro")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/retro/")]
    public class RetroSettings : SnapshotVolumeComponent
    {
        public RetroSettings()
        {
            displayName = "Retro";
        }

        [Tooltip("Is the effect active?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Which kind of retro drawing mode to use.")]
        public RetroDrawingModeParameter drawingMode = new RetroDrawingModeParameter(RetroDrawingMode.GameBoy);

        // SNES settings.

        [Tooltip("How many red levels are supported.")]
        public ClampedIntParameter redLevels = new ClampedIntParameter(2, 2, 256);

        [Tooltip("How many green levels are supported.")]
        public ClampedIntParameter greenLevels = new ClampedIntParameter(2, 2, 256);

        [Tooltip("How many blue levels are supported.")]
        public ClampedIntParameter blueLevels = new ClampedIntParameter(2, 2, 256);

        // Game Boy settings.

        [Tooltip("Darkest colour (black areas).")]
        public ColorParameter darkestColor = new ColorParameter(new Color(0.11f, 0.21f, 0.08f));

        [Tooltip("Second darkest colour (dark grey areas).")]
        public ColorParameter darkColor = new ColorParameter(new Color(0.24f, 0.38f, 0.21f));

        [Tooltip("Second lightest colour (light grey areas).")]
        public ColorParameter lightColor = new ColorParameter(new Color(0.57f, 0.67f, 0.21f));

        [Tooltip("Lightest colour (white areas).")]
        public ColorParameter lightestColor = new ColorParameter(new Color(0.75f, 0.82f, 0.46f));

        // Shared settings.

        [Tooltip("Modify the input colors via a power ramp. 1 = original mapping, " +
            "higher = favors darker output, lower = favors lighter output.")]
        public ClampedFloatParameter powerRamp = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);

        public override bool IsActive() => enabled.value && active;
    }
}
