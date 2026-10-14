using System;
using UnityEngine;

namespace VRB
{
    /// <summary>A bowling pin: remembers where it stood and reports the state the judge needs.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Pin : MonoBehaviour
    {
        /// <summary>Above this tilt a pin counts as lying down, where it needs rolling resistance to come to rest.</summary>
        const float LyingTiltDegrees = 60f;

        /// <summary>Lying pins slower than this get extra damping; fast tumbling pins are left alone.</summary>
        const float DampedBelowSpeed = 2f;

        [Tooltip("Angular damping while the pin lies on its side. A cone-shaped pin otherwise rolls in circles for many seconds.")]
        [SerializeField] float m_LyingAngularDamping = 2f;

        Rigidbody m_Body;
        float m_StandingAngularDamping;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        PitZone m_Pit;

        /// <summary>Raised whenever this pin collides with something (ball, another pin, the lane).</summary>
        public event Action<Pin, Collision> Collided;

        /// <summary>True while the pin's base is inside the pit volume.</summary>
        public bool InPit => m_Pit != null && m_Pit.Contains(transform.position);

        public float TiltDegrees => Vector3.Angle(transform.up, Vector3.up);
        public float Displacement => Vector3.Distance(transform.position, m_HomePosition);
        public float LinearSpeed => m_Body.linearVelocity.magnitude;
        public float AngularSpeed => m_Body.angularVelocity.magnitude;
        public bool IsSleeping => m_Body.IsSleeping();

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_StandingAngularDamping = m_Body.angularDamping;
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
            m_Pit = FindFirstObjectByType<PitZone>();
        }

        void Start()
        {
            // Start still so pins never jitter before the first throw.
            m_Body.Sleep();
        }

        void FixedUpdate()
        {
            if (m_Body.IsSleeping()) return;

            var lying = TiltDegrees > LyingTiltDegrees && LinearSpeed < DampedBelowSpeed;
            m_Body.angularDamping = lying ? m_LyingAngularDamping : m_StandingAngularDamping;
        }

        void OnCollisionEnter(Collision collision)
        {
            Collided?.Invoke(this, collision);
        }

        /// <summary>Put the pin back on its spot, upright and motionless.</summary>
        public void ResetToHome()
        {
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            m_Body.position = m_HomePosition;
            m_Body.rotation = m_HomeRotation;
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            m_Body.Sleep();
        }
    }
}
