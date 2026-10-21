using System;
using System.Linq;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class ScorecardTests
    {
        static Scorecard Play(params int[] throws)
        {
            var card = new Scorecard();
            foreach (var pins in throws) card.RecordThrow(pins);
            return card;
        }

        static int[] Repeat(int value, int count) => Enumerable.Repeat(value, count).ToArray();

        // ------------------------------------------------------------------ whole games

        [Test]
        public void GutterGame_ScoresZero()
        {
            var card = Play(Repeat(0, 20));

            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(0));
        }

        [Test]
        public void PerfectGame_ScoresThreeHundred()
        {
            var card = Play(Repeat(10, 12));

            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(300));
            Assert.That(card.Frames[9].CumulativeScore, Is.EqualTo(300));
        }

        [Test]
        public void NinesAndMisses_ScoreNinety()
        {
            var card = Play(Enumerable.Repeat(new[] { 9, 0 }, 10).SelectMany(x => x).ToArray());

            Assert.That(card.Total, Is.EqualTo(90));
        }

        [Test]
        public void AllFivesSpares_ScoreOneFifty()
        {
            var card = Play(Repeat(5, 21));

            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(150));
        }

        [Test]
        public void AMixedGame_ScoresTheKnownTotal()
        {
            // X 7/ 9- X -8 8/ -6 X X X81
            var card = Play(10, 7, 3, 9, 0, 10, 0, 8, 8, 2, 0, 6, 10, 10, 10, 8, 1);

            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(167));
            var running = card.Frames.Select(f => f.CumulativeScore).ToArray();
            Assert.That(running, Is.EqualTo(new int?[] { 20, 39, 48, 66, 74, 84, 90, 120, 148, 167 }));
        }

        // ------------------------------------------------------------------ bonuses that are still pending

        [Test]
        public void AStrike_IsNotFinalUntilTwoMoreThrowsAreIn()
        {
            var card = Play(10);

            Assert.That(card.Frames[0].CumulativeScore, Is.Null);
            card.RecordThrow(3);
            Assert.That(card.Frames[0].CumulativeScore, Is.Null);
            card.RecordThrow(4);
            Assert.That(card.Frames[0].CumulativeScore, Is.EqualTo(17));
        }

        [Test]
        public void ASpare_IsNotFinalUntilOneMoreThrowIsIn()
        {
            var card = Play(6, 4);

            Assert.That(card.Frames[0].CumulativeScore, Is.Null);
            card.RecordThrow(5);
            Assert.That(card.Frames[0].CumulativeScore, Is.EqualTo(15));
        }

        [Test]
        public void TheTotal_CountsTheBonusesThatAreAlreadyKnown()
        {
            var card = Play(10);
            Assert.That(card.Total, Is.EqualTo(10));

            card.RecordThrow(3);
            Assert.That(card.Total, Is.EqualTo(16), "10 + 3 for the strike, plus the 3 itself.");

            card.RecordThrow(4);
            Assert.That(card.Total, Is.EqualTo(24), "17 for the strike frame, plus 3 and 4 in the open frame.");
        }

        [Test]
        public void AnOpenFrame_IsFinalAtOnce()
        {
            var card = Play(3, 4);

            Assert.That(card.Frames[0].CumulativeScore, Is.EqualTo(7));
        }

        // ------------------------------------------------------------------ pins left standing between throws

        [Test]
        public void TheFirstThrowOfAFrame_FacesTenPins()
        {
            var card = new Scorecard();

            Assert.That(card.PinsStanding, Is.EqualTo(10));
            Assert.That(card.CurrentFrame, Is.EqualTo(1));
            Assert.That(card.NextThrowInFrame, Is.EqualTo(1));
        }

        [Test]
        public void TheSecondThrow_FacesWhatTheFirstLeft()
        {
            var card = Play(7);

            Assert.That(card.PinsStanding, Is.EqualTo(3));
            Assert.That(card.CurrentFrame, Is.EqualTo(1));
            Assert.That(card.NextThrowInFrame, Is.EqualTo(2));
        }

        [Test]
        public void AfterAStrike_TheNextFrameStartsWithAFreshRack()
        {
            var card = Play(10);

            Assert.That(card.PinsStanding, Is.EqualTo(10));
            Assert.That(card.CurrentFrame, Is.EqualTo(2));
            Assert.That(card.NextThrowInFrame, Is.EqualTo(1));
        }

        [Test]
        public void AfterTwoThrows_TheNextFrameStartsWithAFreshRack()
        {
            var card = Play(3, 4);

            Assert.That(card.PinsStanding, Is.EqualTo(10));
            Assert.That(card.CurrentFrame, Is.EqualTo(2));
        }

        [Test]
        public void ThrowingMorePinsThanAreStanding_IsRejected()
        {
            var card = Play(7);

            Assert.Throws<ArgumentOutOfRangeException>(() => card.RecordThrow(4));
            Assert.That(card.Throws.Count, Is.EqualTo(1), "A rejected throw is not recorded.");
        }

        [Test]
        public void ANegativeThrow_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Scorecard().RecordThrow(-1));
        }

        // ------------------------------------------------------------------ the tenth frame

        [Test]
        public void TenthFrame_OpenEndsAfterTwoThrows()
        {
            var card = Play(Concat(Repeat(0, 18), 3, 4));

            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(7));
            Assert.That(card.PinsStanding, Is.EqualTo(0));
            Assert.Throws<InvalidOperationException>(() => card.RecordThrow(1));
        }

        [Test]
        public void TenthFrame_ASpareEarnsOneFillBallOnAFreshRack()
        {
            var card = Play(Concat(Repeat(0, 18), 6, 4));

            Assert.That(card.IsComplete, Is.False);
            Assert.That(card.CurrentFrame, Is.EqualTo(10));
            Assert.That(card.NextThrowInFrame, Is.EqualTo(3));
            Assert.That(card.PinsStanding, Is.EqualTo(10));

            card.RecordThrow(7);
            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(17));
        }

        [Test]
        public void TenthFrame_AStrikeEarnsTwoFillBalls_TheSecondOnTheRemainder()
        {
            var card = Play(Concat(Repeat(0, 18), 10, 6));

            Assert.That(card.IsComplete, Is.False);
            Assert.That(card.PinsStanding, Is.EqualTo(4));

            card.RecordThrow(4);
            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(20));
        }

        [Test]
        public void TenthFrame_TwoStrikesRackTheThirdBallFresh()
        {
            var card = Play(Concat(Repeat(0, 18), 10, 10));

            Assert.That(card.PinsStanding, Is.EqualTo(10));
            card.RecordThrow(9);
            Assert.That(card.IsComplete, Is.True);
            Assert.That(card.Total, Is.EqualTo(29));
        }

        [Test]
        public void TenthFrame_AfterAnOpenSecondThrow_NoThirdBall()
        {
            var card = Play(Concat(Repeat(0, 18), 5, 3));

            Assert.That(card.IsComplete, Is.True);
        }

        // ------------------------------------------------------------------ what the scoreboard shows

        [Test]
        public void Marks_ShowStrikesSparesAndMisses()
        {
            var card = Play(10, 7, 3, 0, 9, 4, 0);

            Assert.That(card.Frames[0].Marks, Is.EqualTo(new[] { "X" }));
            Assert.That(card.Frames[1].Marks, Is.EqualTo(new[] { "7", "/" }));
            Assert.That(card.Frames[2].Marks, Is.EqualTo(new[] { "-", "9" }));
            Assert.That(card.Frames[3].Marks, Is.EqualTo(new[] { "4", "-" }));
        }

        [Test]
        public void Marks_AMissFollowedByTenIsASpare_NotAStrike()
        {
            var card = Play(0, 10, 5);

            Assert.That(card.Frames[0].Marks, Is.EqualTo(new[] { "-", "/" }));
            Assert.That(card.Frames[0].CumulativeScore, Is.EqualTo(15));
        }

        [Test]
        public void Marks_TenthFrameShowsEachBall()
        {
            var card = Play(Concat(Repeat(0, 18), 10, 6, 4));
            Assert.That(card.Frames[9].Marks, Is.EqualTo(new[] { "X", "6", "/" }));

            card = Play(Concat(Repeat(0, 18), 6, 4, 10));
            Assert.That(card.Frames[9].Marks, Is.EqualTo(new[] { "6", "/", "X" }));

            card = Play(Concat(Repeat(0, 18), 10, 10, 10));
            Assert.That(card.Frames[9].Marks, Is.EqualTo(new[] { "X", "X", "X" }));
        }

        [Test]
        public void AlwaysTenFrames_EvenBeforeTheyAreBowled()
        {
            var card = Play(5);

            Assert.That(card.Frames.Count, Is.EqualTo(10));
            Assert.That(card.Frames[0].Throws.Count, Is.EqualTo(1));
            Assert.That(card.Frames[1].Throws.Count, Is.EqualTo(0));
            Assert.That(card.Frames[1].IsComplete, Is.False);
            Assert.That(card.Frames[0].Number, Is.EqualTo(1));
            Assert.That(card.Frames[9].Number, Is.EqualTo(10));
        }

        [Test]
        public void Reset_StartsANewGame()
        {
            var card = Play(10, 10, 10);

            card.Reset();

            Assert.That(card.Throws.Count, Is.EqualTo(0));
            Assert.That(card.Total, Is.EqualTo(0));
            Assert.That(card.CurrentFrame, Is.EqualTo(1));
            Assert.That(card.IsComplete, Is.False);
        }

        static int[] Concat(int[] head, params int[] tail) => head.Concat(tail).ToArray();
    }
}
