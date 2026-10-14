using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class MotionBufferTests
    {
        [Test]
        public void DropsSamplesOlderThanTheKeepWindow()
        {
            var buffer = new MotionBuffer(0.5f);

            for (var i = 0; i <= 100; i++) buffer.Add(i * 0.01f, new Vec3(0f, 0f, i));

            Assert.That(buffer.Samples[buffer.Count - 1].Time - buffer.Samples[0].Time, Is.LessThanOrEqualTo(0.5f + 1e-4f));
            Assert.That(buffer.Count, Is.LessThan(60));
        }

        [Test]
        public void AlwaysKeepsTwoSamples_EvenIfTheyAreFarApart()
        {
            var buffer = new MotionBuffer(0.1f);

            buffer.Add(0f, Vec3.Zero);
            buffer.Add(5f, new Vec3(1f, 0f, 0f));

            Assert.That(buffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void Clear_EmptiesTheBuffer()
        {
            var buffer = new MotionBuffer();
            buffer.Add(0f, Vec3.Zero);

            buffer.Clear();

            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void BufferedSamples_FeedTheCalculator()
        {
            var buffer = new MotionBuffer();
            for (var i = 0; i <= 50; i++) buffer.Add(i * 0.01f, new Vec3(0f, 0f, 5f * i * 0.01f));

            var v = ThrowCalculator.ReleaseVelocity(buffer.Samples, ThrowAssistLevel.Weak, new ThrowAssistSettings(), Vec3.Forward);

            Assert.That(v.Z, Is.EqualTo(5f).Within(0.01f));
        }
    }
}
