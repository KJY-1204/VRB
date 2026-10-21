using System;
using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Chooses which of the hall's lanes the game is played on. The game itself always stays on the lane at x = 0, so
    /// the hall is slid sideways until the chosen lane is under it. The decorative pins of that lane are hidden (the game
    /// has its own), the others are shown, and only the chair belonging to that lane can be sat on.
    /// </summary>
    public class LaneSelector : MonoBehaviour
    {
        [SerializeField] Transform m_Hall;
        [SerializeField] Vector3 m_BasePosition;
        [Tooltip("Sideways distance of each lane from the first, in meters (+X).")]
        [SerializeField] float[] m_LaneOffsets = { 0f };
        [SerializeField] GameObject[] m_DecorPins;
        [SerializeField] SeatSpot[] m_Seats;

        public event Action<int> LaneChanged;

        readonly System.Collections.Generic.HashSet<int> m_RemoteLanes = new System.Collections.Generic.HashSet<int>();

        public int Selected { get; private set; }
        public int LaneCount => m_LaneOffsets.Length;

        /// <summary>Sideways distance (meters, +X) of a lane from the first lane of the hall.</summary>
        public float OffsetOf(int lane) => m_LaneOffsets[Mathf.Clamp(lane, 0, LaneCount - 1)];

        /// <summary>
        /// Where a lane is relative to the lane being played, which is always at x = 0 (the hall slides, the game does not).
        /// </summary>
        public float SidewaysFromPlayed(int lane) => OffsetOf(lane) - OffsetOf(Selected);

        /// <summary>
        /// Marks a lane as one another player is bowling on, drawn live by <see cref="RemoteLanes"/>; its still decoration
        /// pins are hidden meanwhile.
        /// </summary>
        public void SetRemote(int lane, bool remote)
        {
            var changed = remote ? m_RemoteLanes.Add(lane) : m_RemoteLanes.Remove(lane);
            if (changed) ApplyDecorations();
        }

        void Awake()
        {
            Apply();
        }

        /// <summary>Slide the hall so the given lane (0-based) is the one being played.</summary>
        public void Select(int lane)
        {
            lane = Mathf.Clamp(lane, 0, LaneCount - 1);
            var changed = lane != Selected;
            Selected = lane;
            Apply();
            if (changed) LaneChanged?.Invoke(lane);
        }

        void Apply()
        {
            // Never leave the player on a chair that is about to move away.
            foreach (var seat in m_Seats)
                if (seat != null) seat.Stand();

            m_Hall.localPosition = m_BasePosition - new Vector3(m_LaneOffsets[Selected], 0f, 0f);

            ApplyDecorations();

            for (var i = 0; i < m_Seats.Length; i++)
                if (m_Seats[i] != null) m_Seats[i].gameObject.SetActive(i == Selected);
        }

        // The lane being played has the game's own pins, and lanes with another player have theirs drawn live.
        void ApplyDecorations()
        {
            for (var i = 0; i < m_DecorPins.Length; i++)
                if (m_DecorPins[i] != null) m_DecorPins[i].SetActive(i != Selected && !m_RemoteLanes.Contains(i));
        }
    }
}
