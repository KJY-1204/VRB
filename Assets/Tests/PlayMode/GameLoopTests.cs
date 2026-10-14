using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>End-to-end checks of the scoring-display capability on the real scene.</summary>
    public class GameLoopTests
    {
        GameController m_Game;
        LevelSelectionView m_Selection;
        ScoreboardView m_Scoreboard;
        PinRack m_Rack;
        XRGrabInteractable m_Grab;
        BallThrower m_Thrower;
        Rigidbody m_Body;
        XRInteractionManager m_Manager;
        XRDirectInteractor m_Hand;
        Vector3 m_StandPosition;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Game = Object.FindFirstObjectByType<GameController>();
            m_Selection = Object.FindFirstObjectByType<LevelSelectionView>(FindObjectsInactive.Include);
            m_Scoreboard = Object.FindFirstObjectByType<ScoreboardView>(FindObjectsInactive.Include);
            m_Rack = Object.FindFirstObjectByType<PinRack>();

            var ball = GameObject.Find("Ball");
            m_Grab = ball.GetComponent<XRGrabInteractable>();
            m_Thrower = ball.GetComponent<BallThrower>();
            m_Body = ball.GetComponent<Rigidbody>();
            m_StandPosition = ball.transform.position;
            m_Manager = Object.FindFirstObjectByType<XRInteractionManager>();

            var handObject = new GameObject("TestHand");
            handObject.transform.position = m_StandPosition;
            var trigger = handObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.05f;
            m_Hand = handObject.AddComponent<XRDirectInteractor>();
            yield return null;
        }

        [TearDown]
        public void RemoveHand()
        {
            if (m_Hand != null) Object.Destroy(m_Hand.gameObject);
        }

        bool CanGrab() => ((IXRSelectInteractable)m_Grab).IsSelectableBy((IXRSelectInteractor)m_Hand);

        void PressStart()
        {
            // The Start button is the real UI control; invoking it follows the same path as a ray press.
            m_Selection.transform.Find("StartButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        void ChooseAndStart(ThrowAssistLevel level)
        {
            m_Selection.Pick(level);
            PressStart();
        }

        /// <summary>Grab the ball, carry it to the lane center, swing straight down the lane and let go.</summary>
        IEnumerator ThrowDownTheLane(float speed = 7f)
        {
            Assert.That(CanGrab(), Is.True, "Precondition: the ball can be grabbed.");
            m_Hand.transform.position = m_StandPosition;
            m_Manager.SelectEnter((IXRSelectInteractor)m_Hand, (IXRSelectInteractable)m_Grab);
            yield return null;

            var center = new Vector3(0f, m_StandPosition.y, m_StandPosition.z);
            m_Hand.transform.position = center;
            yield return new WaitForSeconds(0.3f);

            var t0 = Time.time;
            const float swing = 0.12f; // 7 m/s x 0.12 s = 0.84 m, so the hand stays behind the foul line
            while (Time.time - t0 < swing)
            {
                m_Hand.transform.position = center + new Vector3(0f, 0f, speed * (Time.time - t0));
                yield return null;
            }

            m_Hand.transform.position = center + new Vector3(0f, 0f, speed * (Time.time - t0));
            m_Manager.SelectExit((IXRSelectInteractor)m_Hand, (IXRSelectInteractable)m_Grab);
        }

        IEnumerator WaitForPhase(GamePhase phase, float timeout)
        {
            var waited = 0f;
            while (m_Game.Flow.Phase != phase && waited < timeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(m_Game.Flow.Phase, Is.EqualTo(phase), $"Timed out waiting for {phase}.");
        }

        [UnityTest]
        public IEnumerator NewGame_ShowsSelection_WithWeakPreselected_AndHidesTheScoreboard()
        {
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
            Assert.That(m_Scoreboard.gameObject.activeSelf, Is.False);
            Assert.That(m_Selection.Selected, Is.EqualTo(ThrowAssistLevel.Weak));
            Assert.That(m_Selection.IsHighlighted(ThrowAssistLevel.Weak), Is.True);
            Assert.That(m_Selection.IsHighlighted(ThrowAssistLevel.Strong), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChoosingALevel_ChangesTheHighlightAndDescription()
        {
            var before = m_Selection.DescriptionText;

            m_Selection.Pick(ThrowAssistLevel.Strong);

            Assert.That(m_Selection.IsHighlighted(ThrowAssistLevel.Strong), Is.True);
            Assert.That(m_Selection.IsHighlighted(ThrowAssistLevel.Weak), Is.False);
            Assert.That(m_Selection.DescriptionText, Is.Not.EqualTo(before));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BeforeConfirming_TheBallCannotBeGrabbed()
        {
            m_Hand.transform.position = m_StandPosition;
            m_Selection.Pick(ThrowAssistLevel.Medium);

            Assert.That(CanGrab(), Is.False, "Throwing must not start before the selection is confirmed.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Confirming_StartsTheGame_WithTheChosenLevel()
        {
            ChooseAndStart(ThrowAssistLevel.Strong);
            yield return null;

            Assert.That(m_Game.Flow.Phase, Is.EqualTo(GamePhase.Ready));
            Assert.That(m_Thrower.Level, Is.EqualTo(ThrowAssistLevel.Strong));
            Assert.That(m_Selection.gameObject.activeSelf, Is.False);
            Assert.That(m_Scoreboard.gameObject.activeSelf, Is.True);
            m_Hand.transform.position = m_StandPosition;
            Assert.That(CanGrab(), Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutPickingAnything_TheGameStartsWithWeak()
        {
            PressStart();
            yield return null;

            Assert.That(m_Thrower.Level, Is.EqualTo(ThrowAssistLevel.Weak));
        }

        [UnityTest]
        public IEnumerator FullThrow_ShowsTheResult_ThenResetsForTheNextThrow()
        {
            PressStart();
            yield return null;

            yield return ThrowDownTheLane();
            yield return WaitForPhase(GamePhase.ShowingResult, 40f);

            var pins = m_Game.Flow.LastPinsFallen;
            Assert.That(m_Scoreboard.LastText, Is.EqualTo(pins.ToString()));
            Assert.That(m_Scoreboard.TotalText, Is.EqualTo(pins.ToString()));
            Assert.That(m_Scoreboard.ThrowsText, Is.EqualTo("1"));
            Assert.That(m_Scoreboard.StatusText, Does.Contain(pins == 10 ? "STRIKE" : pins + " pins"));
            Assert.That(pins, Is.GreaterThan(0), "A straight 7 m/s throw down the center should hit the pins.");

            yield return WaitForPhase(GamePhase.Ready, 10f);

            Assert.That(m_Rack.CountFallen(), Is.EqualTo(0), "All ten pins are back after the result.");
            Assert.That(Vector3.Distance(m_Body.position, m_StandPosition), Is.LessThan(0.1f), "A fresh ball is on the stand.");
            m_Hand.transform.position = m_StandPosition;
            Assert.That(CanGrab(), Is.True, "The ball can be grabbed again for the next throw.");
        }

        [UnityTest]
        public IEnumerator Restart_ClearsTheRecord_AndReturnsToSelection()
        {
            PressStart();
            yield return null;
            yield return ThrowDownTheLane();
            yield return WaitForPhase(GamePhase.ShowingResult, 40f);
            yield return WaitForPhase(GamePhase.Ready, 10f);
            Assert.That(m_Game.Flow.Session.ThrowCount, Is.EqualTo(1));

            m_Scoreboard.transform.Find("RestartButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            Assert.That(m_Game.Flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
            Assert.That(m_Game.Flow.Session.TotalPins, Is.EqualTo(0));
            Assert.That(m_Game.Flow.Session.ThrowCount, Is.EqualTo(0));
            Assert.That(m_Selection.gameObject.activeSelf, Is.True);
            Assert.That(m_Scoreboard.gameObject.activeSelf, Is.False);
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(0));

            // A different level can be chosen for the new game.
            m_Selection.Pick(ThrowAssistLevel.RawPhysics);
            PressStart();
            Assert.That(m_Thrower.Level, Is.EqualTo(ThrowAssistLevel.RawPhysics));
        }

        [UnityTest]
        public IEnumerator Scoreboard_ShowsStrikeWhenAllTenFall_AndZeroForAMiss()
        {
            var session = new GameSession();
            session.RecordThrow(10);
            m_Scoreboard.Refresh(GamePhase.ShowingResult, session, 10);
            Assert.That(m_Scoreboard.StatusText, Does.Contain("STRIKE"));

            session.RecordThrow(0);
            m_Scoreboard.Refresh(GamePhase.ShowingResult, session, 0);
            Assert.That(m_Scoreboard.LastText, Is.EqualTo("0"));
            Assert.That(m_Scoreboard.TotalText, Is.EqualTo("10"));
            Assert.That(m_Scoreboard.ThrowsText, Is.EqualTo("2"));
            yield return null;
        }
    }
}
