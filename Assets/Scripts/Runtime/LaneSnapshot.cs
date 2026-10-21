using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Where the pins and the ball are on one lane at one moment, in the lane's own frame (foul line at z = 0, lane
    /// centre at x = 0). A device sends this about its own lane so that others can draw it on theirs. Bodies 0 to 9 are
    /// the pins in rack order and body 10 is the ball.
    /// </summary>
    public sealed class LaneSnapshot
    {
        public const int PinCount = 10;
        public const int BallIndex = PinCount;
        public const int BodyCount = PinCount + 1;

        public readonly Vector3[] Positions = new Vector3[BodyCount];
        public readonly Quaternion[] Rotations = new Quaternion[BodyCount];

        /// <summary>One bit per body: set when it is there to be drawn (swept-away pins are not).</summary>
        public uint ActiveMask;

        public static uint AllActive => (1u << BodyCount) - 1u;

        public bool IsActive(int body) => (ActiveMask & (1u << body)) != 0u;

        public void CopyFrom(LaneSnapshot other)
        {
            System.Array.Copy(other.Positions, Positions, BodyCount);
            System.Array.Copy(other.Rotations, Rotations, BodyCount);
            ActiveMask = other.ActiveMask;
        }

        /// <summary>True when something moved enough (or appeared or vanished) to be worth sending.</summary>
        public bool Differs(LaneSnapshot other, float distance = 0.004f, float angleDegrees = 1f)
        {
            if (ActiveMask != other.ActiveMask) return true;

            for (var i = 0; i < BodyCount; i++)
            {
                if (!IsActive(i)) continue;
                if ((Positions[i] - other.Positions[i]).sqrMagnitude > distance * distance) return true;
                if (Quaternion.Angle(Rotations[i], other.Rotations[i]) > angleDegrees) return true;
            }

            return false;
        }

        /// <summary>Takes what arrived over the network; false (and nothing changed) if it is not the right shape.</summary>
        public bool TrySet(Vector3[] positions, Quaternion[] rotations, uint activeMask)
        {
            if (positions == null || rotations == null || positions.Length != BodyCount || rotations.Length != BodyCount)
                return false;

            System.Array.Copy(positions, Positions, BodyCount);
            System.Array.Copy(rotations, Rotations, BodyCount);
            ActiveMask = activeMask & AllActive;
            return true;
        }
    }
}
