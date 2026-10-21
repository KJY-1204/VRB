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

        /// <summary>Pins still on the deck: ten on a fresh rack, fewer for the second ball of a frame.</summary>
        public int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var pin in m_Pins)
                    if (pin.gameObject.activeSelf) count++;
                return count;
            }
        }

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

        /// <summary>A fresh rack: every pin back on its spot, including the ones that were taken off the deck.</summary>
        public void ResetAll()
        {
            m_Judging = false;
            foreach (var pin in m_Pins)
            {
                pin.gameObject.SetActive(true);
                pin.ResetToHome();
            }
        }

        /// <summary>
        /// What the pinsetter does between the two balls of a frame: sweeps away the fallen pins and sets the ones still
        /// standing back on their spots, upright.
        /// </summary>
        public void ClearFallenAndRespot()
        {
            m_Judging = false;
            foreach (var pin in m_Pins)
            {
                if (!pin.gameObject.activeSelf) continue;

                if (IsFallen(pin)) pin.gameObject.SetActive(false);
                else pin.ResetToHome();
            }
        }

        /// <summary>Fallen pins among those still on the deck (pins already swept away do not count again).</summary>
        public int CountFallen()
        {
            var fallen = 0;
            foreach (var pin in m_Pins)
                if (pin.gameObject.activeSelf && IsFallen(pin)) fallen++;
            return fallen;
        }

        bool IsFallen(Pin pin) => PinJudge.IsFallen(pin.TiltDegrees, IsOffDeck(pin), m_Settings);

        /// <summary>The rack sits on the lane center, so a pin's sideways offset from it is its offset from the center.</summary>
        bool IsOffDeck(Pin pin)
        {
            var position = pin.transform.position;
            return PinJudge.IsOffDeck(position.x - transform.position.x, position.y, pin.InPit, m_Settings);
        }

        public bool AllPinsAtRest()
        {
            foreach (var pin in m_Pins)
            {
                if (!pin.gameObject.activeSelf) continue;

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
