using System;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void NewSession_IsEmpty()
        {
            var session = new GameSession();

            Assert.That(session.ThrowCount, Is.EqualTo(0));
            Assert.That(session.TotalPins, Is.EqualTo(0));
        }

        [Test]
        public void RecordThrow_KeepsOrderAndTotals()
        {
            var session = new GameSession();

            session.RecordThrow(7);
            session.RecordThrow(0);
            session.RecordThrow(10);

            Assert.That(session.ThrowCount, Is.EqualTo(3));
            Assert.That(session.TotalPins, Is.EqualTo(17));
            Assert.That(session.Throws[0].Order, Is.EqualTo(1));
            Assert.That(session.Throws[0].PinsFallen, Is.EqualTo(7));
            Assert.That(session.Throws[1].PinsFallen, Is.EqualTo(0));
            Assert.That(session.Throws[2].Order, Is.EqualTo(3));
            Assert.That(session.Throws[2].PinsFallen, Is.EqualTo(10));
        }

        [Test]
        public void TheScore_FollowsTheScoringRules()
        {
            var session = new GameSession();

            session.RecordThrow(10);
            session.RecordThrow(7);
            session.RecordThrow(3);

            Assert.That(session.Score, Is.EqualTo(30), "X counts 10 + 7 + 3 = 20, and the 7/ frame counts 10 so far.");
            Assert.That(session.TotalPins, Is.EqualTo(20));
        }

        [Test]
        public void ASecondThrow_CannotKnockDownMoreThanWasLeft()
        {
            var session = new GameSession();
            session.RecordThrow(7);

            Assert.Throws<ArgumentOutOfRangeException>(() => session.RecordThrow(4));
            Assert.That(session.ThrowCount, Is.EqualTo(1));
            Assert.That(session.TotalPins, Is.EqualTo(7));
        }

        [Test]
        public void Reset_ClearsHistoryAndTotal()
        {
            var session = new GameSession();
            session.RecordThrow(7);
            session.RecordThrow(3);

            session.Reset();

            Assert.That(session.ThrowCount, Is.EqualTo(0));
            Assert.That(session.TotalPins, Is.EqualTo(0));
        }

        [Test]
        public void AfterReset_OrderStartsAtOneAgain()
        {
            var session = new GameSession();
            session.RecordThrow(4);
            session.Reset();

            var record = session.RecordThrow(5);

            Assert.That(record.Order, Is.EqualTo(1));
        }

        [TestCase(-1)]
        [TestCase(11)]
        public void RecordThrow_RejectsOutOfRangeCounts(int pins)
        {
            var session = new GameSession();

            Assert.Throws<ArgumentOutOfRangeException>(() => session.RecordThrow(pins));
            Assert.That(session.ThrowCount, Is.EqualTo(0));
        }

        [Test]
        public void SameInputs_ProduceSameResults_WithoutAnyEngineInput()
        {
            var a = new GameSession();
            var b = new GameSession();

            foreach (var pins in new[] { 3, 6, 0, 10 })
            {
                a.RecordThrow(pins);
                b.RecordThrow(pins);
            }

            Assert.That(a.TotalPins, Is.EqualTo(b.TotalPins));
            Assert.That(a.ThrowCount, Is.EqualTo(b.ThrowCount));
            Assert.That(a.Score, Is.EqualTo(b.Score));
        }
    }
}
