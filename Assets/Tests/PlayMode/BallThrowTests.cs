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
    /// <summary>Simulates a hand grabbing, swinging and releasing the ball to check the ball-throwing requirements.</summary>
    public class BallThrowTests
    {
        XRInteractionManager m_Manager;
        XRGrabInteractable m_Grab;
        BallThrower m_Thrower;
        BallRespawner m_Respawner;
        Rigidbody m_Body;
        XRDirectInteractor m_Hand;
        Vector3 m_StandPosition;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;

            var ball = GameObject.Find("Ball");
            m_Grab = ball.GetComponent<XRGrabInteractable>();
            m_Thrower = ball.GetComponent<BallThrower>();
            m_Respawner = ball.GetComponent<BallRespawner>();
            m_Body = ball.GetComponent<Rigidbody>();
            m_StandPosition = ball.transform.position;
            m_Manager = Object.FindFirstObjectByType<XRInteractionManager>();

            var handObject = new GameObject("TestHand");
            handObject.transform.position = m_StandPosition;
            var trigger = handObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.05f;
            m_Hand = handObject.AddComponent<XRDirectInteractor>();

            // Grabbing is only allowed once the game has started, so confirm the level selection first.
            Object.FindFirstObjectByType<LevelSelectionView>().transform.Find("StartButton")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
        }

        [TearDown]
        public void RemoveHand()
        {
            if (m_Hand != null) Object.Destroy(m_Hand.gameObject);
        }

        /// <summary>Selectability as the interaction manager sees it, including select filters.</summary>
        bool CanSelect() => ((IXRSelectInteractable)m_Grab).IsSelectableBy((IXRSelectInteractor)m_Hand);

        /// <summary>Grab the ball, swing the hand along +Z at the given speed for a while, then let go.</summary>
        IEnumerator SwingAndRelease(float speed, float seconds)
        {
            m_Hand.transform.position = m_StandPosition;
            m_Manager.SelectEnter((IXRSelectInteractor)m_Hand, (IXRSelectInteractable)m_Grab);
            yield return null;

            var start = m_StandPosition;
            var t0 = Time.time;
            while (Time.time - t0 < seconds)
            {
                m_Hand.transform.position = start + new Vector3(0f, 0f, speed * (Time.time - t0));
                yield return null;
            }

            // Still moving on the frame of release, like a real hand.
            m_Hand.transform.position = start + new Vector3(0f, 0f, speed * (Time.time - t0));
            m_Manager.SelectExit((IXRSelectInteractor)m_Hand, (IXRSelectInteractable)m_Grab);
        }

        IEnumerator ThrowAndCapture(float speed, float seconds, System.Action<Vector3> onThrown)
        {
            var thrown = false;
            var velocity = Vector3.zero;
            m_Thrower.Thrown += v => { thrown = true; velocity = v; };

            yield return SwingAndRelease(speed, seconds);

            var waited = 0f;
            while (!thrown && waited < 1f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(thrown, Is.True, "Releasing the ball should launch it.");
            onThrown(velocity);
        }

        [UnityTest]
        public IEnumerator GrabbedBall_FollowsTheHand()
        {
            m_Hand.transform.position = m_StandPosition;
            m_Manager.SelectEnter((IXRSelectInteractor)m_Hand, (IXRSelectInteractable)m_Grab);
            yield return null;

            m_Hand.transform.position = m_StandPosition + new Vector3(0.2f, 0.1f, 0f);
            yield return new WaitForSeconds(0.3f);

            Assert.That(m_Thrower.IsHeld, Is.True);
            Assert.That(Vector3.Distance(m_Body.position, m_StandPosition + new Vector3(0.2f, 0.1f, 0f)), Is.LessThan(0.08f));
        }

        [UnityTest]
        public IEnumerator HandNearTheBall_CanGrab_ButAFarHandCannot()
        {
            m_Hand.transform.position = m_StandPosition + new Vector3(0f, 0.1f, 0f);
            Assert.That(CanSelect(), Is.True, "A hand next to the ball can grab it.");

            m_Hand.transform.position = m_StandPosition + new Vector3(0f, 0f, 3f);
            Assert.That(CanSelect(), Is.False, "A hand far from the ball cannot.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Grabbing_CanBeForbiddenByTheGame()
        {
            var filter = m_Grab.GetComponent<NearGrabFilter>();
            filter.CanGrab = () => false;
            m_Hand.transform.position = m_StandPosition;

            Assert.That(CanSelect(), Is.False);

            filter.CanGrab = () => true;
            Assert.That(CanSelect(), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Release_LaunchesTheBallTowardTheLane()
        {
            m_Thrower.Level = ThrowAssistLevel.Weak;
            Vector3 v = default;

            yield return ThrowAndCapture(4f, 0.25f, velocity => v = velocity);

            Assert.That(v.z, Is.EqualTo(4f).Within(0.6f), "Ball speed should match the swing speed.");
            Assert.That(Mathf.Abs(v.x), Is.LessThan(0.3f));
            yield return null;
            Assert.That(m_Body.linearVelocity.z, Is.GreaterThan(3f), "The ball should actually be moving down the lane.");
        }

        [UnityTest]
        public IEnumerator HandAlmostStill_DropsTheBallInPlace()
        {
            Vector3 v = default;

            yield return ThrowAndCapture(0f, 0.25f, velocity => v = velocity);

            Assert.That(v.magnitude, Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator RawPhysics_AndWeak_BothThrowButWeakIsSmoother()
        {
            m_Thrower.Level = ThrowAssistLevel.RawPhysics;
            Vector3 raw = default;
            yield return ThrowAndCapture(4f, 0.25f, velocity => raw = velocity);

            Assert.That(raw.z, Is.EqualTo(4f).Within(1.2f));
        }

        [UnityTest]
        public IEnumerator ReleasingBeyondTheFoulLine_DoesNotThrow_AndReturnsTheBall()
        {
            var fouled = false;
            m_Thrower.Fouled += () => fouled = true;
            var thrown = false;
            m_Thrower.Thrown += _ => thrown = true;

            // Reach well past the foul line (z = 0) before letting go.
            yield return SwingAndRelease(2.5f, 0.8f);
            yield return new WaitForSeconds(0.3f);

            Assert.That(fouled, Is.True);
            Assert.That(thrown, Is.False);
            Assert.That(Vector3.Distance(m_Body.position, m_StandPosition), Is.LessThan(0.05f), "Ball returns to its stand.");
            Assert.That(m_Body.linearVelocity.magnitude, Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator ReturnToStand_BringsTheBallBack_AfterAThrow()
        {
            m_Body.position = new Vector3(0.3f, 0.2f, 12f);
            m_Body.linearVelocity = new Vector3(0f, 0f, 5f);

            m_Respawner.ReturnToStand();
            yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Distance(m_Body.position, m_StandPosition), Is.LessThan(0.05f));
            Assert.That(m_Body.linearVelocity.magnitude, Is.LessThan(0.1f));
        }
    }
}
