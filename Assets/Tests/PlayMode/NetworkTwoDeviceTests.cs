using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>
    /// Two devices in one process: the scene's own network manager hosts, a second one joins it over the loopback
    /// address. This checks that what a player reports (name, throws) really reaches the other device, and that every
    /// device is given a lane of its own.
    /// </summary>
    public class NetworkTwoDeviceTests
    {
        NetworkSession m_HostSession;
        NetworkManager m_Host;
        NetworkManager m_Client;
        GameObject m_ClientObject;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            foreach (var old in Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None))
            {
                if (old.IsListening) old.Shutdown();
                Object.Destroy(old.gameObject);
            }

            NetPlayer.All.Clear();
            yield return null;
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            // The bridge would put the host's own game on whichever player appears; here the test reports by hand.
            Object.Destroy(Object.FindFirstObjectByType<NetworkBridge>());
            yield return null;

            m_HostSession = Object.FindFirstObjectByType<NetworkSession>();
            m_Host = m_HostSession.Manager;
        }

        [UnityTearDown]
        public IEnumerator Leave()
        {
            if (m_Client != null && m_Client.IsListening) m_Client.Shutdown();
            if (m_ClientObject != null) Object.Destroy(m_ClientObject);
            if (m_HostSession != null) m_HostSession.Disconnect();

            var waited = 0f;
            while (m_Host != null && (m_Host.IsListening || m_Host.ShutdownInProgress) && waited < 3f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        NetPlayer PlayerOn(NetworkManager manager, ulong ownerClientId) =>
            NetPlayer.All.FirstOrDefault(p => p != null && p.IsSpawned && p.NetworkManager == manager && p.OwnerClientId == ownerClientId);

        IEnumerator UntilTrue(System.Func<bool> condition, float seconds = 5f)
        {
            var waited = 0f;
            while (!condition() && waited < seconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>Starts hosting, then joins with a second network manager and waits until both players exist.</summary>
        IEnumerator HostAndJoin()
        {
            Assert.That(m_HostSession.StartHost(), Is.True);
            yield return UntilTrue(() => PlayerOn(m_Host, m_Host.LocalClientId) != null);

            m_ClientObject = new GameObject("Second device");
            m_Client = m_ClientObject.AddComponent<NetworkManager>();
            var transport = m_ClientObject.AddComponent<UnityTransport>();
            m_Client.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = m_Host.NetworkConfig.PlayerPrefab,
                ConnectionApproval = true,
            };
            foreach (var list in m_Host.NetworkConfig.Prefabs.NetworkPrefabsLists)
                m_Client.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            m_Client.NetworkConfig.Prefabs.Initialize();

            transport.SetConnectionData("127.0.0.1", m_HostSession.Port);
            Assert.That(m_Client.StartClient(), Is.True);

            yield return UntilTrue(() => m_Client.IsConnectedClient && PlayerOn(m_Client, m_Client.LocalClientId) != null);
            Assert.That(m_Client.IsConnectedClient, Is.True, "The second device should have joined.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryDevice_GetsALaneOfItsOwn()
        {
            yield return HostAndJoin();

            var hostPlayer = PlayerOn(m_Host, m_Host.LocalClientId);
            var clientPlayerOnHost = PlayerOn(m_Host, m_Client.LocalClientId);
            Assert.That(clientPlayerOnHost, Is.Not.Null, "The host should have a player object for the joined device.");

            yield return UntilTrue(() => clientPlayerOnHost.Lane >= 0);
            Assert.That(hostPlayer.Lane, Is.EqualTo(0));
            Assert.That(clientPlayerOnHost.Lane, Is.EqualTo(1));

            var clientPlayer = PlayerOn(m_Client, m_Client.LocalClientId);
            yield return UntilTrue(() => clientPlayer.Lane == 1);
            Assert.That(clientPlayer.Lane, Is.EqualTo(1), "The joined device is told its lane.");
        }

        [UnityTest]
        public IEnumerator ANameAndThrowsReportedByTheJoinedDevice_ReachTheHost()
        {
            yield return HostAndJoin();
            var clientPlayer = PlayerOn(m_Client, m_Client.LocalClientId);
            var onHost = PlayerOn(m_Host, m_Client.LocalClientId);

            clientPlayer.SetName("Bo");
            clientPlayer.PublishThrows(new[] { 10, 7, 2 });
            yield return UntilTrue(() => onHost.PlayerName == "Bo" && onHost.Throws.Count == 3);

            Assert.That(onHost.PlayerName, Is.EqualTo("Bo"));
            Assert.That(onHost.Throws, Is.EqualTo(new[] { 10, 7, 2 }));

            var match = Match.Replay(new[] { (onHost.PlayerName, onHost.Throws) }, 0);
            Assert.That(match.Players[0].Session.Score, Is.EqualTo(28), "The host can work out the other player's score.");
        }

        [UnityTest]
        public IEnumerator TheHostsReport_ReachesTheJoinedDevice()
        {
            yield return HostAndJoin();
            var hostPlayer = PlayerOn(m_Host, m_Host.LocalClientId);
            var onClient = PlayerOn(m_Client, m_Host.LocalClientId);
            Assert.That(onClient, Is.Not.Null, "The joined device should also see the host's player.");

            hostPlayer.SetName("Ann");
            hostPlayer.PublishThrows(new[] { 3, 4 });
            yield return UntilTrue(() => onClient.PlayerName == "Ann" && onClient.Throws.Count == 2);

            Assert.That(onClient.PlayerName, Is.EqualTo("Ann"));
            Assert.That(onClient.Throws, Is.EqualTo(new[] { 3, 4 }));
        }

        [UnityTest]
        public IEnumerator APictureOfTheLane_ReachesTheOtherDevice_ButNotTheSender()
        {
            yield return HostAndJoin();
            var clientPlayer = PlayerOn(m_Client, m_Client.LocalClientId);
            var onHost = PlayerOn(m_Host, m_Client.LocalClientId);

            var picture = new LaneSnapshot { ActiveMask = LaneSnapshot.AllActive & ~(1u << 2) };
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
            {
                picture.Positions[i] = new Vector3(0.1f * i, 0.2f, 5f);
                picture.Rotations[i] = Quaternion.identity;
            }

            // Unreliable, so send a few times, as the broadcaster does.
            for (var attempt = 0; attempt < 20 && onHost.SnapshotTime < 0f; attempt++)
            {
                clientPlayer.SendSnapshot(picture);
                yield return new WaitForSeconds(0.1f);
            }

            Assert.That(onHost.SnapshotTime, Is.GreaterThanOrEqualTo(0f), "The picture should arrive.");
            Assert.That(onHost.Snapshot.Positions[4].x, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(onHost.Snapshot.IsActive(2), Is.False);
            Assert.That(clientPlayer.SnapshotTime, Is.LessThan(0f), "The sender does not get its own picture back.");
        }

        [UnityTest]
        public IEnumerator APoseOfTheHeadAndHands_ReachesTheOtherDevice()
        {
            yield return HostAndJoin();
            var clientPlayer = PlayerOn(m_Client, m_Client.LocalClientId);
            var onHost = PlayerOn(m_Host, m_Client.LocalClientId);

            var pose = new AvatarSnapshot();
            for (var i = 0; i < AvatarSnapshot.PartCount; i++)
            {
                pose.Positions[i] = new Vector3(i, 1.2f, -1.6f);
                pose.Rotations[i] = Quaternion.identity;
            }

            for (var attempt = 0; attempt < 20 && onHost.PoseTime < 0f; attempt++)
            {
                clientPlayer.SendPose(pose);
                yield return new WaitForSeconds(0.1f);
            }

            Assert.That(onHost.PoseTime, Is.GreaterThanOrEqualTo(0f), "The pose should arrive.");
            Assert.That(onHost.Pose.Positions[AvatarSnapshot.RightHand].x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(clientPlayer.PoseTime, Is.LessThan(0f), "The sender does not get its own pose back.");
        }

        [UnityTest]
        public IEnumerator ANewGame_EmptiesTheThrowsOnTheOtherDevice()
        {
            yield return HostAndJoin();
            var clientPlayer = PlayerOn(m_Client, m_Client.LocalClientId);
            var onHost = PlayerOn(m_Host, m_Client.LocalClientId);
            clientPlayer.PublishThrows(new[] { 5, 4, 10 });
            yield return UntilTrue(() => onHost.Throws.Count == 3);

            clientPlayer.PublishThrows(new int[0]);
            yield return UntilTrue(() => onHost.Throws.Count == 0);

            Assert.That(onHost.Throws, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ThrowsAreOnlyAddedTo_NotRewritten()
        {
            yield return HostAndJoin();
            var clientPlayer = PlayerOn(m_Client, m_Client.LocalClientId);
            var onHost = PlayerOn(m_Host, m_Client.LocalClientId);

            clientPlayer.PublishThrows(new[] { 7 });
            yield return UntilTrue(() => onHost.Throws.Count == 1);
            clientPlayer.PublishThrows(new[] { 7, 3 });
            yield return UntilTrue(() => onHost.Throws.Count == 2);

            Assert.That(onHost.Throws, Is.EqualTo(new[] { 7, 3 }));
        }
    }
}
