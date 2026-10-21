using TMPro;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>Score shown on the hall's TV above the pins: the player up, a big total and a short status line.</summary>
    public class TvScoreView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_LabelText;
        [SerializeField] TMP_Text m_TotalText;
        [SerializeField] TMP_Text m_StatusText;

        public string LabelText => m_LabelText.text;
        public string TotalText => m_TotalText.text;
        public string StatusText => m_StatusText.text;

        public void Refresh(GamePhase phase, Match match)
        {
            // While a result is shown the turn has not moved on, so this is still whoever just threw.
            var player = match.Current;
            m_LabelText.text = match.HasMultiplePlayers ? player.Name + "  TOTAL" : "TOTAL";
            m_TotalText.text = player.Session.Score.ToString();
            m_StatusText.text = phase == GamePhase.SelectingLevel
                ? "Choose a level"
                : ScoreboardView.StatusFor(phase, match);
            m_StatusText.color = UiPalette.StatusColor(phase, match);
        }
    }
}
