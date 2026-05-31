using UnityEngine;

namespace VRAdaptation
{
    /// <summary>
    /// 컨트롤러(또는 손) Transform에 부착하는 접촉 프로브.
    /// 활성화되면 <see cref="SurfaceGlowManager"/>에 자신을 등록하고,
    /// 매니저가 이 프로브의 <see cref="TipPosition"/>을 기준으로 벽 접촉을 검사한다.
    ///
    /// 감지 기준점은 컨트롤러 루트가 아니라 "팁(끝)"이어야 시각적으로 자연스럽다.
    /// 컨트롤러 루트(그립) 기준 로컬 오프셋(<see cref="m_TipOffset"/>)으로 팁 위치를 지정한다.
    /// XRI/패키지 타입에 의존하지 않도록 단순 Transform 기반으로 동작한다.
    /// </summary>
    public class GlowContactProbe : MonoBehaviour
    {
        [Tooltip("컨트롤러 루트(그립) 기준 팁의 로컬 오프셋(m). 기본값은 forward 10cm(=Muzzle 위치와 동일).")]
        [SerializeField] Vector3 m_TipOffset = new Vector3(0f, 0f, 0.1f);

        [Tooltip("이 프로브의 접촉 반경(m). 0 이하이면 매니저 기본값을 사용한다.")]
        [SerializeField] float m_RadiusOverride = 0f;

        public float RadiusOverride => m_RadiusOverride;

        /// <summary>벽 접촉을 검사할 월드 좌표(컨트롤러 팁).</summary>
        public Vector3 TipPosition => transform.TransformPoint(m_TipOffset);

        void OnEnable()  => SurfaceGlowManager.Register(this);
        void OnDisable() => SurfaceGlowManager.Unregister(this);

        // 에디터에서 팁 위치를 항상 표시 (디버깅 용이)
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireSphere(TipPosition, 0.02f);
            Gizmos.DrawLine(transform.position, TipPosition);
        }
    }
}
