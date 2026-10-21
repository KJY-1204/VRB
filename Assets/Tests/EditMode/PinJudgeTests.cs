using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class PinJudgeTests
    {
        PinJudgeSettings m_Settings;

        [SetUp]
        public void SetUp() => m_Settings = new PinJudgeSettings();

        [Test]
        public void LyingPin_IsFallen()
        {
            Assert.That(PinJudge.IsFallen(90f, false, m_Settings), Is.True);
        }

        [Test]
        public void LeaningPinWithinThreshold_IsStanding()
        {
            Assert.That(PinJudge.IsFallen(20f, false, m_Settings), Is.False);
        }

        [Test]
        public void TiltExactlyAtThreshold_IsStillStanding()
        {
            Assert.That(PinJudge.IsFallen(m_Settings.tiltThresholdDegrees, false, m_Settings), Is.False);
        }

        [Test]
        public void PinOffTheDeck_IsFallenEvenIfUpright()
        {
            Assert.That(PinJudge.IsFallen(0f, true, m_Settings), Is.True);
        }

        [Test]
        public void StandingPinShovedAlongTheDeck_StillStands()
        {
            // A pin the ball pushed 40 cm sideways and 60 cm back but left standing is not a fallen pin.
            Assert.That(PinJudge.IsOffDeck(0.4f, 0f, false, m_Settings), Is.False);
            Assert.That(PinJudge.IsFallen(5f, false, m_Settings), Is.False);
        }

        [Test]
        public void PinInThePit_IsOffTheDeck()
        {
            Assert.That(PinJudge.IsOffDeck(0f, 0f, true, m_Settings), Is.True);
        }

        [Test]
        public void PinPastTheLaneEdge_IsOffTheDeck()
        {
            Assert.That(PinJudge.IsOffDeck(m_Settings.deckHalfWidth + 0.01f, 0f, false, m_Settings), Is.True);
            Assert.That(PinJudge.IsOffDeck(-(m_Settings.deckHalfWidth + 0.01f), 0f, false, m_Settings), Is.True, "Either side.");
        }

        [Test]
        public void PinStandingAtTheEdgeOfTheLane_IsStillOnTheDeck()
        {
            Assert.That(PinJudge.IsOffDeck(m_Settings.deckHalfWidth - 0.01f, 0f, false, m_Settings), Is.False);
        }

        [Test]
        public void PinInTheGutter_IsOffTheDeck()
        {
            Assert.That(PinJudge.IsOffDeck(0.4f, -0.12f, false, m_Settings), Is.True, "Down in the gutter, even if it stands.");
        }

        [Test]
        public void ALyingPinOnTheLane_IsOnTheDeckButStillFallen()
        {
            Assert.That(PinJudge.IsOffDeck(0.2f, 0.06f, false, m_Settings), Is.False);
            Assert.That(PinJudge.IsFallen(90f, false, m_Settings), Is.True);
        }

        [Test]
        public void IsAtRest_RequiresBothSpeedsBelowThreshold()
        {
            Assert.That(PinJudge.IsAtRest(0.01f, 0.1f, m_Settings), Is.True);
            Assert.That(PinJudge.IsAtRest(0.5f, 0.1f, m_Settings), Is.False);
            Assert.That(PinJudge.IsAtRest(0.01f, 3f, m_Settings), Is.False);
        }
    }

    public class SettleTrackerTests
    {
        PinJudgeSettings m_Settings;
        SettleTracker m_Tracker;

        [SetUp]
        public void SetUp()
        {
            m_Settings = new PinJudgeSettings { restDuration = 1.5f, maxWaitSeconds = 10f };
            m_Tracker = new SettleTracker(m_Settings);
        }

        [Test]
        public void SettlesOnce_AfterRestingLongEnough()
        {
            var settledCount = 0;
            for (var i = 0; i < 40; i++)
                if (m_Tracker.Update(0.1f, true)) settledCount++;

            Assert.That(settledCount, Is.EqualTo(1));
            Assert.That(m_Tracker.IsSettled, Is.True);
            Assert.That(m_Tracker.TimedOut, Is.False);
        }

        [Test]
        public void Movement_RestartsTheRestTimer()
        {
            for (var i = 0; i < 10; i++) m_Tracker.Update(0.1f, true); // 1.0 s of rest
            m_Tracker.Update(0.1f, false);                              // a pin moves again

            var settledEarly = false;
            for (var i = 0; i < 10; i++) settledEarly |= m_Tracker.Update(0.1f, true); // 1.0 s more

            Assert.That(settledEarly, Is.False);
            Assert.That(m_Tracker.Update(0.6f, true), Is.True);
        }

        [Test]
        public void KeepsMoving_JudgesAfterMaxWait_ExactlyOnce()
        {
            var settledCount = 0;
            for (var i = 0; i < 200; i++)
                if (m_Tracker.Update(0.1f, false)) settledCount++;

            Assert.That(settledCount, Is.EqualTo(1));
            Assert.That(m_Tracker.TimedOut, Is.True);
        }

        [Test]
        public void Reset_AllowsAnotherThrowToBeJudged()
        {
            for (var i = 0; i < 20; i++) m_Tracker.Update(0.1f, true);
            Assert.That(m_Tracker.IsSettled, Is.True);

            m_Tracker.Reset();

            Assert.That(m_Tracker.IsSettled, Is.False);
            var settled = false;
            for (var i = 0; i < 20; i++) settled |= m_Tracker.Update(0.1f, true);
            Assert.That(settled, Is.True);
        }
    }
}
