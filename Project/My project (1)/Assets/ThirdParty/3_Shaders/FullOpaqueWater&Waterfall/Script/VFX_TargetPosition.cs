using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace VFX
{
    [ExecuteAlways]
    public class TargetPosition : MonoBehaviour
    {
        public Transform target;

        private int shaderPropertyID_Current;
        private int shaderPropertyID_Past;

        private Queue<(float time, Vector3 position)> positionHistory = new Queue<(float, Vector3)>();

        void Start()
        {
            shaderPropertyID_Current = Shader.PropertyToID("_TargetTurbulencePose");
            shaderPropertyID_Past = Shader.PropertyToID("_TargetTurbulencePose2");

#if UNITY_EDITOR
            EditorApplication.update += UpdateInEditor;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= UpdateInEditor;
#endif
        }

        void FixedUpdate()
        {
            if (Application.isPlaying)
            {
                // [수정] 빌드본에서는 Time.time을 사용하고, 에디터에서는 EditorApplication.timeSinceStartup을 사용하도록 분기
                float currentTime = GetCurrentTime();
                UpdateShader(currentTime);
            }
        }

#if UNITY_EDITOR
        void UpdateInEditor()
        {
            if (!Application.isPlaying)
            {
                UpdateShader((float)EditorApplication.timeSinceStartup);
            }
        }
#endif

        // 시간을 가져오는 헬퍼 함수
        private float GetCurrentTime()
        {
#if UNITY_EDITOR
            return (float)EditorApplication.timeSinceStartup;
#else
            return Time.time; // 빌드본(런타임)에서는 이 값을 사용합니다.
#endif
        }

        void UpdateShader(float currentTime)
        {
            if (target == null) return;

            Vector3 currentPosition = target.position;
            positionHistory.Enqueue((currentTime, currentPosition));

            Vector3 pastPosition = currentPosition; 

            while (positionHistory.Count > 0)
            {
                var (time, position) = positionHistory.Peek();
                float age = currentTime - time;

                if (age > 0.2f)
                {
                    positionHistory.Dequeue();
                }
                else
                {
                    pastPosition = position;
                    break;
                }
            }

            Shader.SetGlobalVector(shaderPropertyID_Current, currentPosition);
            Shader.SetGlobalVector(shaderPropertyID_Past, pastPosition);
        }
    }
}