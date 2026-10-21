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

        [Tooltip("Head, left hand and right hand of the player on this lane (see AvatarSnapshot).")]
        [SerializeField] Transform[] m_AvatarParts = new Transform[AvatarSnapshot.PartCount];

        readonly LaneSnapshot m_Target = new LaneSnapshot();
        readonly AvatarSnapshot m_PoseTarget = new AvatarSnapshot();
        bool m_HasTarget;
        bool m_HasPose;

        /// <summary>One part of the player drawn here: <see cref="AvatarSnapshot.Head"/> and the two hands.</summary>
        public Transform AvatarPart(int part) => m_AvatarParts[part];

        public bool IsAvatarShown => m_HasPose;

        /// <summary>Follow this pose of the player's head and hands. The first one (or <paramref name="jump"/>) is taken at once.</summary>
        public void SetPose(AvatarSnapshot pose, bool jump = false)
        {
            m_PoseTarget.CopyFrom(pose);
            var first = !m_HasPose;
            m_HasPose = true;
            for (var i = 0; i < AvatarSnapshot.PartCount; i++)
            {
                m_AvatarParts[i].gameObject.SetActive(true);
                if (jump || first) m_AvatarParts[i].SetLocalPositionAndRotation(m_PoseTarget.Positions[i], m_PoseTarget.Rotations[i]);
            }
        }

        void HideAvatar()
        {
            m_HasPose = false;
            foreach (var part in m_AvatarParts) part.gameObject.SetActive(false);
        }

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
            if (!show)
            {
                m_HasTarget = false;
                HideAvatar();
            }
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
            var blend = 1f - Mathf.Exp(-m_Smoothing * Time.deltaTime);
            if (m_HasPose)
            {
                for (var i = 0; i < AvatarSnapshot.PartCount; i++)
                {
                    var part = m_AvatarParts[i];
                    part.SetLocalPositionAndRotation(
                        Vector3.Lerp(part.localPosition, m_PoseTarget.Positions[i], blend),
                        Quaternion.Slerp(part.localRotation, m_PoseTarget.Rotations[i], blend));
                }
            }

            if (!m_HasTarget) return;

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
