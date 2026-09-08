using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Thermal")]
    public class ThermalFeature : ScriptableRendererFeature
    {
        ThermalRenderPass pass;

        public override void Create()
        {
            pass = new ThermalRenderPass();
            name = "Thermal";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<ThermalSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class ThermalRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Thermal"; }
        }

        public ThermalRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Thermal");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<ThermalSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);
            TextureHandle blurredMask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_BlurredMask", false);

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<ThermalFeature>(renderGraph, frameData, settings, "SS2 Thermal Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            if (settings.maskMode == MaskMode.None)
            {
                // Perform the intermediate copy pass (source -> temp).
                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Thermal Copy Color", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourceData.activeColorTexture;
                    passData.bilinear = true;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
                }
            }
            else
            {
                TextureHandle maskTexture;

                if (useGlobalMask && frameData.Contains<GlobalMaskData>())
                {
                    var globalMaskData = frameData.Get<GlobalMaskData>();
                    maskTexture = globalMaskData.globalMaskedObjects;
                }
                else
                {
                    maskTexture = localMaskTextureHandle;
                }

                // Horizontal mask blur pass (mask -> blurred mask).
                using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Thermal Horizontal Mask Blur", out var passData, profilingSampler))
                {
                    passData.material = PassMaterial;
                    passData.inputTexture = maskTexture;

                    builder.UseTexture(maskTexture, AccessFlags.Read);

                    builder.SetRenderAttachment(blurredMask, 0, AccessFlags.Write);
                    builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                    {
                        // Set Thermal effect properties.
                        var settings = VolumeManager.instance.stack.GetComponent<ThermalSettings>();

                        data.material.SetInteger("_KernelSize", settings.maskBlur.value);
                        data.material.SetFloat("_Spread", settings.maskBlur.value / 6.0f);
                        data.material.SetInteger("_BlurStepSize", settings.maskBlurStepSize.value);

                        Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 1);
                    });
                }

                // Vertical mask blur pass composited onto color copy ({source, blurred mask} -> temp).
                using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Thermal Vertical Mask Blur", out var passData, profilingSampler))
                {
                    passData.material = PassMaterial;
                    passData.inputTexture = resourceData.activeColorTexture;
                    passData.maskTexture = blurredMask;

                    builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                    builder.UseTexture(blurredMask, AccessFlags.Read);

                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                    {
                        // Set Thermal effect properties.
                        var settings = VolumeManager.instance.stack.GetComponent<ThermalSettings>();

                        data.material.SetTexture(maskHandleName, data.maskTexture);
                        data.material.SetFloat("_MaskBrightnessBoost", settings.addBrightnessToMask.value);

                        if (settings.thermalMaskMode.value == ThermalMaskMode.RenderMaskObjectsOnly)
                        {
                            data.material.EnableKeyword("_RENDER_ONLY_MASK");
                        }
                        else
                        {
                            data.material.DisableKeyword("_RENDER_ONLY_MASK");
                        }

                        Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 2);
                    });
                }
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Thermal Main Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = copiedColor;
                //passData.maskTexture = blurredMask;

                builder.UseTexture(copiedColor, AccessFlags.Read);

                /*
                if(settings.maskMode.value != MaskMode.None && settings.thermalMaskMode.value == ThermalMaskMode.RenderMaskObjectsOnly)
                {
                    builder.UseTexture(blurredMask, AccessFlags.Read);
                }
                */

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Thermal effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<ThermalSettings>();

                    data.material.SetColor("_Color0", settings.color0.value);
                    data.material.SetFloat("_Threshold0", settings.threshold0.value);
                    data.material.SetColor("_Color1", settings.color1.value);
                    data.material.SetFloat("_Threshold1", settings.threshold1.value);
                    data.material.SetColor("_Color2", settings.color2.value);
                    data.material.SetFloat("_Threshold2", settings.threshold2.value);
                    data.material.SetColor("_Color3", settings.color3.value);
                    data.material.SetFloat("_Threshold3", settings.threshold3.value);
                    data.material.SetColor("_Color4", settings.color4.value);
                    data.material.SetFloat("_Threshold4", settings.threshold4.value);
                    data.material.SetColor("_Color5", settings.color5.value);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }
    }
}
