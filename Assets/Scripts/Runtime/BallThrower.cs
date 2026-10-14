using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// Replaces the toolkit's default throw: records the hand's motion while the ball is held and, on release,
    /// launches the ball with a velocity computed for the chosen correction level.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Ball))]
    public class BallThrower : MonoBehaviour
    {
        [SerializeField] BowlingConfig m_Config;
        [SerializeField] float m_FoulLineZ = 0f;

        XRGrabInteractable m_Grab;
        Ball m_Ball;
        ThrowAssistSettings m_Settings;
        readonly MotionBuffer m_Buffer = new MotionBuffer(0.5f);
        IXRSelectInteractor m_Holder;
        Vector3 m_PendingVelocity;
        bool m_HasPending;

        /// <summary>The correction level applied to the next throw.</summary>
        public ThrowAssistLevel Level { get; set; } = ThrowAssist.Default;

        public bool IsHeld => m_Holder != null;

        /// <summary>Raised when the ball has been launched, with its initial velocity.</summary>
        public event Action<Vector3> Thrown;

        /// <summary>Raised when the ball was released beyond the foul line and so was not thrown.</summary>
        public event Action Fouled;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Ball = GetComponent<Ball>();
            m_Settings = m_Config != null ? m_Config.throwAssist : new ThrowAssistSettings();
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnSelectEntered);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            m_Holder = args.interactorObject;
            m_Buffer.Clear();
            Record();
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_Holder == null) return;

            Record();
            var handZ = m_Holder.GetAttachTransform(m_Grab).position.z;
            m_Holder = null;

            if (args.isCanceled) return;

            if (handZ > m_FoulLineZ)
            {
                Fouled?.Invoke();
                return;
            }

            var velocity = ThrowCalculator.ReleaseVelocity(m_Buffer.Samples, Level, m_Settings, Vec3.Forward);
            m_PendingVelocity = new Vector3(velocity.X, velocity.Y, velocity.Z);
            m_HasPending = true;
        }

        void LateUpdate()
        {
            if (m_Holder != null) Record();
        }

        void FixedUpdate()
        {
            if (!m_HasPending) return;

            // Applied here, after the toolkit has finished detaching, so its own velocity cannot overwrite ours.
            m_HasPending = false;
            m_Ball.Body.linearVelocity = m_PendingVelocity;
            m_Ball.Body.angularVelocity = Vector3.zero;
            Thrown?.Invoke(m_PendingVelocity);
        }

        void Record()
        {
            var p = m_Holder.GetAttachTransform(m_Grab).position;
            m_Buffer.Add(Time.time, new Vec3(p.x, p.y, p.z));
        }
    }
}
