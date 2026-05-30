using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRAdaptation.AimTrainer
{
    /// <summary>
    /// 총구멍 데칼 오브젝트 풀. 최대 30개 유지, 초과 시 가장 오래된 것을 재활용한다.
    /// </summary>
    public class BulletHolePool : MonoBehaviour
    {
        public static BulletHolePool Instance { get; private set; }

        [SerializeField] int m_PoolSize = 30;
        [SerializeField] float m_HoleSize = 0.025f;       // 2.5cm
        [SerializeField] float m_FadeOutDuration = 0.25f;  // 재활용 시 페이드아웃 시간

        readonly Queue<GameObject> m_Pool = new();
        readonly Queue<GameObject> m_Active = new(); // 순서 추적 (FIFO)

        Material m_HoleMat;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // 검정 Unlit 머티리얼 생성
            m_HoleMat = new Material(Shader.Find("Unlit/Color")) { color = Color.black };

            for (int i = 0; i < m_PoolSize; i++)
            {
                var go = CreateHole();
                go.SetActive(false);
                m_Pool.Enqueue(go);
            }
        }

        GameObject CreateHole()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BulletHole";
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * m_HoleSize;
            Destroy(go.GetComponent<MeshCollider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = m_HoleMat;
            return go;
        }

        public void Spawn(Vector3 position, Vector3 normal)
        {
            GameObject hole;

            if (m_Pool.Count > 0)
            {
                hole = m_Pool.Dequeue();
            }
            else
            {
                // 가장 오래된 것 재활용
                hole = m_Active.Dequeue();
                StartCoroutine(RecycleWithFade(hole, position, normal));
                return;
            }

            PlaceHole(hole, position, normal);
            m_Active.Enqueue(hole);
        }

        void PlaceHole(GameObject hole, Vector3 position, Vector3 normal)
        {
            hole.SetActive(true);
            // 표면에서 1mm 떨어뜨려 z-fighting 방지
            hole.transform.position = position + normal * 0.001f;
            hole.transform.rotation = Quaternion.LookRotation(-normal);

            var r = hole.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var mat = r.material;
                var c = mat.color;
                c.a = 1f;
                mat.color = c;
            }
        }

        IEnumerator RecycleWithFade(GameObject hole, Vector3 newPos, Vector3 newNormal)
        {
            // 짧게 페이드아웃 후 새 위치에 재배치
            var r = hole.GetComponent<MeshRenderer>();
            float elapsed = 0f;
            while (elapsed < m_FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                if (r != null)
                {
                    var mat = r.material;
                    var c = mat.color;
                    c.a = 1f - elapsed / m_FadeOutDuration;
                    mat.color = c;
                }
                yield return null;
            }

            PlaceHole(hole, newPos, newNormal);
            m_Active.Enqueue(hole);
        }
    }
}
