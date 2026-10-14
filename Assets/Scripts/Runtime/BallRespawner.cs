using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRB
{
    /// <summary>Puts the ball back on its stand: after a throw is resolved, or when it was released over the foul line.</summary>
    [RequireComponent(typeof(Ball))]
    public class BallRespawner : MonoBehaviour
    {
        Ball m_Ball;
        XRGrabInteractable m_Grab;
        BallThrower m_Thrower;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;

        void Awake()
        {
            m_Ball = GetComponent<Ball>();
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Thrower = GetComponent<BallThrower>();
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
        }

        void OnEnable()
        {
            if (m_Thrower != null) m_Thrower.Fouled += ReturnToStand;
        }

        void OnDisable()
        {
            if (m_Thrower != null) m_Thrower.Fouled -= ReturnToStand;
        }

        public void ReturnToStand()
        {
            if (m_Grab != null && m_Grab.isSelected && m_Grab.interactionManager != null)
                m_Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)m_Grab);

            var body = m_Ball.Body;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = m_HomePosition;
            body.rotation = m_HomeRotation;
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            body.Sleep();
        }
    }
}
