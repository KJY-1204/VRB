using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VRB.Tests
{
    public class ScoreboardPlacementTests
    {
        [UnityTest]
        public IEnumerator TheScoreboard_IsBesideThePlayerOnTheLeft_AndFacesThem()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;

            var board = Object.FindFirstObjectByType<ScoreboardView>(FindObjectsInactive.Include).transform;
            var head = GameObject.Find("XR Origin (XR Rig)").GetComponentInChildren<Camera>().transform;

            var toBoard = board.position - head.position;
            var flat = new Vector3(toBoard.x, 0f, toBoard.z);
            var signedAngle = Vector3.SignedAngle(Vector3.forward, flat, Vector3.up);

            Assert.That(signedAngle, Is.InRange(-100f, -60f), "Off to the left, so the head has to turn to see it.");
            Assert.That(flat.magnitude, Is.InRange(0.8f, 3f), "Close enough to read.");
            Assert.That(Mathf.Abs(board.position.y - head.position.y), Is.LessThan(0.4f), "About eye height.");
            // A world-space canvas reads correctly when looked at along its forward, so that points away from the player.
            Assert.That(Vector3.Dot(board.forward, flat.normalized), Is.GreaterThan(0.9f), "Its front faces the player.");
        }
    }
}
