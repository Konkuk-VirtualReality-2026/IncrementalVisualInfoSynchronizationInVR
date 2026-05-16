using UnityEngine;
using System.Collections.Generic;

namespace VRAdaptation
{
    public class GlobalAdaptationEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] LayerMask m_TargetLayers;
        [SerializeField] bool m_ApplyToAllLayers = false;
        [SerializeField] Shader m_AdaptationShader;
        
        [Header("State")]
        [SerializeField] bool m_EffectActive = true;

        private struct RendererState
        {
            public Renderer renderer;
            public Material[] originalMaterials;
        }

        private List<RendererState> m_StoredStates = new List<RendererState>();

        private static readonly string[] SkipKeywords = { 
            "vignette", "sky", "overlay", "camera", "hand", "controller", 
            "teleport", "gaze", "cursor", "ui", "canvas", "atmosphere", "dome" 
        };

        // Property Name Mapping
        private static readonly string[] TexPropNames = { "_BaseMap", "_MainTex", "_Texture", "_Albedo" };
        private static readonly string[] BumpPropNames = { "_BumpMap", "_NormalMap" };
        private static readonly string[] ColorPropNames = { "_BaseColor", "_Color" };

        public void SetEffectActive(bool active)
        {
            if (m_EffectActive == active) return;
            m_EffectActive = active;

            if (m_EffectActive) ApplyEffect();
            else RestoreEffect();
        }

        void Awake()
        {
            if (m_EffectActive) ApplyEffect();
        }

        [ContextMenu("Apply Effect")]
        public void ApplyEffect()
        {
            if (m_AdaptationShader == null)
            {
                m_AdaptationShader = Shader.Find("VR/AdaptationProgressive");
                if (m_AdaptationShader == null) return;
            }

            m_StoredStates.Clear();
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (var ren in allRenderers)
            {
                string objName = ren.gameObject.name.ToLower();
                bool shouldSkip = false;

                foreach (string keyword in SkipKeywords)
                {
                    if (objName.Contains(keyword)) { shouldSkip = true; break; }
                }

                if (ren.gameObject.layer == 5) shouldSkip = true;

                bool inLayer = ((1 << ren.gameObject.layer) & m_TargetLayers) != 0;
                
                if (!shouldSkip && (m_ApplyToAllLayers || inLayer))
                {
                    m_StoredStates.Add(new RendererState { renderer = ren, originalMaterials = ren.sharedMaterials });

                    Material[] newMaterials = new Material[ren.sharedMaterials.Length];
                    for (int i = 0; i < ren.sharedMaterials.Length; i++)
                    {
                        Material original = ren.sharedMaterials[i];
                        if (original == null) continue;

                        newMaterials[i] = new Material(m_AdaptationShader);
                        newMaterials[i].name = original.name + " (Adaptation)";
                        
                        // Copy Base Map
                        foreach (var prop in TexPropNames)
                        {
                            if (original.HasProperty(prop)) {
                                newMaterials[i].SetTexture("_BaseMap", original.GetTexture(prop));
                                break;
                            }
                        }

                        // Copy Normal Map
                        foreach (var prop in BumpPropNames)
                        {
                            if (original.HasProperty(prop)) {
                                newMaterials[i].SetTexture("_BumpMap", original.GetTexture(prop));
                                break;
                            }
                        }

                        // Copy Base Color
                        foreach (var prop in ColorPropNames)
                        {
                            if (original.HasProperty(prop)) {
                                newMaterials[i].SetColor("_BaseColor", original.GetColor(prop));
                                break;
                            }
                        }
                        
                        // Copy PBR properties if they exist
                        if (original.HasProperty("_Metallic")) newMaterials[i].SetFloat("_Metallic", original.GetFloat("_Metallic"));
                        if (original.HasProperty("_Smoothness")) newMaterials[i].SetFloat("_Smoothness", original.GetFloat("_Smoothness"));
                        else if (original.HasProperty("_Glossiness")) newMaterials[i].SetFloat("_Smoothness", original.GetFloat("_Glossiness"));
                    }
                    ren.materials = newMaterials;
                }
            }
            Debug.Log($"[VRAdaptation] Adaptation applied to {m_StoredStates.Count} objects.");
        }

        [ContextMenu("Restore Effect")]
        public void RestoreEffect()
        {
            foreach (var state in m_StoredStates)
            {
                if (state.renderer != null)
                    state.renderer.materials = state.originalMaterials;
            }
            m_StoredStates.Clear();
            Debug.Log("[VRAdaptation] Materials restored.");
        }
    }
}
