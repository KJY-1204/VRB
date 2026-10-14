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
    /// Throw history and totals for one game. Input is only the fallen-pin count of each throw,
    /// so it is independent of controllers, physics and rendering.
    /// </summary>
    public sealed class GameSession
    {
        public const int PinCount = 10;

        readonly List<ThrowRecord> m_Throws = new List<ThrowRecord>();

        public IReadOnlyList<ThrowRecord> Throws => m_Throws;
        public int ThrowCount => m_Throws.Count;
        public int TotalPins { get; private set; }

        public ThrowRecord RecordThrow(int pinsFallen)
        {
            if (pinsFallen < 0 || pinsFallen > PinCount)
                throw new ArgumentOutOfRangeException(nameof(pinsFallen), pinsFallen,
                    $"Fallen pin count must be between 0 and {PinCount}.");

            var record = new ThrowRecord(m_Throws.Count + 1, pinsFallen);
            m_Throws.Add(record);
            TotalPins += pinsFallen;
            return record;
        }

        public void Reset()
        {
            m_Throws.Clear();
            TotalPins = 0;
        }
    }
}
