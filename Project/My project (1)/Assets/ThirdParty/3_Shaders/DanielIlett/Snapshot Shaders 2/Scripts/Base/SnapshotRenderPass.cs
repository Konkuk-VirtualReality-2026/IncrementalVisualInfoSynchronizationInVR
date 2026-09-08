using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Rendering.RendererUtils;

namespace DanielIlett.SnapshotShaders2.URP
{
    public abstract class SnapshotRenderPass : ScriptableRenderPass
    {
        protected virtual string ShaderName { get; }

        private const string maskShaderName = "Hidden/SnapshotShaders2/Mask";
        protected const string maskHandleName = "_MaskedObjects";
        protected const string useMaskKeyword = "_SS2_USE_MASK";

        protected Material _passMaterial = null;
        protected Material PassMaterial
        {
            get
            {
                if (_passMaterial == null)
                {
                    var shader = Shader.Find(ShaderName);

                    if (shader == null)
                    {
                        Debug.LogError($"Cannot find shader: \"{ShaderName}\".");
                        return null;
                    }

                    _passMaterial = new Material(shader);
                }
                return _passMaterial;
            }
            set
            {
                _passMaterial = value;
            }
        }

        protected Material _maskMaterial = null;
        protected Material MaskMaterial
        {
            get
            {
                if (_maskMaterial == null)
                {
                    var shader = Shader.Find(maskShaderName);

                    if (shader == null)
                    {
                        Debug.LogError($"Cannot find shader: \"{maskShaderName}\".");
                        return null;
                    }

                    _maskMaterial = new Material(shader);
                }
                return _maskMaterial;
            }
            set
            {
                _maskMaterial = value;
            }
        }

        protected Mesh _skyboxMesh = null;
        protected Mesh SkyboxMesh
        {
            get
            {
                // Skybox mesh is just a plane we render at the far clip plane, behind everything else.
                if(_skyboxMesh == null)
                {
                    _skyboxMesh = new Mesh();

                    Vector3[] vertices = 
                    {
                        new Vector3(-1.0f, -1.0f, 0.0f),
                        new Vector3( 1.0f, -1.0f, 0.0f),
                        new Vector3( 1.0f,  1.0f, 0.0f),
                        new Vector3(-1.0f,  1.0f, 0.0f)
                    };

                    int[] triangles = 
                    {
                        0, 2, 1, 0, 3, 2
                    };

                    _skyboxMesh.vertices = vertices;
                    _skyboxMesh.triangles = triangles;
                    _skyboxMesh.RecalculateBounds();
                }

                return _skyboxMesh;
            }
        }

        protected static RenderTextureDescriptor GetCopyPassDescriptor(RenderTextureDescriptor descriptor)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = (int)DepthBits.None;

            return descriptor;
        }

        protected static RenderTextureDescriptor GetMaskPassDescriptor(RenderTextureDescriptor descriptor)
        {
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = (int)DepthBits.None;
            descriptor.colorFormat = RenderTextureFormat.R8;

            return descriptor;
        }

        protected class CopyPassData
        {
            public TextureHandle inputTexture;
            public bool bilinear;
        }

