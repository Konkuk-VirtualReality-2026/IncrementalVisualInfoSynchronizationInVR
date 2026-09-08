using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace DanielIlett.SnapshotShaders2.URP
{
    [DisallowMultipleRendererFeature("Snapshot Shaders 2/Blur")]
    public sealed class BlurFeature : ScriptableRendererFeature
    {
        BlurRenderPass pass;

        public override void Create()
        {
            pass = new BlurRenderPass();
            name = "Blur";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<BlurSettings>();

            if(settings != null && settings.IsActive())
            {
                renderer.EnqueuePass(pass);
            }
        }
    }

    sealed class BlurRenderPass : SnapshotRenderPass
    {
        protected override string ShaderName
        {
            get { return "Hidden/SnapshotShaders2/Blur"; }
        }

        public BlurRenderPass()
        {
            profilingSampler = new ProfilingSampler("SS2 - Blur");
            requiresIntermediateTexture = true;
        }

        private void HorizontalBlurPass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle inputTexture, TextureHandle targetTexture,
            TextureHandle localMaskTextureHandle, bool useGlobalMask, bool useLocalMask, int passIndex)
        {
            // Perform one pass of the Gaussian Blur effect.
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Horizontal Blur Pass", out var passData, profilingSampler))
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

                builder.SetRenderAttachment(targetTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Blur effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<BlurSettings>();

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    data.material.SetInteger("_KernelSize", settings.strength.value);
                    data.material.SetFloat("_Spread", settings.strength.value / 6.0f);
                    data.material.SetInteger("_BlurStepSize", settings.blurStepSize.value);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, passIndex);
                });
            }
        }

        private class VerticalPassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public TextureHandle horizontalTexture;
        }

        private void VerticalBlurPass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle inputTexture, TextureHandle targetTexture,
            TextureHandle horizontalTexture, TextureHandle localMaskTextureHandle, bool useGlobalMask, bool useLocalMask, int passIndex)
        {
            // Perform one pass of the Gaussian Blur effect.
            using (var builder = renderGraph.AddRasterRenderPass<VerticalPassData>("SS2 Vertical Blur Pass", out var passData, profilingSampler))
            {
                passData.material = PassMaterial;
                passData.inputTexture = inputTexture;
                passData.horizontalTexture = horizontalTexture;

                builder.UseTexture(inputTexture, AccessFlags.Read);
                builder.UseTexture(horizontalTexture, AccessFlags.Read);

                if (useGlobalMask && frameData.Contains<GlobalMaskData>())
                {
                    var globalMaskData = frameData.Get<GlobalMaskData>();
                    builder.UseTexture(globalMaskData.globalMaskedObjects, AccessFlags.Read);
                }
                else if (useLocalMask)
                {
                    builder.UseTexture(localMaskTextureHandle, AccessFlags.Read);
                }

                builder.SetRenderAttachment(targetTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((VerticalPassData data, RasterGraphContext context) =>
                {
                    data.material.SetTexture("_HorizontalTexture", data.horizontalTexture);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, passIndex);
                });
            }
        }

        private void RadialBlurPass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle inputTexture, TextureHandle targetTexture,
            TextureHandle localMaskTextureHandle, bool useGlobalMask, bool useLocalMask, int passIndex)
        {
            // Perform the Radial Blur effect pass.
            using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Radial Blur Pass", out var passData, profilingSampler))
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

                builder.SetRenderAttachment(targetTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                {
                    // Set Blur effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<BlurSettings>();

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    data.material.SetInteger("_KernelSize", settings.strength.value);
                    data.material.SetFloat("_Spread", settings.strength.value / 6.0f);
                    data.material.SetInteger("_BlurStepSize", settings.blurStepSize.value);

                    Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, passIndex);
                });
            }
        }

        private class LightStreaksPassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public TextureHandle targetTexture;
            public TextureHandle maskTexture;
        }

        private void LightStreaksPass(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData, 
            TextureHandle inputTexture, TextureHandle targetTexture, TextureHandle localMaskTextureHandle, bool useGlobalMask, bool useLocalMask)
        {
            var descriptor = cameraData.cameraTargetDescriptor;
            int fullWidth = descriptor.width;
            int fullHeight = descriptor.height;
            int mipCount = Mathf.CeilToInt(Mathf.Log(fullHeight));

            TextureHandle[] lightStreakMips = new TextureHandle[mipCount];

            for (int i = 0; i < mipCount; i++)
            {
                int texWidth = Mathf.Max(1, fullWidth >> i);
                int texHeight = Mathf.Max(1, fullHeight >> i);

                var mipDescriptor = GetTempColorDescriptor(cameraData.cameraTargetDescriptor);
                mipDescriptor.width = texWidth;
                mipDescriptor.height = texHeight;

                lightStreakMips[i] = UniversalRenderer.CreateRenderGraphTexture
                (
                    renderGraph,
                    mipDescriptor,
                    $"LightStreakMip{i}",
                    false
                );
            }

            // Perform the Light Streaks effect pass.
            using (var builder = renderGraph.AddUnsafePass<LightStreaksPassData>("SS2 Light Streaks Pass", out var passData, profilingSampler))
            {
                builder.AllowPassCulling(false);
                passData.material = PassMaterial;
                passData.inputTexture = inputTexture;
                passData.targetTexture = targetTexture;

                builder.UseTexture(inputTexture, AccessFlags.Read);
                builder.UseTexture(targetTexture, AccessFlags.Write);

                for (int i = 0; i < mipCount; ++i)
                {
                    builder.UseTexture(lightStreakMips[i], AccessFlags.ReadWrite);
                }

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

                builder.SetRenderFunc((LightStreaksPassData data, UnsafeGraphContext context) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                    // Set Blur effect properties.
                    var settings = VolumeManager.instance.stack.GetComponent<BlurSettings>();

                    if ((RTHandle)data.maskTexture != null)
                    {
                        data.material.SetTexture(maskHandleName, data.maskTexture);
                    }

                    data.material.SetInteger("_KernelSize", settings.strength.value);
                    data.material.SetFloat("_Spread", settings.strength.value / 6.0f);
                    data.material.SetInteger("_BlurStepSize", settings.blurStepSize.value);
                    data.material.SetFloat("_LuminanceThreshold", settings.luminanceThreshold.value);

                    // Thresholding pre-pass.
                    Blitter.BlitCameraTexture(cmd, data.inputTexture, lightStreakMips[0], data.material, 5);

                    // Downsample and blur passes.
                    for (int i = 0; i < mipCount - 1; ++i)
                    {
                        int src = i;
                        int dst = i + 1;

                        Blitter.BlitCameraTexture(cmd, lightStreakMips[src], lightStreakMips[dst], data.material, 6);
                    }

                    // Upsample and combine passes.
                    for (int i = mipCount - 1; i > 0; --i)
                    {
                        int src = i;
                        int dst = i - 1;

                        Blitter.BlitCameraTexture(cmd, lightStreakMips[src], lightStreakMips[dst], data.material, 7);
                    }

                    // Final combine pass.
                    Blitter.BlitCameraTexture(cmd, lightStreakMips[0], data.targetTexture, data.material, 7);
                });
            }
        }

        private RenderTextureDescriptor GetTempColorDescriptor(RenderTextureDescriptor descriptor)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = (int)DepthBits.None;
            descriptor.colorFormat = RenderTextureFormat.ARGB32;

            return descriptor;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
            renderPassEvent = settings.renderPassEvent.value;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Draw a local mask if required, which sets appropriate keywords on the PassMaterial.
            var maskHandleSettings = DrawMaskIfRequired<BlurFeature>(renderGraph, frameData, settings, "SS2 Blur Local Mask");
            bool useGlobalMask = maskHandleSettings.useGlobalMask;
            bool useLocalMask = maskHandleSettings.useLocalMask;
            TextureHandle localMaskTextureHandle = maskHandleSettings.localMaskTextureHandle;

            var colorCopyDescriptor = GetCopyPassDescriptor(cameraData.cameraTargetDescriptor);
            TextureHandle copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_ColorCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("SS2 Blur Copy Color", out var passData, profilingSampler))
            {
                passData.inputTexture = resourceData.activeColorTexture;
                passData.bilinear = true;

                builder.UseTexture(resourceData.activeColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(context.cmd, data.inputTexture, data.bilinear));
            }

            switch(settings.blurMode.value)
            {
                case BlurMode.Gaussian:
                {
                    var tempColorDescriptor = GetTempColorDescriptor(cameraData.cameraTargetDescriptor);
                    TextureHandle tempColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, tempColorDescriptor, "_TempColor", true);

                    HorizontalBlurPass(renderGraph, frameData, resourceData.activeColorTexture, tempColor, localMaskTextureHandle, useGlobalMask, useLocalMask, 0);
                    VerticalBlurPass(renderGraph, frameData, copiedColor, resourceData.activeColorTexture, tempColor, localMaskTextureHandle, useGlobalMask, useLocalMask, 1);
                    break;
                }
                case BlurMode.Box:
                {
                    var tempColorDescriptor = GetTempColorDescriptor(cameraData.cameraTargetDescriptor);
                    TextureHandle tempColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, tempColorDescriptor, "_TempColor", true);

                    HorizontalBlurPass(renderGraph, frameData, resourceData.activeColorTexture, tempColor, localMaskTextureHandle, useGlobalMask, useLocalMask, 2);
                    VerticalBlurPass(renderGraph, frameData, copiedColor, resourceData.activeColorTexture, tempColor, localMaskTextureHandle, useGlobalMask, useLocalMask, 3);
                    break;
                }
                case BlurMode.Radial:
                {
                    RadialBlurPass(renderGraph, frameData, copiedColor, resourceData.activeColorTexture, localMaskTextureHandle, useGlobalMask, useLocalMask, 4);
                    break;
                }
                case BlurMode.LightStreaks:
                {
                        /*
                    var descriptor = cameraData.cameraTargetDescriptor;
                    int fullWidth = descriptor.width;
                    int fullHeight = descriptor.height;
                    int mipCount = Mathf.CeilToInt(Mathf.Log(fullHeight));
                    TextureHandle[] lightStreakMips = new TextureHandle[mipCount];
                    for (int i = 0; i < mipCount; i++)
                    {
                        int texWidth = Mathf.Max(1, fullWidth >> i);
                        int texHeight = Mathf.Max(1, fullHeight >> i);

                        var mipDescriptor = GetTempColorDescriptor(cameraData.cameraTargetDescriptor);
                        mipDescriptor.width = texWidth;
                        mipDescriptor.height = texHeight;

                        lightStreakMips[i] = UniversalRenderer.CreateRenderGraphTexture
                        (
                            renderGraph,
                            mipDescriptor,
                            $"LightStreakMip{i}",
                            false
                        );
                    }
                        */

                    LightStreaksPass(renderGraph, frameData, cameraData, copiedColor, resourceData.activeColorTexture, localMaskTextureHandle, useGlobalMask, useLocalMask);












                        /*
                    // Downsample and blur passes.
                    for (int i = 0; i < mipCount - 1; ++i)
                    {
                        int src = i;
                        int dst = i + 1;

                        using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>($"SS2 Light Streaks Downsample {i}", out var passData))
                        {
                            passData.material = PassMaterial;
                            passData.inputTexture = lightStreakMips[src];
                                
                            builder.AllowPassCulling(false);

                            builder.UseTexture(lightStreakMips[src], AccessFlags.Read);
                            builder.SetRenderAttachment(lightStreakMips[dst], 0, AccessFlags.Write);

                            builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                            {
                                Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 6);
                            });
                        }
                    }

                    // Upsample and combine passes.
                    for (int i = mipCount - 1; i > 0; --i)
                    {
                        int src = i;
                        int dst = i - 1;

                        using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>($"SS2 Light Streaks Upsample {i}", out var passData))
                        {
                            passData.material = PassMaterial;
                            passData.inputTexture = lightStreakMips[src];
                            builder.AllowPassCulling(false);

                            builder.UseTexture(lightStreakMips[src], AccessFlags.Read);
                            builder.SetRenderAttachment(lightStreakMips[dst], 0, AccessFlags.Write);

                            builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                            {
                                Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 7);
                            });
                        }
                    }

                    using (var builder = renderGraph.AddRasterRenderPass<BasicPassData>("SS2 Light Streaks Combine", out var passData))
                    {
                        passData.material = PassMaterial;
                        passData.inputTexture = lightStreakMips[0];

                        builder.UseTexture(lightStreakMips[0], AccessFlags.Read);
                        builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                        builder.AllowPassCulling(false);

                        builder.SetRenderFunc((BasicPassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(context.cmd, data.inputTexture, new Vector4(1, 1, 0, 0), data.material, 7);
                        });
                    }
                        */

                    break;
                }
            }
        }
    }
}
