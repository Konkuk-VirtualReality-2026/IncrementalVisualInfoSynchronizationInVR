using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Sepia Tone")]
    public class SepiaToneFeature : ScriptableRendererFeature
    {
        SepiaToneRenderPass pass;

        public override void Create()
        {
            pass = new SepiaToneRenderPass();
            name = "Sepia Tone";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<SepiaToneSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class SepiaToneRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/SepiaTone"; }
        }

        public SepiaToneRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Sepia Tone");
            requiresIntermediateTexture = true;
        }

        private class SepiaTonePassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public TextureHandle maskTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<SepiaToneSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
            Camera camera = cameraData.camera;
            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<SepiaToneFeature>(renderGraph, frameData, settings, "SS2 Sepia Tone Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Sepia Tone Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<SepiaTonePassData>("SS2 Sepia Tone Main Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = copiedColor;

                builder.UseTexture(copiedColor, AccessFlags.Read);

                if (useGlobalMask && frameData.Contains<GlobalMaskData>())
                {
                    var globalMaskData = frameData.Get<GlobalMaskData>();
                    builder.UseTexture(globalMaskData.globalMaskedObjects, AccessFlags.Read);
                    passData.maskTexture = globalMaskData.globalMaskedObjects;
                }
                else if (useLocalMask)
                {
                    builder.UseTexture(localMaskTextureHandle, AccessFlags.Read);
                    passData.maskTexture = localMaskTextureHandle;
                }

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((SepiaTonePassData data, RasterGraphContext context) =>
                {
                    // Set Sepia Tone effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<SepiaToneSettings>();
                    data.material.SetFloat("_Strength", settings.strength.value);

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }
    }
}
