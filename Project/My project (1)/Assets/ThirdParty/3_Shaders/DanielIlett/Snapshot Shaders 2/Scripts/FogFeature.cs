using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Fog")]
    public class FogFeature : ScriptableRendererFeature
    {
        FogRenderPass pass;

        public override void Create()
        {
            pass = new FogRenderPass();
            name = "Fog";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<FogSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class FogRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Fog"; }
        }

        public FogRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Fog");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<FogSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<FogFeature>(renderGraph, frameData, settings, "SS2 Fog Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Fog Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Fog Main Pass", out var passData, profilingSampler))
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
                    // Set Fog effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<FogSettings>();
                    data.material.SetFloat("_DistanceStrength", settings.distanceStrength.value);
                    data.material.SetFloat("_StartDistance", settings.startDistance.value);
                    data.material.SetColor("_StartDistanceColor", settings.startDistanceColor.value);
                    data.material.SetFloat("_FullStrengthDistance", settings.fullStrengthDistance.value);
                    data.material.SetColor("_FullStrengthDistanceColor", settings.fullStrengthDistanceColor.value);
                    data.material.SetFloat("_DistanceFalloff", settings.distanceFalloff.value);

                    data.material.SetFloat("_HeightStrength", settings.heightStrength.value);
                    data.material.SetFloat("_StartHeight", settings.startHeight.value);
                    data.material.SetColor("_StartHeightColor", settings.startHeightColor.value);
                    data.material.SetFloat("_FullStrengthHeight", settings.fullStrengthHeight.value);
                    data.material.SetFloat("_FullStrengthHeightAtSkybox", settings.skyboxHeight.value);
                    data.material.SetColor("_FullStrengthHeightColor", settings.fullStrengthHeightColor.value);
                    data.material.SetFloat("_HeightFalloff", settings.heightFalloff.value);

                    if(settings.useFogAtSkybox.value)
                    {
                        data.material.EnableKeyword("_USE_SKYBOX_FOG");
                    }
                    else
                    {
                        data.material.DisableKeyword("_USE_SKYBOX_FOG");
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
