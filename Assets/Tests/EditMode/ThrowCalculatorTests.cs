using System.Collections.Generic;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class ThrowCalculatorTests
    {
        static readonly Vec3 Lane = Vec3.Forward;
        ThrowAssistSettings m_Settings;

        [SetUp]
        public void SetUp() => m_Settings = new ThrowAssistSettings();

        /// <summary>Hand moving at a constant velocity, sampled every dt seconds for the given duration.</summary>
        static List<MotionSample> Steady(Vec3 velocity, float duration = 0.3f, float dt = 0.01f)
        {
            var list = new List<MotionSample>();
            for (var t = 0f; t <= duration + 1e-5f; t += dt)
                list.Add(new MotionSample(t, velocity * t));
            return list;
        }

        static void AssertVec(Vec3 actual, Vec3 expected, float tolerance = 1e-3f)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(tolerance), "x");
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tolerance), "y");
            Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(tolerance), "z");
        }

        [Test]
        public void FewerThanTwoSamples_GivesNoVelocity()
        {
            var one = new List<MotionSample> { new MotionSample(0f, Vec3.Zero) };

            AssertVec(ThrowCalculator.ReleaseVelocity(one, ThrowAssistLevel.Weak, m_Settings, Lane), Vec3.Zero);
            AssertVec(ThrowCalculator.ReleaseVelocity(new List<MotionSample>(), ThrowAssistLevel.RawPhysics, m_Settings, Lane), Vec3.Zero);
        }

        [Test]
        public void RawPhysics_UsesVelocityAtTheInstantOfRelease()
        {
            var samples = Steady(new Vec3(0f, 0f, 5f));
            // A sudden spike in the very last interval (a flick of the wrist).
            var last = samples[samples.Count - 1];
            samples.Add(new MotionSample(last.Time + 0.01f, last.Position + new Vec3(0f, 0f, 0.10f)));

            var v = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.RawPhysics, m_Settings, Lane);

            AssertVec(v, new Vec3(0f, 0f, 10f), 0.01f);
        }

        [Test]
        public void Weak_AveragesOverTheWindow_SoSpikesAreSmoothed()
        {
            var samples = Steady(new Vec3(0f, 0f, 5f));
            var last = samples[samples.Count - 1];
            samples.Add(new MotionSample(last.Time + 0.01f, last.Position + new Vec3(0f, 0f, 0.10f)));

            var weak = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Weak, m_Settings, Lane);

            Assert.That(weak.Z, Is.GreaterThan(5f));
            Assert.That(weak.Z, Is.LessThan(7f), "The 10 m/s spike should be mostly averaged away.");
        }

        [Test]
        public void Weak_DoesNotChangeDirection()
        {
            var samples = Steady(new Vec3(1f, 0f, 4f));

            var v = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Weak, m_Settings, Lane);

            AssertVec(v, new Vec3(1f, 0f, 4f), 0.01f);
        }

        [Test]
        public void Window_IgnoresMotionOlderThanTheAveragingWindow()
        {
            // First 0.2 s of backswing in the opposite direction, then the final 0.1 s forward at 6 m/s.
            var samples = new List<MotionSample>();
            var position = Vec3.Zero;
            for (var i = 0; i <= 30; i++)
            {
                var t = i * 0.01f;
                var velocity = t < 0.2f ? new Vec3(0f, 0f, -3f) : new Vec3(0f, 0f, 6f);
                if (i > 0) position = position + velocity * 0.01f;
                samples.Add(new MotionSample(t, position));
            }

            var v = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Weak, m_Settings, Lane);

            Assert.That(v.Z, Is.EqualTo(6f).Within(0.3f));
        }

        [Test]
        public void Medium_PullsTheDirectionTowardTheLane()
        {
            var samples = Steady(new Vec3(2f, 0f, 4f));

            var raw = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Weak, m_Settings, Lane);
            var medium = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Medium, m_Settings, Lane);

            Assert.That(Vec3.AngleDegrees(medium, Lane), Is.LessThan(Vec3.AngleDegrees(raw, Lane)));
        }

        [Test]
        public void Strong_IsCloserToTheLaneThanMedium()
        {
            var samples = Steady(new Vec3(2f, 0f, 4f));

            var medium = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Medium, m_Settings, Lane);
            var strong = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Strong, m_Settings, Lane);

            Assert.That(Vec3.AngleDegrees(strong, Lane), Is.LessThan(Vec3.AngleDegrees(medium, Lane)));
        }

        [TestCase(ThrowAssistLevel.Weak)]
        [TestCase(ThrowAssistLevel.Medium)]
        [TestCase(ThrowAssistLevel.Strong)]
        public void Correction_NeverIncreasesSpeed(ThrowAssistLevel level)
        {
            var samples = Steady(new Vec3(3f, 0.5f, 5f));
            var average = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Weak, m_Settings, Lane);

            var corrected = ThrowCalculator.ReleaseVelocity(samples, level, m_Settings, Lane);

            Assert.That(corrected.Magnitude, Is.LessThanOrEqualTo(average.Magnitude + 1e-3f));
        }

        [Test]
        public void CorrectedLevels_CapUpwardSpeed_ButRawPhysicsDoesNot()
        {
            var samples = Steady(new Vec3(0f, 4f, 5f));

            var raw = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.RawPhysics, m_Settings, Lane);
            var weak = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Weak, m_Settings, Lane);

            Assert.That(raw.Y, Is.EqualTo(4f).Within(0.01f));
            Assert.That(weak.Y, Is.LessThanOrEqualTo(m_Settings.maxUpwardSpeed + 1e-3f));
        }

        [Test]
        public void ThrowingAwayFromTheLane_IsNotPulledAround()
        {
            var samples = Steady(new Vec3(0f, 0f, -4f));

            var strong = ThrowCalculator.ReleaseVelocity(samples, ThrowAssistLevel.Strong, m_Settings, Lane);

            AssertVec(strong, new Vec3(0f, 0f, -4f), 0.01f);
        }

        [Test]
        public void HandAtRest_GivesAlmostNoSpeed()
        {
            var samples = Steady(Vec3.Zero);

            foreach (ThrowAssistLevel level in System.Enum.GetValues(typeof(ThrowAssistLevel)))
                Assert.That(ThrowCalculator.ReleaseVelocity(samples, level, m_Settings, Lane).Magnitude,
                    Is.LessThan(0.01f), level.ToString());
        }

        [Test]
        public void PullFor_MatchesLevelOrdering()
        {
            Assert.That(m_Settings.PullFor(ThrowAssistLevel.RawPhysics), Is.EqualTo(0f));
            Assert.That(m_Settings.PullFor(ThrowAssistLevel.Weak), Is.EqualTo(0f));
            Assert.That(m_Settings.PullFor(ThrowAssistLevel.Medium), Is.GreaterThan(m_Settings.PullFor(ThrowAssistLevel.Weak)));
            Assert.That(m_Settings.PullFor(ThrowAssistLevel.Strong), Is.GreaterThan(m_Settings.PullFor(ThrowAssistLevel.Medium)));
        }
    }
}
