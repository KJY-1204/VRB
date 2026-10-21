using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Tells the other devices where the pins and the ball are on this device's lane. It sends about fifteen pictures a
    /// second while anything moves, and one a second otherwise so that someone who joins later soon sees the lane too.
    /// Offline it does nothing.
    /// </summary>
    public class LaneBroadcaster : MonoBehaviour
    {
        [SerializeField] PinRack m_Rack;
        [SerializeField] Transform m_Ball;
        [SerializeField] float m_Interval = 1f / 15f;
        [SerializeField] float m_KeepAliveSeconds = 1f;

        readonly LaneSnapshot m_Current = new LaneSnapshot();
        readonly LaneSnapshot m_Sent = new LaneSnapshot();
        Pin[] m_Pins;
        float m_NextCheck;
        float m_LastSent = -100f;

        public int SentCount { get; private set; }

        void Awake()
        {
            m_Pins = m_Rack.GetComponentsInChildren<Pin>(true);
        }

        /// <summary>The lane as it is now, in the lane's own frame (this device's lane is the one at x = 0).</summary>
        public LaneSnapshot Capture()
        {
            m_Current.ActiveMask = 0;
            for (var i = 0; i < LaneSnapshot.PinCount && i < m_Pins.Length; i++)
            {
                var pin = m_Pins[i].transform;
                m_Current.Positions[i] = pin.position;
                m_Current.Rotations[i] = pin.rotation;
                if (m_Pins[i].gameObject.activeSelf) m_Current.ActiveMask |= 1u << i;
            }

            m_Current.Positions[LaneSnapshot.BallIndex] = m_Ball.position;
            m_Current.Rotations[LaneSnapshot.BallIndex] = m_Ball.rotation;
            m_Current.ActiveMask |= 1u << LaneSnapshot.BallIndex;
            return m_Current;
        }

        void Update()
        {
            if (Time.time < m_NextCheck) return;
            m_NextCheck = Time.time + m_Interval;

            var local = NetPlayer.Local;
            if (local == null) return;

            Capture();
            var due = Time.time - m_LastSent >= m_KeepAliveSeconds;
            if (!due && !m_Current.Differs(m_Sent)) return;

            local.SendSnapshot(m_Current);
            m_Sent.CopyFrom(m_Current);
            m_LastSent = Time.time;
            SentCount++;
        }
    }
}
