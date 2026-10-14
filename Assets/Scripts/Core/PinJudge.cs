using System;

namespace VRB.Core
{
    public static class PinJudge
    {
        /// <summary>
        /// A pin is fallen when it is tilted past the threshold, has moved far from its original spot,
        /// or has dropped into the pit.
        /// </summary>
        public static bool IsFallen(float tiltDegrees, float displacement, bool inPit, PinJudgeSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return inPit
                || tiltDegrees > settings.tiltThresholdDegrees
                || displacement > settings.displacementThreshold;
        }

        /// <summary>True when a pin moves slowly enough to be considered at rest.</summary>
        public static bool IsAtRest(float linearSpeed, float angularSpeed, PinJudgeSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return linearSpeed < settings.restLinearSpeed && angularSpeed < settings.restAngularSpeed;
        }
    }
}
