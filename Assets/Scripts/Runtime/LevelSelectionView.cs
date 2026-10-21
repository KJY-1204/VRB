using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRB.Core;

namespace VRB
{
    /// <summary>Pre-game screen: pick one of the four throw correction levels, then start.</summary>
    public class LevelSelectionView : MonoBehaviour
    {
        static readonly Color SelectedColor = UiPalette.Selected;
        static readonly Color NormalColor = UiPalette.Button;

        [SerializeField] Button[] m_LevelButtons = new Button[4];   // index = (int)ThrowAssistLevel
        [SerializeField] Button[] m_LaneButtons = new Button[0];    // index = lane, 0-based
        [SerializeField] Button[] m_PlayerButtons = new Button[0];  // index = player count - 1
        [SerializeField] Button m_StartButton;
        [SerializeField] TMP_Text m_DescriptionText;

        public event Action<ThrowAssistLevel> LevelPicked;
        public event Action<int> LanePicked;
        public event Action<int> PlayersPicked;
        public event Action StartPressed;

        public ThrowAssistLevel Selected { get; private set; } = ThrowAssist.Default;
        public int SelectedLane { get; private set; }
        public int SelectedPlayers { get; private set; } = 1;
        public string DescriptionText => m_DescriptionText != null ? m_DescriptionText.text : string.Empty;

        void Awake()
        {
            for (var i = 0; i < m_LevelButtons.Length; i++)
            {
                var level = (ThrowAssistLevel)i;
                m_LevelButtons[i].onClick.AddListener(() => Pick(level));
            }

            for (var i = 0; i < m_LaneButtons.Length; i++)
            {
                var lane = i;
                m_LaneButtons[i].onClick.AddListener(() => PickLane(lane));
            }

            for (var i = 0; i < m_PlayerButtons.Length; i++)
            {
                var count = i + 1;
                m_PlayerButtons[i].onClick.AddListener(() => PickPlayers(count));
            }

            m_StartButton.onClick.AddListener(() => StartPressed?.Invoke());
            Refresh();
        }

        /// <summary>Called by the player pressing a player-count button.</summary>
        public void PickPlayers(int count)
        {
            SelectedPlayers = count;
            Refresh();
            PlayersPicked?.Invoke(count);
        }

        public bool IsPlayersHighlighted(int count)
        {
            var image = m_PlayerButtons[count - 1].targetGraphic as Image;
            return image != null && image.color == SelectedColor;
        }

        /// <summary>Called by the player pressing a lane button.</summary>
        public void PickLane(int lane)
        {
            SelectedLane = lane;
            Refresh();
            LanePicked?.Invoke(lane);
        }

        public bool IsLaneHighlighted(int lane)
        {
            var image = m_LaneButtons[lane].targetGraphic as Image;
            return image != null && image.color == SelectedColor;
        }

        /// <summary>Called by the player pressing a level button.</summary>
        public void Pick(ThrowAssistLevel level)
        {
            Selected = level;
            Refresh();
            LevelPicked?.Invoke(level);
        }

        /// <summary>Set the highlighted level without raising an event (used when the game resets).</summary>
        public void SetSelected(ThrowAssistLevel level)
        {
            Selected = level;
            Refresh();
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public bool IsHighlighted(ThrowAssistLevel level)
        {
            var image = m_LevelButtons[(int)level].targetGraphic as Image;
            return image != null && image.color == SelectedColor;
        }

        void Refresh()
        {
            for (var i = 0; i < m_LevelButtons.Length; i++)
            {
                if (m_LevelButtons[i].targetGraphic is Image image)
                    image.color = i == (int)Selected ? SelectedColor : NormalColor;
            }

            for (var i = 0; i < m_LaneButtons.Length; i++)
            {
                if (m_LaneButtons[i].targetGraphic is Image image)
                    image.color = i == SelectedLane ? SelectedColor : NormalColor;
            }

            for (var i = 0; i < m_PlayerButtons.Length; i++)
            {
                if (m_PlayerButtons[i].targetGraphic is Image image)
                    image.color = i + 1 == SelectedPlayers ? SelectedColor : NormalColor;
            }

            if (m_DescriptionText != null) m_DescriptionText.text = Describe(Selected);
        }

        static string Describe(ThrowAssistLevel level)
        {
            switch (level)
            {
                case ThrowAssistLevel.RawPhysics: return "Your hand speed is used exactly as it is.";
                case ThrowAssistLevel.Weak: return "Smooths out hand shake. Recommended.";
                case ThrowAssistLevel.Medium: return "Smooths and gently steers toward the pins.";
                case ThrowAssistLevel.Strong: return "Smooths and strongly steers toward the pins.";
                default: return string.Empty;
            }
        }
    }
}
