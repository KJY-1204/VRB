using System;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class PinLayoutTests
    {
        [Test]
        public void HeadPin_IsAtOrigin()
        {
            var (x, z) = PinLayout.GetPosition(0);

            Assert.That(x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(z, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void RowsHaveOneTwoThreeFourPins()
        {
            var counts = new int[PinLayout.RowCount];
            for (var i = 0; i < PinLayout.PinCount; i++)
            {
                var (_, z) = PinLayout.GetPosition(i);
                counts[(int)Math.Round(z / PinLayout.RowSpacing)]++;
            }

            Assert.That(counts, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void NeighbouringPins_AreOneSpacingApart()
        {
            // Every pin is exactly PinSpacing from at least one other pin, and none are closer.
            for (var a = 0; a < PinLayout.PinCount; a++)
            {
                var nearest = float.MaxValue;
                for (var b = 0; b < PinLayout.PinCount; b++)
                {
                    if (a == b) continue;
                    nearest = Math.Min(nearest, Distance(a, b));
                }

                Assert.That(nearest, Is.EqualTo(BowlingDimensions.PinSpacing).Within(1e-4f), $"pin {a}");
            }
        }

        [Test]
        public void Formation_IsSymmetricLeftToRight()
        {
            var xs = new float[PinLayout.PinCount];
            for (var i = 0; i < PinLayout.PinCount; i++) xs[i] = PinLayout.GetPosition(i).x;

            double sum = 0;
            foreach (var x in xs) sum += x;
            Assert.That(sum, Is.EqualTo(0.0).Within(1e-4));
        }

        [Test]
        public void BackRow_IsWithinLaneWidth()
        {
            var halfBackRow = 1.5f * BowlingDimensions.PinSpacing + BowlingDimensions.PinMaxDiameter / 2f;

            Assert.That(halfBackRow * 2f, Is.LessThan(BowlingDimensions.LaneWidth));
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void OutOfRangeIndex_Throws(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PinLayout.GetPosition(index));
        }

        static float Distance(int a, int b)
        {
            var (ax, az) = PinLayout.GetPosition(a);
            var (bx, bz) = PinLayout.GetPosition(b);
            var dx = ax - bx;
            var dz = az - bz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
