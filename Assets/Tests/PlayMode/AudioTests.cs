using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>Checks the pin-physics sound requirements: rolling, impact and strike sounds are triggered.</summary>
    public class AudioTests
    {
        PinRack m_Rack;
        Rigidbody m_Ball;
        AudioSource m_RollSource;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("VRB", LoadSceneMode.Single);
            yield return null;

            m_Rack = Object.FindFirstObjectByType<PinRack>();
            var ball = GameObject.Find("Ball");
            m_Ball = ball.GetComponent<Rigidbody>();
            m_RollSource = ball.GetComponent<AudioSource>();
        }

        [UnityTest]
        public IEnumerator RollingBall_PlaysRollingSound_ThatFollowsSpeed()
        {
            Assert.That(m_RollSource.volume, Is.LessThan(0.05f), "A resting ball should be silent.");

            m_Ball.position = new Vector3(0.02f, 0.112f, 0.3f);
            m_Ball.linearVelocity = new Vector3(0f, 0f, 6f);
            yield return new WaitForSeconds(0.8f);

            Assert.That(m_RollSource.isPlaying, Is.True);
            Assert.That(m_RollSource.volume, Is.GreaterThan(0.3f), "A ball rolling at speed should be clearly audible.");
        }

        [UnityTest]
        public IEnumerator BallHittingPins_PlaysImpactSound()
        {
            var voices = m_Rack.GetComponentsInChildren<AudioSource>().Where(a => a.name.StartsWith("PinVoice")).ToArray();
            Assert.That(voices.Length, Is.GreaterThan(0));
            Assert.That(voices.Any(v => v.isPlaying), Is.False, "No impact sound before anything is hit.");

            m_Ball.position = new Vector3(0.02f, 0.112f, 0.3f);
            m_Ball.linearVelocity = new Vector3(0f, 0f, 8f);

            var heard = false;
            var waited = 0f;
            while (!heard && waited < 6f)
            {
                heard = voices.Any(v => v.isPlaying);
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(heard, Is.True, "Hitting the pins should play an impact sound.");
        }

        [UnityTest]
        public IEnumerator AllTenPinsDown_PlaysStrikeSound()
        {
            var strike = m_Rack.GetComponentsInChildren<AudioSource>().First(a => a.name == "StrikeVoice");
            Assert.That(strike.isPlaying, Is.False);

            foreach (var pin in m_Rack.GetComponentsInChildren<Pin>())
            {
                var body = pin.GetComponent<Rigidbody>();
                body.rotation = Quaternion.Euler(0f, 0f, 90f);
                body.position += Vector3.up * 0.06f;
            }

            m_Rack.BeginJudging();
            var waited = 0f;
            while (!strike.isPlaying && waited < 6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(strike.isPlaying, Is.True, "Knocking down all ten pins should play the strike sound.");
        }
    }
}
