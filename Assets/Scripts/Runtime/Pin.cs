using System;
using UnityEngine;

namespace VRB
{
    /// <summary>A bowling pin: remembers where it stood and reports the state the judge needs.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Pin : MonoBehaviour
    {
        Rigidbody m_Body;
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
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
            m_Pit = FindFirstObjectByType<PitZone>();
        }

        void Start()
        {
            // Start still so pins never jitter before the first throw.
            m_Body.Sleep();
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
