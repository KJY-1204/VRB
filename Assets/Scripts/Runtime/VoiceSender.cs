using UnityEngine;
using UnityEngine.InputSystem;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// Listens to the microphone and sends what is said to the other players, in chunks of 40 ms. Quiet stretches are
    /// not sent. Offline (or without a microphone) it does nothing. The Y button of the left controller mutes and
    /// unmutes.
    /// </summary>
    public class VoiceSender : MonoBehaviour
    {
        [SerializeField] bool m_Muted;

        readonly VoiceGate m_Gate = new VoiceGate();
        readonly float[] m_Chunk = new float[VoiceCodec.ChunkSamples];
        InputAction m_MuteAction;
        AudioClip m_Recording;
        string m_Device;
        int m_ReadPosition;
        bool m_PermissionAsked;

        public bool Muted
        {
            get => m_Muted;
            set => m_Muted = value;
        }

        public bool IsRecording => m_Recording != null;

        public int SentCount { get; private set; }

        void OnEnable()
        {
            m_MuteAction = new InputAction("MuteVoice", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
            m_MuteAction.performed += OnMutePressed;
            m_MuteAction.Enable();
        }

        void OnDisable()
        {
            m_MuteAction.performed -= OnMutePressed;
            m_MuteAction.Dispose();
            StopRecording();
        }

        void OnMutePressed(InputAction.CallbackContext context) => m_Muted = !m_Muted;

        void Update()
        {
            // Only listen while in a network game, so nothing is recorded for nobody.
            var local = NetPlayer.Local;
            if (local == null)
            {
                if (m_Recording != null) StopRecording();
                return;
            }

            if (m_Recording == null && !TryStartRecording()) return;

            var length = m_Recording.samples;
            var position = Microphone.GetPosition(m_Device);
            if (position < 0) return;

            var available = (position - m_ReadPosition + length) % length;
            while (available >= m_Chunk.Length)
            {
                m_Recording.GetData(m_Chunk, m_ReadPosition);
                m_ReadPosition = (m_ReadPosition + m_Chunk.Length) % length;
                available -= m_Chunk.Length;

                // The gate keeps running while muted, so un-muting does not start mid-word.
                if (!m_Gate.ShouldSend(m_Chunk, m_Chunk.Length) || m_Muted) continue;

                local.SendVoice(VoiceCodec.Encode(m_Chunk, m_Chunk.Length));
                SentCount++;
            }
        }

        bool TryStartRecording()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                if (!m_PermissionAsked)
                {
                    m_PermissionAsked = true;
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
                }

                return false;
            }
#endif
            if (Microphone.devices.Length == 0) return false;

            m_Device = null; // the default microphone
            m_Recording = Microphone.Start(m_Device, true, 1, VoiceCodec.SampleRate);
            m_ReadPosition = 0;
            return m_Recording != null;
        }

        void StopRecording()
        {
            if (m_Recording == null) return;

            Microphone.End(m_Device);
            m_Recording = null;
        }
    }
}
