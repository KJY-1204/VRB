using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

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

        static AvatarSnapshot Pose(float x)
        {
            var pose = new AvatarSnapshot();
            for (var i = 0; i < AvatarSnapshot.PartCount; i++)
            {
                pose.Positions[i] = new Vector3(x + i * 0.3f, 1.5f - i * 0.4f, -1.6f);
                pose.Rotations[i] = Quaternion.identity;
            }

            return pose;
        }

        [UnityTest]
        public IEnumerator APlayersHeadAndHands_AreDrawnAtTheirLane()
        {
            m_Lanes.Select(0);
            m_Remote.Apply(2, Sample(0f));
            Assert.That(m_Remote.Lane(2).IsAvatarShown, Is.False, "nothing to draw before a pose arrives");

            m_Remote.ApplyPose(2, Pose(0f));
            yield return null;

            var lane = m_Remote.Lane(2);
            Assert.That(lane.IsAvatarShown, Is.True);
            Assert.That(lane.AvatarPart(AvatarSnapshot.Head).localPosition.y, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(lane.AvatarPart(AvatarSnapshot.RightHand).localPosition.x, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(lane.AvatarPart(AvatarSnapshot.LeftHand).gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator TheAvatarGoesWhenTheLaneIsLeft()
        {
            m_Remote.Apply(1, Sample(0f));
            m_Remote.ApplyPose(1, Pose(0f));
            yield return null;

            m_Remote.Lane(1).Show(false);
            m_Remote.Lane(1).Show(true);
            Assert.That(m_Remote.Lane(1).IsAvatarShown, Is.False);
        }

        [UnityTest]
        public IEnumerator TheHandsGlideToANewPose()
        {
            m_Remote.Apply(1, Sample(0f));
            m_Remote.ApplyPose(1, Pose(0f));
            yield return null;

            m_Remote.Lane(1).SetPose(Pose(1f));
            yield return null;
            var x = m_Remote.Lane(1).AvatarPart(AvatarSnapshot.Head).localPosition.x;
            Assert.That(x, Is.GreaterThan(0f).And.LessThan(1f));

            for (var i = 0; i < 60; i++) yield return null;
            Assert.That(m_Remote.Lane(1).AvatarPart(AvatarSnapshot.Head).localPosition.x, Is.EqualTo(1f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator TheBroadcaster_ReadsTheHeadAndHandsOfTheRig()
        {
            var avatar = Object.FindFirstObjectByType<AvatarBroadcaster>();
            var pose = avatar.Capture();
            yield return null;

            Assert.That(pose.Positions[AvatarSnapshot.Head].z, Is.EqualTo(-1.6f).Within(1f), "the player stands near the foul line");
            Assert.That(pose.Positions[AvatarSnapshot.Head].y, Is.InRange(0.5f, 2.5f), "at head height");
            Assert.That(pose.Positions[AvatarSnapshot.LeftHand].z, Is.EqualTo(-1.6f).Within(1f));
            Assert.That(pose.Positions[AvatarSnapshot.RightHand].z, Is.EqualTo(-1.6f).Within(1f));
        }

        [Test]
        public void AStandingPlayer_HasLegsDownToTheFloor()
        {
            var frame = AvatarBody.Solve(new Vector3(0f, 1.6f, -1.6f), Quaternion.identity);

            Assert.That(frame.Seated, Is.False);
            Assert.That(frame.Hip.y, Is.LessThan(frame.Neck.y));
            Assert.That(frame.LegsSize.y, Is.EqualTo(frame.Hip.y).Within(0.001f));
            Assert.That(frame.LegsCenter.y, Is.EqualTo(frame.Hip.y / 2f).Within(0.001f));
        }

        [Test]
        public void ASittingPlayer_HasTheirLegsOutInFront()
        {
            var frame = AvatarBody.Solve(new Vector3(0f, 1.05f, -1.6f), Quaternion.identity);

            Assert.That(frame.Seated, Is.True);
            Assert.That(frame.LegsSize.y, Is.LessThan(0.2f), "thighs lie flat");
            Assert.That(frame.LegsCenter.z, Is.GreaterThan(frame.Hip.z + 0.1f), "and reach forward (+Z is down the lane)");
        }

        [Test]
        public void TheBody_TurnsWithTheHeadSideways_ButNotWithNodding()
        {
            var turned = AvatarBody.Solve(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 90f, 0f));
            Assert.That(turned.RightShoulder.z, Is.LessThan(turned.LeftShoulder.z - 0.3f), "facing +X puts the right shoulder at -Z");

            var nodding = AvatarBody.Solve(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(60f, 0f, 0f));
            var level = AvatarBody.Solve(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            Assert.That(nodding.Hip, Is.EqualTo(level.Hip).Using(new Vector3EqualityComparer(0.001f)));
        }

        [UnityTest]
        public IEnumerator TheBody_ReachesFromTheShouldersToTheHands()
        {
            m_Remote.Apply(2, Sample(0f));
            var pose = Pose(0f);
            pose.Rotations[AvatarSnapshot.Head] = Quaternion.identity;
            pose.Positions[AvatarSnapshot.RightHand] = new Vector3(0.6f, 1.0f, -1.2f);
            m_Remote.ApplyPose(2, pose);
            yield return null;

            var lane = m_Remote.Lane(2);
            Assert.That(lane.Torso.gameObject.activeInHierarchy, Is.True);
            var arm = lane.RightArm;
            var hand = lane.AvatarPart(AvatarSnapshot.RightHand).localPosition;
            var armTip = arm.localPosition + arm.localRotation * Vector3.up * arm.localScale.y;
            Assert.That(Vector3.Distance(armTip, hand), Is.LessThan(0.02f), "one end of the arm is at the hand");
        }

        [UnityTest]
        public IEnumerator TheName_IsShownAboveTheHead_AndGoesWithTheAvatar()
        {
            m_Remote.Apply(1, Sample(0f));
            m_Remote.Lane(1).SetName("Mina");
            m_Remote.ApplyPose(1, Pose(0f));
            yield return null;

            var lane = m_Remote.Lane(1);
            Assert.That(lane.DisplayedName, Is.EqualTo("Mina"));
            Assert.That(lane.NameTag.localPosition.y, Is.GreaterThan(lane.AvatarPart(AvatarSnapshot.Head).localPosition.y + 0.2f));

            lane.Show(false);
            lane.Show(true);
            Assert.That(lane.Torso.gameObject.activeInHierarchy, Is.False, "no body without a pose");
        }

        [Test]
        public void PosesOnlyDiffer_WhenAPartMovedEnough()
        {
            var a = Pose(0f);
            var b = Pose(0f);
            Assert.That(a.Differs(b), Is.False);

            b.Positions[AvatarSnapshot.LeftHand].y += 0.02f;
            Assert.That(a.Differs(b), Is.True);
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
