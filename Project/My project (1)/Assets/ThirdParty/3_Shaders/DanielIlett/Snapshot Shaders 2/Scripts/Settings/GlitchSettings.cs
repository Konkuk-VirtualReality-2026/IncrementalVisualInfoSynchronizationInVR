using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Glitch")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/glitch/")]
    public class GlitchSettings : SnapshotVolumeComponent
    {
        public GlitchSettings()
        {
            displayName = "Glitch";
        }

        // Offset glitches.

        [Tooltip("How strongly the screen UVs are offset by the glitch texture.")]
        public FloatParameter offsetStrength = new ClampedFloatParameter(0.0f, 0.0f, 0.2f);

        [Tooltip("Texture which controls the strength of the glitch offset based on y-coordinate.")]
        public TextureParameter offsetTexture = new TextureParameter(null);

        public BoolParameter usePointFilter = new BoolParameter(false);

        public ClampedFloatParameter offsetSpeed = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("Controls how many times the glitch texture repeats vertically.")]
        public ClampedFloatParameter offsetTiling = new ClampedFloatParameter(1.0f, 0.0f, 50.0f);

        // Slice segment glitches.

        public BoolParameter useSliceBandGlitches = new BoolParameter(false);

        public ClampedFloatParameter sliceBandStrength = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);

        public ClampedFloatParameter sliceBandSpeed = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public ClampedFloatParameter sliceBandJitter = new ClampedFloatParameter(0.0f, 0.0f, 0.25f);

        public ClampedFloatParameter sliceBandWidth = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public ClampedFloatParameter sliceBandMinDuration = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public ClampedFloatParameter sliceBandMaxDuration = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public ClampedFloatParameter sliceBandFrequency = new ClampedFloatParameter(0.0f, 0.0f, 5.0f);

        // Block artifact glitches.

        public BoolParameter useBlockArtifacts = new BoolParameter(false);

        public Vector2Parameter blockArtifactTiling = new Vector2Parameter(Vector2.one);

        public ClampedFloatParameter blockArtifactChance = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public ClampedFloatParameter blockArtifactStrengthMin = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);

        public ClampedFloatParameter blockArtifactStrengthMax = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);

        public ClampedFloatParameter blockArtifactSpeed = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public override bool IsActive() => (offsetStrength.value > Mathf.Epsilon ||
            useSliceBandGlitches.value || useBlockArtifacts.value) && active;
    }
}

