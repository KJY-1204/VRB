using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    public class TvScoreTests
    {
        GameController m_Game;
        TvScoreView m_Tv;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Game = Object.FindFirstObjectByType<GameController>();
            m_Tv = Object.FindFirstObjectByType<TvScoreView>();
        }

        [UnityTest]
        public IEnumerator Tv_StartsAtZero_AndAsksForALevel()
        {
            Assert.That(m_Tv, Is.Not.Null, "The scene has a TV score view.");
            Assert.That(m_Tv.TotalText, Is.EqualTo("0"));
            Assert.That(m_Tv.StatusText, Is.EqualTo("Choose a level"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tv_ShowsTheTotalAndResult_AfterAThrow()
        {
            m_Game.Flow.ConfirmSelection();
            Assert.That(m_Tv.StatusText, Is.EqualTo("Frame 1 - Ball 1"));

            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(7);
            yield return null;

            Assert.That(m_Tv.TotalText, Is.EqualTo("7"));
            Assert.That(m_Tv.StatusText, Is.EqualTo("7 pins down"));
        }

        [UnityTest]
        public IEnumerator Tv_ShowsStrike_WhenAllTenFall()
        {
            m_Game.Flow.ConfirmSelection();
            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(GameSession.PinCount);
            yield return null;

            Assert.That(m_Tv.TotalText, Is.EqualTo(GameSession.PinCount.ToString()));
            Assert.That(m_Tv.StatusText, Is.EqualTo("STRIKE!"));
        }

        [UnityTest]
        public IEnumerator Restart_ClearsTheTv()
        {
            m_Game.Flow.ConfirmSelection();
            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(4);
            yield return null;

            m_Game.Restart();
            yield return null;

            Assert.That(m_Tv.TotalText, Is.EqualTo("0"));
            Assert.That(m_Tv.StatusText, Is.EqualTo("Choose a level"));
        }
    }
}
