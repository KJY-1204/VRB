using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>Runs the real VRB scene and checks pin and ball physics behave as the pin-physics spec requires.</summary>
    public class PinPhysicsTests
    {
        PinRack m_Rack;
        Pin[] m_Pins;
        Rigidbody m_Ball;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;

            m_Rack = Object.FindFirstObjectByType<PinRack>();
            m_Pins = m_Rack.GetComponentsInChildren<Pin>();
            m_Ball = GameObject.Find("Ball").GetComponent<Rigidbody>();
        }

        void Throw(Vector3 position, Vector3 velocity)
        {
            m_Ball.position = position;
            m_Ball.linearVelocity = velocity;
            m_Ball.angularVelocity = Vector3.zero;
        }

        [UnityTest]
        public IEnumerator NewGame_HasTenUprightMotionlessPins()
        {
            yield return new WaitForSeconds(1.5f);

            Assert.That(m_Pins.Length, Is.EqualTo(PinLayout.PinCount));
            foreach (var pin in m_Pins)
            {
                Assert.That(pin.TiltDegrees, Is.LessThan(1f), pin.name);
                Assert.That(pin.LinearSpeed, Is.LessThan(0.01f), pin.name);
                Assert.That(pin.Displacement, Is.LessThan(0.01f), pin.name);
            }

            Assert.That(m_Rack.CountFallen(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FastBall_DoesNotPassThroughHeadPin()
        {
            var head = m_Pins[0];
            // Aim straight at the head pin at 25 m/s (faster than any realistic throw).
            Throw(new Vector3(0f, 0.12f, BowlingDimensions.LaneLength - 3f), new Vector3(0f, 0f, 25f));

            yield return new WaitForSeconds(1.0f);

            Assert.That(head.Displacement > 0.05f || head.TiltDegrees > 10f, Is.True,
                "The ball should have hit the head pin instead of tunnelling through it.");
        }

        [UnityTest]
        public IEnumerator Throw_IsJudgedOnce_AfterPinsSettle()
        {
            var judgements = 0;
            var fallen = -1;
            m_Rack.Judged += count => { judgements++; fallen = count; };

            Throw(new Vector3(0.02f, 0.112f, 0.3f), new Vector3(0f, 0f, 8f));

            // Judging starts when the ball reaches the pin deck, as in the real game flow.
            var waited = 0f;
            while (m_Ball.position.z < BowlingDimensions.LaneLength - 0.3f && waited < 10f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            m_Rack.BeginJudging();

            while (judgements == 0 && waited < 35f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(judgements, Is.EqualTo(1), "The throw should be judged once.");
            Assert.That(fallen, Is.InRange(1, 10), "A centered 8 m/s throw should knock down at least one pin.");
            Assert.That(m_Rack.LastJudgementTimedOut, Is.False,
                "Pins should come to rest well before the maximum wait; a timeout means they never settle.");
            Assert.That(m_Rack.AllPinsAtRest(), Is.True, "Judgement should only happen after the pins stopped.");

            yield return new WaitForSeconds(2f);
            Assert.That(judgements, Is.EqualTo(1), "A judged throw must not be judged again.");
        }


        [UnityTest]
        public IEnumerator CountFallen_ReturnsExactNumberOfKnockedDownPins()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(0));

            // Lay three pins on their side, leave the rest standing.
            for (var i = 0; i < 3; i++)
            {
                var body = m_Pins[i].GetComponent<Rigidbody>();
                body.rotation = Quaternion.Euler(0f, 0f, 90f);
                body.position += Vector3.up * 0.06f;
            }

            yield return new WaitForFixedUpdate();
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(3));

            // A pin shoved far from its spot while still upright also counts.
            var shoved = m_Pins[5].GetComponent<Rigidbody>();
            shoved.position += new Vector3(0f, 0f, 0.6f);
            yield return new WaitForFixedUpdate();
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(4));

            // A pin in the pit counts even if it stands upright.
            var pit = Object.FindFirstObjectByType<PitZone>();
            var inPit = m_Pins[7].GetComponent<Rigidbody>();
            inPit.position = pit.transform.position;
            yield return new WaitForFixedUpdate();
            Assert.That(m_Pins[7].InPit, Is.True);
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator BetweenBalls_FallenPinsAreSweptAway_AndStandingOnesAreSetBackUp()
        {
            yield return new WaitForSeconds(0.5f);
            for (var i = 0; i < 3; i++)
            {
                var body = m_Pins[i].GetComponent<Rigidbody>();
                body.rotation = Quaternion.Euler(0f, 0f, 90f);
                body.position += Vector3.up * 0.06f;
            }
            // One standing pin got nudged and leans, but is still standing.
            var leaning = m_Pins[6].GetComponent<Rigidbody>();
            leaning.rotation = Quaternion.Euler(0f, 0f, 12f);
            yield return new WaitForFixedUpdate();
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(3));

            m_Rack.ClearFallenAndRespot();
            yield return new WaitForSeconds(0.5f);

            Assert.That(m_Rack.ActiveCount, Is.EqualTo(7), "The three fallen pins are off the deck.");
            for (var i = 0; i < 3; i++) Assert.That(m_Pins[i].gameObject.activeSelf, Is.False, m_Pins[i].name);
            Assert.That(m_Rack.CountFallen(), Is.EqualTo(0), "Swept-away pins are not counted again.");
            for (var i = 3; i < m_Pins.Length; i++)
            {
                Assert.That(m_Pins[i].TiltDegrees, Is.LessThan(1f), m_Pins[i].name + " stands up straight again.");
                Assert.That(m_Pins[i].Displacement, Is.LessThan(0.01f), m_Pins[i].name + " is back on its spot.");
            }

            m_Rack.ResetAll();
            yield return new WaitForSeconds(0.5f);
            Assert.That(m_Rack.ActiveCount, Is.EqualTo(10), "A fresh rack brings every pin back.");
        }

        [UnityTest]
        public IEnumerator AfterSweeping_OnlyTheStandingPinsAreJudged()
        {
            yield return new WaitForSeconds(0.5f);
            for (var i = 0; i < 4; i++)
            {
                var body = m_Pins[i].GetComponent<Rigidbody>();
                body.rotation = Quaternion.Euler(0f, 0f, 90f);
                body.position += Vector3.up * 0.06f;
            }
            yield return new WaitForFixedUpdate();
            m_Rack.ClearFallenAndRespot();
            yield return new WaitForSeconds(0.3f);

            var judged = -1;
            m_Rack.Judged += count => judged = count;
            var knocked = m_Pins[8].GetComponent<Rigidbody>();
            knocked.rotation = Quaternion.Euler(0f, 0f, 90f);
            knocked.position += Vector3.up * 0.06f;
            m_Rack.BeginJudging();
            var waited = 0f;
            while (judged < 0 && waited < 15f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // The laid pin may take a neighbour down with it, so allow a little more than one; what must not happen is the
            // four swept-away pins being counted again (that would give five or more).
            Assert.That(judged, Is.InRange(1, 3), "Only pins that fell on this ball count, not the four from before.");
        }

        [UnityTest]
        public IEnumerator PinsThatNeverStop_AreJudgedOnceAfterMaxWait()
        {
            m_Rack.SetSettings(new PinJudgeSettings { maxWaitSeconds = 2f, restDuration = 1.5f });
            var judgements = 0;
            m_Rack.Judged += _ => judgements++;
            var spinner = m_Pins[0].GetComponent<Rigidbody>();

            m_Rack.BeginJudging();
            var elapsed = 0f;
            while (elapsed < 4f)
            {
                spinner.angularVelocity = new Vector3(0f, 8f, 0f); // keeps this pin moving the whole time
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(judgements, Is.EqualTo(1), "Judged exactly once even though a pin never rests.");
            Assert.That(m_Rack.LastJudgementTimedOut, Is.True);
        }

        [UnityTest]
        public IEnumerator ResetAll_RestoresAllTenPinsToTheirSpots()
        {
            Throw(new Vector3(0.02f, 0.112f, 0.3f), new Vector3(0f, 0f, 9f));
            yield return new WaitForSeconds(6f);
            Assert.That(m_Rack.CountFallen(), Is.GreaterThan(0), "Precondition: the throw knocked something down.");

            m_Rack.ResetAll();
            yield return new WaitForSeconds(1f);

            Assert.That(m_Rack.CountFallen(), Is.EqualTo(0));
            foreach (var pin in m_Pins)
            {
                Assert.That(pin.TiltDegrees, Is.LessThan(1f), pin.name);
                Assert.That(pin.Displacement, Is.LessThan(0.01f), pin.name);
                Assert.That(pin.LinearSpeed, Is.LessThan(0.01f), pin.name);
                Assert.That(pin.InPit, Is.False, pin.name);
            }
        }
    }
}
