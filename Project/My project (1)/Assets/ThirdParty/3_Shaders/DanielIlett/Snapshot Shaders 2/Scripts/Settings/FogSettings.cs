using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Fog")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/fog/")]
    public class FogSettings : SnapshotVolumeComponent
    {
        public FogSettings()
        {
            displayName = "Fog";
        }

        [Tooltip("Visual strength of the distance-based fog.")]
        public ClampedFloatParameter distanceStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("The distance from the camera at which distance-fog starts appearing.")]
        public FloatParameter startDistance = new FloatParameter(10.0f);

        [Tooltip("Color of the distance-fog at the Start Distance.")]
        public ColorParameter startDistanceColor = new ColorParameter(Color.white, true, true, true);

        [Tooltip("The distance from the camera at which distance-fog is at maximum strength.")]
        public FloatParameter fullStrengthDistance = new FloatParameter(20.0f);

        [Tooltip("Color of the distance-fog at the Full Strength Distance.")]
        public ColorParameter fullStrengthDistanceColor = new ColorParameter(Color.white, true, true, true);

        [Tooltip("Controls the color curve between the Start and Full Strength Distances.")]
        public ClampedFloatParameter distanceFalloff = new ClampedFloatParameter(1.0f, 0.01f, 10.0f);

        [Tooltip("Visual strength of the height-based fog.")]
        public ClampedFloatParameter heightStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("The height in world space at which height-fog starts appearing.")]
        public FloatParameter startHeight = new FloatParameter(0.0f);

        [Tooltip("Color of the height-fog at the Start Height.")]
        public ColorParameter startHeightColor = new ColorParameter(Color.white, true, true, true);

        [Tooltip("The height in world space at which height-fog is at maximum strength.\n" + 
            "Note that if this value is lower than start height, the fog gets stronger as height drops, and vice versa.")]
        public FloatParameter fullStrengthHeight = new FloatParameter(-20.0f);

        [Tooltip("Color of the height-fog at the Full Strength Height.")]
        public ColorParameter fullStrengthHeightColor = new ColorParameter(Color.white, true, true, true);

        [Tooltip("Should the fog be visible at the skybox?")]
        public BoolParameter useFogAtSkybox = new BoolParameter(true);

        [Tooltip("An alternative value for Full Strength Height. " +
            "The two height values are interpolated based on the camera depth. When a pixel is at Full Strength Distance, " +
            "then Full Strength Height is used. When a pixel is at the skybox distance, Skybox Height is used.\n" +
            "This helps to smooth the distance- and height-based fog values.")]
        public FloatParameter skyboxHeight = new FloatParameter(-100.0f);

        [Tooltip("Controls the color curve between the Start and Full Strength Heights.")]
        public ClampedFloatParameter heightFalloff = new ClampedFloatParameter(1.0f, 0.01f, 10.0f);

        public override bool IsActive() => (distanceStrength.value > Mathf.Epsilon || 
            heightStrength.value > Mathf.Epsilon) && active;
    }
}

