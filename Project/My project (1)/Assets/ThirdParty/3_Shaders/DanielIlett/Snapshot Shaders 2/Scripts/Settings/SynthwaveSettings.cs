using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Synthwave")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/synthwave/")]
    public class SynthwaveSettings : SnapshotVolumeComponent
    {
        public SynthwaveSettings()
        {
            displayName = "Synthwave";
        }

        [Tooltip("Use the Scene Color instead of Background Color?")]
        public BoolParameter useSceneColor = new BoolParameter(false);

        [Tooltip("Color of the background if Use Scene Color is turned off.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);

        [Tooltip("Color of the synthwave lines at the bottom of the screen. HDR colors will glow if a Bloom effect is present.")]
        public ColorParameter lineColor1 = new ColorParameter(Color.white, true, true, true);

        [ColorUsage(true, true)]
        [Tooltip("Color of the synthwave lines at the top of the screen. HDR colors will glow if a Bloom effect is present.")]
        public ColorParameter lineColor2 = new ColorParameter(Color.white, true, true, true);

        [Tooltip("Controls the mix between the two line colors. " +
            "Lower values favour the top color (2). Higher values favor the bottom color (1). " + 
            "A value of 1 is a neutral mix (although it may not appear to be perceptually).")]
        public FloatParameter lineColorMix = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);

        [Tooltip("Thickness of the lines in world space units.")]
        public FloatParameter lineWidth = new FloatParameter(0.0f);

        [Tooltip("Falloff between synthwave lines and background color in world space units.")]
        public FloatParameter lineSoftness = new FloatParameter(0.05f);

        [Tooltip("Space between lines along each axis in world space units.")]
        public Vector3Parameter gapWidth = new Vector3Parameter(Vector3.one);

        [Tooltip("Offset from (0, 0, 0) along each axis in world space units.")]
        public Vector3Parameter lineOffset = new Vector3Parameter(Vector3.zero);

        [Tooltip("Distance from the camera where the synthwave lines start to fade out.")]
        public FloatParameter startFadeoutDistance = new FloatParameter(15.0f);

        [Tooltip("Distance from the camera where the synthwave lines become completely invisible.")]
        public FloatParameter endFadeoutDistance = new FloatParameter(30.0f);

        [SerializeField]
        [Tooltip("Synthwave lines are shown only along these axes.")]
        public AxisMaskParameter axisMask = new AxisMaskParameter(AxisMask.XZ);

        public override bool IsActive() => (lineWidth.value > Mathf.Epsilon && active);
    }
}

