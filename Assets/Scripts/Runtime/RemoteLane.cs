using UnityEngine;

namespace VRB
{
    /// <summary>
    /// A lane in the hall where someone on another device is bowling: ten pins and a ball that only look the part. They
    /// are set to follow the pictures sent from that device (<see cref="LaneSnapshot"/>), smoothly, since those arrive
    /// about fifteen times a second.
    /// </summary>
    public class RemoteLane : MonoBehaviour
    {
        [SerializeField] Transform[] m_Bodies = new Transform[LaneSnapshot.BodyCount];
        [Tooltip("How quickly the bodies catch up with the latest picture, per second.")]
        [SerializeField] float m_Smoothing = 25f;
        [Tooltip("A body that is this far from where it should be jumps there instead of gliding (a rack being set up).")]
        [SerializeField] float m_SnapDistance = 2f;

        [Tooltip("Head, left hand and right hand of the player on this lane (see AvatarSnapshot).")]
        [SerializeField] Transform[] m_AvatarParts = new Transform[AvatarSnapshot.PartCount];

        [Header("Body (all children of the body root, which is switched with the avatar)")]
        [SerializeField] Transform m_BodyRoot;
        [SerializeField] Transform m_Torso;
        [SerializeField] Transform m_Legs;
        [SerializeField] Transform m_LeftArm;
        [SerializeField] Transform m_RightArm;
        [SerializeField] Transform m_NameTag;
        [SerializeField] TMPro.TMP_Text m_NameText;
        [SerializeField] float m_NameHeight = 0.3f;
        [Tooltip("Where the player's voice comes from; follows the head.")]
        [SerializeField] RemoteVoice m_Voice;
        [SerializeField] Color m_NameColor = Color.white;
        [SerializeField] Color m_SpeakingColor = new Color(0.45f, 1f, 0.55f);

        readonly LaneSnapshot m_Target = new LaneSnapshot();
        readonly AvatarSnapshot m_PoseTarget = new AvatarSnapshot();
        bool m_HasTarget;
        bool m_HasPose;

        /// <summary>One part of the player drawn here: <see cref="AvatarSnapshot.Head"/> and the two hands.</summary>
        public Transform AvatarPart(int part) => m_AvatarParts[part];

        public bool IsAvatarShown => m_HasPose;

        /// <summary>The body parts that are drawn from the head and hands, for tests.</summary>
        public Transform Torso => m_Torso;
        public Transform Legs => m_Legs;
        public Transform LeftArm => m_LeftArm;
        public Transform RightArm => m_RightArm;
        public Transform NameTag => m_NameTag;
        public RemoteVoice Voice => m_Voice;

        public string DisplayedName => m_NameText != null ? m_NameText.text : string.Empty;

        /// <summary>The name floating above the player's head.</summary>
        public void SetName(string playerName)
        {
            if (m_NameText != null && m_NameText.text != playerName) m_NameText.text = playerName ?? string.Empty;
        }

        /// <summary>Follow this pose of the player's head and hands. The first one (or <paramref name="jump"/>) is taken at once.</summary>
        public void SetPose(AvatarSnapshot pose, bool jump = false)
        {
            m_PoseTarget.CopyFrom(pose);
            var first = !m_HasPose;
            m_HasPose = true;
            for (var i = 0; i < AvatarSnapshot.PartCount; i++)
            {
                m_AvatarParts[i].gameObject.SetActive(true);
                if (jump || first) m_AvatarParts[i].SetLocalPositionAndRotation(m_PoseTarget.Positions[i], m_PoseTarget.Rotations[i]);
            }

            if (m_BodyRoot != null) m_BodyRoot.gameObject.SetActive(true);
            UpdateBody();
        }

        void HideAvatar()
        {
            m_HasPose = false;
            foreach (var part in m_AvatarParts) part.gameObject.SetActive(false);
            if (m_BodyRoot != null) m_BodyRoot.gameObject.SetActive(false);
        }

