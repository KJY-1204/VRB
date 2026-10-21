using System;

namespace VRB.Core
{
    public static class PinJudge
    {
        /// <summary>
        /// A pin counts as down when it has been knocked over (tilted past the threshold) or has left the deck. A pin the
        /// ball merely pushed along the deck and left standing does not count, as in the real game, where the pinsetter
        /// puts standing pins back on their spots.
        /// </summary>
        public static bool IsFallen(float tiltDegrees, bool offDeck, PinJudgeSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return offDeck || tiltDegrees > settings.tiltThresholdDegrees;
        }

        /// <summary>
        /// True when a pin is no longer on the pin deck: in the pit, past either edge of the lane, or down in a gutter.
        /// </summary>
        /// <param name="lateral">Sideways distance of the pin's base from the lane center.</param>
        /// <param name="baseHeight">Height of the pin's base above the lane surface (negative in a gutter).</param>
        public static bool IsOffDeck(float lateral, float baseHeight, bool inPit, PinJudgeSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return inPit
                || Math.Abs(lateral) > settings.deckHalfWidth
                || baseHeight < settings.deckFloorHeight;
        }

        /// <summary>True when a pin moves slowly enough to be considered at rest.</summary>
        public static bool IsAtRest(float linearSpeed, float angularSpeed, PinJudgeSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return linearSpeed < settings.restLinearSpeed && angularSpeed < settings.restAngularSpeed;
        }
    }
}
