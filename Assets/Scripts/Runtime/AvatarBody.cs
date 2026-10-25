using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Works out where the body of another player is from just their head and shoulders' worth of information: the
    /// torso hangs below the head, the arms reach from the shoulders to the hands, and a head that is low (the player is
    /// sitting on a chair) puts the legs out in front instead of down to the floor. Everything is in the lane's frame.
    /// </summary>
    public static class AvatarBody
    {
        /// <summary>A head lower than this means the player is sitting (a standing eye height is 1.4 m or more).</summary>
        public const float SeatedHeadHeight = 1.2f;

        public struct Frame
        {
            public bool Seated;
            public Vector3 Neck;
            public Vector3 Hip;
            public Vector3 LeftShoulder;
            public Vector3 RightShoulder;
            public Vector3 LegsCenter;
            public Vector3 LegsSize;
            public Quaternion LegsRotation;
        }

        public static Frame Solve(Vector3 head, Quaternion headRotation)
        {
            // The body turns with where the head looks sideways, not with nodding or tilting it.
            var forward = Vector3.ProjectOnPlane(headRotation * Vector3.forward, Vector3.up);
            forward = forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
            var yaw = Quaternion.LookRotation(forward, Vector3.up);
            var right = yaw * Vector3.right;

            var frame = new Frame { Seated = head.y < SeatedHeadHeight, LegsRotation = yaw };
            frame.Neck = head + Vector3.down * 0.14f - forward * 0.03f;
            var hipY = Mathf.Max(0.3f, head.y - (frame.Seated ? 0.5f : 0.62f));
            frame.Hip = new Vector3(head.x, hipY, head.z) - forward * 0.05f;

            var shoulder = frame.Neck + Vector3.down * 0.06f;
            frame.LeftShoulder = shoulder - right * 0.19f;
            frame.RightShoulder = shoulder + right * 0.19f;

            if (frame.Seated)
            {
                frame.LegsSize = new Vector3(0.28f, 0.14f, 0.42f);
                frame.LegsCenter = frame.Hip + forward * 0.21f + Vector3.down * 0.07f;
            }
            else
            {
                frame.LegsSize = new Vector3(0.28f, frame.Hip.y, 0.16f);
                frame.LegsCenter = new Vector3(frame.Hip.x, frame.Hip.y / 2f, frame.Hip.z);
            }

            return frame;
        }
    }
}
