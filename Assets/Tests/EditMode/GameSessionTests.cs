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

            foreach (var pins in new[] { 3, 9, 0, 10 })
            {
                a.RecordThrow(pins);
                b.RecordThrow(pins);
            }

            Assert.That(a.TotalPins, Is.EqualTo(b.TotalPins));
            Assert.That(a.ThrowCount, Is.EqualTo(b.ThrowCount));
        }
    }
}
