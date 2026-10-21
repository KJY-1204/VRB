using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>Several players on the real scene: choosing how many, and what the scoreboard and the TV show.</summary>
    public class MultiplayerTests
    {
        GameController m_Game;
        LevelSelectionView m_Selection;
        ScoreboardView m_Scoreboard;
        TvScoreView m_Tv;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Game = Object.FindFirstObjectByType<GameController>();
            m_Selection = Object.FindFirstObjectByType<LevelSelectionView>(FindObjectsInactive.Include);
            m_Scoreboard = Object.FindFirstObjectByType<ScoreboardView>(FindObjectsInactive.Include);
            m_Tv = Object.FindFirstObjectByType<TvScoreView>();
        }

        [UnityTest]
        public IEnumerator TheGameStartsWithOnePlayer_AndSelectionShowsIt()
        {
            Assert.That(m_Game.Flow.Match.PlayerCount, Is.EqualTo(1));
            Assert.That(m_Selection.SelectedPlayers, Is.EqualTo(1));
            Assert.That(m_Selection.IsPlayersHighlighted(1), Is.True);
            Assert.That(m_Selection.IsPlayersHighlighted(2), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PickingThePlayerCount_SetsUpThatManyPlayers()
        {
            m_Selection.PickPlayers(3);
            yield return null;

            Assert.That(m_Game.Flow.Match.PlayerCount, Is.EqualTo(3));
            Assert.That(m_Selection.IsPlayersHighlighted(3), Is.True);
            Assert.That(m_Selection.IsPlayersHighlighted(1), Is.False);
        }

        [UnityTest]
        public IEnumerator ThePlayerCount_CannotChangeOnceTheGameHasStarted()
        {
            m_Selection.PickPlayers(2);
            m_Game.Flow.ConfirmSelection();

            m_Selection.PickPlayers(4);
            yield return null;

            Assert.That(m_Game.Flow.Match.PlayerCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Restart_KeepsThePlayerCount()
        {
            m_Selection.PickPlayers(3);
            m_Game.Flow.ConfirmSelection();

            m_Game.Restart();
            yield return null;

            Assert.That(m_Game.Flow.Match.PlayerCount, Is.EqualTo(3));
            Assert.That(m_Game.Flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
        }

        [UnityTest]
        public IEnumerator TheScoreboard_HasARowForEachPlayer_AndHidesTheRest()
        {
            var match = new Match(2);

            m_Scoreboard.Refresh(GamePhase.Ready, match);

            Assert.That(m_Scoreboard.IsRowVisible(1), Is.True);
            Assert.That(m_Scoreboard.IsRowVisible(2), Is.True);
            Assert.That(m_Scoreboard.IsRowVisible(3), Is.False);
            Assert.That(m_Scoreboard.IsRowVisible(4), Is.False);
            Assert.That(m_Scoreboard.PlayerNameText(1), Is.EqualTo("P1"));
            Assert.That(m_Scoreboard.PlayerNameText(2), Is.EqualTo("P2"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EachPlayersRow_ShowsTheirOwnFrames()
        {
            var match = new Match(2);
            match.RecordThrow(10);
            match.AdvanceTurn();
            match.RecordThrow(7);
            match.RecordThrow(3);
            match.AdvanceTurn();
            match.RecordThrow(4);

            m_Scoreboard.Refresh(GamePhase.Ready, match);

            Assert.That(m_Scoreboard.FrameMarksText(1, 1), Is.EqualTo("X"));
            Assert.That(m_Scoreboard.FrameMarksText(1, 2), Is.EqualTo("4"), "P1 is on frame 2 with a first ball of 4.");
            Assert.That(m_Scoreboard.FrameScoreText(1, 1), Is.Empty, "The strike still needs one more ball.");
            Assert.That(m_Scoreboard.PlayerTotalText(1), Is.EqualTo("18"), "X counts 10 + the 4 so far, plus the 4 itself.");

            Assert.That(m_Scoreboard.FrameMarksText(2, 1), Is.EqualTo("7 /"));
            Assert.That(m_Scoreboard.FrameScoreText(2, 1), Is.Empty, "The spare's bonus ball is not thrown yet.");
            Assert.That(m_Scoreboard.PlayerTotalText(2), Is.EqualTo("10"), "P1's 4 is not P2's bonus.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheStatusLine_NamesThePlayer_WhenThereAreSeveral()
        {
            var match = new Match(2);
            Assert.That(ScoreboardView.StatusFor(GamePhase.Ready, match), Is.EqualTo("P1: Frame 1 - Ball 1"));

            match.RecordThrow(10);
            Assert.That(ScoreboardView.StatusFor(GamePhase.ShowingResult, match), Is.EqualTo("P1: STRIKE!"),
                "The result belongs to the player who threw, even though it is about to be someone else's turn.");

            match.AdvanceTurn();
            Assert.That(ScoreboardView.StatusFor(GamePhase.Ready, match), Is.EqualTo("P2: Frame 1 - Ball 1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheStatusLine_StaysPlain_ForOnePlayer()
        {
            var match = new Match(1);

            Assert.That(ScoreboardView.StatusFor(GamePhase.Ready, match), Is.EqualTo("Frame 1 - Ball 1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTv_ShowsWhoIsUp_AndTheirTotal()
        {
            var match = new Match(2);
            match.RecordThrow(10);
            match.AdvanceTurn();

            m_Tv.Refresh(GamePhase.Ready, match);

            Assert.That(m_Tv.LabelText, Is.EqualTo("P2  TOTAL"));
            Assert.That(m_Tv.TotalText, Is.EqualTo("0"));
            Assert.That(m_Tv.StatusText, Is.EqualTo("P2: Frame 1 - Ball 1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTv_KeepsTheOldLabel_ForOnePlayer()
        {
            m_Tv.Refresh(GamePhase.Ready, new Match(1));

            Assert.That(m_Tv.LabelText, Is.EqualTo("TOTAL"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameOver_NamesTheWinner_OrCallsATie()
        {
            var match = new Match(2);
            for (var i = 0; i < 40; i++)
            {
                // P2 knocks one pin down in every frame, P1 none.
                match.RecordThrow(match.Current.Number == 2 && match.Current.Session.Card.NextThrowInFrame == 1 ? 1 : 0);
                match.AdvanceTurn();
            }

            m_Scoreboard.Refresh(GamePhase.GameOver, match);
            Assert.That(m_Scoreboard.StatusText, Is.EqualTo("P2 wins - 10"));

            var tied = new Match(2);
            for (var i = 0; i < 40; i++)
            {
                tied.RecordThrow(0);
                tied.AdvanceTurn();
            }

            m_Scoreboard.Refresh(GamePhase.GameOver, tied);
            Assert.That(m_Scoreboard.StatusText, Is.EqualTo("Tie - 0"));
            yield return null;
        }
    }
}
