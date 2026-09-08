using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Painting")]
    public sealed class PaintingFeature : SnapshotRendererFeature
    {
        PaintingRenderPass pass;

        public override void Create()
        {
            pass = new PaintingRenderPass();
            name = "Painting";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            AddRenderPasses<PaintingSettings>(renderer, pass);
        }
    }

    sealed class PaintingRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Painting"; }
        }

        public PaintingRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Painting");
            requiresIntermediateTexture = true;
        }

        private class PaintingPassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public TextureHandle maskTexture;
            public TextureHandle prePassTexture;
        }

        private RenderTextureDescriptor GetPrePassDescriptor(RenderTextureDescriptor descriptor)
        {
            descriptor.colorFormat = RenderTextureFormat.ARGBFloat;
            return descriptor;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<PaintingSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            var prePassDescriptor = GetPrePassDescriptor(colorCopyDescriptor);
            TextureHandle prePassHandle = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<PaintingFeature>(renderGraph, frameData, settings, "SS2 Painting Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);
            prePassHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, prePassDescriptor, "_PrePassTex", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Painting Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Painting Pre Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = copiedColor;

                builder.UseTexture(copiedColor, AccessFlags.Read);

                //builder.AllowPassCulling(false); 

                builder.SetRenderAttachment(prePassHandle, 0, AccessFlags.Write);
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Painting effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<PaintingSettings>();
                    data.material.SetInteger("_KernelSize", settings.kernelSize.value);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<PaintingPassData>("SS2 Painting Main Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = copiedColor;
                passData.prePassTexture = prePassHandle;

                builder.UseTexture(copiedColor, AccessFlags.Read);
                builder.UseTexture(prePassHandle, AccessFlags.Read);

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
                builder.SetRenderFunc((PaintingPassData data, RasterGraphContext context) =>
                {
                    // Set Painting effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<PaintingSettings>();
                    data.material.SetInteger("_KernelSize", settings.kernelSize.value);
                    data.material.SetTexture("_PrePassData", data.prePassTexture);

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }
        }
    }
}
