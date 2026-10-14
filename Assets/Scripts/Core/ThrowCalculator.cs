using System;
using System.Collections.Generic;

namespace VRB.Core
{
    /// <summary>A hand position at a moment in time.</summary>
    public readonly struct MotionSample
    {
        public float Time { get; }
        public Vec3 Position { get; }

        public MotionSample(float time, Vec3 position)
        {
            Time = time;
            Position = position;
        }
    }

    /// <summary>Turns recent hand motion into the ball's release velocity for the chosen correction level.</summary>
    public static class ThrowCalculator
    {
        /// <param name="samples">Hand positions in time order, ending at the moment of release.</param>
        /// <param name="laneForward">Unit vector pointing down the lane.</param>
        public static Vec3 ReleaseVelocity(IReadOnlyList<MotionSample> samples, ThrowAssistLevel level,
            ThrowAssistSettings settings, Vec3 laneForward)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (samples.Count < 2) return Vec3.Zero;

            if (level == ThrowAssistLevel.RawPhysics)
                return InstantVelocity(samples);

            var velocity = AverageVelocity(samples, settings.averagingWindowSeconds);

            if (velocity.Y > settings.maxUpwardSpeed)
                velocity = new Vec3(velocity.X, settings.maxUpwardSpeed, velocity.Z);

            return PullTowardLane(velocity, settings.PullFor(level), settings.minSpeedForPull, laneForward);
        }

        /// <summary>Velocity over the final sample interval only: what the hand was doing at the instant of release.</summary>
        static Vec3 InstantVelocity(IReadOnlyList<MotionSample> samples)
        {
            var last = samples[samples.Count - 1];
            var previous = samples[samples.Count - 2];
            return Between(previous, last);
        }

        /// <summary>Mean velocity over the last <paramref name="windowSeconds"/> before release.</summary>
        static Vec3 AverageVelocity(IReadOnlyList<MotionSample> samples, float windowSeconds)
        {
            var last = samples[samples.Count - 1];
            var start = samples[samples.Count - 2];

            for (var i = samples.Count - 3; i >= 0; i--)
            {
                if (last.Time - samples[i].Time > windowSeconds) break;
                start = samples[i];
            }

            return Between(start, last);
        }

        static Vec3 Between(MotionSample from, MotionSample to)
        {
            var dt = to.Time - from.Time;
            return dt > 1e-6f ? (to.Position - from.Position) / dt : Vec3.Zero;
        }

        /// <summary>Rotates the direction toward the lane while keeping the speed, so a pull never adds speed.</summary>
        static Vec3 PullTowardLane(Vec3 velocity, float pull, float minSpeed, Vec3 laneForward)
        {
            var speed = velocity.Magnitude;
            if (pull <= 0f || speed < minSpeed) return velocity;

            var direction = velocity / speed;
            var forward = laneForward.Normalized;

            // Throwing away from the lane is not something to "help" with.
            if (Vec3.Dot(direction, forward) <= 0f) return velocity;

            var blended = (direction * (1f - pull) + forward * pull).Normalized;
            return blended * speed;
        }
    }
}
