using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// The screen for playing together on one network: open a game and read out the address, or join one by typing its
    /// address on a keypad. It opens from the ONLINE button of the start screen and takes that screen's place while open.
    /// Which of its panels shows follows the state of the <see cref="NetworkSession"/>.
    /// </summary>
    public class NetworkMenuView : MonoBehaviour
    {
        [SerializeField] NetworkSession m_Session;
        [SerializeField] LevelSelectionView m_Selection;
        [SerializeField] GameObject m_Canvas;

        [Header("Panels")]
        [SerializeField] GameObject m_OfflinePanel;
        [SerializeField] GameObject m_HostingPanel;
        [SerializeField] GameObject m_JoinPanel;
        [SerializeField] GameObject m_ConnectedPanel;

        [Header("Texts")]
        [SerializeField] TMP_Text m_StatusText;
        [SerializeField] TMP_Text m_AddressText;
        [SerializeField] TMP_Text m_EntryText;
        [SerializeField] TMP_Text m_ConnectedText;

        [Header("Buttons")]
        [SerializeField] Button m_HostButton;
        [SerializeField] Button m_JoinButton;
        [SerializeField] Button m_CloseButton;
        [SerializeField] Button[] m_DoneButtons = new Button[0];     // close the menu and keep the game going
        [SerializeField] Button[] m_LeaveButtons = new Button[0];    // stop hosting or leave the game
        [SerializeField] Button[] m_KeyButtons = new Button[11];     // 0-9, then the dot
        [SerializeField] Button m_DeleteButton;
        [SerializeField] Button m_ConnectButton;
        [SerializeField] Button m_JoinBackButton;

        readonly AddressEntry m_Entry = new AddressEntry();
        bool m_Joining;
        string m_HostAddress = string.Empty;

        public bool IsOpen => m_Canvas != null && m_Canvas.activeSelf;

        public string EntryText => m_Entry.Text;
        public string StatusText => m_StatusText.text;
        public string AddressText => m_AddressText.text;
        public string ConnectedText => m_ConnectedText.text;
        public bool IsJoining => m_Joining;

        public bool ShowsOffline => m_OfflinePanel.activeSelf;
        public bool ShowsHosting => m_HostingPanel.activeSelf;
        public bool ShowsJoin => m_JoinPanel.activeSelf;
        public bool ShowsConnected => m_ConnectedPanel.activeSelf;

        void Awake()
        {
            m_HostButton.onClick.AddListener(PressHost);
            m_JoinButton.onClick.AddListener(PressJoin);
            m_CloseButton.onClick.AddListener(Close);
            foreach (var button in m_DoneButtons) button.onClick.AddListener(Close);
            foreach (var button in m_LeaveButtons) button.onClick.AddListener(PressLeave);
            for (var i = 0; i < m_KeyButtons.Length; i++)
            {
                var key = i < 10 ? (char)('0' + i) : '.';
                m_KeyButtons[i].onClick.AddListener(() => PressKey(key));
            }

            m_DeleteButton.onClick.AddListener(PressDelete);
            m_ConnectButton.onClick.AddListener(PressConnect);
            m_JoinBackButton.onClick.AddListener(PressBack);
        }

        void OnEnable()
        {
            m_Session.StateChanged += OnStateChanged;
            NetPlayer.Changed += Refresh;
            m_Selection.NetworkPressed += Open;
        }

        void OnDisable()
        {
            m_Session.StateChanged -= OnStateChanged;
            NetPlayer.Changed -= Refresh;
            m_Selection.NetworkPressed -= Open;
        }

        void Start()
        {
            m_Canvas.SetActive(false);
            Refresh();
        }

        public void Open()
        {
            m_Canvas.SetActive(true);
            m_Selection.SetSuppressed(true);
            Refresh();
        }

        public void Close()
        {
            m_Canvas.SetActive(false);
            m_Selection.SetSuppressed(false);
        }

        public void PressHost()
        {
            if (m_Session.StartHost()) m_HostAddress = NetworkSession.LocalAddress();
            Refresh();
        }

        public void PressJoin()
        {
            m_Joining = true;
            m_Entry.Set(AddressEntry.SubnetPrefix(NetworkSession.LocalAddress()));
            Refresh();
        }

        public void PressKey(char key)
        {
            m_Entry.Append(key);
            Refresh();
        }

        public void PressDelete()
        {
            m_Entry.Backspace();
            Refresh();
        }

        public void PressConnect()
        {
            m_Session.StartClient(m_Entry.Text);
            Refresh();
        }

        /// <summary>Back out of the joining screen to the first one.</summary>
        public void PressBack()
        {
            m_Joining = false;
            if (m_Session.State != NetState.Offline) m_Session.Disconnect();
            Refresh();
        }

        /// <summary>Stop hosting, or leave the game that was joined.</summary>
        public void PressLeave()
        {
            m_Joining = false;
            m_Session.Disconnect();
            Refresh();
        }

        void OnStateChanged(NetState state) => Refresh();

        void Refresh()
        {
            var state = m_Session.State;
            var connected = state == NetState.Connected;
            var hosting = state == NetState.Hosting;
            var joining = !connected && !hosting && (m_Joining || state == NetState.Connecting);

            m_HostingPanel.SetActive(hosting);
            m_ConnectedPanel.SetActive(connected);
            m_JoinPanel.SetActive(joining);
            m_OfflinePanel.SetActive(!hosting && !connected && !joining);

            m_StatusText.text = m_Session.StatusMessage;
            m_StatusText.color = state == NetState.Failed ? UiPalette.Strike : UiPalette.Amber;

            m_AddressText.text = hosting ? m_HostAddress : string.Empty;
            m_EntryText.text = m_Entry.Text.Length == 0 ? "_" : m_Entry.Text + (m_Entry.IsComplete ? string.Empty : "_");
            m_ConnectButton.interactable = m_Entry.IsComplete && state != NetState.Connecting;
            m_ConnectedText.text = connected ? ConnectedMessage() : string.Empty;
        }

        string ConnectedMessage()
        {
            var local = NetPlayer.All.FirstOrDefault(p => p != null && p.IsSpawned && p.IsLocalPlayer);
            return local != null && local.Lane >= 0 ? "You are on lane " + (local.Lane + 1) : "Connected";
        }
    }
}
