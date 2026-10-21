using TMPro;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>Score shown on the hall's TV above the pins: a big total with a short status line.</summary>
    public class TvScoreView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_TotalText;
        [SerializeField] TMP_Text m_StatusText;

        public string TotalText => m_TotalText.text;
        public string StatusText => m_StatusText.text;

        public void Refresh(GamePhase phase, GameSession session)
        {
            m_TotalText.text = session.Score.ToString();
            m_StatusText.text = phase == GamePhase.SelectingLevel
                ? "Choose a level"
                : ScoreboardView.StatusFor(phase, session);
            m_StatusText.color = UiPalette.StatusColor(phase, session);
        }
    }
}
