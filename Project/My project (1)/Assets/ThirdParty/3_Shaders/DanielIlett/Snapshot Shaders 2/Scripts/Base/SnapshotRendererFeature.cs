using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    public abstract class SnapshotRendererFeature : ScriptableRendererFeature
    {
        protected void AddRenderPasses<T>(ScriptableRenderer renderer, SnapshotRenderPass pass) 
            where T : SnapshotVolumeComponent
        {
            var settings = VolumeManager.instance.stack.GetComponent<T>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }
}
