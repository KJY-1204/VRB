using System;
using System.Linq;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class MatchTests
    {
        /// <summary>Records a throw and ends the turn if it is over, like the game does once the result was shown.</summary>
        static void Throw(Match match, int pins)
        {
            match.RecordThrow(pins);
            match.AdvanceTurn();
        }

        static void ThrowAll(Match match, params int[] throws)
        {
            foreach (var pins in throws) Throw(match, pins);
        }

        // ------------------------------------------------------------------ setting up

        [Test]
        public void ASinglePlayerMatch_IsTheOrdinaryGame()
        {
            var match = new Match();

            Assert.That(match.PlayerCount, Is.EqualTo(1));
            Assert.That(match.HasMultiplePlayers, Is.False);
            Assert.That(match.Current, Is.SameAs(match.Players[0]));
        }

        [Test]
        public void PlayersAreNamedInOrder_AndEachHasItsOwnSession()
        {
            var match = new Match(3);

            Assert.That(match.Players.Select(p => p.Name), Is.EqualTo(new[] { "P1", "P2", "P3" }));
            Assert.That(match.Players.Select(p => p.Number), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(match.Players.Select(p => p.Session).Distinct().Count(), Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void ThePlayerCount_MustBeOneToFour(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Match(count));
        }

        // ------------------------------------------------------------------ whose turn it is

        [Test]
        public void TheFirstPlayerStarts_AndKeepsTheTurnForTheSecondBall()
        {
            var match = new Match(2);

            Throw(match, 7);

            Assert.That(match.Current.Name, Is.EqualTo("P1"), "A frame has two balls.");
        }

        [Test]
        public void AfterTheSecondBall_TheNextPlayerGoes()
        {
            var match = new Match(2);

            ThrowAll(match, 7, 2);

            Assert.That(match.Current.Name, Is.EqualTo("P2"));
        }

        [Test]
        public void AStrike_EndsTheTurnAtOnce()
        {
            var match = new Match(2);

            Throw(match, 10);

            Assert.That(match.Current.Name, Is.EqualTo("P2"));
        }

        [Test]
        public void TheTurnComesBackAroundToTheFirstPlayer()
        {
            var match = new Match(2);

            ThrowAll(match, 10, 3, 4);

            Assert.That(match.Current.Name, Is.EqualTo("P1"));
            Assert.That(match.Players[0].Session.Card.CurrentFrame, Is.EqualTo(2));
            Assert.That(match.Players[1].Session.Card.CurrentFrame, Is.EqualTo(2));
        }

        [Test]
        public void ThePlayerWhoThrew_IsRememberedUntilTheTurnMoves()
        {
            var match = new Match(2);
            Assert.That(match.LastThrower, Is.Null);

            match.RecordThrow(10);

            Assert.That(match.Current.Name, Is.EqualTo("P1"), "The turn only moves on once the result has been shown.");
            Assert.That(match.LastThrower.Name, Is.EqualTo("P1"));

            match.AdvanceTurn();
            Assert.That(match.Current.Name, Is.EqualTo("P2"));
            Assert.That(match.LastThrower.Name, Is.EqualTo("P1"));
        }

        [Test]
        public void AdvanceTurn_ReportsWhetherTheTurnMoved()
        {
            var match = new Match(2);

            match.RecordThrow(4);
            Assert.That(match.AdvanceTurn(), Is.False, "Still P1's second ball.");

            match.RecordThrow(4);
            Assert.That(match.AdvanceTurn(), Is.True);
        }

        [Test]
        public void AlonePlayer_NeverHasTheTurnMove()
        {
            var match = new Match(1);

            match.RecordThrow(10);

            Assert.That(match.AdvanceTurn(), Is.False);
            Assert.That(match.Current.Name, Is.EqualTo("P1"));
        }

        [Test]
        public void ThreePlayers_TakeTurnsInOrder()
        {
            var match = new Match(3);
            var order = new System.Collections.Generic.List<string>();

            for (var i = 0; i < 6; i++)
            {
                order.Add(match.Current.Name);
                Throw(match, 10);
            }

            Assert.That(order, Is.EqualTo(new[] { "P1", "P2", "P3", "P1", "P2", "P3" }));
        }

        // ------------------------------------------------------------------ the tenth frame

        [Test]
        public void InTheTenthFrame_APlayerKeepsTheTurnForTheBonusBalls()
        {
            var match = new Match(2);
            // Nine strikes each.
            for (var i = 0; i < 18; i++) Throw(match, 10);
            Assert.That(match.Current.Name, Is.EqualTo("P1"));

            Throw(match, 10);
            Assert.That(match.Current.Name, Is.EqualTo("P1"), "A tenth-frame strike earns two more balls.");
            Throw(match, 10);
            Assert.That(match.Current.Name, Is.EqualTo("P1"));
            Throw(match, 10);

            Assert.That(match.Current.Name, Is.EqualTo("P2"), "P1 is done; P2 bowls the tenth.");
            Assert.That(match.Players[0].Session.IsComplete, Is.True);
        }

        // ------------------------------------------------------------------ the end

        [Test]
        public void TheMatchIsComplete_WhenEveryPlayerHasFinished()
        {
            var match = new Match(2);

            // Gutter balls take 20 throws each, shared out frame by frame, so 40 in all.
            for (var i = 0; i < 39; i++) Throw(match, 0);
            Assert.That(match.IsComplete, Is.False, "One ball still to go for P2.");

            Throw(match, 0);
            Assert.That(match.IsComplete, Is.True);
            Assert.That(match.Players.All(p => p.Session.IsComplete), Is.True);
        }

        [Test]
        public void RecordingAfterTheEnd_IsRejected()
        {
            var match = new Match(1);
            for (var i = 0; i < 20; i++) Throw(match, 0);

            Assert.Throws<InvalidOperationException>(() => match.RecordThrow(1));
        }

        [Test]
        public void TheHighestScoreWins_AndATieSharesIt()
        {
            var match = new Match(3);
            // P1 scores 3, P2 scores 7, P3 scores 7 (first frame only, the rest gutter balls).
            ThrowAll(match, 3, 0);
            ThrowAll(match, 4, 3);
            ThrowAll(match, 2, 5);

            Assert.That(match.Winners.Select(p => p.Name), Is.EqualTo(new[] { "P2", "P3" }));
        }

        [Test]
        public void Ranking_PutsTheBestFirst_AndKeepsSeatingOrderForTies()
        {
            var match = new Match(3);
            ThrowAll(match, 3, 0);
            ThrowAll(match, 9, 0);
            ThrowAll(match, 3, 0);

            Assert.That(match.Ranking.Select(p => p.Name), Is.EqualTo(new[] { "P2", "P1", "P3" }));
        }

        [Test]
        public void Reset_StartsOver_WithTheSamePlayers()
        {
            var match = new Match(2);
            ThrowAll(match, 10, 3, 4);

            match.Reset();

            Assert.That(match.PlayerCount, Is.EqualTo(2));
            Assert.That(match.Current.Name, Is.EqualTo("P1"));
            Assert.That(match.LastThrower, Is.Null);
            Assert.That(match.Players.All(p => p.Session.ThrowCount == 0), Is.True);
            Assert.That(match.IsComplete, Is.False);
        }
    }
}
