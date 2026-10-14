using System;

namespace VRB.Core
{
    /// <summary>
    /// Decides when a throw can be judged: once every pin has been at rest for long enough, or when
    /// the maximum wait has elapsed. Reports "settled" exactly once per run so a throw is judged once.
    /// </summary>
    public sealed class SettleTracker
    {
        readonly PinJudgeSettings m_Settings;
        float m_Elapsed;
        float m_RestingFor;

        public bool IsSettled { get; private set; }
        public bool TimedOut { get; private set; }

        public SettleTracker(PinJudgeSettings settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Reset()
        {
            m_Elapsed = 0f;
            m_RestingFor = 0f;
            IsSettled = false;
            TimedOut = false;
        }

        /// <summary>Advance by deltaTime. Returns true only on the update that completes settling.</summary>
        public bool Update(float deltaTime, bool allPinsAtRest)
        {
            if (IsSettled) return false;

            m_Elapsed += deltaTime;
            m_RestingFor = allPinsAtRest ? m_RestingFor + deltaTime : 0f;

            if (m_RestingFor >= m_Settings.restDuration)
            {
                IsSettled = true;
                return true;
            }

            if (m_Elapsed >= m_Settings.maxWaitSeconds)
            {
                IsSettled = true;
                TimedOut = true;
                return true;
            }

            return false;
        }
    }
}
