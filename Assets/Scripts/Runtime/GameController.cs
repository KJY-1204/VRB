using System.Collections;
using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// Connects the scene to the game flow: level selection, throwing, judging, showing the result,
    /// resetting pins and ball, and restarting.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] PinRack m_Rack;
        [SerializeField] BallThrower m_Thrower;
        [SerializeField] BallRespawner m_Respawner;
        [SerializeField] NearGrabFilter m_GrabFilter;
        [SerializeField] LevelSelectionView m_SelectionView;
        [SerializeField] ScoreboardView m_Scoreboard;
        [SerializeField] TvScoreView m_Tv;
        [SerializeField] float m_ResultSeconds = 3f;

        ThrowWatcher m_Watcher;
        Rigidbody m_BallBody;
        Coroutine m_ResultRoutine;

        public GameFlow Flow { get; private set; }

        void Awake()
        {
            Flow = new GameFlow();
            m_Watcher = new ThrowWatcher(BowlingDimensions.LaneLength - 0.3f);
            m_BallBody = m_Thrower.GetComponent<Rigidbody>();
        }

        void OnEnable()
        {
            Flow.PhaseChanged += OnPhaseChanged;
            m_GrabFilter.CanGrab = () => Flow.CanThrow;
            m_Thrower.Thrown += OnThrown;
            m_Rack.Judged += OnJudged;
            m_SelectionView.LevelPicked += OnLevelPicked;
            m_SelectionView.StartPressed += OnStartPressed;
            m_Scoreboard.RestartPressed += Restart;
        }

        void OnDisable()
        {
            Flow.PhaseChanged -= OnPhaseChanged;
            m_GrabFilter.CanGrab = null;
            m_Thrower.Thrown -= OnThrown;
            m_Rack.Judged -= OnJudged;
            m_SelectionView.LevelPicked -= OnLevelPicked;
            m_SelectionView.StartPressed -= OnStartPressed;
            m_Scoreboard.RestartPressed -= Restart;
        }

        void Start()
        {
            ShowCurrentPhase();
        }

        void Update()
        {
            if (Flow.Phase != GamePhase.Throwing) return;

            if (m_Watcher.Update(Time.deltaTime, m_BallBody.position.z, m_BallBody.linearVelocity.magnitude))
            {
                Flow.ThrowReachedPins();
                m_Rack.BeginJudging();
            }
        }

        void OnLevelPicked(ThrowAssistLevel level)
        {
            if (Flow.Phase == GamePhase.SelectingLevel) Flow.SelectLevel(level);
        }

        void OnStartPressed()
        {
            if (Flow.Phase != GamePhase.SelectingLevel) return;

            m_Thrower.Level = Flow.SelectedLevel;
            Flow.ConfirmSelection();
        }

        void OnThrown(Vector3 velocity)
        {
            if (!Flow.CanThrow) return;

            m_Watcher.Reset();
            Flow.BallReleased();
        }

        void OnJudged(int fallen)
        {
            if (Flow.Phase != GamePhase.Settling) return;

            Flow.PinsSettled(fallen);
            m_ResultRoutine = StartCoroutine(FinishResult());
        }

        IEnumerator FinishResult()
        {
            yield return new WaitForSeconds(m_ResultSeconds);

            Flow.ResultShown();
            m_Rack.ResetAll();
            m_Respawner.ReturnToStand();
            Flow.ResetComplete();
            m_ResultRoutine = null;
        }

        /// <summary>Clear the record and return to level selection.</summary>
        public void Restart()
        {
            if (m_ResultRoutine != null)
            {
                StopCoroutine(m_ResultRoutine);
                m_ResultRoutine = null;
            }

            m_Rack.ResetAll();
            m_Respawner.ReturnToStand();
            Flow.Restart();
            m_SelectionView.SetSelected(Flow.SelectedLevel);
        }

        void OnPhaseChanged(GamePhase previous, GamePhase current) => ShowCurrentPhase();

        void ShowCurrentPhase()
        {
            var selecting = Flow.Phase == GamePhase.SelectingLevel;
            m_SelectionView.SetVisible(selecting);
            m_Scoreboard.SetVisible(!selecting);
            m_Scoreboard.Refresh(Flow.Phase, Flow.Session, Flow.LastPinsFallen);
            if (m_Tv != null) m_Tv.Refresh(Flow.Phase, Flow.Session, Flow.LastPinsFallen);
        }
    }
}
