using UnityEngine;

namespace VRB
{
    /// <summary>
    /// A lane in the hall where someone on another device is bowling: ten pins and a ball that only look the part. They
    /// are set to follow the pictures sent from that device (<see cref="LaneSnapshot"/>), smoothly, since those arrive
    /// about fifteen times a second.
    /// </summary>
    public class RemoteLane : MonoBehaviour
    {
        [SerializeField] Transform[] m_Bodies = new Transform[LaneSnapshot.BodyCount];
        [Tooltip("How quickly the bodies catch up with the latest picture, per second.")]
        [SerializeField] float m_Smoothing = 25f;
        [Tooltip("A body that is this far from where it should be jumps there instead of gliding (a rack being set up).")]
        [SerializeField] float m_SnapDistance = 2f;

        readonly LaneSnapshot m_Target = new LaneSnapshot();
        bool m_HasTarget;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>One of the stand-ins: 0 to 9 are the pins, 10 is the ball.</summary>
        public Transform Body(int index) => m_Bodies[index];

        /// <summary>Put the lane where it belongs next to the one being played (meters sideways, in the lane's frame).</summary>
        public void SetSideways(float x)
        {
            var position = transform.localPosition;
            position.x = x;
            transform.localPosition = position;
        }

        public void Show(bool show)
        {
            if (gameObject.activeSelf == show) return;

            gameObject.SetActive(show);
            if (!show) m_HasTarget = false;
        }

        /// <summary>Follow this picture from now on. The first one a lane gets (or <paramref name="jump"/>) is taken at once.</summary>
        public void SetTarget(LaneSnapshot snapshot, bool jump = false)
        {
            m_Target.CopyFrom(snapshot);
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
                m_Bodies[i].gameObject.SetActive(m_Target.IsActive(i));

            if (jump || !m_HasTarget) Place();
            m_HasTarget = true;
        }

        void Update()
        {
            if (!m_HasTarget) return;

            var blend = 1f - Mathf.Exp(-m_Smoothing * Time.deltaTime);
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
            {
                var body = m_Bodies[i];
                if (!body.gameObject.activeSelf) continue;

                if ((body.localPosition - m_Target.Positions[i]).sqrMagnitude > m_SnapDistance * m_SnapDistance)
                {
                    body.SetLocalPositionAndRotation(m_Target.Positions[i], m_Target.Rotations[i]);
                    continue;
                }

                body.SetLocalPositionAndRotation(
                    Vector3.Lerp(body.localPosition, m_Target.Positions[i], blend),
                    Quaternion.Slerp(body.localRotation, m_Target.Rotations[i], blend));
            }
        }

        void Place()
        {
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
                m_Bodies[i].SetLocalPositionAndRotation(m_Target.Positions[i], m_Target.Rotations[i]);
        }
    }
}
