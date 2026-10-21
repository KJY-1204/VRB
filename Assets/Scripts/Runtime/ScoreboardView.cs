using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRB.Core;

namespace VRB
{
    /// <summary>In-game board: last throw, running total and a restart button, readable from the throwing spot.</summary>
    public class ScoreboardView : MonoBehaviour
    {
        [SerializeField] TMP_Text m_StatusText;
        [SerializeField] TMP_Text m_LastText;
        [SerializeField] TMP_Text m_TotalText;
        [SerializeField] TMP_Text m_ThrowsText;
        [SerializeField] Button m_RestartButton;

        public event Action RestartPressed;

        public string StatusText => m_StatusText.text;
        public string LastText => m_LastText.text;
        public string TotalText => m_TotalText.text;
        public string ThrowsText => m_ThrowsText.text;

        void Awake()
        {
            m_RestartButton.onClick.AddListener(() => RestartPressed?.Invoke());
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void Refresh(GamePhase phase, GameSession session, int lastPins)
        {
            m_StatusText.text = StatusFor(phase, lastPins);
            m_StatusText.color = UiPalette.StatusColor(phase, lastPins);
            m_LastText.text = session.ThrowCount == 0 ? "-" : lastPins.ToString();
            m_TotalText.text = session.TotalPins.ToString();
            m_ThrowsText.text = session.ThrowCount.ToString();
        }

        public static string StatusFor(GamePhase phase, int lastPins)
        {
            switch (phase)
            {
                case GamePhase.Ready: return "Grab the ball and throw!";
                case GamePhase.Throwing: return "Rolling...";
                case GamePhase.Settling: return "Counting pins...";
                case GamePhase.ShowingResult: return lastPins == GameSession.PinCount ? "STRIKE!" : $"{lastPins} pins down";
                case GamePhase.Resetting: return "Setting up...";
                default: return string.Empty;
            }
        }
    }
}
