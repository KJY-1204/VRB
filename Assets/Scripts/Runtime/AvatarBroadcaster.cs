using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Tells the other devices where this player's head and hands are: about fifteen times a second while they move,
    /// and once a second otherwise. Offline it does nothing.
    /// </summary>
    public class AvatarBroadcaster : MonoBehaviour
    {
        [SerializeField] Transform m_Head;
        [SerializeField] Transform m_LeftHand;
        [SerializeField] Transform m_RightHand;
        [SerializeField] float m_Interval = 1f / 15f;
        [SerializeField] float m_KeepAliveSeconds = 1f;

        readonly AvatarSnapshot m_Current = new AvatarSnapshot();
        readonly AvatarSnapshot m_Sent = new AvatarSnapshot();
        float m_NextCheck;
        float m_LastSent = -100f;

        public int SentCount { get; private set; }

        /// <summary>The player as they are now, in the lane's own frame (this device's lane is the one at x = 0).</summary>
        public AvatarSnapshot Capture()
        {
            Put(AvatarSnapshot.Head, m_Head);
            Put(AvatarSnapshot.LeftHand, m_LeftHand);
            Put(AvatarSnapshot.RightHand, m_RightHand);
            return m_Current;
        }

        void Put(int part, Transform source) =>
            source.GetPositionAndRotation(out m_Current.Positions[part], out m_Current.Rotations[part]);

        void Update()
        {
            if (Time.time < m_NextCheck) return;
            m_NextCheck = Time.time + m_Interval;

            var local = NetPlayer.Local;
            if (local == null) return;

            Capture();
            var due = Time.time - m_LastSent >= m_KeepAliveSeconds;
            if (!due && !m_Current.Differs(m_Sent)) return;

            local.SendPose(m_Current);
            m_Sent.CopyFrom(m_Current);
            m_LastSent = Time.time;
            SentCount++;
        }
    }
}
