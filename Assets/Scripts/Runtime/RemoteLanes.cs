using System.Linq;
using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Shows the lanes of the other players: each one with a player on it gets its stand-in pins and ball, set to follow
    /// what that player's device reports. Lanes nobody else is on keep their still decoration.
    /// </summary>
    public class RemoteLanes : MonoBehaviour
    {
        [SerializeField] LaneSelector m_Lanes;
        [SerializeField] RemoteLane[] m_RemoteLanes = new RemoteLane[0];

        public RemoteLane Lane(int index) => m_RemoteLanes[index];

        void OnEnable()
        {
            NetPlayer.Changed += Refresh;
            NetPlayer.SnapshotReceived += OnSnapshot;
            NetPlayer.PoseReceived += OnPose;
            NetPlayer.VoiceReceived += OnVoice;
            m_Lanes.LaneChanged += OnLaneChanged;
        }

        void OnDisable()
        {
            NetPlayer.Changed -= Refresh;
            NetPlayer.SnapshotReceived -= OnSnapshot;
            NetPlayer.PoseReceived -= OnPose;
            NetPlayer.VoiceReceived -= OnVoice;
            m_Lanes.LaneChanged -= OnLaneChanged;
        }

        void Start() => Refresh();

        void OnLaneChanged(int lane) => Refresh();

        /// <summary>Show the lanes that have another player on them, hide the rest, and put each next to the played lane.</summary>
        public void Refresh()
        {
            for (var lane = 0; lane < m_RemoteLanes.Length; lane++)
            {
                var player = RemotePlayerOn(lane);
                var show = player != null;

                m_Lanes.SetRemote(lane, show);
                m_RemoteLanes[lane].SetSideways(m_Lanes.SidewaysFromPlayed(lane));
                m_RemoteLanes[lane].Show(show);
                if (show) m_RemoteLanes[lane].SetName(player.PlayerName);
                if (show && player.SnapshotTime >= 0f) m_RemoteLanes[lane].SetTarget(player.Snapshot);
                if (show && player.PoseTime >= 0f) m_RemoteLanes[lane].SetPose(player.Pose);
            }
        }

        // A voice is heard from the lane of the one speaking.
        void OnVoice(NetPlayer player, byte[] encoded)
        {
            var lane = player.Lane;
            if (lane < 0 || lane >= m_RemoteLanes.Length || !m_RemoteLanes[lane].IsShown) return;

            m_RemoteLanes[lane].Voice.Push(encoded);
        }

        void OnPose(NetPlayer player)
        {
            var lane = player.Lane;
            if (lane < 0 || lane >= m_RemoteLanes.Length || !m_RemoteLanes[lane].IsShown) return;

            m_RemoteLanes[lane].SetPose(player.Pose);
        }

        /// <summary>Draw the player of a lane as if their device had just reported this pose (used by tests).</summary>
        public void ApplyPose(int lane, AvatarSnapshot pose)
        {
            m_RemoteLanes[lane].SetPose(pose, jump: true);
        }

        void OnSnapshot(NetPlayer player)
        {
            var lane = player.Lane;
            if (lane < 0 || lane >= m_RemoteLanes.Length || !m_RemoteLanes[lane].IsShown) return;

            m_RemoteLanes[lane].SetTarget(player.Snapshot);
        }

        /// <summary>Draw a lane as if a player there had just reported this (used by tests).</summary>
        public void Apply(int lane, LaneSnapshot snapshot)
        {
            m_Lanes.SetRemote(lane, true);
            m_RemoteLanes[lane].SetSideways(m_Lanes.SidewaysFromPlayed(lane));
            m_RemoteLanes[lane].Show(true);
            m_RemoteLanes[lane].SetTarget(snapshot, jump: true);
        }

        // Somebody who is not on this device, on a lane other than the one being played.
        NetPlayer RemotePlayerOn(int lane)
        {
            if (lane == m_Lanes.Selected) return null;

            return NetPlayer.All.FirstOrDefault(p => p != null && p.IsSpawned && !p.IsLocalPlayer && p.Lane == lane);
        }
    }
}
