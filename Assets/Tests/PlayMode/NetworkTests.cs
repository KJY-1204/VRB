using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>
    /// Hosting a game on this device: the player object appears, gets a lane, reports its throws, and the scoreboard
    /// shows it. Joining from another device is the network library's job and is not repeated here.
    /// </summary>
    public class NetworkTests
    {
        NetworkSession m_Session;
        GameController m_Game;
        ScoreboardView m_Scoreboard;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            // The network manager outlives scene loads, so one left by an earlier test has to go first.
            foreach (var old in Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None))
            {
                if (old.IsListening) old.Shutdown();
                Object.Destroy(old.gameObject);
            }

            NetPlayer.All.Clear();
            yield return null;
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Session = Object.FindFirstObjectByType<NetworkSession>();
            m_Game = Object.FindFirstObjectByType<GameController>();
            m_Scoreboard = Object.FindFirstObjectByType<ScoreboardView>(FindObjectsInactive.Include);
        }

        [UnityTearDown]
        public IEnumerator Leave()
        {
            if (m_Session != null) m_Session.Disconnect();
            var waited = 0f;
            while (m_Session != null && m_Session.Manager != null && (m_Session.Manager.IsListening || m_Session.Manager.ShutdownInProgress) && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator HostAndWaitForPlayer()
        {
            Assert.That(m_Session.StartHost(), Is.True);
            var waited = 0f;
            while (NetPlayer.All.Count == 0 && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(NetPlayer.All.Count, Is.EqualTo(1), "The host's own player object should spawn.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSceneHasTheNetworkPieces()
        {
            Assert.That(m_Session, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<NetworkBridge>(), Is.Not.Null);
            Assert.That(m_Session.State, Is.EqualTo(NetState.Offline));
            Assert.That(m_Session.Manager.NetworkConfig.PlayerPrefab, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HostingAGame_CreatesTheHostsPlayer_OnLaneOne()
        {
            yield return HostAndWaitForPlayer();

            var player = NetPlayer.All[0];
            Assert.That(m_Session.State, Is.EqualTo(NetState.Hosting));
            Assert.That(player.IsLocalPlayer, Is.True);
            Assert.That(player.Lane, Is.EqualTo(0));
            Assert.That(player.PlayerName, Is.EqualTo("Player"), "The bridge names the local player.");
        }

        [UnityTest]
        public IEnumerator TheHostCannotStartTwice()
        {
            yield return HostAndWaitForPlayer();

            Assert.That(m_Session.StartHost(), Is.False);
            Assert.That(m_Session.StartClient("192.168.0.2"), Is.False);
        }

        [UnityTest]
        public IEnumerator ThrowsAreReported_AndEmptiedByARestart()
        {
            yield return HostAndWaitForPlayer();
            var player = NetPlayer.All[0];

            m_Game.Flow.ConfirmSelection();
            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(7);
            yield return null;
            Assert.That(player.Throws, Is.EqualTo(new[] { 7 }), "A judged throw is reported at once.");

            m_Game.Restart();
            yield return null;
            Assert.That(player.Throws, Is.Empty, "A new game starts the report over.");
        }

        [UnityTest]
        public IEnumerator TheScoreboardShowsTheReportedPlayers()
        {
            yield return HostAndWaitForPlayer();

            m_Game.Flow.ConfirmSelection();
            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(10);
            yield return null;

            Assert.That(m_Scoreboard.IsRowVisible(1), Is.True);
            Assert.That(m_Scoreboard.PlayerNameText(1), Is.EqualTo("Player"));
            Assert.That(m_Scoreboard.FrameMarksText(1, 1), Is.EqualTo("X"));
            Assert.That(m_Scoreboard.PlayerTotalText(1), Is.EqualTo("10"));
        }

        [UnityTest]
        public IEnumerator TheRoster_IsTheReportedGame_AsAMatch()
        {
            yield return HostAndWaitForPlayer();
            m_Game.Flow.ConfirmSelection();
            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(4);
            yield return null;

            var roster = NetPlayer.Roster();

            Assert.That(roster.PlayerCount, Is.EqualTo(1));
            Assert.That(roster.Players[0].Name, Is.EqualTo("Player"));
            Assert.That(roster.Players[0].Session.Score, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator AMalformedAddress_FailsWithAMessage()
        {
            Assert.That(m_Session.StartClient("not an address"), Is.False);

            Assert.That(m_Session.State, Is.EqualTo(NetState.Failed));
            Assert.That(m_Session.StatusMessage, Does.Contain("address"));
            Assert.That(m_Session.Manager.IsListening, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Disconnecting_GoesBackOffline_AndRemovesThePlayer()
        {
            yield return HostAndWaitForPlayer();

            m_Session.Disconnect();
            var waited = 0f;
            while (NetPlayer.All.Count > 0 && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(m_Session.State, Is.EqualTo(NetState.Offline));
            Assert.That(NetPlayer.All.Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator WithoutAGame_TheScoreboardShowsOnlyTheLocalMatch()
        {
            var match = new Match(1);
            m_Game.Flow.ConfirmSelection();
            yield return null;

            Assert.That(m_Game.ScoreboardMatch, Is.Null, "No network game: the bridge stays out of the way.");
            Assert.That(m_Scoreboard.PlayerNameText(1), Is.EqualTo("P1"));
            Assert.That(match.PlayerCount, Is.EqualTo(1));
        }
    }
}
