using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Keeps a panel in the edge of the player's view, like a rear-view mirror: turned toward the head, to one side and
    /// a little up, and following the head's heading (not its pitch) with a lag, so looking down at the ball does not
    /// drag it along and head turns do not make it jitter.
    /// </summary>
    public class PeripheralFollow : MonoBehaviour
    {
        [SerializeField] Camera m_Head;
        [Tooltip("In the head's heading frame: x to the right, y up, z forward, in meters.")]
        [SerializeField] Vector3 m_Offset = new Vector3(-0.55f, 0.22f, 0.95f);
        [SerializeField] float m_SmoothTime = 0.35f;

        Vector3 m_Velocity;

        void Start()
        {
            SnapToTarget();
        }

        void LateUpdate()
        {
            var target = TargetPosition();
            transform.position = Vector3.SmoothDamp(transform.position, target, ref m_Velocity, m_SmoothTime);
            FaceHead();
        }

        /// <summary>Jump straight to where the panel belongs (first frame, or after the player is moved).</summary>
        public void SnapToTarget()
        {
            transform.position = TargetPosition();
            m_Velocity = Vector3.zero;
            FaceHead();
        }

        Vector3 TargetPosition()
        {
            var head = m_Head.transform;
            var heading = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
            return head.position + heading * m_Offset;
        }

        void FaceHead()
        {
            var away = transform.position - m_Head.transform.position;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
