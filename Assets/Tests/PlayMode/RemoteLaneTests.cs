using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VRB.Tests
{
    /// <summary>Another player's lane drawn live on this device, and this device's own lane captured for sending.</summary>
    public class RemoteLaneTests
    {
        RemoteLanes m_Remote;
        LaneSelector m_Lanes;
        LaneBroadcaster m_Broadcaster;

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

            m_Remote = Object.FindFirstObjectByType<RemoteLanes>();
            m_Lanes = Object.FindFirstObjectByType<LaneSelector>();
            m_Broadcaster = Object.FindFirstObjectByType<LaneBroadcaster>();
        }

        static LaneSnapshot Sample(float x)
        {
            var snapshot = new LaneSnapshot { ActiveMask = LaneSnapshot.AllActive };
            for (var i = 0; i < LaneSnapshot.BodyCount; i++)
            {
                snapshot.Positions[i] = new Vector3(x, 0.1f, 10f + i * 0.1f);
                snapshot.Rotations[i] = Quaternion.identity;
            }

            return snapshot;
        }

        [UnityTest]
        public IEnumerator NobodyElseOnline_NoRemoteLaneShows()
        {
            for (var i = 0; i < m_Lanes.LaneCount; i++)
                Assert.That(m_Remote.Lane(i).IsShown, Is.False, "lane " + i);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AReportedLane_ShowsNextToThePlayedLane_AtTheReportedSpots()
        {
            m_Lanes.Select(0);
            m_Remote.Apply(2, Sample(0f));
            yield return null;

            var lane = m_Remote.Lane(2);
            Assert.That(lane.IsShown, Is.True);
            Assert.That(lane.transform.localPosition.x, Is.EqualTo(m_Lanes.SidewaysFromPlayed(2)).Within(0.001f));
            Assert.That(lane.Body(3).localPosition.z, Is.EqualTo(10.3f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator ASweptPin_IsHidden()
        {
            var snapshot = Sample(0f);
            snapshot.ActiveMask &= ~(1u << 4);
            m_Remote.Apply(1, snapshot);
            yield return null;

            Assert.That(m_Remote.Lane(1).Body(4).gameObject.activeSelf, Is.False);
            Assert.That(m_Remote.Lane(1).Body(5).gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator TheStandInsGlideToANewPicture()
        {
            m_Remote.Apply(1, Sample(0f));
            yield return null;
            var lane = m_Remote.Lane(1);

            lane.SetTarget(Sample(0.5f));
            yield return null;
            var first = lane.Body(0).localPosition.x;
            Assert.That(first, Is.GreaterThan(0f).And.LessThan(0.5f));

            for (var i = 0; i < 60; i++) yield return null;
            Assert.That(lane.Body(0).localPosition.x, Is.EqualTo(0.5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator TheLanePlayedIsNeverDrawnAsRemote()
        {
            m_Lanes.Select(1);
            yield return null;
            m_Remote.Refresh();
            Assert.That(m_Remote.Lane(1).IsShown, Is.False);
        }

        [UnityTest]
        public IEnumerator ACapture_HasAllTenPinsAndTheBall_WhereTheyStand()
        {
            var snapshot = m_Broadcaster.Capture();
            yield return null;

            Assert.That(snapshot.ActiveMask, Is.EqualTo(LaneSnapshot.AllActive));
            var headPin = snapshot.Positions[0];
            Assert.That(headPin.z, Is.EqualTo(Core.BowlingDimensions.LaneLength).Within(0.1f));
        }

        [Test]
        public void SnapshotsOnlyDiffer_WhenSomethingMovedEnough()
        {
            var a = Sample(0f);
            var b = Sample(0f);
            Assert.That(a.Differs(b), Is.False);

            b.Positions[LaneSnapshot.BallIndex].x += 0.01f;
            Assert.That(a.Differs(b), Is.True);

            b = Sample(0f);
            b.ActiveMask &= ~1u;
            Assert.That(a.Differs(b), Is.True);
        }

        [Test]
        public void ASnapshotOfTheWrongShape_IsRefused()
        {
            var snapshot = new LaneSnapshot();
            Assert.That(snapshot.TrySet(new Vector3[3], new Quaternion[3], 0u), Is.False);
            Assert.That(snapshot.TrySet(null, null, 0u), Is.False);
        }
    }
}
