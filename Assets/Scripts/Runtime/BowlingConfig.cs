using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>Tunable gameplay values in one place so they can be adjusted without touching code.</summary>
    [CreateAssetMenu(fileName = "BowlingConfig", menuName = "VRB/Bowling Config")]
    public class BowlingConfig : ScriptableObject
    {
        public PinJudgeSettings pinJudge = new PinJudgeSettings();
    }
}
