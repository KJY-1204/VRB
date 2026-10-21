using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>The screen for hosting and joining: what shows when, and what the keypad types.</summary>
    public class NetworkMenuTests
    {
        NetworkSession m_Session;
        NetworkMenuView m_Menu;
        LevelSelectionView m_Selection;
        GameController m_Game;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
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
            m_Menu = Object.FindFirstObjectByType<NetworkMenuView>();
            m_Selection = Object.FindFirstObjectByType<LevelSelectionView>(FindObjectsInactive.Include);
            m_Game = Object.FindFirstObjectByType<GameController>();
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

        IEnumerator UntilPlayer()
        {
            var waited = 0f;
            while (NetPlayer.All.Count == 0 && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheMenuStartsClosed_AndTheStartScreenShows()
        {
            Assert.That(m_Menu.IsOpen, Is.False);
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Online_OpensTheMenu_InPlaceOfTheStartScreen()
        {
            var online = m_Selection.transform.Find("NetworkButton").GetComponent<UnityEngine.UI.Button>();

            online.onClick.Invoke();
            yield return null;

            Assert.That(m_Menu.IsOpen, Is.True);
            Assert.That(m_Selection.gameObject.activeSelf, Is.False);
            Assert.That(m_Menu.ShowsOffline, Is.True);
            Assert.That(m_Menu.ShowsHosting || m_Menu.ShowsJoin || m_Menu.ShowsConnected, Is.False);
        }

        [UnityTest]
        public IEnumerator TheStartScreen_StaysHidden_WhileTheMenuIsOpen_EvenWhenTheGameRedrawsIt()
        {
            m_Menu.Open();

            m_Game.RefreshBoards();
            m_Game.Restart();
            yield return null;

            Assert.That(m_Selection.gameObject.activeSelf, Is.False, "The game asking for its start screen must not cover the menu.");

            m_Menu.Close();
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Back_ClosesTheMenu_AndBringsTheStartScreenBack()
        {
            m_Menu.Open();

            m_Menu.Close();
            yield return null;

            Assert.That(m_Menu.IsOpen, Is.False);
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Host_OpensAGame_AndShowsTheAddressToReadOut()
        {
            m_Menu.Open();

            m_Menu.PressHost();
            yield return UntilPlayer();

            Assert.That(m_Session.State, Is.EqualTo(NetState.Hosting));
            Assert.That(m_Menu.ShowsHosting, Is.True);
            Assert.That(m_Menu.ShowsOffline, Is.False);
            Assert.That(m_Menu.AddressText, Is.EqualTo(NetworkSession.LocalAddress()));
        }

        [UnityTest]
        public IEnumerator Leave_StopsHosting_AndShowsTheFirstScreenAgain()
        {
            m_Menu.Open();
            m_Menu.PressHost();
            yield return UntilPlayer();

            m_Menu.PressLeave();
            yield return null;

            Assert.That(m_Session.State, Is.EqualTo(NetState.Offline));
            Assert.That(m_Menu.ShowsOffline, Is.True);
            Assert.That(m_Menu.ShowsHosting, Is.False);
        }

        [UnityTest]
        public IEnumerator Join_ShowsTheKeypad_StartedWithTheNetworksFirstThreeNumbers()
        {
            m_Menu.Open();

            m_Menu.PressJoin();
            yield return null;

            Assert.That(m_Menu.ShowsJoin, Is.True);
            Assert.That(m_Menu.EntryText, Is.EqualTo(AddressEntry.SubnetPrefix(NetworkSession.LocalAddress())));
        }

        void ClearEntry()
        {
            while (m_Menu.EntryText.Length > 0) m_Menu.PressDelete();
        }

        UnityEngine.UI.Button ConnectButton() =>
            GameObject.Find("NetworkMenuCanvas").transform.Find("JoinPanel/ConnectButton").GetComponent<UnityEngine.UI.Button>();

        [UnityTest]
        public IEnumerator TheKeypad_TypesAnAddress_AndDelRemovesTheLastKey()
        {
            m_Menu.Open();
            m_Menu.PressJoin();
            ClearEntry();

            foreach (var key in "10.0.0.7") m_Menu.PressKey(key);
            Assert.That(m_Menu.EntryText, Is.EqualTo("10.0.0.7"));

            m_Menu.PressDelete();
            Assert.That(m_Menu.EntryText, Is.EqualTo("10.0.0."));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheKeypad_RefusesKeysThatCouldNotMakeAnAddress()
        {
            m_Menu.Open();
            m_Menu.PressJoin();
            ClearEntry();

            m_Menu.PressKey('.');
            m_Menu.PressKey('3');
            m_Menu.PressKey('0');
            m_Menu.PressKey('0');

            Assert.That(m_Menu.EntryText, Is.EqualTo("30"), "No leading dot, and 300 is not a number an address can hold.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Connect_NeedsACompleteAddress()
        {
            m_Menu.Open();
            m_Menu.PressJoin();
            ClearEntry();
            foreach (var key in "192.168.0") m_Menu.PressKey(key);

            Assert.That(ConnectButton().interactable, Is.False, "An unfinished address cannot be connected to.");

            m_Menu.PressKey('.');
            m_Menu.PressKey('5');
            Assert.That(ConnectButton().interactable, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnUnfinishedAddress_IsRefusedWithAMessage_OnTheKeypad()
        {
            m_Menu.Open();
            m_Menu.PressJoin();
            ClearEntry();
            foreach (var key in "192.168.0") m_Menu.PressKey(key);

            m_Menu.PressConnect();
            yield return null;

            Assert.That(m_Session.State, Is.EqualTo(NetState.Failed));
            Assert.That(m_Menu.StatusText, Does.Contain("address"));
            Assert.That(m_Menu.ShowsJoin, Is.True, "Stay on the keypad to correct it.");
            Assert.That(m_Session.Manager.IsListening, Is.False);
        }

        [UnityTest]
        public IEnumerator Back_FromTheKeypad_ReturnsToTheFirstScreen()
        {
            m_Menu.Open();
            m_Menu.PressJoin();

            m_Menu.PressBack();
            yield return null;

            Assert.That(m_Menu.ShowsOffline, Is.True);
            Assert.That(m_Menu.IsJoining, Is.False);
        }

        [UnityTest]
        public IEnumerator InANetworkGame_TheLaneAndPlayerCountCannotBePicked()
        {
            m_Menu.Open();
            m_Menu.PressHost();
            yield return UntilPlayer();
            yield return null;

            var lane = m_Selection.transform.Find("Lane_2").GetComponent<UnityEngine.UI.Button>();
            var players = m_Selection.transform.Find("Players_3").GetComponent<UnityEngine.UI.Button>();
            Assert.That(lane.interactable, Is.False, "The host hands out the lane.");
            Assert.That(players.interactable, Is.False, "One player per device.");
            Assert.That(m_Game.Flow.Match.PlayerCount, Is.EqualTo(1));

            m_Menu.PressLeave();
            var waited = 0f;
            while (NetPlayer.All.Count > 0 && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            yield return null;
            Assert.That(lane.interactable, Is.True, "Back to normal once the game is left.");
            Assert.That(players.interactable, Is.True);
        }
    }
}
