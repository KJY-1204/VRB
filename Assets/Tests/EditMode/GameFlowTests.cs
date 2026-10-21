using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class GameFlowTests
    {
        [Test]
        public void NewGame_StartsInSelection_WithWeakPreselected_AndCannotThrow()
        {
            var flow = new GameFlow();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
            Assert.That(flow.SelectedLevel, Is.EqualTo(ThrowAssistLevel.Weak));
            Assert.That(flow.CanThrow, Is.False);
        }

        [Test]
        public void ThrowingIsBlocked_UntilSelectionIsConfirmed()
        {
            var flow = new GameFlow();

            Assert.Throws<InvalidOperationException>(() => flow.BallReleased());

            flow.ConfirmSelection();

            Assert.That(flow.CanThrow, Is.True);
        }

        [Test]
        public void SelectedLevel_IsKeptAfterConfirmation()
        {
            var flow = new GameFlow();
            flow.SelectLevel(ThrowAssistLevel.Strong);

            flow.ConfirmSelection();

            Assert.That(flow.SelectedLevel, Is.EqualTo(ThrowAssistLevel.Strong));
        }

        [Test]
        public void LevelCannotChange_AfterGameStarted()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();

            Assert.Throws<InvalidOperationException>(() => flow.SelectLevel(ThrowAssistLevel.Medium));
        }

        [Test]
        public void FullThrowCycle_FollowsExpectedPhaseOrder()
        {
            var flow = new GameFlow();
            var visited = new List<GamePhase>();
            flow.PhaseChanged += (_, next) => visited.Add(next);

            flow.ConfirmSelection();
            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(7);
            flow.ResultShown();
            flow.ResetComplete();

            Assert.That(visited, Is.EqualTo(new[]
            {
                GamePhase.Ready,
                GamePhase.Throwing,
                GamePhase.Settling,
                GamePhase.ShowingResult,
                GamePhase.Resetting,
                GamePhase.Ready,
            }));
            Assert.That(flow.Session.TotalPins, Is.EqualTo(7));
            Assert.That(flow.LastPinsFallen, Is.EqualTo(7));
            Assert.That(flow.CanThrow, Is.True);
        }

        [Test]
        public void CannotThrowAgain_UntilResetIsComplete()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();
            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(3);

            Assert.That(flow.CanThrow, Is.False);
            Assert.Throws<InvalidOperationException>(() => flow.BallReleased());
        }

        [Test]
        public void OutOfOrderEvents_AreRejected()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();

            Assert.Throws<InvalidOperationException>(() => flow.PinsSettled(5));
            Assert.Throws<InvalidOperationException>(() => flow.ResultShown());
            Assert.Throws<InvalidOperationException>(() => flow.ResetComplete());
            Assert.That(flow.Session.ThrowCount, Is.EqualTo(0));
        }

        [Test]
        public void ThreeThrows_AccumulateInSession()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();

            foreach (var pins in new[] { 7, 0, 10 })
            {
                flow.BallReleased();
                flow.ThrowReachedPins();
                flow.PinsSettled(pins);
                flow.ResultShown();
                flow.ResetComplete();
            }

            Assert.That(flow.Session.ThrowCount, Is.EqualTo(3));
            Assert.That(flow.Session.TotalPins, Is.EqualTo(17));
        }

        static void Throw(GameFlow flow, int pins)
        {
            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(pins);
            flow.ResultShown();
            if (flow.Phase == GamePhase.Resetting) flow.ResetComplete();
        }

        [Test]
        public void AfterTheFirstBallOfAFrame_TheStandingPinsStay()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();

            Throw(flow, 7);

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Ready));
            Assert.That(flow.Session.PinsStanding, Is.EqualTo(3));
        }

        [Test]
        public void AfterAStrikeOrTheSecondBall_TheRackIsSetUpAgain()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();
            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(10);
            flow.ResultShown();
            Assert.That(flow.NeedsFreshRack, Is.True, "Strike.");
            flow.ResetComplete();

            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(7);
            flow.ResultShown();
            Assert.That(flow.NeedsFreshRack, Is.False, "First ball left three standing.");
            flow.ResetComplete();

            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(2);
            flow.ResultShown();
            Assert.That(flow.NeedsFreshRack, Is.True, "Second ball ends the frame.");
        }

        [Test]
        public void AfterTheLastThrow_TheGameIsOver()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();

            for (var i = 0; i < 20; i++) Throw(flow, 0);

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.GameOver));
            Assert.That(flow.Session.IsComplete, Is.True);
            Assert.That(flow.CanThrow, Is.False);
            Assert.Throws<InvalidOperationException>(() => flow.BallReleased());
        }

        [Test]
        public void ATenthFrameStrike_GivesTwoMoreThrowsBeforeTheGameIsOver()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();
            for (var i = 0; i < 18; i++) Throw(flow, 0);

            Throw(flow, 10);
            Throw(flow, 10);
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Ready));
            Throw(flow, 10);

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.GameOver));
            Assert.That(flow.Session.Score, Is.EqualTo(30));
        }

        // ------------------------------------------------------------------ several players

        [Test]
        public void ThePlayerCount_CanBeChosenBeforeTheGameStarts()
        {
            var flow = new GameFlow();

            flow.SetPlayerCount(3);

            Assert.That(flow.Match.PlayerCount, Is.EqualTo(3));
            Assert.That(flow.CurrentPlayer.Name, Is.EqualTo("P1"));
        }

        [Test]
        public void ThePlayerCount_CannotChangeOnceTheGameHasStarted()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();

            Assert.Throws<InvalidOperationException>(() => flow.SetPlayerCount(2));
            Assert.That(flow.Match.PlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void SessionIsTheCurrentPlayersGame()
        {
            var flow = new GameFlow();
            flow.SetPlayerCount(2);
            flow.ConfirmSelection();

            Throw(flow, 10);

            Assert.That(flow.CurrentPlayer.Name, Is.EqualTo("P2"));
            Assert.That(flow.Session, Is.SameAs(flow.Match.Players[1].Session));
            Assert.That(flow.Session.ThrowCount, Is.EqualTo(0));
            Assert.That(flow.Match.Players[0].Session.ThrowCount, Is.EqualTo(1));
        }

        [Test]
        public void WhileTheResultIsShown_ItIsStillTheThrowersTurn()
        {
            var flow = new GameFlow();
            flow.SetPlayerCount(2);
            flow.ConfirmSelection();
            flow.BallReleased();
            flow.ThrowReachedPins();

            flow.PinsSettled(10);

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.ShowingResult));
            Assert.That(flow.CurrentPlayer.Name, Is.EqualTo("P1"), "The board still shows P1's strike.");
            Assert.That(flow.Match.LastThrower.Name, Is.EqualTo("P1"));
        }

        [Test]
        public void WhenTheTurnMoves_TheNextPlayerGetsAFreshRack_AndAnEventIsRaised()
        {
            var flow = new GameFlow();
            flow.SetPlayerCount(2);
            flow.ConfirmSelection();
            Player from = null, to = null;
            flow.TurnChanged += (previous, current) => { from = previous; to = current; };

            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(7);
            flow.ResultShown();
            Assert.That(flow.CurrentPlayer.Name, Is.EqualTo("P1"), "A second ball for P1.");
            Assert.That(flow.NeedsFreshRack, Is.False);
            Assert.That(from, Is.Null, "No turn change yet.");
            flow.ResetComplete();

            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(2);
            flow.ResultShown();

            Assert.That(flow.CurrentPlayer.Name, Is.EqualTo("P2"));
            Assert.That(flow.NeedsFreshRack, Is.True, "P2 starts a frame on a full rack.");
            Assert.That(from.Name, Is.EqualTo("P1"));
            Assert.That(to.Name, Is.EqualTo("P2"));
        }

        [Test]
        public void ATwoPlayerGame_EndsWhenBothAreDone_AndShowsTheWinner()
        {
            var flow = new GameFlow();
            flow.SetPlayerCount(2);
            flow.ConfirmSelection();

            // P1 knocks over one pin a frame, P2 two; every frame is an open frame of one ball and a gutter ball.
            for (var turn = 0; turn < 20; turn++)
            {
                Throw(flow, turn % 2 == 0 ? 1 : 2);
                Throw(flow, 0);
            }

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.GameOver));
            Assert.That(flow.Match.IsComplete, Is.True);
            Assert.That(flow.Match.Players[0].Session.Score, Is.EqualTo(10));
            Assert.That(flow.Match.Players[1].Session.Score, Is.EqualTo(20));
            Assert.That(flow.Match.Winners.Select(p => p.Name), Is.EqualTo(new[] { "P2" }));
        }

        [Test]
        public void GameOver_WaitsForTheLastPlayer()
        {
            var flow = new GameFlow();
            flow.SetPlayerCount(2);
            flow.ConfirmSelection();

            for (var i = 0; i < 39; i++) Throw(flow, 0);

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Ready), "P1 is done but P2 has a ball to go.");
            Throw(flow, 0);
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.GameOver));
        }

        [Test]
        public void Restart_KeepsThePlayers_ButClearsTheirGames()
        {
            var flow = new GameFlow();
            flow.SetPlayerCount(3);
            flow.ConfirmSelection();
            Throw(flow, 10);
            Throw(flow, 10);

            flow.Restart();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
            Assert.That(flow.Match.PlayerCount, Is.EqualTo(3));
            Assert.That(flow.CurrentPlayer.Name, Is.EqualTo("P1"));
            Assert.That(flow.Match.Players.All(p => p.Session.ThrowCount == 0), Is.True);
            Assert.DoesNotThrow(() => flow.SetPlayerCount(2));
        }

        [Test]
        public void Restart_FromGameOver_StartsOver()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();
            for (var i = 0; i < 20; i++) Throw(flow, 0);

            flow.Restart();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
            Assert.That(flow.Session.IsComplete, Is.False);
            Assert.That(flow.Session.PinsStanding, Is.EqualTo(10));
        }

        [Test]
        public void Restart_ClearsRecord_AndReturnsToSelection_WithLevelSelectableAgain()
        {
            var flow = new GameFlow();
            flow.SelectLevel(ThrowAssistLevel.Strong);
            flow.ConfirmSelection();
            flow.BallReleased();
            flow.ThrowReachedPins();
            flow.PinsSettled(8);

            flow.Restart();

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
            Assert.That(flow.Session.ThrowCount, Is.EqualTo(0));
            Assert.That(flow.Session.TotalPins, Is.EqualTo(0));
            Assert.That(flow.LastPinsFallen, Is.EqualTo(0));
            Assert.That(flow.SelectedLevel, Is.EqualTo(ThrowAssistLevel.Weak));
            Assert.That(flow.CanThrow, Is.False);
            Assert.DoesNotThrow(() => flow.SelectLevel(ThrowAssistLevel.RawPhysics));
        }

        [Test]
        public void PinsSettled_WithInvalidCount_DoesNotAdvancePhase()
        {
            var flow = new GameFlow();
            flow.ConfirmSelection();
            flow.BallReleased();
            flow.ThrowReachedPins();

            Assert.Throws<ArgumentOutOfRangeException>(() => flow.PinsSettled(11));

            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Settling));
            Assert.That(flow.Session.ThrowCount, Is.EqualTo(0));
        }
    }
}
