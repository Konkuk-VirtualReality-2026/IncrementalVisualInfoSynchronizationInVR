using System.Collections.Generic;
using UnityEngine;

namespace HovlStudio
{
    public class HS_MeshEffect : MonoBehaviour
    {
        public GameObject particleEffects;
        public Material meshMaterial;
        public GameObject selectedObject;

        // Call this from editor button to apply mesh/skinned mesh to particle systems under particleEffects
        public void ApplyMeshEffect()
        {
            // If no selectedObject provided, use parent if available
            if (selectedObject == null && transform.parent != null)
            {
                selectedObject = transform.parent.gameObject;
            }

            if (selectedObject == null)
            {
                Debug.LogWarning("HS_MeshEffect: No selectedObject or parent found to derive mesh from.");
                return;
            }

            // Try to find SkinnedMeshRenderer first, then MeshRenderer
            var skinned = selectedObject.GetComponentInChildren<SkinnedMeshRenderer>();
            var meshRenderer = selectedObject.GetComponentInChildren<MeshRenderer>();

            if (skinned == null && meshRenderer == null)
            {
                Debug.LogWarning("HS_MeshEffect: selectedObject does not contain a MeshRenderer or SkinnedMeshRenderer.");
                return;
            }

            if (particleEffects == null)
            {
                Debug.LogWarning("HS_MeshEffect: particleEffects GameObject is not assigned.");
                return;
            }

            // If a meshMaterial is assigned, ensure it is present as the last material when there are2+ materials (and otherwise ensure at least2 slots)
            if (meshMaterial != null)
            {
                if (skinned != null)
                {
                    var mats = new List<Material>(skinned.sharedMaterials ?? new Material[0]);
                    if (mats.Count >=2)
                    {
                        // Add as last material if not already present
                        if (!mats.Contains(meshMaterial))
                            mats.Add(meshMaterial);
                    }
                    else if (mats.Count ==1)
                    {
                        if (!mats.Contains(meshMaterial))
                            mats.Add(meshMaterial);
                    }
                    else // zero materials
                    {
                        mats.Add(null);
                        mats.Add(meshMaterial);
                    }

                    skinned.sharedMaterials = mats.ToArray();
                }
                else if (meshRenderer != null)
                {
                    var mats = new List<Material>(meshRenderer.sharedMaterials ?? new Material[0]);
                    if (mats.Count >=2)
                    {
                        if (!mats.Contains(meshMaterial))
                            mats.Add(meshMaterial);
                    }
                    else if (mats.Count ==1)
                    {
                        if (!mats.Contains(meshMaterial))
                            mats.Add(meshMaterial);
                    }
                    else
                    {
                        mats.Add(null);
                        mats.Add(meshMaterial);
                    }

                    meshRenderer.sharedMaterials = mats.ToArray();
                }
            }

            // Compute multiplier based on selectedObject bounds
            var rend = selectedObject.GetComponentInChildren<Renderer>();
            Bounds bounds = rend != null ? rend.bounds : new Bounds(Vector3.zero, Vector3.one);

            float boundsMultiplier = ComputeEmissionMultiplier(bounds.size);

            // Incorporate object's lossy scale so that if scale == bounds.size, multiplier becomes1.
            Vector3 scale = selectedObject.transform.lossyScale;
            float scaleBasis = ComputeEmissionMultiplier(scale);
            // Avoid divide by zero
            if (scaleBasis <=0f) scaleBasis =1f;

            float finalMultiplier = boundsMultiplier / scaleBasis;

            // Find all ParticleSystems under particleEffects (including inactive)
            var systems = particleEffects.GetComponentsInChildren<ParticleSystem>(true);

            foreach (var ps in systems)
            {
                if (ps == null) continue;

                // Enable and configure shape
                var shape = ps.shape;
                shape.enabled = true;

                if (skinned != null)
                {
                    // Use SkinnedMeshRenderer shape
                    shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
                    shape.skinnedMeshRenderer = skinned;
                }
                else
                {
                    // Use MeshRenderer shape
                    shape.shapeType = ParticleSystemShapeType.MeshRenderer;
                    shape.meshRenderer = meshRenderer;
                }

                // Adjust emission.rateOverTime by finalMultiplier.
                var emission = ps.emission;
                // Read current rateOverTime constant value (if it was a curve this will take the 'constant' value)
                var current = emission.rateOverTime;
                float baseConstant = current.constant;

                // Compute applied multiplier but only apply half of the change from1.0
                // applied =1 + (finalMultiplier -1) *0.5
                float appliedMultiplier =1f + (finalMultiplier -1f) *0.5f;

                // If appliedMultiplier is approximately1, skip modifying the rate over time
                if (Mathf.Approximately(appliedMultiplier,1f))
                {
                    // leave emission as-is
                }
                else
                {
                    // Apply multiplier and set as constant rate (this simplifies different curve modes)
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseConstant * appliedMultiplier);
                }

                // Additionally adjust rate over distance if needed or other emission settings here
            }

            //Debug.Log("HS_MeshEffect: Applied mesh/skinned-mesh to particle systems. BoundsMult=" + boundsMultiplier + ", ScaleBasis=" + scaleBasis + ", FinalMult=" + finalMultiplier);
        }

        // Multiply emission.rateOverTime by given multiplier for all particle systems under particleEffects
        public void MultiplyEmissionRate(float multiplier)
        {
            if (particleEffects == null)
            {
                Debug.LogWarning("HS_MeshEffect: particleEffects GameObject is not assigned.");
                return;
            }

            var systems = particleEffects.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var emission = ps.emission;
                var current = emission.rateOverTime;
                float baseConstant = current.constant;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseConstant * multiplier);
            }
        }

        // Computes an emission multiplier based on size. This implementation uses the average of pairwise face areas:
        // multiplier = (x*y + x*z + y*z) /3
        // This provides reasonable scaling for non-uniform bounding boxes.
        private float ComputeEmissionMultiplier(Vector3 size)
        {
            float xy = size.x * size.y;
            float xz = size.x * size.z;
            float yz = size.y * size.z;
            float pairwiseAvg = (xy + xz + yz) /3f;

            // For a1x1x1 box this yields1. Keep a minimum to avoid zeroing emission.
            return Mathf.Max(0.0001f, pairwiseAvg);
        }
    }
}