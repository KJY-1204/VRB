using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VRB.Tests
{
    public class SeatTests
    {
        SeatSpot m_Seat;
        Transform m_Rig;
        Camera m_Head;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Seat = Object.FindFirstObjectByType<SeatSpot>();
            m_Rig = GameObject.Find("XR Origin (XR Rig)").transform;
            m_Head = m_Rig.GetComponentInChildren<Camera>();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest]
        public IEnumerator Sitting_PutsTheHeadOverTheSeat_AndStandingReturnsToTheSpot()
        {
            Assert.That(m_Seat, Is.Not.Null, "The scene has a seat.");
            var standing = m_Rig.position;
            var headHeight = m_Head.transform.position.y;

            m_Seat.Sit();
            yield return null;

            Assert.That(m_Seat.IsSeated, Is.True);
            Assert.That(Vector3.Distance(Flat(m_Head.transform.position), Flat(m_Seat.transform.position)),
                Is.LessThan(0.05f), "The head ends up over the seat.");
            Assert.That(m_Head.transform.position.y, Is.EqualTo(headHeight).Within(0.001f),
                "Height is left to the real sitting posture.");

            m_Seat.Stand();
            yield return null;

            Assert.That(m_Seat.IsSeated, Is.False);
            Assert.That(Vector3.Distance(m_Rig.position, standing), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator Toggle_SitsThenStands_AndRepeatedSitDoesNotLoseTheStandingSpot()
        {
            var standing = m_Rig.position;

            m_Seat.Toggle();
            m_Seat.Sit();
            Assert.That(m_Seat.IsSeated, Is.True);

            m_Seat.Toggle();
            yield return null;

            Assert.That(m_Seat.IsSeated, Is.False);
            Assert.That(Vector3.Distance(m_Rig.position, standing), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator TheSeat_IsInsideTheHall_AndReachableByTheRay()
        {
            var collider = m_Seat.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(Mathf.Abs(m_Seat.transform.position.x), Is.LessThan(2.4f), "Inside the side walls.");
            Assert.That(m_Seat.transform.position.z, Is.InRange(-4.9f, -1.0f), "Behind the foul line, in front of the back wall.");
            yield return null;
        }
    }
}
