using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// The scoreboard: a message line, one row per player with the ten frames (marks and running score) and the player's
    /// total, and a restart button, readable from beside the throwing spot.
    /// </summary>
    public class ScoreboardView : MonoBehaviour
    {
        /// <summary>The widgets of one player's row.</summary>
        [Serializable]
        public class RowView
        {
            public GameObject Root;
            public Image Highlight;
            public TMP_Text Name;
            public TMP_Text[] Marks = new TMP_Text[Scorecard.FrameCount];
            public TMP_Text[] Scores = new TMP_Text[Scorecard.FrameCount];
            public Image[] Cards = new Image[Scorecard.FrameCount];
            public TMP_Text Total;
        }

        static readonly Color NoHighlight = new Color(1f, 1f, 1f, 0f);
        static readonly Color RowHighlight = new Color(0.16f, 0.78f, 1f, 0.22f);

        [SerializeField] TMP_Text m_StatusText;
        [SerializeField] RowView[] m_Rows = new RowView[Match.MaxPlayers];
        [SerializeField] Button m_RestartButton;

        public event Action RestartPressed;

        public string StatusText => m_StatusText.text;

        /// <summary>The first player's total, which is the whole score in a one-player game.</summary>
        public string TotalText => PlayerTotalText(1);

        /// <summary>A player's total (players are numbered from 1).</summary>
        public string PlayerTotalText(int player) => m_Rows[player - 1].Total.text;

        public string PlayerNameText(int player) => m_Rows[player - 1].Name.text;
        public bool IsRowVisible(int player) => m_Rows[player - 1].Root.activeSelf;

        /// <summary>The marks shown in a frame (1-based) of the first player, for example "X", "7 /" or "X 6 /".</summary>
        public string FrameMarksText(int frame) => FrameMarksText(1, frame);

        public string FrameMarksText(int player, int frame) => m_Rows[player - 1].Marks[frame - 1].text;

        /// <summary>The running score under a frame; empty while its bonus is still to come.</summary>
        public string FrameScoreText(int frame) => FrameScoreText(1, frame);

        public string FrameScoreText(int player, int frame) => m_Rows[player - 1].Scores[frame - 1].text;

        void Awake()
        {
            m_RestartButton.onClick.AddListener(() => RestartPressed?.Invoke());
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void Refresh(GamePhase phase, Match match)
        {
            m_StatusText.text = StatusFor(phase, match);
            m_StatusText.color = UiPalette.StatusColor(phase, match);

            for (var p = 0; p < m_Rows.Length; p++)
            {
                var row = m_Rows[p];
                var present = p < match.PlayerCount;
                row.Root.SetActive(present);
                if (!present) continue;

                var player = match.Players[p];
                var card = player.Session.Card;
                var isCurrent = player == match.Current && phase != GamePhase.SelectingLevel && !match.IsComplete;

                row.Name.text = player.Name;
                row.Total.text = player.Session.Score.ToString();
                row.Highlight.color = isCurrent && match.HasMultiplePlayers ? RowHighlight : NoHighlight;

                for (var i = 0; i < Scorecard.FrameCount; i++)
                {
                    var frame = card.Frames[i];
                    row.Marks[i].text = string.Join(" ", frame.Marks);
                    row.Scores[i].text = frame.CumulativeScore.HasValue ? frame.CumulativeScore.Value.ToString() : string.Empty;

                    // Mark the frame being bowled.
                    var current = isCurrent && !card.IsComplete && i == card.CurrentFrame - 1;
                    row.Cards[i].color = current ? UiPalette.CardHighlight : UiPalette.Card;
                }
            }
        }

        public static string StatusFor(GamePhase phase, Match match)
        {
            var who = match.HasMultiplePlayers ? match.Current.Name + ": " : string.Empty;
            switch (phase)
            {
                case GamePhase.Ready:
                    var card = match.Current.Session.Card;
                    return $"{who}Frame {card.CurrentFrame} - Ball {card.NextThrowInFrame}";
                case GamePhase.Throwing: return who + "Rolling...";
                case GamePhase.Settling: return who + "Counting pins...";
                case GamePhase.ShowingResult: return ResultFor(match);
                case GamePhase.Resetting: return who + "Setting up...";
                case GamePhase.GameOver: return GameOverFor(match);
                default: return string.Empty;
            }
        }

        static string ResultFor(Match match)
        {
            // The turn has not moved yet, so this is still the player who threw.
            var thrower = match.LastThrower ?? match.Current;
            var who = match.HasMultiplePlayers ? thrower.Name + ": " : string.Empty;
            var session = thrower.Session;
            switch (session.Card.LastMark)
            {
                case "X": return who + "STRIKE!";
                case "/": return who + "SPARE!";
                case "-": return who + "Miss";
                default: return who + $"{session.Throws[session.ThrowCount - 1].PinsFallen} pins down";
            }
        }

        static string GameOverFor(Match match)
        {
            var winners = match.Winners;
            if (!match.HasMultiplePlayers) return $"Game over - {winners[0].Session.Score} points";

            return winners.Count == 1
                ? $"{winners[0].Name} wins - {winners[0].Session.Score}"
                : $"Tie - {winners[0].Session.Score}";
        }
    }
}
