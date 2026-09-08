using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    [System.Serializable, VolumeComponentMenu("Snapshot Shaders 2/Scanline")]
    [HelpURL("https://danielilett.com/snapshot-shaders-2/scanline/")]
    public class ScanlineSettings : SnapshotVolumeComponent
    {
        public ScanlineSettings()
        {
            displayName = "Scanline";
        }

        [Tooltip("Small texture containing the scanline pattern which will be tiled over the screen.")]
        public TextureParameter scanlineTexture = new TextureParameter(null);

        [Tooltip("How strongly the scanline texture should be multiplied with the original camera texture.")]
        public ClampedFloatParameter scanlineStrength = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("How many pixels the scanline texture takes up (i.e. setting this to 4 will tile the texture over 4x4 pixels).")]
        public ClampedFloatParameter scanlineSize = new ClampedFloatParameter(1.0f, 1.0f, 64.0f);

        [Tooltip("How quickly the scanlines scroll over the screen vertically.")]
        public ClampedFloatParameter scanlineScrollSpeed = new ClampedFloatParameter(0.0f, -10.0f, 10.0f);

        [Tooltip("Should the scanlines use point filtering for a crisper pattern?")]
        public BoolParameter usePointFiltering = new BoolParameter(false);

        public override bool IsActive() => (scanlineStrength.value > Mathf.Epsilon && 
            scanlineTexture.value != null && active);
    }
}

