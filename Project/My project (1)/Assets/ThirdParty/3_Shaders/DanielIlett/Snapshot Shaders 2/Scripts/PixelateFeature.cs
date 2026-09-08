using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Pixelate")]
    public sealed class PixelateFeature : SnapshotRendererFeature
    {
        PixelateRenderPass pass;

        public override void Create()
        {
            pass = new PixelateRenderPass();
            name = "Pixelate";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            AddRenderPasses<PixelateSettings>(renderer, pass);
        }
    }

    sealed class PixelateRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Pixelate"; }
        }

        public PixelateRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Pixelate");
            requiresIntermediateTexture = true;
        }

        private RenderTextureDescriptor GetPixelatedDescriptor(RenderTextureDescriptor descriptor, PixelateSettings settings)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = (int)DepthBits.None;

            int width = Mathf.Max(1, descriptor.width / settings.downsampleSize.value);
            int height = Mathf.Max(1, descriptor.height / settings.downsampleSize.value);

            descriptor.width = width;
            descriptor.height = height;

            return descriptor;
        }

        private void CombinePixelatePass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle inputTexture,
            TextureHandle targetTexture, TextureHandle localMaskTextureHandle, PixelateSettings settings,
            bool useGlobalMask, bool useLocalMask)
        {
            // Perform combine (color + mask) pass (copy -> temp).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Pixelate Copy Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = inputTexture;

                builder.UseTexture(inputTexture, AccessFlags.Read);

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

                if (settings.usePointFiltering.value)
                {
                    PassMaterial.EnableKeyword("_USE_POINT_FILTER");
                }
                else
                {
                    PassMaterial.DisableKeyword("_USE_POINT_FILTER");
                }

                builder.SetRenderAttachment(targetTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Pixelate effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<PixelateSettings>();

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }

        private void CompositePixelatePass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle inputTexture,
            TextureHandle targetTexture, TextureHandle localMaskTextureHandle, PixelateSettings settings,
            bool useGlobalMask, bool useLocalMask)
        {
            // Perform composite pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Pixelate Main Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = inputTexture;

                builder.UseTexture(inputTexture, AccessFlags.Read);

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

                if (settings.usePointFiltering.value)
                {
                    PassMaterial.EnableKeyword("_USE_POINT_FILTER");
                }
                else
                {
                    PassMaterial.DisableKeyword("_USE_POINT_FILTER");
                }

                builder.SetRenderAttachment(targetTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Pixelate effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<PixelateSettings>();

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    var maskExpansionAmount = (useGlobalMask || useLocalMask) ? (settings.expandMask.value ? 1 : 0) : 0;
                    data.material.SetInteger("_MaskExpansion", maskExpansionAmount);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<PixelateSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<PixelateFeature>(renderGraph, frameData, settings, "SS2 Pixelate Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            var filterMode = settings.usePointFiltering.value ? FilterMode.Point : FilterMode.Bilinear;

            // Perform the intermediate copy pass (source -> temp).
            var colorCopyDescriptor = GetPixelatedDescriptor(cameraData.cameraTargetDescriptor, settings);
            TextureHandle copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_PixelateCopy", false, filterMode: filterMode);

            CombinePixelatePass(renderGraph, frameData, resourceData.activeColorTexture, copiedColor, localMaskTextureHandle, settings, useGlobalMask, useLocalMask);
            CompositePixelatePass(renderGraph, frameData, copiedColor, resourceData.activeColorTexture, localMaskTextureHandle, settings, useGlobalMask, useLocalMask);
        }
    }
}
