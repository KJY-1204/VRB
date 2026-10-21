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
        [SerializeField] string m_LocalName = "Player";

        NetPlayer m_Local;
        GameFlow m_Flow;

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

        void OnLocalReady(NetPlayer player)
        {
            m_Local = player;
            player.SetName(m_LocalName);
            m_Game.ScoreboardMatch = NetPlayer.Roster;
            Publish();
            OnPlayersChanged();
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
            // The host hands every player a lane of their own; slide the hall to ours.
            if (m_Local != null && m_Local.IsSpawned && m_Local.Lane >= 0 && m_Lanes.Selected != m_Local.Lane)
                m_Lanes.Select(m_Local.Lane);

            m_Game.RefreshBoards();
        }
    }
}
