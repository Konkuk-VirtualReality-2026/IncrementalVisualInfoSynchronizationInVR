using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Greyscale")]
    public sealed class GreyscaleFeature : ScriptableRendererFeature
    {
        GreyscaleRenderPass pass;

        public override void Create()
        {
            pass = new GreyscaleRenderPass();
            name = "Greyscale";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<GreyscaleSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class GreyscaleRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Greyscale"; }
        }

        public GreyscaleRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Greyscale");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<GreyscaleSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<GreyscaleFeature>(renderGraph, frameData, settings, "SS2 Greyscale Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Greyscale Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Greyscale Main Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = copiedColor;

                builder.UseTexture(copiedColor, AccessFlags.Read);

                if(useGlobalMask && frameData.Contains<GlobalMaskData>())
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
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Greyscale effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<GreyscaleSettings>();
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
