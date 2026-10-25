using System;

namespace VRB.Core
{
    /// <summary>
    /// Decides whether a chunk of microphone sound is worth sending: speech (and a short while after it, so word endings
    /// are not cut off) goes out, room noise and silence do not.
    /// </summary>
    public sealed class VoiceGate
    {
        readonly float m_Threshold;
        readonly int m_HangoverChunks;
        int m_Left;

        /// <param name="threshold">Loudness (RMS, 0 to 1) from which a chunk counts as speech.</param>
        /// <param name="hangoverChunks">How many quiet chunks still go out after speech.</param>
        public VoiceGate(float threshold = 0.012f, int hangoverChunks = 8)
        {
            m_Threshold = threshold;
            m_HangoverChunks = hangoverChunks;
        }

        public static float Loudness(float[] samples, int count)
        {
            if (count <= 0) return 0f;

            double sum = 0;
            for (var i = 0; i < count; i++) sum += samples[i] * samples[i];
            return (float)Math.Sqrt(sum / count);
        }

        public bool ShouldSend(float[] samples, int count)
        {
            if (Loudness(samples, count) >= m_Threshold)
            {
                m_Left = m_HangoverChunks;
                return true;
            }

            if (m_Left <= 0) return false;

            m_Left--;
            return true;
        }
    }
}
