using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    public abstract class SnapshotVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("Should this effect mask out specific layers?\n" +
            "  None: no masking applied. This is the most efficient mode.\n" +
            "  Global: uses the Global Mask attached to this volume profile.\n" +
            "  Local: uses a mask specific to this effect only.")]
        public MaskModeParameter maskMode = new MaskModeParameter(MaskMode.None);

        [Tooltip("Local mask settings for this effect only.")]
        public MaskSettingsParameter maskSettings = new MaskSettingsParameter(new MaskSettings(true));

        [Tooltip("If a mask is being used, should it be inverted for this effect?")]
        public BoolParameter invertMask = new BoolParameter(false);

        public abstract bool IsActive();
    }
}
