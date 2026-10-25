using System;

namespace VRB.Core
{
    /// <summary>
    /// Smooths out the uneven arrival of voice packets. Samples are written as they come in and read out at a steady
    /// rate by the audio system (on another thread). Playback only starts once a little has piled up, and when the
    /// pile runs dry it goes quiet and waits for it to refill. If too much piles up, the oldest is dropped so the
    /// voice never lags far behind.
    /// </summary>
    public sealed class VoiceBuffer
    {
        readonly float[] m_Data;
        readonly int m_Prebuffer;
        readonly object m_Lock = new object();
        int m_Start;
        int m_Count;
        bool m_Playing;

        public VoiceBuffer(int capacity, int prebuffer)
        {
            m_Data = new float[capacity];
            m_Prebuffer = Math.Min(prebuffer, capacity);
        }

        /// <summary>Samples waiting to be played.</summary>
        public int Available
        {
            get { lock (m_Lock) return m_Count; }
        }

        public void Write(float[] samples, int count)
        {
            lock (m_Lock)
            {
                var from = 0;
                if (count > m_Data.Length)
                {
                    from = count - m_Data.Length;
                    count = m_Data.Length;
                }

                var overflow = m_Count + count - m_Data.Length;
                if (overflow > 0)
                {
                    m_Start = (m_Start + overflow) % m_Data.Length;
                    m_Count -= overflow;
                }

                for (var i = 0; i < count; i++)
                    m_Data[(m_Start + m_Count + i) % m_Data.Length] = samples[from + i];
                m_Count += count;
            }
        }

        /// <summary>Fills the array with the next samples, or silence while there is nothing (enough) to play.</summary>
        public void Read(float[] destination)
        {
            lock (m_Lock)
            {
                if (!m_Playing && m_Count >= m_Prebuffer && m_Count > 0) m_Playing = true;

                var n = m_Playing ? Math.Min(destination.Length, m_Count) : 0;
                for (var i = 0; i < n; i++) destination[i] = m_Data[(m_Start + i) % m_Data.Length];
                for (var i = n; i < destination.Length; i++) destination[i] = 0f;

                m_Start = (m_Start + n) % m_Data.Length;
                m_Count -= n;
                if (m_Count == 0) m_Playing = false;
            }
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                m_Start = 0;
                m_Count = 0;
                m_Playing = false;
            }
        }
    }
}
