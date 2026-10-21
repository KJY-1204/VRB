using System;
using System.Collections.Generic;
using System.Linq;

namespace VRB.Core
{
    /// <summary>One person in a match: a name and a game of their own.</summary>
    public sealed class Player
    {
        public int Number { get; }
        public string Name { get; }
        public GameSession Session { get; } = new GameSession();

        public Player(int number)
        {
            Number = number;
            Name = "P" + number;
        }
    }

    /// <summary>
    /// A game for one to four players, each with their own scorecard. Players take turns frame by frame: one frame (a
    /// strike, or two balls), then the next player, and so on; in the tenth frame a player bowls all their balls,
    /// bonus balls included, before the next one starts theirs. Only the number of pins each throw knocked down goes in,
    /// so it is independent of controllers, physics, rendering and the network.
    /// </summary>
    public sealed class Match
    {
        public const int MaxPlayers = 4;

        readonly List<Player> m_Players = new List<Player>();
        int m_Current;

        public Match(int playerCount = 1)
        {
            if (playerCount < 1 || playerCount > MaxPlayers)
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount,
                    $"A match has between 1 and {MaxPlayers} players.");

            for (var i = 1; i <= playerCount; i++) m_Players.Add(new Player(i));
        }

        public IReadOnlyList<Player> Players => m_Players;
        public int PlayerCount => m_Players.Count;
        public bool HasMultiplePlayers => m_Players.Count > 1;

        /// <summary>The player whose turn it is. It stays with the player who just threw until <see cref="AdvanceTurn"/>.</summary>
        public Player Current => m_Players[m_Current];

        /// <summary>Whoever threw last, or null before the first throw; the turn may already have moved on from them.</summary>
        public Player LastThrower { get; private set; }

        public bool IsComplete => m_Players.All(p => p.Session.IsComplete);

        /// <summary>The player (or players, on a tie) with the highest score so far.</summary>
        public IReadOnlyList<Player> Winners
        {
            get
            {
                var best = m_Players.Max(p => p.Session.Score);
                return m_Players.Where(p => p.Session.Score == best).ToList();
            }
        }

        /// <summary>Everyone from highest score to lowest; players tied on score keep their seating order.</summary>
        public IReadOnlyList<Player> Ranking => m_Players.OrderByDescending(p => p.Session.Score).ToList();

        /// <summary>Records what the current player's throw knocked down. The turn does not move yet.</summary>
        public ThrowRecord RecordThrow(int pinsFallen)
        {
            if (IsComplete) throw new InvalidOperationException("The match is already complete.");

            var record = Current.Session.RecordThrow(pinsFallen);
            LastThrower = Current;
            return record;
        }

        /// <summary>
        /// Hands the turn to the next player who still has frames to bowl, if the current player's turn is over (a
        /// frame finished, or their game complete). Returns true when the turn moved to someone else.
        /// </summary>
        public bool AdvanceTurn()
        {
            if (IsComplete || !TurnIsOver(Current)) return false;

            for (var step = 1; step <= m_Players.Count; step++)
            {
                var next = (m_Current + step) % m_Players.Count;
                if (m_Players[next].Session.IsComplete) continue;

                var changed = next != m_Current;
                m_Current = next;
                return changed;
            }

            return false;
        }

        /// <summary>Clears every scorecard and gives the first player the turn; the players stay.</summary>
        public void Reset()
        {
            foreach (var player in m_Players) player.Session.Reset();
            m_Current = 0;
            LastThrower = null;
        }

        // A turn is over once the player starts a new frame (their next ball is the first of one) or has finished.
        static bool TurnIsOver(Player player) =>
            player.Session.IsComplete || player.Session.Card.NextThrowInFrame == 1;
    }
}
