using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    public class LaneSelectionTests
    {
        GameController m_Game;
        LevelSelectionView m_Selection;
        LaneSelector m_Lanes;
        Transform m_Hall;
        Transform m_Pins;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;
            yield return null;

            m_Game = Object.FindFirstObjectByType<GameController>();
            m_Selection = Object.FindFirstObjectByType<LevelSelectionView>(FindObjectsInactive.Include);
            m_Lanes = Object.FindFirstObjectByType<LaneSelector>();
            m_Hall = GameObject.Find("Hall").transform;
            m_Pins = GameObject.Find("Pins").transform;
        }

        static GameObject DecorPins(int lane) => GameObject.Find("Hall").transform.Find("FbxPins_Lane" + (lane + 1)).gameObject;

        [UnityTest]
        public IEnumerator TheHall_HasFourLanes_AndStartsOnTheFirst()
        {
            Assert.That(m_Lanes.LaneCount, Is.EqualTo(4));
            Assert.That(m_Lanes.Selected, Is.EqualTo(0));
            Assert.That(m_Selection.IsLaneHighlighted(0), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PickingALane_SlidesTheHall_AndLeavesTheGamesOwnLaneAlone()
        {
            var hallStart = m_Hall.position;
            var pinsStart = m_Pins.position;

            m_Selection.PickLane(2);
            yield return null;

            Assert.That(m_Lanes.Selected, Is.EqualTo(2));
            Assert.That(m_Hall.position.x, Is.EqualTo(hallStart.x - 3.55f).Within(0.01f), "The third lane slides to the center.");
            Assert.That(m_Hall.position.z, Is.EqualTo(hallStart.z).Within(0.001f), "It only moves sideways.");
            Assert.That(m_Pins.position, Is.EqualTo(pinsStart), "The game's pins stay where they are.");
            Assert.That(m_Selection.IsLaneHighlighted(2), Is.True);
            Assert.That(m_Selection.IsLaneHighlighted(0), Is.False);
        }

        [UnityTest]
        public IEnumerator OnlyTheLanesWithoutRealPins_ShowDecorativePins()
        {
            m_Selection.PickLane(3);
            yield return null;

            for (var lane = 0; lane < 4; lane++)
                Assert.That(DecorPins(lane).activeSelf, Is.EqualTo(lane != 3), "Lane " + (lane + 1));
        }

        [UnityTest]
        public IEnumerator EachLane_HasItsOwnChair_AndOnlyTheCurrentOneIsActive()
        {
            Assert.That(Object.FindObjectsByType<SeatSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(4));
            Assert.That(Object.FindObjectsByType<SeatSpot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            m_Selection.PickLane(1);
            yield return null;

            var seat = Object.FindFirstObjectByType<SeatSpot>();
            Assert.That(seat.name, Is.EqualTo("Seat_Lane2"));
            Assert.That(Mathf.Abs(seat.transform.position.x), Is.LessThan(2.4f), "The chair is inside the side walls.");
        }

        [UnityTest]
        public IEnumerator ChangingLane_StandsThePlayerUpFirst()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)").transform;
            var standing = rig.position;
            Object.FindFirstObjectByType<SeatSpot>().Sit();
            Assert.That(rig.position, Is.Not.EqualTo(standing));

            m_Selection.PickLane(1);
            yield return null;

            Assert.That(Vector3.Distance(rig.position, standing), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator TheLane_CannotBeChangedOnceTheGameHasStarted_AndSurvivesARestart()
        {
            m_Selection.PickLane(2);
            m_Game.Flow.ConfirmSelection();

            m_Selection.PickLane(1);
            yield return null;
            Assert.That(m_Lanes.Selected, Is.EqualTo(2), "Mid-game the lane stays.");

            m_Game.Restart();
            yield return null;
            Assert.That(m_Game.Flow.Phase, Is.EqualTo(GamePhase.SelectingLevel));
            Assert.That(m_Lanes.Selected, Is.EqualTo(2), "A restart keeps the lane, so the player can change it.");

            m_Selection.PickLane(0);
            yield return null;
            Assert.That(m_Lanes.Selected, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TheTv_ShowsTheScore_OnAnyLane()
        {
            m_Selection.PickLane(3);
            m_Game.Flow.ConfirmSelection();
            m_Game.Flow.BallReleased();
            m_Game.Flow.ThrowReachedPins();
            m_Game.Flow.PinsSettled(6);
            yield return null;

            var tv = Object.FindFirstObjectByType<TvScoreView>();
            Assert.That(tv.TotalText, Is.EqualTo("6"));
            Assert.That(tv.transform.position.x, Is.EqualTo(0f).Within(0.01f), "The TV stays over the lane in play.");
        }
    }
}
