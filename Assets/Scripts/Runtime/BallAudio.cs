using UnityEngine;

namespace VRB
{
    /// <summary>Plays a looping rolling sound whose volume and pitch follow the ball's speed while it touches the lane.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
    public class BallAudio : MonoBehaviour
    {
        const float GroundedGraceSeconds = 0.15f;

        [SerializeField] AudioClip m_RollClip;
        [SerializeField] float m_FullVolumeSpeed = 6f;
        [SerializeField] float m_FadeSpeed = 6f;

        Rigidbody m_Body;
        AudioSource m_Source;
        float m_LastGroundedTime = float.NegativeInfinity;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Source = GetComponent<AudioSource>();
            m_Source.clip = m_RollClip;
            m_Source.loop = true;
            m_Source.playOnAwake = false;
            m_Source.volume = 0f;
            if (m_RollClip != null) m_Source.Play();
        }

        void OnCollisionStay(Collision collision)
        {
            if (collision.collider.GetComponentInParent<Pin>() != null) return;

            for (var i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    m_LastGroundedTime = Time.time;
                    return;
                }
            }
        }

        void Update()
        {
            var grounded = Time.time - m_LastGroundedTime < GroundedGraceSeconds;
            var speed = m_Body.linearVelocity.magnitude;
            var target = grounded ? Mathf.Clamp01(speed / m_FullVolumeSpeed) : 0f;

            m_Source.volume = Mathf.MoveTowards(m_Source.volume, target, m_FadeSpeed * Time.deltaTime);
            m_Source.pitch = Mathf.Lerp(0.85f, 1.15f, Mathf.Clamp01(speed / 10f));
        }
    }
}
