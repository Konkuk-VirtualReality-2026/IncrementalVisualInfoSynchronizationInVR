#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace HovlStudio.Editor
{
 [CustomEditor(typeof(MonoBehaviour), true)]
 public class HS_MeshEffectEditor : UnityEditor.Editor
 {
 // slider value stored per-editor instance
 private float emissionMultiplierSlider =1f;

 public override void OnInspectorGUI()
 {
 DrawDefaultInspector();

 var targetType = target.GetType();
 // Match by full name to be specific; fallback to simple name
 if (targetType.FullName == "HovlStudio.HS_MeshEffect" || targetType.Name == "HS_MeshEffect")
 {
 if (GUILayout.Button("Apply mesh effect"))
 {
 // Allow undo and mark scene dirty
 Undo.RecordObject(target, "Apply mesh effect");

 // Invoke ApplyMeshEffect via reflection
 var method = targetType.GetMethod("ApplyMeshEffect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
 if (method != null)
 {
 method.Invoke(target, null);
 EditorUtility.SetDirty(target);
 }
 else
 {
 Debug.LogWarning("HS_MeshEffectEditor: Could not find ApplyMeshEffect method via reflection.");
 }
 }

 GUILayout.Space(6);

 // Slider0..2 for manual multiplier
 emissionMultiplierSlider = EditorGUILayout.Slider("Emission multiplier", emissionMultiplierSlider,0f,2f);
 if (GUILayout.Button("Apply emission multiplier"))
 {
 // Invoke MultiplyEmissionRate(float)
 var method = targetType.GetMethod("MultiplyEmissionRate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
 if (method != null)
 {
 method.Invoke(target, new object[] { emissionMultiplierSlider });
 EditorUtility.SetDirty(target);
 }
 else
 {
 Debug.LogWarning("HS_MeshEffectEditor: Could not find MultiplyEmissionRate method via reflection.");
 }
 }
 }
 }
 }
}
#endif
