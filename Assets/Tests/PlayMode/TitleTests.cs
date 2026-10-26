using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VRB.Tests
{
    /// <summary>The main screen the game opens on: PLAY, ONLINE, CREDITS, and the way back to it.</summary>
    public class TitleTests
    {
        TitleView m_Title;
        LevelSelectionView m_Selection;
        NetworkMenuView m_Network;
        NetworkSession m_Session;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            foreach (var old in Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None))
            {
                if (old.IsListening) old.Shutdown();
                Object.Destroy(old.gameObject);
            }

            NetPlayer.All.Clear();
            TitleView.ShowAtStart = true;
            yield return null;
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Title = Object.FindFirstObjectByType<TitleView>();
            m_Selection = Object.FindFirstObjectByType<LevelSelectionView>(FindObjectsInactive.Include);
            m_Network = Object.FindFirstObjectByType<NetworkMenuView>();
            m_Session = Object.FindFirstObjectByType<NetworkSession>();
        }

        [UnityTearDown]
        public IEnumerator Leave()
        {
            TitleView.ShowAtStart = false;
            if (m_Session != null) m_Session.Disconnect();
            var waited = 0f;
            while (m_Session != null && m_Session.Manager != null && (m_Session.Manager.IsListening || m_Session.Manager.ShutdownInProgress) && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        static void Press(Component screen, string path) => screen.transform.Find(path).GetComponent<Button>().onClick.Invoke();

        [UnityTest]
        public IEnumerator TheGameOpensOnTheMainScreen_NotTheStartScreen()
        {
            Assert.That(m_Title.IsShown, Is.True);
            Assert.That(m_Selection.gameObject.activeSelf, Is.False);
            Assert.That(m_Title.ShowsCredits, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Play_GoesToTheStartScreen()
        {
            m_Title.PressPlay();
            yield return null;

            Assert.That(m_Title.IsShown, Is.False);
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator TheMenuButtonOfTheStartScreen_ComesBackToTheMainScreen()
        {
            m_Title.PressPlay();
            yield return null;

            Press(m_Selection, "MenuButton");
            yield return null;

            Assert.That(m_Title.IsShown, Is.True);
            Assert.That(m_Selection.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Credits_NameTheModelsMakerAndItsLicense_AndBackLeaves()
        {
            m_Title.PressCredits();
            yield return null;

            Assert.That(m_Title.ShowsCredits, Is.True);
            Assert.That(m_Title.CreditsText, Does.Contain("Bowling Club"));
            Assert.That(m_Title.CreditsText, Does.Contain("tiunov.se"));
            Assert.That(m_Title.CreditsText, Does.Contain("CC BY 4.0"));
            Assert.That(m_Title.CreditsText, Does.Contain("sketchfab.com/3d-models/bowling-club-0b8fae45fcda4fe78f93bb2a899401a6"));
            Assert.That(m_Title.CreditsText, Does.Contain("Changes made"));

            m_Title.PressCreditsBack();
            Assert.That(m_Title.ShowsCredits, Is.False);
            Assert.That(m_Title.IsShown, Is.True);
        }

        [UnityTest]
        public IEnumerator Online_OpensTheNetworkMenu_AndBackComesBackToTheMainScreen()
        {
            m_Title.PressOnline();
            yield return null;

            Assert.That(m_Network.IsOpen, Is.True);
            Assert.That(m_Title.IsShown, Is.False);
            Assert.That(m_Selection.gameObject.activeSelf, Is.False);

            m_Network.Close();
            yield return null;

            Assert.That(m_Network.IsOpen, Is.False);
            Assert.That(m_Title.IsShown, Is.True, "no game was opened, so the main screen is back");
            Assert.That(m_Selection.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator AfterOpeningAGame_TheNetworkMenuGoesOnToTheStartScreen()
        {
            m_Title.PressOnline();
            m_Network.PressHost();
            yield return null;
            m_Network.Close();
            yield return null;

            Assert.That(m_Title.IsShown, Is.False);
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator TheNetworkMenuOpenedFromTheStartScreen_DoesNotPopTheMainScreen()
        {
            m_Title.PressPlay();
            yield return null;

            Press(m_Selection, "NetworkButton");
            yield return null;
            m_Network.Close();
            yield return null;

            Assert.That(m_Title.IsShown, Is.False);
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
        }
    }
}
