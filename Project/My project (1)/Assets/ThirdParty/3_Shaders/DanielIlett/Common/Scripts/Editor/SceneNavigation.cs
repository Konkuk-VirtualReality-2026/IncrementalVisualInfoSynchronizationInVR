using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace DanielIlett
{
    [InitializeOnLoad]
    public class SceneNavigationEditor : Editor
    {
#if UNITY_EDITOR
        private const float buttonWidth = 160.0f;
        private const float buttonHeight = 30.0f;

        private static SceneReference sceneToLoad = null;
        private static List<SceneNavigationButton> buttonsToCheck = new();

        protected static GUIStyle _buttonStyle;
        protected static GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12
                    };
                }

                return _buttonStyle;
            }
        }

        static SceneNavigationEditor()
        {
            SceneView.duringSceneGui -= OnDuringSceneGUI;
            SceneView.duringSceneGui += OnDuringSceneGUI;

            SceneView.beforeSceneGui -= OnBeforeSceneGUI;
            SceneView.beforeSceneGui += OnBeforeSceneGUI;
        }

        private static void OnBeforeSceneGUI(SceneView sceneView)
        {
            if (sceneToLoad != null)
            {
                var aboutToLoadScene = sceneToLoad;
                sceneToLoad = null;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    var sceneCamera = sceneView.camera.transform;
                    var offset = sceneView.pivot - sceneCamera.position;
                    sceneView.pivot = new Vector3(0.0f, 1.0f, -10.0f) + offset;
                    EditorSceneManager.OpenScene(aboutToLoadScene.ScenePath);
                }
            }

            buttonsToCheck.Clear();

            var buttons = SceneNavigationButton.Buttons;

            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].scene.ScenePath == string.Empty)
                {
                    continue;
                }

                var buttonPos = buttons[i].transform.position;
                var cameraPos = sceneView.camera.transform.position;

                if (Vector3.Distance(buttonPos, cameraPos) < 15.0f)
                {
                    if (Vector3.Dot(sceneView.camera.transform.forward, buttonPos - cameraPos) > 0.0f)
                    {
                        buttonsToCheck.Add(buttons[i]);
                    }
                }
            }
        }

        private static void OnDuringSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();

            var buttons = SceneNavigationButton.Buttons;

            for (int i = 0; i < buttonsToCheck.Count; i++)
            {
                var btn = buttonsToCheck[i];
                var buttonPos = btn.transform.position;

                var position = HandleUtility.WorldToGUIPoint(buttonPos);
                var rect = new Rect(position.x - buttonWidth / 2.0f, position.y - buttonHeight / 2.0f, buttonWidth, buttonHeight);

                if (GUI.Button(rect, new GUIContent($"<b>{btn.sceneName}</b>", $"Load the {btn.sceneName} scene."), ButtonStyle))
                {
                    sceneToLoad = btn.scene;
                }
            }

            Handles.EndGUI();
        }
#endif
    }
}
