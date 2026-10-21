using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VRB.Tests
{
    public class MirrorTests
    {
        GameController m_Game;
        MirrorView m_Mirror;
        PeripheralFollow m_Follow;
        Camera m_Head;
        Transform m_Rig;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Game = Object.FindFirstObjectByType<GameController>();
            m_Mirror = Object.FindFirstObjectByType<MirrorView>();
            m_Follow = Object.FindFirstObjectByType<PeripheralFollow>();
            m_Rig = GameObject.Find("XR Origin (XR Rig)").transform;
            m_Head = m_Rig.GetComponentInChildren<Camera>();
        }

        [UnityTest]
        public IEnumerator TheCamera_FeedsATexture_AndLooksDownTheLaneAtThePinsAndTheTv()
        {
            var camera = m_Mirror.GetComponent<Camera>();
            Assert.That(camera.targetTexture, Is.Not.Null);
            Assert.That(camera.stereoTargetEye, Is.EqualTo(StereoTargetEyeMask.None), "It is not one of the headset's eyes.");

            var toPins = GameObject.Find("Pins").transform.position - camera.transform.position;
            Assert.That(Vector3.Angle(camera.transform.forward, toPins), Is.LessThan(camera.fieldOfView / 2f), "The pins are in view.");
            var toTv = GameObject.Find("TvCanvas").transform.position - camera.transform.position;
            Assert.That(Vector3.Angle(camera.transform.forward, toTv), Is.LessThan(camera.fieldOfView / 2f), "The TV is in view.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheCamera_RendersOnlyWhileSomethingChanges()
        {
            yield return new WaitForSeconds(1.5f);
            Assert.That(m_Mirror.IsRendering, Is.False, "Idle in the menu: the picture stays as it was.");

            m_Game.Flow.ConfirmSelection();
            yield return null;
            Assert.That(m_Mirror.IsRendering, Is.True, "A phase change refreshes it.");

            yield return new WaitForSeconds(1.5f);
            Assert.That(m_Mirror.IsRendering, Is.False, "Waiting for the throw, the picture stays as it was.");

            m_Game.Flow.BallReleased();
            yield return null;
            Assert.That(m_Mirror.IsRendering, Is.True, "While the ball rolls and the pins settle it renders.");
        }

        [UnityTest]
        public IEnumerator ThePanel_SitsToTheLeftAndAbove_InFrontOfTheHead()
        {
            m_Follow.SnapToTarget();
            yield return null;

            var local = m_Head.transform.InverseTransformPoint(m_Follow.transform.position);
            Assert.That(local.z, Is.GreaterThan(0.5f), "In front of the player.");
            Assert.That(local.x, Is.LessThan(-0.3f), "To the left.");
            Assert.That(local.y, Is.GreaterThan(0f), "A little above the eyes.");
            Assert.That(Vector3.Distance(m_Follow.transform.position, m_Head.transform.position), Is.LessThan(1.5f));
        }

        [UnityTest]
        public IEnumerator ThePanel_FollowsTheHeading_ButNotLookingDown()
        {
            m_Follow.SnapToTarget();
            var height = m_Follow.transform.position.y;

            m_Rig.Rotate(0f, 90f, 0f);
            yield return new WaitForSeconds(2f);

            var local = m_Head.transform.InverseTransformPoint(m_Follow.transform.position);
            Assert.That(local.z, Is.GreaterThan(0.5f), "It came around to the front again.");
            Assert.That(local.x, Is.LessThan(-0.3f), "Still on the left.");
            Assert.That(m_Follow.transform.position.y, Is.EqualTo(height).Within(0.05f));
        }
    }
}
