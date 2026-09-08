using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Global Mask")]
    public class GlobalMaskFeature : ScriptableRendererFeature
    {
        GlobalMaskRenderPass pass;

        public override void Create()
        {
            pass = new GlobalMaskRenderPass();
            name = "Global Mask";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            var settings = SnapshotHelper.GetClosestSettings<GlobalMaskSettings>(camera);

            if (settings != null && settings.IsActive())
            {
                pass.ConfigureInput(ScriptableRenderPassInput.Depth);
                renderer.EnqueuePass(pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    sealed class GlobalMaskRenderPass : SnapshotRenderPass
    {
        public GlobalMaskRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - GlobalMask");
            requiresIntermediateTexture = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var globalMaskSettings = VolumeManager.instance.stack.GetComponent<GlobalMaskSettings>();
            renderPassEvent = globalMaskSettings.renderPassEvent.value;

            var maskSettings = globalMaskSettings.maskSettings.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

            if (cameraData.isPreviewCamera)
            {
                return;
            }

            Camera camera = cameraData.camera;

            var descriptor = GetMaskPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle maskedObjectsHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_GlobalMaskedObjects", false);

            // Render the mask.
            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(passName, out var passData, profilingSampler))
            {
                var lightModes = maskSettings.lightModes;

                passData.rendererList = GetRendererList(renderGraph, renderingData, camera, maskSettings, lightModes.Convert(), MaskMaterial);

                passData.clearColor = Color.black;
                passData.drawSkyboxToMask = maskSettings.drawSkyboxToMask;

                if (maskSettings.drawSkyboxToMask)
                {
                    passData.transformationMatrix = GetSkyboxMeshTransform(camera);
                }

                MaskMaterial.SetFloat("_MaskColor", 1.0f);

                // Make the global mask available to future passes.
                GlobalMaskData globalMaskData;

                if (frameData.Contains<GlobalMaskData>())
                {
                    globalMaskData = frameData.Get<GlobalMaskData>();
                }
                else
                {
                    globalMaskData = frameData.Create<GlobalMaskData>();
                }

                globalMaskData.globalMaskedObjects = maskedObjectsHandle;
                builder.SetRenderAttachment(maskedObjectsHandle, 0, AccessFlags.Write);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) => DrawMaskObjects(data, context, true));
            }
        }
    }

    public sealed class GlobalMaskData : ContextItem
    {
        public TextureHandle globalMaskedObjects;

        public override void Reset()
        {
            globalMaskedObjects = TextureHandle.nullHandle;
        }
    }
}
