using System;
using System.Collections.Generic;

namespace VRB.Core
{
    /// <summary>One throw: its 1-based order in the game and the pins it knocked down.</summary>
    public readonly struct ThrowRecord
    {
        public int Order { get; }
        public int PinsFallen { get; }

        public ThrowRecord(int order, int pinsFallen)
        {
            Order = order;
            PinsFallen = pinsFallen;
        }
    }

    /// <summary>
    /// Throw history and scoring for one game. Input is only the fallen-pin count of each throw (counting just the pins
    /// that were standing for it), so it is independent of controllers, physics and rendering.
    /// </summary>
    public sealed class GameSession
    {
        public const int PinCount = 10;

        readonly List<ThrowRecord> m_Throws = new List<ThrowRecord>();

        public IReadOnlyList<ThrowRecord> Throws => m_Throws;
        public int ThrowCount => m_Throws.Count;

        /// <summary>Every pin knocked down so far, without strike and spare bonuses.</summary>
        public int TotalPins { get; private set; }

        /// <summary>Frames, bonuses and the pins left standing between throws.</summary>
        public Scorecard Card { get; } = new Scorecard();

        /// <summary>The game score so far; the official total once <see cref="IsComplete"/>.</summary>
        public int Score => Card.Total;

        public bool IsComplete => Card.IsComplete;

        /// <summary>Pins standing for the next throw: fewer than ten while a frame is still open.</summary>
        public int PinsStanding => Card.PinsStanding;

        public ThrowRecord RecordThrow(int pinsFallen)
        {
            // The scorecard knows how many pins can fall and rejects the rest, before anything is recorded here.
            Card.RecordThrow(pinsFallen);

            var record = new ThrowRecord(m_Throws.Count + 1, pinsFallen);
            m_Throws.Add(record);
            TotalPins += pinsFallen;
            return record;
        }

        public void Reset()
        {
            m_Throws.Clear();
            Card.Reset();
            TotalPins = 0;
        }
    }
}