        protected class BasicPassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public TextureHandle maskTexture;
        }

        protected class MaskPassData
        {
            public RendererListHandle rendererList;
            public TextureHandle depthHandle;
            public Color clearColor;
            public bool drawSkyboxToMask;
            public Matrix4x4 transformationMatrix;
        }

        protected RendererListHandle GetRendererList(RenderGraph renderGraph, UniversalRenderingData renderingData, Camera camera, MaskSettings settings, List<ShaderTagId> shaderTagIDs, Material material)
        {
            var cullingResults = renderingData.cullResults;

            RenderQueueRange renderQueueRange = settings.renderQueue.Convert();

            int passIndex = 0;
            SortingCriteria sortingCriteria =
                (settings.renderQueue == RenderQueueType.Transparent) ? SortingCriteria.CommonTransparent : SortingCriteria.CommonOpaque;

            var rendererListDesc = new RendererListDesc(shaderTagIDs.ToArray(), cullingResults, camera)
            {
                renderQueueRange = renderQueueRange,
                layerMask = settings.layerMask.value,
                renderingLayerMask = settings.renderingLayerMask.value,
                overrideMaterial = material,
                overrideMaterialPassIndex = passIndex,
                sortingCriteria = sortingCriteria
            };

            return renderGraph.CreateRendererList(rendererListDesc);
        }

        // Somehow TextureHandle can't be used when returning multiple types from a method.
        protected struct MaskHandleSettings
        {
            public TextureHandle localMaskTextureHandle;
            public bool useGlobalMask;
            public bool useLocalMask;

            public MaskHandleSettings(TextureHandle localMaskTextureHandle, bool useGlobalMask, bool useLocalMask)
            {
                this.localMaskTextureHandle = localMaskTextureHandle;
                this.useGlobalMask = useGlobalMask;
                this.useLocalMask = useLocalMask;
            }
        }

        protected MaskHandleSettings DrawMaskIfRequired<T>(RenderGraph renderGraph, ContextContainer frameData, 
            SnapshotVolumeComponent settings, string passName) 
            where T : ScriptableRendererFeature
        {
            Camera camera = frameData.Get<UniversalCameraData>().camera;

            bool useGlobalMask = false;
            bool useLocalMask = false;
            TextureHandle localMaskTextureHandle = TextureHandle.nullHandle;

            switch (settings.maskMode.value)
            {
                case MaskMode.Global:
                    {
                        var globalMaskSettings = SnapshotHelper.GetClosestSettings<GlobalMaskSettings>(camera);
                        bool shouldRenderWithMask = globalMaskSettings != null && globalMaskSettings.IsActive() &&
                            globalMaskSettings.renderPassEvent.value <= settings.renderPassEvent.value;

#if UNITY_EDITOR
                        shouldRenderWithMask &= SnapshotHelper.CheckFeatureIsMaskCompatible<T>();
#endif
                        if (shouldRenderWithMask)
                        {
                            useGlobalMask = true;
                        }
                    }
                    break;
                case MaskMode.Local:
                    {
                        var localMaskSettings = settings.maskSettings.value;
                        localMaskTextureHandle = DrawLocalMask(renderGraph, frameData, localMaskSettings, "_LocalMaskedObjects", passName);
                        useLocalMask = true;
                    }
                    break;
            }

            // Set appropriate modes on the PassMaterial based on the component's mask settings.
            if (useLocalMask || useGlobalMask)
            {
                PassMaterial.EnableKeyword(useMaskKeyword);
            }
            else
            {
                PassMaterial.DisableKeyword(useMaskKeyword);
            }

            PassMaterial.SetFloat("_InvertMask", settings.invertMask.value ? 1.0f : 0.0f);

            return new MaskHandleSettings(localMaskTextureHandle, useGlobalMask, useLocalMask);
        }

        protected static void ExecuteCopyPass(RasterCommandBuffer cmd, RTHandle source, bool bilinear)
        {
            Blitter.BlitTexture(cmd, source, new Vector4(1, 1, 0, 0), 0.0f, bilinear);
        }

        protected void DrawMaskObjects(MaskPassData data, RasterGraphContext context, bool clear)
        {
            if (clear)
            {
                context.cmd.ClearRenderTarget(true, true, data.clearColor);
            }

            context.cmd.DrawRendererList(data.rendererList);

            if(data.drawSkyboxToMask)
            {
                context.cmd.DrawMesh(SkyboxMesh, data.transformationMatrix, MaskMaterial);
            }
        }

        protected Matrix4x4 GetSkyboxMeshTransform(Camera camera)
        {
            Vector3 offset = camera.transform.position + camera.farClipPlane * 0.99f * camera.transform.forward;
            Quaternion rotation = camera.transform.rotation;
            float farClipPlane = camera.farClipPlane;
            Vector3 planeScale = new Vector3(farClipPlane * camera.aspect, farClipPlane, farClipPlane);

            return Matrix4x4.Translate(offset) * Matrix4x4.Rotate(rotation) * Matrix4x4.Scale(planeScale);
        }

        protected TextureHandle DrawLocalMask(RenderGraph renderGraph, ContextContainer frameData, 
            MaskSettings maskSettings, string textureName, string passName)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            Camera camera = cameraData.camera;

            var descriptor = GetMaskPassDescriptor(cameraData.cameraTargetDescriptor); 
            TextureHandle maskedObjectsHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, textureName, false);

            // Render the mask.
            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(passName, out var passData, profilingSampler))
            {
                var lightModes = maskSettings.lightModes;

                passData.rendererList = GetRendererList(renderGraph, renderingData, camera, maskSettings, lightModes.Convert(), MaskMaterial);

                passData.clearColor = Color.black;
                passData.drawSkyboxToMask = maskSettings.drawSkyboxToMask;

                if(maskSettings.drawSkyboxToMask)
                {
                    passData.transformationMatrix = GetSkyboxMeshTransform(camera);
                }

                MaskMaterial.SetFloat("_MaskColor", 1.0f);
                builder.SetRenderAttachment(maskedObjectsHandle, 0, AccessFlags.Write);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) => 
                    DrawMaskObjects(data, context, true));
            }

            return maskedObjectsHandle;
        }
    }
}
