using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// One connected player, as everybody sees them. Each player plays their own game on their own lane and only reports
    /// to the others: their name, the lane they were given, and the pin count of every throw (from which anyone can
    /// work out the scorecard, see <see cref="Match.Replay"/>). The owner writes the name and the throws; the host
    /// hands out the lane.
    /// </summary>
    public class NetPlayer : NetworkBehaviour
    {
        /// <summary>Everyone currently in the match, in no particular order; see <see cref="Roster"/>.</summary>
        public static readonly List<NetPlayer> All = new List<NetPlayer>();

        /// <summary>Raised when any player joins, leaves or changes their name, lane or throws.</summary>
        public static event Action Changed;

        /// <summary>Raised when the player object that belongs to this device exists and is ready to use.</summary>
        public static event Action<NetPlayer> LocalReady;

        /// <summary>Raised when a new picture of another player's lane has arrived.</summary>
        public static event Action<NetPlayer> SnapshotReceived;

        /// <summary>Raised when a new pose of another player's head and hands has arrived.</summary>
        public static event Action<NetPlayer> PoseReceived;

        readonly NetworkVariable<FixedString32Bytes> m_Name = new NetworkVariable<FixedString32Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<int> m_Lane = new NetworkVariable<int>(-1);

        NetworkList<int> m_Throws;

        public string PlayerName => m_Name.Value.ToString();

        /// <summary>The lane the host gave this player (0-based), or -1 until it has.</summary>
        public int Lane => m_Lane.Value;

        public IReadOnlyList<int> Throws
        {
            get
            {
                var list = new List<int>(m_Throws.Count);
                for (var i = 0; i < m_Throws.Count; i++) list.Add(m_Throws[i]);
                return list;
            }
        }

        public new bool IsLocalPlayer => IsOwner;

        /// <summary>The player object of this device, or null when not in a network game.</summary>
        public static NetPlayer Local { get; private set; }

        /// <summary>The latest picture of this player's lane (pins and ball), as received from their device.</summary>
        public LaneSnapshot Snapshot { get; } = new LaneSnapshot();

        /// <summary>When (<see cref="Time.time"/>) the last picture arrived; negative before the first.</summary>
        public float SnapshotTime { get; private set; } = -1f;

        /// <summary>Tell the other devices where the pins and the ball are on this player's lane (the owner only).</summary>
        public void SendSnapshot(LaneSnapshot snapshot)
        {
            if (!IsOwner || !IsSpawned) return;
            SnapshotRpc(snapshot.Positions, snapshot.Rotations, snapshot.ActiveMask);
        }

        // A lost picture does not matter, the next one is only a moment away, so these go unreliably (and cheaply).
        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        void SnapshotRpc(Vector3[] positions, Quaternion[] rotations, uint activeMask)
        {
            if (!Snapshot.TrySet(positions, rotations, activeMask)) return;

            SnapshotTime = Time.time;
            SnapshotReceived?.Invoke(this);
        }

        /// <summary>The latest pose of this player's head and hands, as received from their device.</summary>
        public AvatarSnapshot Pose { get; } = new AvatarSnapshot();

        /// <summary>When (<see cref="Time.time"/>) the last pose arrived; negative before the first.</summary>
        public float PoseTime { get; private set; } = -1f;

        /// <summary>Tell the other devices where this player's head and hands are (the owner only).</summary>
        public void SendPose(AvatarSnapshot pose)
        {
            if (!IsOwner || !IsSpawned) return;
            PoseRpc(pose.Positions, pose.Rotations);
        }

        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        void PoseRpc(Vector3[] positions, Quaternion[] rotations)
        {
            if (!Pose.TrySet(positions, rotations)) return;

            PoseTime = Time.time;
            PoseReceived?.Invoke(this);
        }

        /// <summary>Raised when a chunk of another player's coded voice has arrived.</summary>
        public static event Action<NetPlayer, byte[]> VoiceReceived;

        /// <summary>Send a chunk of coded speech to the other players (the owner only).</summary>
        public void SendVoice(byte[] encoded)
        {
            if (!IsOwner || !IsSpawned) return;
            VoiceRpc(encoded);
        }

        // Late or lost speech is of no use, so it goes unreliably like the pictures.
        [Rpc(SendTo.NotOwner, Delivery = RpcDelivery.Unreliable)]
        void VoiceRpc(byte[] encoded) => VoiceReceived?.Invoke(this, encoded);

        void Awake()
        {
            // A NetworkList has to exist before the object spawns.
            m_Throws = new NetworkList<int>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            m_Name.OnValueChanged += OnAnyChange;
            m_Lane.OnValueChanged += OnAnyChange;
            m_Throws.OnListChanged += OnListChange;
            All.Add(this);
            if (IsOwner) Local = this;

            if (IsServer) m_Lane.Value = FreeLane();

            Changed?.Invoke();
            if (IsOwner) LocalReady?.Invoke(this);
        }

        public override void OnNetworkDespawn()
        {
            m_Name.OnValueChanged -= OnAnyChange;
            m_Lane.OnValueChanged -= OnAnyChange;
            m_Throws.OnListChanged -= OnListChange;
            All.Remove(this);
            if (Local == this) Local = null;
            Changed?.Invoke();
        }

        void OnAnyChange(FixedString32Bytes previous, FixedString32Bytes current) => Changed?.Invoke();
        void OnAnyChange(int previous, int current) => Changed?.Invoke();
        void OnListChange(NetworkListEvent<int> change) => Changed?.Invoke();

        /// <summary>The lowest lane nobody has, so every player bowls on a lane of their own.</summary>
        int FreeLane()
        {
            var taken = new HashSet<int>(All.Where(p => p != this).Select(p => p.Lane));
            var lane = 0;
            while (taken.Contains(lane)) lane++;
            return lane;
        }

        /// <summary>Set the name everyone sees (the owner only).</summary>
        public void SetName(string playerName)
        {
            if (!IsOwner) return;
            m_Name.Value = new FixedString32Bytes(playerName ?? string.Empty);
        }

        /// <summary>
        /// Report the pin counts of this player's throws so far (the owner only). Throws are only ever added to, except
        /// for a new game, which starts the list again.
        /// </summary>
        public void PublishThrows(IReadOnlyList<int> throws)
        {
            if (!IsOwner) return;

            if (m_Throws.Count > throws.Count) m_Throws.Clear();
            for (var i = 0; i < m_Throws.Count; i++)
            {
                if (m_Throws[i] == throws[i]) continue;
                m_Throws.Clear();
                break;
            }

            for (var i = m_Throws.Count; i < throws.Count; i++) m_Throws.Add(throws[i]);
        }

        /// <summary>
        /// Everyone's scorecards as one match, lane by lane, for the scoreboard. The local player is the one marked as up.
        /// </summary>
        public static Match Roster()
        {
            var players = All.Where(p => p != null && p.IsSpawned)
                .OrderBy(p => p.Lane < 0 ? int.MaxValue : p.Lane)
                .ThenBy(p => p.OwnerClientId)
                .Take(Match.MaxPlayers)
                .ToList();
            if (players.Count == 0) return null;

            var entries = players
                .Select(p => (Name: p.PlayerName, Throws: p.Throws))
                .ToList();
            var local = Math.Max(0, players.FindIndex(p => p.IsOwner));
            return Match.Replay(entries, local);
        }
    }
}
