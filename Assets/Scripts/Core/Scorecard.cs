using System;
using System.Collections.Generic;

namespace VRB.Core
{
    /// <summary>One frame of a game: what was thrown in it and how it reads on a scoreboard.</summary>
    public sealed class FrameScore
    {
        public int Number { get; }

        /// <summary>Pins knocked down by each throw of this frame, in order.</summary>
        public IReadOnlyList<int> Throws { get; }

        /// <summary>The symbol for each throw: X for a strike, / for a spare, - for a miss, otherwise the pin count.</summary>
        public IReadOnlyList<string> Marks { get; }

        public bool IsComplete { get; }

        /// <summary>
        /// The game score up to and including this frame, or null while a strike or spare in it is still waiting for
        /// its bonus throws.
        /// </summary>
        public int? CumulativeScore { get; }

        public FrameScore(int number, IReadOnlyList<int> throws, IReadOnlyList<string> marks, bool isComplete,
            int? cumulativeScore)
        {
            Number = number;
            Throws = throws;
            Marks = marks;
            IsComplete = isComplete;
            CumulativeScore = cumulativeScore;
        }
    }

    /// <summary>
    /// Ten-pin scoring. The only input is how many pins each throw knocked down; everything else (frames, bonuses,
    /// how many pins stand for the next throw, when the game ends) follows from the rules, so it is independent of
    /// controllers, physics and rendering.
    /// </summary>
    public sealed class Scorecard
    {
        public const int FrameCount = 10;
        public const int PinCount = 10;

        readonly List<int> m_Throws = new List<int>();
        FrameScore[] m_Frames;
        int m_Total;
        int m_PinsStanding = PinCount;
        int m_CurrentFrame = 1;
        int m_NextThrowInFrame = 1;
        bool m_IsComplete;

        public Scorecard()
        {
            Rebuild();
        }

        public IReadOnlyList<int> Throws => m_Throws;

        /// <summary>Always ten entries; frames not bowled yet have no throws.</summary>
        public IReadOnlyList<FrameScore> Frames => m_Frames;

        /// <summary>
        /// The score so far: every pin, plus the strike and spare bonuses that can already be counted. Once the game is
        /// complete this is the official total.
        /// </summary>
        public int Total => m_Total;

        public bool IsComplete => m_IsComplete;

        /// <summary>The scoreboard symbol of the latest throw (X, /, - or the pin count); empty before the first one.</summary>
        public string LastMark
        {
            get
            {
                for (var frame = FrameCount - 1; frame >= 0; frame--)
                {
                    var marks = m_Frames[frame].Marks;
                    if (marks.Count > 0) return marks[marks.Count - 1];
                }
                return string.Empty;
            }
        }

        /// <summary>The frame the next throw belongs to, 1 to 10 (stays 10 once the game is complete).</summary>
        public int CurrentFrame => m_CurrentFrame;

        /// <summary>Which throw of <see cref="CurrentFrame"/> comes next, 1 to 3.</summary>
        public int NextThrowInFrame => m_NextThrowInFrame;

        /// <summary>
        /// Pins standing for the next throw: ten on a fresh rack, fewer when the previous throw of the frame left some.
        /// Zero once the game is complete.
        /// </summary>
        public int PinsStanding => m_PinsStanding;

        public void RecordThrow(int pinsDown)
        {
            if (m_IsComplete)
                throw new InvalidOperationException("The game is already complete.");
            if (pinsDown < 0 || pinsDown > m_PinsStanding)
                throw new ArgumentOutOfRangeException(nameof(pinsDown), pinsDown,
                    $"A throw knocks down between 0 and {m_PinsStanding} pins here.");

            m_Throws.Add(pinsDown);
            Rebuild();
        }

        public void Reset()
        {
            m_Throws.Clear();
            Rebuild();
        }

