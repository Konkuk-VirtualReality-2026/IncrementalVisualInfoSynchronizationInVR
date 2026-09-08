using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Synthwave")]
    public class SynthwaveFeature : ScriptableRendererFeature
    {
        SynthwaveRenderPass pass;

        public override void Create()
        {
            pass = new SynthwaveRenderPass();
            name = "Synthwave";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class SynthwaveRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Synthwave"; }
        }

        public SynthwaveRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Synthwave");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<SynthwaveFeature>(renderGraph, frameData, settings, "SS2 Synthwave Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Synthwave Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Synthwave Main Pass", out var passData, profilingSampler))
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
                    // Set Synthwave effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<SynthwaveSettings>();
                    data.material.SetColor("_LineColor1", settings.lineColor1.value);
                    data.material.SetColor("_LineColor2", settings.lineColor2.value);
                    data.material.SetFloat("_LineColorMix", settings.lineColorMix.value);
                    data.material.SetFloat("_LineWidth", settings.lineWidth.value);
                    data.material.SetFloat("_LineSoftness", settings.lineSoftness.value);
                    data.material.SetVector("_GapWidth", settings.gapWidth.value);
                    data.material.SetVector("_LineOffset", settings.lineOffset.value);
                    data.material.SetFloat("_StartFadeoutDistance", settings.startFadeoutDistance.value);
                    data.material.SetFloat("_EndFadeoutDistance", settings.endFadeoutDistance.value);
                    data.material.SetVector("_AxisMask", settings.axisMask.value.Convert());

                    if (settings.useSceneColor.value == true)
                    {
                        data.material.EnableKeyword("_USE_SCENE_TEXTURE");
                    }
                    else
                    {
                        data.material.DisableKeyword("_USE_SCENE_TEXTURE");
                        data.material.SetColor("_BackgroundColor", settings.backgroundColor.value);
                    }

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
