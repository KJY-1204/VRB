using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Where a player's head and two hands are at one moment, in the lane's own frame like <see cref="LaneSnapshot"/>.
    /// A device sends this about its player so that others can draw them at their lane.
    /// </summary>
    public sealed class AvatarSnapshot
    {
        public const int Head = 0;
        public const int LeftHand = 1;
        public const int RightHand = 2;
        public const int PartCount = 3;

        public readonly Vector3[] Positions = new Vector3[PartCount];
        public readonly Quaternion[] Rotations = new Quaternion[PartCount];

        public void CopyFrom(AvatarSnapshot other)
        {
            System.Array.Copy(other.Positions, Positions, PartCount);
            System.Array.Copy(other.Rotations, Rotations, PartCount);
        }

        /// <summary>True when a part moved enough to be worth sending.</summary>
        public bool Differs(AvatarSnapshot other, float distance = 0.005f, float angleDegrees = 2f)
        {
            for (var i = 0; i < PartCount; i++)
            {
                if ((Positions[i] - other.Positions[i]).sqrMagnitude > distance * distance) return true;
                if (Quaternion.Angle(Rotations[i], other.Rotations[i]) > angleDegrees) return true;
            }

            return false;
        }

        /// <summary>Takes what arrived over the network; false (and nothing changed) if it is not the right shape.</summary>
        public bool TrySet(Vector3[] positions, Quaternion[] rotations)
        {
            if (positions == null || rotations == null || positions.Length != PartCount || rotations.Length != PartCount)
                return false;

            System.Array.Copy(positions, Positions, PartCount);
            System.Array.Copy(rotations, Rotations, PartCount);
            return true;
        }
    }
}
