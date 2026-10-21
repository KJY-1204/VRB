using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// The scoreboard: a message line, the ten frames with their marks and running scores, the total and a restart
    /// button, readable from beside the throwing spot.
    /// </summary>
    public class ScoreboardView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_StatusText;
        [SerializeField] TMP_Text m_TotalText;
        [SerializeField] TMP_Text[] m_FrameMarks = new TMP_Text[Scorecard.FrameCount];
        [SerializeField] TMP_Text[] m_FrameScores = new TMP_Text[Scorecard.FrameCount];
        [SerializeField] Image[] m_FrameCards = new Image[Scorecard.FrameCount];
        [SerializeField] Button m_RestartButton;

        public event Action RestartPressed;

        public string StatusText => m_StatusText.text;
        public string TotalText => m_TotalText.text;

        /// <summary>The marks shown in a frame (1-based), for example "X", "7 /" or "X 6 /".</summary>
        public string FrameMarksText(int frame) => m_FrameMarks[frame - 1].text;

        /// <summary>The running score shown under a frame (1-based); empty while its bonus is still to come.</summary>
        public string FrameScoreText(int frame) => m_FrameScores[frame - 1].text;

        void Awake()
        {
            m_RestartButton.onClick.AddListener(() => RestartPressed?.Invoke());
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void Refresh(GamePhase phase, GameSession session)
        {
            m_StatusText.text = StatusFor(phase, session);
            m_StatusText.color = UiPalette.StatusColor(phase, session);
            m_TotalText.text = session.Score.ToString();

            var card = session.Card;
            for (var i = 0; i < Scorecard.FrameCount; i++)
            {
                var frame = card.Frames[i];
                m_FrameMarks[i].text = string.Join(" ", frame.Marks);
                m_FrameScores[i].text = frame.CumulativeScore.HasValue ? frame.CumulativeScore.Value.ToString() : string.Empty;

                // Mark the frame being bowled.
                var current = !card.IsComplete && phase != GamePhase.SelectingLevel && i == card.CurrentFrame - 1;
                m_FrameCards[i].color = current ? UiPalette.CardHighlight : UiPalette.Card;
            }
        }

        public static string StatusFor(GamePhase phase, GameSession session)
        {
            switch (phase)
            {
                case GamePhase.Ready: return $"Frame {session.Card.CurrentFrame} - Ball {session.Card.NextThrowInFrame}";
                case GamePhase.Throwing: return "Rolling...";
                case GamePhase.Settling: return "Counting pins...";
                case GamePhase.ShowingResult: return ResultFor(session);
                case GamePhase.Resetting: return "Setting up...";
                case GamePhase.GameOver: return $"Game over - {session.Score} points";
                default: return string.Empty;
            }
        }

        static string ResultFor(GameSession session)
        {
            switch (session.Card.LastMark)
            {
                case "X": return "STRIKE!";
                case "/": return "SPARE!";
                case "-": return "Miss";
                default: return $"{session.Throws[session.ThrowCount - 1].PinsFallen} pins down";
            }
        }
    }
}
