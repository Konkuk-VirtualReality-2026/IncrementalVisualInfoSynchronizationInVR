using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Retro")]
    public sealed class RetroFeature : SnapshotRendererFeature
    {
        RetroRenderPass pass;

        public override void Create()
        {
            pass = new RetroRenderPass();
            name = "Retro";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            AddRenderPasses<RetroSettings>(renderer, pass);
        }
    }

    sealed class RetroRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Retro"; }
        }

        public RetroRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Retro");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<RetroSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<RetroFeature>(renderGraph, frameData, settings, "SS2 Retro Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_RetroCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Retro Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Retro Main Pass", out var passData, profilingSampler))
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
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Retro effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<RetroSettings>();

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    int pass = (int)settings.drawingMode.value;

                    switch(settings.drawingMode.value)
                    {
                        case RetroDrawingMode.GameBoy:
                            data.material.SetColor("_GBDarkest", settings.darkestColor.value);
                            data.material.SetColor("_GBDark", settings.darkColor.value);
                            data.material.SetColor("_GBLight", settings.lightColor.value);
                            data.material.SetColor("_GBLightest", settings.lightestColor.value);
                            break;
                        case RetroDrawingMode.SNES:
                            data.material.SetInteger("_RedLevels", settings.redLevels.value);
                            data.material.SetInteger("_GreenLevels", settings.greenLevels.value);
                            data.material.SetInteger("_BlueLevels", settings.blueLevels.value);
                            break;
                    }

                    data.material.SetFloat("_PowerRamp", settings.powerRamp.value);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, pass);
                });
            }
        }
    }
}

