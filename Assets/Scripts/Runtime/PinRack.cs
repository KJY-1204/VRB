using System;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// The ten pins as a group. After a throw, call <see cref="BeginJudging"/>; once the pins have come to
    /// rest (or the maximum wait passes) <see cref="Judged"/> fires exactly once with the fallen-pin count.
    /// </summary>
    public class PinRack : MonoBehaviour
    {
        [SerializeField] BowlingConfig m_Config;

        Pin[] m_Pins;
        SettleTracker m_Tracker;
        PinJudgeSettings m_Settings;
        bool m_Judging;

        /// <summary>Raised once per judged throw with the number of fallen pins.</summary>
        public event Action<int> Judged;

        public bool IsJudging => m_Judging;

        /// <summary>True if the last judgement happened because the maximum wait ran out, not because pins rested.</summary>
        public bool LastJudgementTimedOut => m_Tracker.TimedOut;
        public int PinCount => m_Pins.Length;

        void Awake()
        {
            m_Pins = GetComponentsInChildren<Pin>();
            m_Settings = m_Config != null ? m_Config.pinJudge : new PinJudgeSettings();
            m_Tracker = new SettleTracker(m_Settings);
        }

        /// <summary>Replace the judging thresholds at runtime (mainly for tests and live tuning).</summary>
        public void SetSettings(PinJudgeSettings settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_Tracker = new SettleTracker(m_Settings);
        }

        public void BeginJudging()
        {
            m_Tracker.Reset();
            m_Judging = true;
        }

        public void ResetAll()
        {
            m_Judging = false;
            foreach (var pin in m_Pins) pin.ResetToHome();
        }

        public int CountFallen()
        {
            var fallen = 0;
            foreach (var pin in m_Pins)
                if (PinJudge.IsFallen(pin.TiltDegrees, pin.Displacement, pin.InPit, m_Settings)) fallen++;
            return fallen;
        }

        public bool AllPinsAtRest()
        {
            foreach (var pin in m_Pins)
            {
                // A pin in the pit already counts as fallen, so its motion no longer matters.
                if (pin.IsSleeping || pin.InPit) continue;
                if (!PinJudge.IsAtRest(pin.LinearSpeed, pin.AngularSpeed, m_Settings)) return false;
            }

            return true;
        }

        void FixedUpdate()
        {
            if (!m_Judging) return;

            if (m_Tracker.Update(Time.fixedDeltaTime, AllPinsAtRest()))
            {
                m_Judging = false;
                Judged?.Invoke(CountFallen());
            }
        }
    }
}
