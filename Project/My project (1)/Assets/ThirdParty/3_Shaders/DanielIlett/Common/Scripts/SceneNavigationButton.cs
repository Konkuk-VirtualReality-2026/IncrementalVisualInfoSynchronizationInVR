using UnityEngine;
using System.Collections.Generic;

namespace DanielIlett
{
    [ExecuteInEditMode]
    public class SceneNavigationButton : MonoBehaviour
    {
#if UNITY_EDITOR
        public string sceneName;
        public SceneReference scene;

        private static List<SceneNavigationButton> _buttons = new(40);

        public static List<SceneNavigationButton> Buttons
        {
            get { return _buttons; }
            private set { _buttons = value; }
        }

        private void OnEnable()
        {
            if (!Buttons.Contains(this))
            {
                Buttons.Add(this);
            }
        }

        private void OnDisable()
        {
            Buttons.Remove(this);
        }
#endif
    }
}