        // Everything is derived from the list of throws, which keeps the rules in one place and leaves no state to drift.
        void Rebuild()
        {
            var frames = new FrameScore[FrameCount];
            var firstThrowOfFrame = new int[FrameCount];
            var kind = new FrameKind[FrameCount];
            var frameThrows = new List<int>[FrameCount];

            var index = 0;
            var current = -1;
            var pinsStanding = PinCount;
            var nextThrowInFrame = 1;
            var complete = false;

            for (var frame = 0; frame < FrameCount; frame++)
            {
                firstThrowOfFrame[frame] = index;
                frameThrows[frame] = new List<int>();
                var throws = frameThrows[frame];
                var isLast = frame == FrameCount - 1;

                // Take the throws that belong to this frame, as many as the rules allow and as many as exist.
                while (index < m_Throws.Count && !IsFrameDone(throws, isLast))
                    throws.Add(m_Throws[index++]);

                var done = IsFrameDone(throws, isLast);
                kind[frame] = ClassifyFrame(throws, isLast);

                if (!done && current < 0)
                {
                    current = frame;
                    pinsStanding = StandingBefore(throws, isLast);
                    nextThrowInFrame = throws.Count + 1;
                }
            }

            if (current < 0)
            {
                complete = true;
                current = FrameCount - 1;
                pinsStanding = 0;
                nextThrowInFrame = frameThrows[FrameCount - 1].Count + 1;
            }

            // Scores: a frame's points are its pins plus any bonus throws that exist so far.
            var running = 0;
            var resolvedSoFar = true;
            for (var frame = 0; frame < FrameCount; frame++)
            {
                var throws = frameThrows[frame];
                var points = Sum(throws);
                var isLast = frame == FrameCount - 1;
                var resolved = IsFrameDone(throws, isLast);

                if (!isLast && throws.Count > 0)
                {
                    var after = firstThrowOfFrame[frame] + throws.Count;
                    var bonusThrows = kind[frame] == FrameKind.Strike ? 2 : kind[frame] == FrameKind.Spare ? 1 : 0;
                    for (var b = 0; b < bonusThrows; b++)
                    {
                        if (after + b < m_Throws.Count) points += m_Throws[after + b];
                        else resolved = false;
                    }
                }

                running += points;
                resolvedSoFar &= resolved;
                frames[frame] = new FrameScore(frame + 1, throws.ToArray(), MarksFor(throws, isLast),
                    IsFrameDone(throws, isLast), resolvedSoFar && throws.Count > 0 ? running : (int?)null);
            }

            m_Frames = frames;
            m_Total = running;
            m_IsComplete = complete;
            m_CurrentFrame = current + 1;
            m_NextThrowInFrame = nextThrowInFrame;
            m_PinsStanding = pinsStanding;
        }

        enum FrameKind { Open, Strike, Spare }

        static bool IsFrameDone(List<int> throws, bool isLast)
        {
            if (!isLast)
                return throws.Count == 2 || (throws.Count == 1 && throws[0] == PinCount);

            if (throws.Count < 2) return false;
            return throws.Count == 3 || throws[0] + throws[1] < PinCount;
        }

        static FrameKind ClassifyFrame(List<int> throws, bool isLast)
        {
            if (throws.Count == 0) return FrameKind.Open;
            if (throws[0] == PinCount) return FrameKind.Strike;
            if (throws.Count >= 2 && throws[0] + throws[1] == PinCount) return FrameKind.Spare;
            return FrameKind.Open;
        }

        /// <summary>Pins standing before the next throw of a frame that is not finished yet.</summary>
        static int StandingBefore(List<int> throws, bool isLast)
        {
            if (throws.Count == 0) return PinCount;

            if (!isLast) return PinCount - throws[0];

            // Tenth frame: a strike or a spare puts a fresh rack up for the next throw.
            if (throws.Count == 1)
                return throws[0] == PinCount ? PinCount : PinCount - throws[0];

            // Two throws and still going: either X then something, or a spare.
            if (throws[0] == PinCount)
                return throws[1] == PinCount ? PinCount : PinCount - throws[1];
            return PinCount;
        }

        static IReadOnlyList<string> MarksFor(List<int> throws, bool isLast)
        {
            var marks = new List<string>();
            var standing = PinCount;
            var freshRack = true;
            foreach (var pins in throws)
            {
                if (pins == PinCount && freshRack) marks.Add("X");
                else if (pins == standing && !freshRack) marks.Add("/");
                else marks.Add(pins == 0 ? "-" : pins.ToString());

                // In the tenth frame clearing the rack puts up a fresh one; elsewhere the frame is over by then.
                var cleared = pins == standing;
                freshRack = cleared && isLast;
                standing = cleared ? PinCount : standing - pins;
            }
            return marks;
        }

        static int Sum(List<int> throws)
        {
            var sum = 0;
            foreach (var pins in throws) sum += pins;
            return sum;
        }
    }
}
