using System;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    public enum NetState
    {
        Offline,
        Hosting,
        Connecting,
        Connected,
        Failed,
    }

    /// <summary>
    /// Starts and stops a game over the local network: one device hosts, the others join with the host's address. Everyone
    /// plays on a lane of their own; this only takes care of getting people connected (at most four, one per lane).
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public class NetworkSession : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;

        [SerializeField] GameObject m_PlayerPrefab;
        [SerializeField] ushort m_Port = DefaultPort;

        NetworkManager m_Manager;
        UnityTransport m_Transport;

        public NetState State { get; private set; } = NetState.Offline;
        public string StatusMessage { get; private set; } = string.Empty;
        public ushort Port => m_Port;
        public NetworkManager Manager => m_Manager;

        public event Action<NetState> StateChanged;

        void Awake()
        {
            m_Manager = GetComponent<NetworkManager>();
            m_Transport = GetComponent<UnityTransport>();

            var config = m_Manager.NetworkConfig;
            config.NetworkTransport = m_Transport;
            config.PlayerPrefab = m_PlayerPrefab;
            config.ConnectionApproval = true;
        }

        // The manager fills in its prefab list in its own Awake, and components of one object do not Awake in a fixed
        // order, so the player prefab is only added (when the list does not already have it) at the moment of starting.
        void EnsurePlayerPrefabRegistered()
        {
            if (m_PlayerPrefab != null && !m_Manager.NetworkConfig.Prefabs.Contains(m_PlayerPrefab))
                m_Manager.AddNetworkPrefab(m_PlayerPrefab);
        }

        void OnEnable()
        {
            m_Manager.ConnectionApprovalCallback = Approve;
            m_Manager.OnClientConnectedCallback += OnClientConnected;
            m_Manager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        void OnDisable()
        {
            if (m_Manager == null) return;
            m_Manager.ConnectionApprovalCallback = null;
            m_Manager.OnClientConnectedCallback -= OnClientConnected;
            m_Manager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        /// <summary>Open the game for others to join. Returns false if it could not start.</summary>
        public bool StartHost()
        {
            if (State == NetState.Hosting || State == NetState.Connecting || State == NetState.Connected) return false;

            EnsurePlayerPrefabRegistered();
            m_Transport.SetConnectionData("127.0.0.1", m_Port, "0.0.0.0");
            if (!m_Manager.StartHost())
            {
                Set(NetState.Failed, "Could not start the game on this device.");
                return false;
            }

            Set(NetState.Hosting, "Waiting for players");
            return true;
        }

        /// <summary>Join a game opened on another device (its address on the local network, such as 192.168.0.12).</summary>
        public bool StartClient(string hostAddress)
        {
            if (State == NetState.Hosting || State == NetState.Connecting || State == NetState.Connected) return false;

            // Four numbers with dots, strictly: the system's own parser would take "192.168.0" as an address too.
            var address = (hostAddress ?? string.Empty).Trim();
            if (!AddressEntry.IsValid(address))
            {
                Set(NetState.Failed, "That is not an address like 192.168.0.12.");
                return false;
            }

            EnsurePlayerPrefabRegistered();
            m_Transport.SetConnectionData(address, m_Port);
            if (!m_Manager.StartClient())
            {
                Set(NetState.Failed, "Could not start joining.");
                return false;
            }

            Set(NetState.Connecting, "Connecting...");
            return true;
        }

        /// <summary>Leave the game, or close it if this device is the host.</summary>
        public void Disconnect()
        {
            if (m_Manager.IsListening) m_Manager.Shutdown();
            Set(NetState.Offline, string.Empty);
        }

        /// <summary>This device's address on the local network, for the host to read out to the others.</summary>
        public static string LocalAddress()
        {
            try
            {
                foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                        return address.ToString();
            }
            catch (SocketException)
            {
                // No network at all.
            }

            return "127.0.0.1";
        }

        // Every lane takes one player, so the fifth device is turned away.
        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var full = m_Manager.ConnectedClientsIds.Count >= Match.MaxPlayers;
            response.Approved = !full;
            response.CreatePlayerObject = !full;
            response.Reason = full ? "All four lanes are taken." : string.Empty;
        }

        void OnClientConnected(ulong clientId)
        {
            if (m_Manager.IsHost) Set(NetState.Hosting, m_Manager.ConnectedClientsIds.Count + " players");
            else if (clientId == m_Manager.LocalClientId) Set(NetState.Connected, "Connected");
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (m_Manager.IsHost)
            {
                if (clientId != m_Manager.LocalClientId) Set(NetState.Hosting, m_Manager.ConnectedClientsIds.Count + " players");
                return;
            }

            // A joining device that is refused or loses the host ends up here.
            var reason = m_Manager.DisconnectReason;
            Set(NetState.Failed, string.IsNullOrEmpty(reason) ? "Could not connect to the host." : reason);
        }

        void Set(NetState state, string message)
        {
            State = state;
            StatusMessage = message;
            StateChanged?.Invoke(state);
        }
    }
}
