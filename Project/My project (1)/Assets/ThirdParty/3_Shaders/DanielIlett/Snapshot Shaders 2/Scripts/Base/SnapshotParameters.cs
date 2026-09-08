using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor.Rendering;
    using UnityEditor;
#endif

namespace DanielIlett.SnapshotShaders2.URP
{
    // Allow each volume settings object to track the render pass event.
    [Serializable]
    public sealed class RenderPassEventParameter : VolumeParameter<RenderPassEvent>
    {
        public RenderPassEventParameter(RenderPassEvent value, bool overrideState = false) : base(value, overrideState) { }
    }

    // Allow mask-drawing volumes to use a RenderQueue as a parameter.
    public enum RenderQueueType
    {
        Opaque,
        Transparent,
        All
    }

    [Serializable]
    public sealed class RenderQueueParameter : VolumeParameter<RenderQueueType>
    {
        public RenderQueueParameter(RenderQueueType value, bool overrideState = false) : base(value, overrideState) { }
    }

    // Allows mask-drawing volumes to restrict the selection to specific LightMode types.
    public enum LightModeType
    {
        [InspectorName("UniversalForward (Lit shaders)")] UniversalForward,
        [InspectorName("UniversalForwardOnly (some custom shaders)")] UniversalForwardOnly,
        [InspectorName("SRPDefaultUnlit (Unlit shaders)")] SRPDefaultUnlit,
        [InspectorName("UniversalGBuffer")] UniversalGBuffer,
        [InspectorName("Universal2D")] Universal2D,
        [InspectorName("ShadowCaster")] ShadowCaster,
        [InspectorName("DepthOnly")] DepthOnly,
        [InspectorName("DepthNormals")] DepthNormals,
        [InspectorName("DepthNormalsOnly")] DepthNormalsOnly,
        [InspectorName("Meta")] Meta
    }

    [Serializable]
    public sealed class LightModeTypeListParameter : VolumeParameter<List<LightModeType>>
    {
        public LightModeTypeListParameter(List<LightModeType> value, bool overrideState = false) : base(value, overrideState) { }
    }

#if UNITY_EDITOR
    // Custom Editor drawer for drawing the LightMode list.
    [VolumeParameterDrawer(typeof(LightModeTypeListParameter))]
    sealed class LightModeTypeListParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Generic)
            {
                return false;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(value, title, true);
            EditorGUI.indentLevel--;

            return true;
        }
    }
#endif

    public enum MaskMode
    {
        None = 0,
        Global = 1,
        Local = 2
    }

    [Serializable]
    public sealed class MaskModeParameter : VolumeParameter<MaskMode>
    {
        public MaskModeParameter(MaskMode value, bool overrideState = false) : base (value, overrideState) { }
    }

    [Serializable]
    public sealed class MaskSettingsParameter : VolumeParameter<MaskSettings>
    {
        public MaskSettingsParameter(MaskSettings value) : base(value, true) { }

        // Make this type of parameter permanently active.
        public override bool overrideState { get => true; set => base.overrideState = true; }
    }

    public enum PaintingDrawingMode
    {
        Oil = 0
    }

    [Serializable]
    public sealed class PaintingDrawingModeParameter : VolumeParameter<PaintingDrawingMode>
    {
        public PaintingDrawingModeParameter(PaintingDrawingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    public enum RetroDrawingMode
    {
        [InspectorName("Game Boy")] GameBoy = 0,
        [InspectorName("SNES (Posterize)")] SNES = 1
    }

    [Serializable]
    public sealed class RetroDrawingModeParameter : VolumeParameter<RetroDrawingMode>
    {
        public RetroDrawingModeParameter(RetroDrawingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public enum NoiseInterpolationMode
    {
        Hermite, 
        Quintic
    }

    [Serializable]
    public sealed class NoiseInterpModeParameter : VolumeParameter<NoiseInterpolationMode>
    {
        public NoiseInterpModeParameter(NoiseInterpolationMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public enum BlurMode
    {
        Gaussian,
        Box,
        Radial,
        LightStreaks
    }

    [Serializable]
    public sealed class BlurModeParameter : VolumeParameter<BlurMode>
    {
        public BlurModeParameter(BlurMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public enum ThermalMaskMode
    {
        RenderMaskObjectsOnly,
        RenderMaskObjectsBright
    }

    [Serializable]
    public sealed class ThermalMaskModeParameter : VolumeParameter<ThermalMaskMode>
    {
        public ThermalMaskModeParameter(ThermalMaskMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public enum CausticsMode
    {
        AlignedToDirectionalLight,
        TriplanarMapped,
        Off
    }

    [Serializable]
    public sealed class CausticsModeParameter : VolumeParameter<CausticsMode>
    {
        public CausticsModeParameter(CausticsMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    [Serializable]
    public enum AxisMask
    {
        X, Y, Z, XY, XZ, YZ, XYZ
    }

    [Serializable]
    public sealed class AxisMaskParameter : VolumeParameter<AxisMask>
    {
        public AxisMaskParameter(AxisMask value, bool overrideState = false) : base(value, overrideState) { }
    }

    public static class SnapshotParameterExtensions
    {
        // Convert RenderQueueType -> RenderQueueRange.
        public static RenderQueueRange Convert(this RenderQueueType renderQueueType)
        {
            if (renderQueueType == RenderQueueType.Opaque)
            {
                return RenderQueueRange.opaque;
            }
            else if (renderQueueType == RenderQueueType.Transparent)
            {
                return RenderQueueRange.transparent;
            }

            return RenderQueueRange.all;
        }

        // Convert LightModeType -> string.
        public static string Convert(this LightModeType lightModeType)
        {
            switch (lightModeType)
            {
                case LightModeType.UniversalForwardOnly:
                    return "UniversalForwardOnly";
                case LightModeType.UniversalForward:
                    return "UniversalForward";
                case LightModeType.SRPDefaultUnlit:
                    return "SRPDefaultUnlit";
                case LightModeType.UniversalGBuffer:
                    return "UniversalGBuffer";
                case LightModeType.Universal2D:
                    return "Universal2D";
                case LightModeType.ShadowCaster:
                    return "ShadowCaster";
                case LightModeType.DepthOnly:
                    return "DepthOnly";
                case LightModeType.DepthNormals:
                    return "DepthNormals";
                case LightModeType.DepthNormalsOnly:
                    return "DepthNormalsOnly";
                case LightModeType.Meta:
                    return "Meta";
            }

            return "";
        }

        // Convert List<LightModeType> -> List<ShaderTagId>.
        public static List<ShaderTagId> Convert(this List<LightModeType> lightModeTypes)
        {
            var lightModeList = new List<ShaderTagId>(lightModeTypes.Count);

            for (int i = 0; i < lightModeTypes.Count; ++i)
            {
                lightModeList.Add(new ShaderTagId(lightModeTypes[i].Convert()));
            }

            return lightModeList;
        }

        public static Vector3 Convert(this AxisMask axisMask)
        {
            switch(axisMask)
            {
                case AxisMask.X:  return new Vector3(1.0f, 0.0f, 0.0f);
                case AxisMask.Y:  return new Vector3(0.0f, 1.0f, 0.0f);
                case AxisMask.Z:  return new Vector3(0.0f, 0.0f, 1.0f);
                case AxisMask.XY: return new Vector3(1.0f, 1.0f, 0.0f);
                case AxisMask.XZ: return new Vector3(1.0f, 0.0f, 1.0f);
                case AxisMask.YZ: return new Vector3(0.0f, 1.0f, 1.0f);
                default: return new Vector3(1.0f, 1.0f, 1.0f);
            }
        }
    }
}
