using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace DanielIlett.SnapshotShaders2.URP.Editor
{
    [VolumeParameterDrawer(typeof(MaskSettingsParameter))]
    public class MaskSettingsParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent label)
        {
            var prop = parameter.value;

            var layerMask = prop.FindPropertyRelative("layerMask");
            var renderingLayerMask = prop.FindPropertyRelative("renderingLayerMask");
            var lightModes = prop.FindPropertyRelative("lightModes");
            var renderQueue = prop.FindPropertyRelative("renderQueue");
            var invertMask = prop.FindPropertyRelative("invertMask");

            EditorGUILayout.PropertyField(parameter.value, label, true);

            return true;
        }

        public override bool IsAutoProperty() => false;
    }
}