        // Torso, arms, legs and name tag follow the (already smoothed) head and hands.
        void UpdateBody()
        {
            if (m_BodyRoot == null) return;

            var head = m_AvatarParts[AvatarSnapshot.Head];
            var frame = AvatarBody.Solve(head.localPosition, head.localRotation);

            Stretch(m_Torso, frame.Neck, frame.Hip, 0.30f);
            Stretch(m_LeftArm, frame.LeftShoulder, m_AvatarParts[AvatarSnapshot.LeftHand].localPosition, 0.07f);
            Stretch(m_RightArm, frame.RightShoulder, m_AvatarParts[AvatarSnapshot.RightHand].localPosition, 0.07f);
            m_Legs.SetLocalPositionAndRotation(frame.LegsCenter, frame.LegsRotation);
            m_Legs.localScale = frame.LegsSize;

            // The name always faces whoever is looking.
            var tagPosition = head.localPosition + Vector3.up * m_NameHeight;
            var viewer = Camera.main;
            var away = viewer != null ? m_BodyRoot.InverseTransformDirection(tagPosition - viewer.transform.position) : Vector3.forward;
            m_NameTag.localPosition = tagPosition;
            if (m_Voice != null)
            {
                m_Voice.transform.localPosition = head.localPosition;
                if (m_NameText != null) m_NameText.color = m_Voice.IsSpeaking ? m_SpeakingColor : m_NameColor;
            }

            if (away.sqrMagnitude > 1e-4f) m_NameTag.localRotation = Quaternion.LookRotation(away, Vector3.up);
        }

        // A unit cylinder (2 m tall, 1 m wide) laid between two points.
        static void Stretch(Transform limb, Vector3 from, Vector3 to, float thickness)
        {
            var along = to - from;
            var length = along.magnitude;
            if (length < 1e-3f) return;

            limb.localPosition = (from + to) / 2f;
            limb.localRotation = Quaternion.FromToRotation(Vector3.up, along / length);
            limb.localScale = new Vector3(thickness, length / 2f, thickness);
        }

        public bool IsShown => gameObject.activeSelf;

        /// <summary>One of the stand-ins: 0 to 9 are the pins, 10 is the ball.</summary>
        public Transform Body(int index) => m_Bodies[index];

        /// <summary>Put the lane where it belongs next to the one being played (meters sideways, in the lane's frame).</summary>
        public void SetSideways(float x)
        {
            var position = transform.localPosition;
            position.x = x;
            transform.localPosition = position;
        }

        public void Show(bool show)
        {
            if (gameObject.activeSelf == show) return;

            gameObject.SetActive(show);
            if (!show)
            {
                m_HasTarget = false;
                HideAvatar();
            }
        }

        /// <summary>Follow this picture from now on. The first one a lane gets (or <paramref name="jump"/>) is taken at once.</summary>
        public void SetTarget(LaneSnapshot snapshot, bool jump = false)
        {
            m_Target.CopyFrom(snapshot);
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
                m_Bodies[i].gameObject.SetActive(m_Target.IsActive(i));

            if (jump || !m_HasTarget) Place();
            m_HasTarget = true;
        }

        void Update()
        {
            var blend = 1f - Mathf.Exp(-m_Smoothing * Time.deltaTime);
            if (m_HasPose)
            {
                for (var i = 0; i < AvatarSnapshot.PartCount; i++)
                {
                    var part = m_AvatarParts[i];
                    part.SetLocalPositionAndRotation(
                        Vector3.Lerp(part.localPosition, m_PoseTarget.Positions[i], blend),
                        Quaternion.Slerp(part.localRotation, m_PoseTarget.Rotations[i], blend));
                }

                UpdateBody();
            }

            if (!m_HasTarget) return;

            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
            {
                var body = m_Bodies[i];
                if (!body.gameObject.activeSelf) continue;

                if ((body.localPosition - m_Target.Positions[i]).sqrMagnitude > m_SnapDistance * m_SnapDistance)
                {
                    body.SetLocalPositionAndRotation(m_Target.Positions[i], m_Target.Rotations[i]);
                    continue;
                }

                body.SetLocalPositionAndRotation(
                    Vector3.Lerp(body.localPosition, m_Target.Positions[i], blend),
                    Quaternion.Slerp(body.localRotation, m_Target.Rotations[i], blend));
            }
        }

        void Place()
        {
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
                m_Bodies[i].SetLocalPositionAndRotation(m_Target.Positions[i], m_Target.Rotations[i]);
        }
    }
}
