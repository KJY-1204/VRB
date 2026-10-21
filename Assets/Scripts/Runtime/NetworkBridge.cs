using System.Linq;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// Ties the game on this device to the others in the match. It reports this player's name and throws as they happen,
    /// takes the lane the host assigns, and makes the scoreboard show everyone's scorecards. When nobody is connected it
    /// does nothing, so the game plays exactly as it does offline.
    /// </summary>
    public class NetworkBridge : MonoBehaviour
    {
        [SerializeField] GameController m_Game;
        [SerializeField] LaneSelector m_Lanes;
        [SerializeField] LevelSelectionView m_Selection;

        NetPlayer m_Local;
        GameFlow m_Flow;
        bool m_Named;

        public NetPlayer Local => m_Local;

        void Start()
        {
            m_Flow = m_Game.Flow;
            m_Flow.PhaseChanged += OnPhaseChanged;
            NetPlayer.Changed += OnPlayersChanged;
            NetPlayer.LocalReady += OnLocalReady;

            // A player that spawned before this bridge started listening.
            foreach (var player in NetPlayer.All)
                if (player.IsLocalPlayer) OnLocalReady(player);
        }

        void OnDestroy()
        {
            if (m_Flow != null) m_Flow.PhaseChanged -= OnPhaseChanged;
            NetPlayer.Changed -= OnPlayersChanged;
            NetPlayer.LocalReady -= OnLocalReady;
            if (m_Game != null) m_Game.ScoreboardMatch = null;
        }

        /// <summary>The name everyone sees until players can choose their own: their lane, "P1" to "P4".</summary>
        public static string NameForLane(int lane) => "P" + (lane + 1);

        void OnLocalReady(NetPlayer player)
        {
            m_Local = player;
            m_Named = false;

            // Everyone plays their own game, one player per device.
            if (m_Flow.Phase == GamePhase.SelectingLevel) m_Flow.SetPlayerCount(1);
            m_Selection.SetNetworked(true);
            m_Selection.SetSelectedPlayers(1);

            m_Game.ScoreboardMatch = NetPlayer.Roster;
            Publish();
            OnPlayersChanged();
        }

        void OnLocalLeft()
        {
            m_Local = null;
            m_Game.ScoreboardMatch = null;
            m_Selection.SetNetworked(false);
            m_Game.RefreshBoards();
        }

        void OnPhaseChanged(GamePhase previous, GamePhase current)
        {
            // A throw has been judged (or a new game started, which empties the list): tell the others.
            if (current == GamePhase.ShowingResult || current == GamePhase.SelectingLevel) Publish();
        }

        void Publish()
        {
            if (m_Local == null || !m_Local.IsSpawned) return;
            m_Local.PublishThrows(m_Flow.Session.Throws.Select(t => t.PinsFallen).ToList());
        }

        void OnPlayersChanged()
        {
            // The player object is taken out of the list as it despawns, before it counts as unspawned.
            if (m_Local != null && (!m_Local.IsSpawned || !NetPlayer.All.Contains(m_Local)))
            {
                OnLocalLeft();
                return;
            }

            // The host hands every player a lane of their own; slide the hall to ours and name ourselves after it.
            if (m_Local != null && m_Local.Lane >= 0)
            {
                if (m_Lanes.Selected != m_Local.Lane) m_Lanes.Select(m_Local.Lane);
                m_Selection.SetSelectedLane(m_Local.Lane);
                if (!m_Named)
                {
                    m_Named = true;
                    m_Local.SetName(NameForLane(m_Local.Lane));
                }
            }

            m_Game.RefreshBoards();
        }
    }
}
