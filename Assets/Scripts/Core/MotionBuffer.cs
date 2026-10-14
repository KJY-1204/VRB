using System.Collections.Generic;

namespace VRB.Core
{
    /// <summary>Keeps the most recent hand positions so the release velocity can be computed from them.</summary>
    public sealed class MotionBuffer
    {
        readonly List<MotionSample> m_Samples = new List<MotionSample>();
        readonly float m_KeepSeconds;

        public MotionBuffer(float keepSeconds = 0.5f)
        {
            m_KeepSeconds = keepSeconds;
        }

        public IReadOnlyList<MotionSample> Samples => m_Samples;
        public int Count => m_Samples.Count;

        public void Add(float time, Vec3 position)
        {
            m_Samples.Add(new MotionSample(time, position));

            // Drop old samples but always keep two so a velocity can still be computed.
            while (m_Samples.Count > 2 && time - m_Samples[0].Time > m_KeepSeconds)
                m_Samples.RemoveAt(0);
        }

        public void Clear() => m_Samples.Clear();
    }
}
