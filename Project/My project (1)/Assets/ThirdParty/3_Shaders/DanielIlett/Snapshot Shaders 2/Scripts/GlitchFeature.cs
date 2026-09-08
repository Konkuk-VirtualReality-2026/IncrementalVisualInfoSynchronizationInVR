using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Glitch")]
    public class GlitchFeature : ScriptableRendererFeature
    {
        GlitchRenderPass pass;

        public override void Create()
        {
            pass = new GlitchRenderPass();
            name = "Glitch";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<GlitchSettings>();

            if (settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class GlitchRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Glitch"; }
        }

        public GlitchRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Glitch");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<GlitchSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = TextureHandle.nullHandle;

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<GlitchFeature>(renderGraph, frameData, settings, "SS2 Glitch Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            // Perform the intermediate copy pass (source -> temp).
            copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Glitch Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            // Perform main pass (temp -> source).
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Glitch Main Pass", out var passData, profilingSampler))
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
                    // Set Glitch effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<GlitchSettings>();
                    data.material.SetFloat("_OffsetStrength", settings.offsetStrength.value);

                    if(settings.offsetTexture.value == null)
                    {
                        data.material.SetTexture("_OffsetTexture", Texture2D.linearGrayTexture);
                    }
                    else
                    {
                        data.material.SetTexture("_OffsetTexture", settings.offsetTexture.value);
                    }
                        
                    data.material.SetFloat("_OffsetSpeed", settings.offsetSpeed.value);
                    data.material.SetFloat("_OffsetTiling", settings.offsetTiling.value);

                    if(settings.usePointFilter.value)
                    {
                        data.material.EnableKeyword("_USE_POINT_FILTER");
                    }
                    else
                    {
                        data.material.DisableKeyword("_USE_POINT_FILTER");
                    }

                    if (settings.useSliceBandGlitches.value)
                    {
                        data.material.EnableKeyword("_USE_SLICE_BANDS");

                        data.material.SetFloat("_SliceBandStrength", settings.sliceBandStrength.value);
                        data.material.SetFloat("_SliceBandSpeed", settings.sliceBandSpeed.value);
                        data.material.SetFloat("_SliceBandJitter", settings.sliceBandJitter.value);
                        data.material.SetFloat("_SliceBandWidth", settings.sliceBandWidth.value);
                        data.material.SetFloat("_SliceBandMinDuration", settings.sliceBandMinDuration.value);
                        data.material.SetFloat("_SliceBandMaxDuration", settings.sliceBandMaxDuration.value);
                        data.material.SetFloat("_SliceBandFrequency", settings.sliceBandFrequency.value);

                    }
                    else
                    {
                        data.material.DisableKeyword("_USE_SLICE_BANDS");
                    }

                    if(settings.useBlockArtifacts.value)
                    {
                        data.material.EnableKeyword("_USE_BLOCK_ARTIFACTS");

                        data.material.SetVector("_BlockArtifactTiling", settings.blockArtifactTiling.value);
                        data.material.SetFloat("_BlockArtifactChance", settings.blockArtifactChance.value);
                        data.material.SetFloat("_BlockArtifactStrengthMin", settings.blockArtifactStrengthMin.value);
                        data.material.SetFloat("_BlockArtifactStrengthMax", settings.blockArtifactStrengthMax.value);
                        data.material.SetFloat("_BlockArtifactSpeed", settings.blockArtifactSpeed.value);
                    }
                    else
                    {
                        data.material.DisableKeyword("_USE_BLOCK_ARTIFACTS");
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
