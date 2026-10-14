using System;

namespace VRB.Core
{
    /// <summary>Tunable thresholds for deciding when pins have settled and which ones count as fallen.</summary>
    [Serializable]
    public sealed class PinJudgeSettings
    {
        /// <summary>A pin tilted more than this many degrees from upright counts as fallen.</summary>
        public float tiltThresholdDegrees = 45f;

        /// <summary>A pin that ended up farther than this many meters from its original spot counts as fallen.</summary>
        public float displacementThreshold = 0.35f;

        /// <summary>A pin is at rest when it moves slower than this (m/s).</summary>
        public float restLinearSpeed = 0.1f;

        /// <summary>A pin is at rest when it spins slower than this (rad/s).</summary>
        public float restAngularSpeed = 1.0f;

        /// <summary>All pins must stay at rest for this many seconds before the throw is judged.</summary>
        public float restDuration = 1.5f;

        /// <summary>Judge anyway after this many seconds, even if some pins still move.</summary>
        public float maxWaitSeconds = 10f;
    }
}
