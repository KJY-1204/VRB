using System;

namespace VRB.Core
{
    /// <summary>Tunable values for the four throw correction levels.</summary>
    [Serializable]
    public sealed class ThrowAssistSettings
    {
        /// <summary>Release velocity is averaged over this many seconds before release (all levels except raw physics).</summary>
        public float averagingWindowSeconds = 0.1f;

        /// <summary>How much of the throw direction is pulled toward the lane (0 = none, 1 = fully along the lane).</summary>
        public float weakPull = 0f;
        public float mediumPull = 0.3f;
        public float strongPull = 0.6f;

        /// <summary>Upward release speed is capped to this (m/s) for corrected levels so the ball does not leap off the lane.</summary>
        public float maxUpwardSpeed = 1f;

        /// <summary>Throws slower than this (m/s) are left alone by the lane pull.</summary>
        public float minSpeedForPull = 0.05f;

        public float PullFor(ThrowAssistLevel level)
        {
            switch (level)
            {
                case ThrowAssistLevel.RawPhysics: return 0f;
                case ThrowAssistLevel.Weak: return weakPull;
                case ThrowAssistLevel.Medium: return mediumPull;
                case ThrowAssistLevel.Strong: return strongPull;
                default: throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
