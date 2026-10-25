using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// The voice of one other player: coded speech chunks go in as they arrive from the network, and come out of a
    /// spatial audio source, so the voice seems to come from where that player's head is.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class RemoteVoice : MonoBehaviour
    {
        // Half a second at most is kept; playing starts once an eighth of a second has arrived.
        readonly VoiceBuffer m_Buffer = new VoiceBuffer(VoiceCodec.SampleRate / 2, VoiceCodec.SampleRate / 8);
        AudioSource m_Source;
        float m_LastPush = -1f;

        /// <summary>True for a moment after voice arrived, so the name can light up while someone talks.</summary>
        public bool IsSpeaking => m_LastPush >= 0f && Time.time - m_LastPush < 0.3f;

        public int Buffered => m_Buffer.Available;

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.clip = AudioClip.Create("RemoteVoice", VoiceCodec.SampleRate, 1, VoiceCodec.SampleRate, true, Fill);
            m_Source.loop = true;
            m_Source.spatialBlend = 1f;
        }

        void OnEnable()
        {
            m_Source.Play();
        }

        void OnDisable()
        {
            m_Buffer.Clear();
            m_LastPush = -1f;
        }

        /// <summary>Called by the audio system (on its own thread) whenever it wants more sound.</summary>
        public void Fill(float[] data) => m_Buffer.Read(data);

        /// <summary>Take a chunk of coded speech from the network.</summary>
        public void Push(byte[] encoded)
        {
            if (encoded == null || encoded.Length == 0 || encoded.Length > VoiceCodec.ChunkSamples * 4) return;

            var samples = new float[encoded.Length];
            VoiceCodec.Decode(encoded, samples);
            m_Buffer.Write(samples, samples.Length);
            m_LastPush = Time.time;
        }
    }
}
