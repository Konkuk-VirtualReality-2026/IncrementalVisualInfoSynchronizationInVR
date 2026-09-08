using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Underwater")]
    public class UnderwaterFeature : ScriptableRendererFeature
    {
        UnderwaterRenderPass pass;

        public override void Create()
        {
            pass = new UnderwaterRenderPass();
            name = "Underwater";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class UnderwaterRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Underwater"; }
        }

        public UnderwaterRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Underwater");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<UnderwaterFeature>(renderGraph, frameData, settings, "SS2 Underwater Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Underwater Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Underwater Main Pass", out var passData, profilingSampler))
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
                    // Set Underwater effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<UnderwaterSettings>();

                    data.material.SetFloat("_WaveStrength", settings.waveStrength.value);
                    data.material.SetTexture("_WaveFlowMap", settings.waveFlowMap.value);
                    data.material.SetFloat("_WaveFlowTiling", settings.waveFlowTiling.value);
                    data.material.SetFloat("_WaveFlowSpeed", settings.waveFlowSpeed.value);

                    var causticsMode = settings.causticsMode.value;

                    if(causticsMode != CausticsMode.Off)
                    {
                        if(causticsMode == CausticsMode.AlignedToDirectionalLight)
                        {
                            data.material.SetMatrix("_MainLightMatrix", RenderSettings.sun.transform.localToWorldMatrix);
                        }

                        var tiling = new Vector2(settings.causticsTiling1.value, settings.causticsTiling2.value);

                        data.material.SetTexture("_CausticsTexture", settings.causticsTexture.value);
                        data.material.SetColor("_CausticsTint", settings.causticsTint.value);
                        data.material.SetVector("_CausticsTiling", tiling);
                        data.material.SetVector("_CausticsScrollVelocity1", settings.causticsScrollVelocity1.value);
                        data.material.SetVector("_CausticsScrollVelocity2", settings.causticsScrollVelocity2.value);
                        data.material.SetFloat("_CausticsStartFade", settings.causticsStartFade.value);
                        data.material.SetFloat("_CausticsEndFade", settings.causticsStartFade.value + 
                            settings.causticsFadeFalloff.value);

                        if(settings.causticsColorSeparation.value > Mathf.Epsilon)
                        {
                            data.material.EnableKeyword("_USE_CAUSTICS_COLOR_SEPARATION");
                            data.material.SetFloat("_CausticsColorSeparation", settings.causticsColorSeparation.value);
                        }
                        else
                        {
                            data.material.DisableKeyword("_USE_CAUSTICS_COLOR_SEPARATION");
                        }
                    }  

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    int pass = 0;

                    switch(causticsMode)
                    {
                        case CausticsMode.TriplanarMapped:
                            pass = 1; break;
                        case CausticsMode.AlignedToDirectionalLight:
                            pass = 2; break;
                    }

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, pass);
                });
            }
        }
    }
}
