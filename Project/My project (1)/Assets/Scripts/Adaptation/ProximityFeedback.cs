using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRAdaptation
{
    /// <summary>
    /// 플레이어가 벽에 가까워질수록 저주파 진동음과 컨트롤러 햅틱을 울린다.
    /// XR Origin(또는 카메라) 하위에 부착한다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProximityFeedback : MonoBehaviour
    {
        [Header("감지 설정")]
        [Tooltip("이 거리(m) 이내에서 피드백 시작")]
        [SerializeField] float m_MaxDistance = 2.5f;
        [Tooltip("레이캐스트 대상 레이어 (벽/지형)")]
        [SerializeField] LayerMask m_WallLayer = ~0;  // 기본 전체 레이어
        [Tooltip("플레이어 위치 기준 Transform (미지정 시 자신)")]
        [SerializeField] Transform m_PlayerRoot;

        [Header("오디오")]
        [Tooltip("루프 클립 미지정 시 내장 사인파 생성기 사용")]
        [SerializeField] AudioClip m_ProximityClip;
        [SerializeField, Range(0f, 1f)] float m_MaxVolume   = 0.45f;
        [SerializeField] float m_FreqMin = 45f;   // Hz (멀 때)
        [SerializeField] float m_FreqMax = 90f;   // Hz (가까울 때)

        [Header("햅틱")]
        [SerializeField, Range(0f, 1f)] float m_MaxHapticAmplitude = 0.5f;
        [Tooltip("햅틱 펄스 간격 (초)")]
        [SerializeField] float m_HapticInterval  = 0.12f;

        // ── 내부 상태 ──────────────────────────────────────────────────────
        AudioSource m_Audio;
        float       m_Proximity;        // 0(멀리) ~ 1(최근접)
        float       m_HapticTimer;

        // 사인파 생성기용 (클립 없을 때)
        float m_Phase;
        bool  m_UseGenerator;

        // 레이캐스트 방향 (수평 8방향 + 대각 고도 4방향)
        static readonly Vector3[] k_Dirs =
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            new Vector3( 1, 0,  1).normalized,
            new Vector3(-1, 0,  1).normalized,
            new Vector3( 1, 0, -1).normalized,
            new Vector3(-1, 0, -1).normalized,
        };

        // ── Unity 생명주기 ─────────────────────────────────────────────────
        void Awake()
        {
            m_Audio = GetComponent<AudioSource>();
            m_Audio.spatialBlend = 0f;   // 2D (공간음 아님, 몸에 울리는 느낌)
            m_Audio.loop         = true;
            m_Audio.playOnAwake  = false;
            m_Audio.volume       = 0f;

            m_UseGenerator = (m_ProximityClip == null);
            if (!m_UseGenerator)
            {
                m_Audio.clip = m_ProximityClip;
                m_Audio.Play();
            }

            if (m_PlayerRoot == null) m_PlayerRoot = transform;
        }

        void Update()
        {
            m_Proximity = CalcProximity();

            // ── 오디오 볼륨 ──────────────────────────────────────────────
            float vol = m_Proximity * m_Proximity * m_MaxVolume;
            m_Audio.volume = Mathf.Lerp(m_Audio.volume, vol, Time.deltaTime * 8f);

            if (!m_UseGenerator)
            {
                if (vol > 0.005f && !m_Audio.isPlaying) m_Audio.Play();
                else if (vol <= 0.005f && m_Audio.isPlaying) m_Audio.Pause();
            }

            // ── 햅틱 펄스 ────────────────────────────────────────────────
            m_HapticTimer += Time.deltaTime;
            float interval = Mathf.Lerp(m_HapticInterval * 3f, m_HapticInterval, m_Proximity);
            if (m_HapticTimer >= interval && m_Proximity > 0.05f)
            {
                float amplitude = m_Proximity * m_Proximity * m_MaxHapticAmplitude;
                SendHaptics(amplitude, interval * 0.8f);
                m_HapticTimer = 0f;
            }
        }

        // 클립 없을 때 사인파로 저주파 진동음 생성
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!m_UseGenerator) return;

            float freq = Mathf.Lerp(m_FreqMin, m_FreqMax, m_Proximity);
            float amp  = m_Proximity * m_Proximity;   // 볼륨은 Update에서 관리

            int samples = data.Length / channels;
            for (int i = 0; i < samples; i++)
            {
                float sample = Mathf.Sin(m_Phase * 2f * Mathf.PI) * amp;
                for (int c = 0; c < channels; c++)
                    data[i * channels + c] = sample;

                m_Phase += freq / AudioSettings.outputSampleRate;
                if (m_Phase >= 1f) m_Phase -= 1f;
            }
        }

        // ── 헬퍼 ──────────────────────────────────────────────────────────

        /// <summary>벽까지의 최단 거리를 0~1 proximity 로 변환</summary>
        float CalcProximity()
        {
            // 가슴 높이 기준 (바닥에서 1.2m)
            Vector3 origin = m_PlayerRoot.position + Vector3.up * 1.2f;
            float minDist  = m_MaxDistance;

            foreach (var dir in k_Dirs)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, m_MaxDistance, m_WallLayer))
                    minDist = Mathf.Min(minDist, hit.distance);
            }

            // 이차 감쇠: 아주 가까울수록 급격히 올라감
            float linear = 1f - (minDist / m_MaxDistance);
            return Mathf.Clamp01(linear);
        }

        /// <summary>XR 컨트롤러 전체에 햅틱 임펄스 전송</summary>
        static void SendHaptics(float amplitude, float duration)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller, devices);

            foreach (var dev in devices)
                dev.SendHapticImpulse(0, amplitude, duration);
        }
    }
}
