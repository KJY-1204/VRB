using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// Feeds a small picture of the pins and the TV score to a screen beside the player's view. The camera is switched
    /// on only while something moves or changes (rolling, pins settling, a phase change), and otherwise the picture
    /// from the last update stays, so the second render costs little on the headset.
    /// </summary>
    public class MirrorView : MonoBehaviour
    {
        [SerializeField] Camera m_Camera;
        [SerializeField] GameController m_Game;
        [SerializeField] float m_AfterChangeSeconds = 1f;

        GameFlow m_Flow;
        float m_ActiveUntil;

        public bool IsRendering => m_Camera.enabled;

        void Start()
        {
            m_Flow = m_Game.Flow;
            m_Flow.PhaseChanged += OnPhaseChanged;
            m_ActiveUntil = Time.time + m_AfterChangeSeconds;
        }

        void OnDestroy()
        {
            if (m_Flow != null) m_Flow.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(GamePhase previous, GamePhase current)
        {
            m_ActiveUntil = Time.time + m_AfterChangeSeconds;
        }

        void Update()
        {
            var moving = m_Flow != null && (m_Flow.Phase == GamePhase.Throwing || m_Flow.Phase == GamePhase.Settling);
            m_Camera.enabled = moving || Time.time < m_ActiveUntil;
        }
    }
}
