using UnityEngine;
using UnityEngine.Rendering;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Underwater")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/underwater/")]
    public class UnderwaterSettings : SnapshotVolumeComponent
    {
        public UnderwaterSettings()
        {
            displayName = "Underwater";
        }

        [Tooltip("A texture representing the movement of screen UVs. The red and green channels of the image " + 
            "encode the strength and direction of movement of each part of the screen.")]
        public TextureParameter waveFlowMap = new TextureParameter(null);

        [Tooltip("How strongly the wave flow map should distort the screen UVs.")]
        public ClampedFloatParameter waveStrength = new ClampedFloatParameter(0.0f, 0.0f, 25.0f);

        [Tooltip("How many times the wave flow map should be tiled over the screen.")]
        public ClampedFloatParameter waveFlowTiling = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);

        [Tooltip("How quickly the wave flow map scrolls over the screen.")]
        public ClampedFloatParameter waveFlowSpeed = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("Caustics are complex light reflections and refractions caused by curved surfaces, such as water ripples.\n\n" + 
            "  Off: No caustics are rendered.\n" + 
            "  Triplanar: Use three axis-aligned texture samples which are combined based on world normals.\n" + 
            "  Light Aligned: Align caustic UVs along the main directional light.")]
        public CausticsModeParameter causticsMode = new CausticsModeParameter(CausticsMode.Off);

        [Tooltip("Texture to use for the caustics.")]
        public TextureParameter causticsTexture = new TextureParameter(null);

        [Tooltip("Tint applied to the caustics. Use the alpha channel to control the overall strength.")]
        public ColorParameter causticsTint = new ColorParameter(Color.white);

        [Tooltip("How frequently to tile the first caustic texture sample.")]
        public NoInterpClampedFloatParameter causticsTiling1 = new NoInterpClampedFloatParameter(0.5f, 0.01f, 2.0f);

        [Tooltip("How frequently to tile the second caustic texture sample.")]
        public NoInterpClampedFloatParameter causticsTiling2 = new NoInterpClampedFloatParameter(0.5f, 0.01f, 2.0f);

        [Tooltip("How quickly the first caustic sample scrolls over scene objects.")]
        public NoInterpVector3Parameter causticsScrollVelocity1 = new NoInterpVector3Parameter(new Vector3(0.75f, 0.25f, 0.0f));

        [Tooltip("How quickly the second caustic sample scrolls over scene objects.")]
        public NoInterpVector3Parameter causticsScrollVelocity2 = new NoInterpVector3Parameter(new Vector3(0.75f, 0.25f, 0.0f));

        [Tooltip("How far the individual color channels of the caustics texture get separated.\n\n" + 
            "Warning: the number of texture samples will be tripled. Triplanar mode will use 9 samples, and \n" + 
            "light aligned mode uses 3.")]
        public ClampedFloatParameter causticsColorSeparation = new ClampedFloatParameter(0.0f, 0.0f, 0.1f);

        [Tooltip("The distance from the camera where the caustics begin fading out.")]
        public FloatParameter causticsStartFade = new FloatParameter(25.0f);

        [Tooltip("The distance over which the caustics fade from full to zero strength.")]
        public FloatParameter causticsFadeFalloff = new FloatParameter(25.0f);

        public override bool IsActive() => (waveStrength.value > Mathf.Epsilon || causticsMode.value != CausticsMode.Off) && active;
    }
}

