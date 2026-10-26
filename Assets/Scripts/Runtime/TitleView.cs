using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRB
{
    /// <summary>
    /// The main screen the game opens with: PLAY goes to the start screen (lane, players, throw assist), ONLINE opens the
    /// network menu, CREDITS shows who made the models used in the game. It takes the start screen's place while open.
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        /// <summary>The credits text. The hall model is under CC BY 4.0, which asks for this to be shown with the game.</summary>
        public const string Credits =
            "<size=32><color=#8FA3BF>3D MODEL</color></size>\n" +
            "\"Bowling Club\" by tiunov.se\n" +
            "<size=29>sketchfab.com/3d-models/bowling-club-0b8fae45fcda4fe78f93bb2a899401a6</size>\n" +
            "Licensed under CC BY 4.0\n" +
            "<size=31>creativecommons.org/licenses/by/4.0</size>\n" +
            "<size=30><color=#8FA3BF>Changes made: scaled, stretched and edited for this game.</color></size>\n\n" +
            "<size=32><color=#8FA3BF>BUILT WITH</color></size>\n" +
            "<size=34>Unity, XR Interaction Toolkit, Netcode for GameObjects</size>";

        /// <summary>Whether a new scene opens on this screen. Tests switch it off so they start on the start screen.</summary>
        public static bool ShowAtStart = true;

        [SerializeField] LevelSelectionView m_Selection;
        [SerializeField] NetworkMenuView m_Network;
        [SerializeField] NetworkSession m_Session;
        [SerializeField] GameObject m_Canvas;
        [SerializeField] GameObject m_MainPanel;
        [SerializeField] GameObject m_CreditsPanel;
        [SerializeField] TMP_Text m_CreditsText;
        [SerializeField] Button m_PlayButton;
        [SerializeField] Button m_OnlineButton;
        [SerializeField] Button m_CreditsButton;
        [SerializeField] Button m_CreditsBackButton;

        bool m_OpenedNetwork;

        public bool IsShown => m_Canvas != null && m_Canvas.activeSelf;
        public bool ShowsCredits => m_CreditsPanel.activeSelf;
        public string CreditsText => m_CreditsText.text;

        void Awake()
        {
            m_PlayButton.onClick.AddListener(PressPlay);
            m_OnlineButton.onClick.AddListener(PressOnline);
            m_CreditsButton.onClick.AddListener(PressCredits);
            m_CreditsBackButton.onClick.AddListener(PressCreditsBack);
            m_CreditsText.text = Credits;
        }

        void OnEnable()
        {
            m_Selection.MenuPressed += Show;
            m_Network.Closed += OnNetworkClosed;
        }

        void OnDisable()
        {
            m_Selection.MenuPressed -= Show;
            m_Network.Closed -= OnNetworkClosed;
        }

        void Start()
        {
            if (ShowAtStart) Show();
            else m_Canvas.SetActive(false);
        }

        /// <summary>Show the main screen in place of the start screen.</summary>
        public void Show()
        {
            m_Canvas.SetActive(true);
            m_MainPanel.SetActive(true);
            m_CreditsPanel.SetActive(false);
            m_Selection.SetSuppressed(true);
        }

        public void PressPlay()
        {
            m_Canvas.SetActive(false);
            m_Selection.SetSuppressed(false);
        }

        public void PressOnline()
        {
            m_OpenedNetwork = true;
            m_Canvas.SetActive(false);
            m_Network.Open();
        }

        public void PressCredits()
        {
            m_MainPanel.SetActive(false);
            m_CreditsPanel.SetActive(true);
        }

        public void PressCreditsBack()
        {
            m_MainPanel.SetActive(true);
            m_CreditsPanel.SetActive(false);
        }

        // Backing out of the network menu without a game goes back here; with a game running it goes on to the start screen.
        void OnNetworkClosed()
        {
            if (!m_OpenedNetwork) return;

            m_OpenedNetwork = false;
            if (m_Session.State != NetState.Hosting && m_Session.State != NetState.Connected) Show();
        }
    }
}
