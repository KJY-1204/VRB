using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>
    /// Rebuilding a match from what other players report over the network: just their names and the pin counts of
    /// their throws, replayed into scorecards.
    /// </summary>
    public class MatchReplayTests
    {
        static (string Name, IReadOnlyList<int> Throws) Entry(string name, params int[] throws) => (name, throws);

        [Test]
        public void EachPlayerKeepsTheirNameAndTheirOwnScore()
        {
            var match = Match.Replay(new[] { Entry("Ann", 10, 7, 2), Entry("Bo", 3, 4) }, 0);

            Assert.That(match.Players.Select(p => p.Name), Is.EqualTo(new[] { "Ann", "Bo" }));
            Assert.That(match.Players[0].Session.Score, Is.EqualTo(28), "X = 10 + 7 + 2, then 7 and 2.");
            Assert.That(match.Players[1].Session.Score, Is.EqualTo(7));
        }

        [Test]
        public void TheReplayScoresLikeTheGameDoes()
        {
            var throws = new[] { 10, 7, 3, 9, 0, 10, 0, 8, 8, 2, 0, 6, 10, 10, 10, 8, 1 };
            var played = new GameSession();
            foreach (var pins in throws) played.RecordThrow(pins);

            var match = Match.Replay(new[] { Entry("Ann", throws) }, 0);

            Assert.That(match.Players[0].Session.Score, Is.EqualTo(played.Score));
            Assert.That(match.Players[0].Session.IsComplete, Is.True);
            Assert.That(match.IsComplete, Is.True);
        }

        [Test]
        public void TheCurrentPlayer_IsTheOneChosen()
        {
            var match = Match.Replay(new[] { Entry("Ann"), Entry("Bo"), Entry("Cy") }, 2);

            Assert.That(match.Current.Name, Is.EqualTo("Cy"));
        }

        [Test]
        public void ThePlayerWhoThrewLast_IsTheCurrentOne()
        {
            var match = Match.Replay(new[] { Entry("Ann", 5), Entry("Bo", 8) }, 1);

            Assert.That(match.LastThrower.Name, Is.EqualTo("Bo"));
        }

        [Test]
        public void WithNoThrowsYet_ThereIsNoLastThrower()
        {
            var match = Match.Replay(new[] { Entry("Ann"), Entry("Bo") }, 0);

            Assert.That(match.LastThrower, Is.Null);
        }

        [Test]
        public void Winners_AreWorkedOutFromTheReplayedScores()
        {
            var match = Match.Replay(new[] { Entry("Ann", 3, 0), Entry("Bo", 9, 0) }, 0);

            Assert.That(match.Winners.Select(p => p.Name), Is.EqualTo(new[] { "Bo" }));
        }

        [Test]
        public void ReportedThrows_ThatBreakTheRules_AreIgnoredFromThatPointOn()
        {
            // 7 leaves three standing, so a second ball of 5 is impossible; nothing after it is trusted either.
            var match = Match.Replay(new[] { Entry("Cheat", 7, 5, 10, 10) }, 0);

            Assert.That(match.Players[0].Session.ThrowCount, Is.EqualTo(1));
            Assert.That(match.Players[0].Session.Score, Is.EqualTo(7));
        }

        [Test]
        public void MoreThrowsThanAGameHolds_AreIgnored()
        {
            var match = Match.Replay(new[] { Entry("Long", Enumerable.Repeat(0, 40).ToArray()) }, 0);

            Assert.That(match.Players[0].Session.ThrowCount, Is.EqualTo(20));
            Assert.That(match.Players[0].Session.IsComplete, Is.True);
        }

        [Test]
        public void NegativeAndHugeCounts_AreIgnored()
        {
            var match = Match.Replay(new[] { Entry("A", -3), Entry("B", 99) }, 0);

            Assert.That(match.Players.All(p => p.Session.ThrowCount == 0), Is.True);
        }

        [Test]
        public void AMatchHoldsOneToFourPlayers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Match.Replay(new (string, IReadOnlyList<int>)[0], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Match.Replay(
                Enumerable.Range(0, 5).Select(i => Entry("P" + i)).ToArray(), 0));
        }

        [Test]
        public void TheCurrentIndex_MustBeAPlayer()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Match.Replay(new[] { Entry("Ann") }, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Match.Replay(new[] { Entry("Ann") }, -1));
        }

        [Test]
        public void ABlankName_FallsBackToTheSeatNumber()
        {
            var match = Match.Replay(new[] { Entry(""), Entry(null) }, 0);

            Assert.That(match.Players.Select(p => p.Name), Is.EqualTo(new[] { "P1", "P2" }));
        }

        [Test]
        public void ALongName_IsShortened_SoItFitsTheBoard()
        {
            var match = Match.Replay(new[] { Entry("ABCDEFGHIJKLMNOPQRSTUVWXYZ") }, 0);

            Assert.That(match.Players[0].Name.Length, Is.LessThanOrEqualTo(Player.MaxNameLength));
        }
    }
}
