using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class ThrowWatcherTests
    {
        const float DeckZ = 18f;

        [Test]
        public void BallRollingTowardThePins_HasNotArrivedYet()
        {
            var watcher = new ThrowWatcher(DeckZ);

            for (var i = 0; i < 30; i++)
                Assert.That(watcher.Update(0.1f, 2f + i * 0.4f, 8f), Is.False);

            Assert.That(watcher.HasArrived, Is.False);
        }

        [Test]
        public void BallReachingThePinDeck_Arrives_ExactlyOnce()
        {
            var watcher = new ThrowWatcher(DeckZ);

            Assert.That(watcher.Update(0.1f, 17.9f, 8f), Is.False);
            Assert.That(watcher.Update(0.1f, 18.1f, 8f), Is.True);
            Assert.That(watcher.Update(0.1f, 19f, 8f), Is.False, "Reported only once.");
            Assert.That(watcher.HasArrived, Is.True);
        }

        [Test]
        public void BallStoppingShortOfThePins_Arrives_AfterBeingStillLongEnough()
        {
            var watcher = new ThrowWatcher(DeckZ);
            var arrived = false;

            for (var i = 0; i < 9; i++) arrived |= watcher.Update(0.1f, 5f, 0.01f);
            Assert.That(arrived, Is.False, "0.9 s of stillness is not enough yet.");

            Assert.That(watcher.Update(0.2f, 5f, 0.01f), Is.True);
        }

        [Test]
        public void BriefStop_DoesNotCount_IfTheBallMovesAgain()
        {
            var watcher = new ThrowWatcher(DeckZ);

            for (var i = 0; i < 8; i++) watcher.Update(0.1f, 5f, 0.01f); // 0.8 s still
            watcher.Update(0.1f, 5.1f, 2f);                              // moves again
            for (var i = 0; i < 8; i++)
                Assert.That(watcher.Update(0.1f, 5.1f, 0.01f), Is.False);
        }

        [Test]
        public void Timeout_ArrivesEvenIfTheBallKeepsCreeping()
        {
            var watcher = new ThrowWatcher(DeckZ, timeoutSeconds: 5f);
            var arrived = false;

            for (var i = 0; i < 60; i++) arrived |= watcher.Update(0.1f, 3f, 0.5f);

            Assert.That(arrived, Is.True);
        }

        [Test]
        public void Reset_AllowsTheNextThrowToBeWatched()
        {
            var watcher = new ThrowWatcher(DeckZ);
            watcher.Update(0.1f, 19f, 8f);

            watcher.Reset();

            Assert.That(watcher.HasArrived, Is.False);
            Assert.That(watcher.Update(0.1f, 19f, 8f), Is.True);
        }
    }
}
