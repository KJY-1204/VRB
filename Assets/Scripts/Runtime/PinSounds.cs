using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Pin impact and strike sounds. The hit clip is a whole crash, so the ball's first contact plays it in full
    /// and later pin-on-pin impacts play short, rate-limited fragments of it instead of stacking full copies.
    /// </summary>
    public class PinSounds : MonoBehaviour
    {
        [SerializeField] PinRack m_Rack;
        [SerializeField] AudioClip m_HitClip;
        [SerializeField] AudioClip m_StrikeClip;
        [SerializeField] int m_Voices = 4;
        [SerializeField] float m_FragmentSeconds = 0.4f;
        [SerializeField] float m_MinFragmentInterval = 0.12f;
        [SerializeField] float m_MinImpactSpeed = 0.8f;

        AudioSource[] m_Pool;
        AudioSource m_StrikeSource;
        int m_NextVoice;
        float m_LastFullPlayTime = float.NegativeInfinity;
        float m_LastFragmentTime = float.NegativeInfinity;

        void Awake()
        {
            if (m_Rack == null) m_Rack = GetComponent<PinRack>();

            m_Pool = new AudioSource[Mathf.Max(1, m_Voices)];
            for (var i = 0; i < m_Pool.Length; i++)
                m_Pool[i] = CreateSource($"PinVoice_{i}", spatial: true);

            m_StrikeSource = CreateSource("StrikeVoice", spatial: false);
        }

        void OnEnable()
        {
            foreach (var pin in GetComponentsInChildren<Pin>()) pin.Collided += OnPinCollided;
            if (m_Rack != null) m_Rack.Judged += OnJudged;
        }

        void OnDisable()
        {
            foreach (var pin in GetComponentsInChildren<Pin>()) pin.Collided -= OnPinCollided;
            if (m_Rack != null) m_Rack.Judged -= OnJudged;
        }

        AudioSource CreateSource(string sourceName, bool spatial)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatial ? 1f : 0f;
            source.minDistance = 2f;
            source.maxDistance = 40f;
            return source;
        }

        void OnPinCollided(Pin pin, Collision collision)
        {
            if (m_HitClip == null) return;

            var speed = collision.relativeVelocity.magnitude;
            var byBall = collision.collider.GetComponentInParent<Ball>() != null;

            if (byBall && Time.time - m_LastFullPlayTime > m_HitClip.length)
            {
                m_LastFullPlayTime = Time.time;
                Play(pin.transform.position, 0f, m_HitClip.length, Mathf.Clamp(speed / 6f, 0.4f, 1f));
                return;
            }

            if (speed < m_MinImpactSpeed) return;
            if (Time.time - m_LastFragmentTime < m_MinFragmentInterval) return;

            m_LastFragmentTime = Time.time;
            var offset = Random.Range(0.2f, Mathf.Max(0.3f, m_HitClip.length - m_FragmentSeconds - 0.1f));
            Play(pin.transform.position, offset, m_FragmentSeconds, Mathf.Clamp(speed / 8f, 0.15f, 0.7f));
        }

        void Play(Vector3 position, float offset, float duration, float volume)
        {
            var source = m_Pool[m_NextVoice];
            m_NextVoice = (m_NextVoice + 1) % m_Pool.Length;

            source.Stop();
            source.transform.position = position;
            source.clip = m_HitClip;
            source.time = Mathf.Clamp(offset, 0f, Mathf.Max(0f, m_HitClip.length - 0.01f));
            source.volume = volume;
            source.Play();
            source.SetScheduledEndTime(AudioSettings.dspTime + duration);
        }

        void OnJudged(int fallen)
        {
            if (fallen < 10 || m_StrikeClip == null) return;
            m_StrikeSource.PlayOneShot(m_StrikeClip);
        }
    }
}
