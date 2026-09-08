using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Thermal")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/thermal/")]
    public class ThermalSettings : SnapshotVolumeComponent
    {
        public ThermalSettings()
        {
            displayName = "Thermal";
        }

        [Tooltip("Should the effect run?")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Only render masked objects, or render everything but masked objects are brighter than usual.")]
        public ThermalMaskModeParameter thermalMaskMode = new ThermalMaskModeParameter(ThermalMaskMode.RenderMaskObjectsBright);

        [Tooltip("When a mask is enabled, how much should it be blurred? This can ensure a nice falloff " + 
            "between highlighted and background objects.")]
        public ClampedIntParameter maskBlur = new ClampedIntParameter(1, 1, 50);
        [Tooltip("The blur algorithm can skip over some pixels, resulting in a sparse kernel for efficiency.")]
        public ClampedIntParameter maskBlurStepSize = new ClampedIntParameter(1, 1, 3);

        [Tooltip("How much brightness should be added to masked objects?")]
        public ClampedFloatParameter addBrightnessToMask = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);

        [Tooltip("Color band for the first layer (darkest pixels in the image).")]
        public ColorParameter color0 = new ColorParameter(new Color(0.016f, 0.028f, 0.026f), true, false, true);
        [Tooltip("Threshold brightness value where Color0 is swapped for Color1.")]
        public ClampedFloatParameter threshold0 = new ClampedFloatParameter(0.2f, 0.0f, 2.0f);

        [Tooltip("Color band for the second layer.")]
        public ColorParameter color1 = new ColorParameter(new Color(0.105f, 0.472f, 0.313f), true, false, true);
        [Tooltip("Threshold brightness value where Color1 is swapped for Color2.")]
        public ClampedFloatParameter threshold1 = new ClampedFloatParameter(0.35f, 0.0f, 2.0f);

        [Tooltip("Color band for the third layer.")]
        public ColorParameter color2 = new ColorParameter(new Color(0.295f, 0.792f, 0.397f), true, false, true);
        [Tooltip("Threshold brightness value where Color2 is swapped for Color3.")]
        public ClampedFloatParameter threshold2 = new ClampedFloatParameter(0.5f, 0.0f, 2.0f);

        [Tooltip("Color band for the fourth layer.")]
        public ColorParameter color3 = new ColorParameter(new Color(0.734f, 1.0f, 0.137f), true, false, true);
        [Tooltip("Threshold brightness value where Color3 is swapped for Color4.")]
        public ClampedFloatParameter threshold3 = new ClampedFloatParameter(0.75f, 0.0f, 2.0f);

        [Tooltip("Color band for the fifth layer.")]
        public ColorParameter color4 = new ColorParameter(new Color(1.0f, 0.058f, 0.0f), true, false, true);
        [Tooltip("Threshold brightness value where Color4 is swapped for Color5.")]
        public ClampedFloatParameter threshold4 = new ClampedFloatParameter(0.95f, 0.0f, 2.0f);

        [Tooltip("Color band for the final layer (brightest pixels in the image).")]
        public ColorParameter color5 = new ColorParameter(Color.white, true, false, true);

        public override bool IsActive() => (enabled.value && active);
    }
}

