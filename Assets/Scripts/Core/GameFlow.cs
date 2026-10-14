using System;

namespace VRB.Core
{
    public enum GamePhase
    {
        SelectingLevel,
        Ready,
        Throwing,
        Settling,
        ShowingResult,
        Resetting,
    }

    /// <summary>
    /// Turn flow of one game: select level, then repeat ready -> throw -> settle -> show result -> reset.
    /// Pure logic; the scene drives it by reporting what happened (ball released, pins settled, ...).
    /// </summary>
    public sealed class GameFlow
    {
        public GameSession Session { get; }
        public GamePhase Phase { get; private set; } = GamePhase.SelectingLevel;
        public ThrowAssistLevel SelectedLevel { get; private set; } = ThrowAssist.Default;
        public int LastPinsFallen { get; private set; }

        /// <summary>Raised on every phase change with (previous, current).</summary>
        public event Action<GamePhase, GamePhase> PhaseChanged;

        public GameFlow() : this(new GameSession()) { }

        public GameFlow(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>True only while the player may grab and throw a ball.</summary>
        public bool CanThrow => Phase == GamePhase.Ready;

        /// <summary>Change the pending level. Only allowed before the game starts.</summary>
        public void SelectLevel(ThrowAssistLevel level)
        {
            Require(GamePhase.SelectingLevel, nameof(SelectLevel));
            SelectedLevel = level;
        }

        /// <summary>Confirm the selection and start the game.</summary>
        public void ConfirmSelection()
        {
            Require(GamePhase.SelectingLevel, nameof(ConfirmSelection));
            SetPhase(GamePhase.Ready);
        }

        /// <summary>The player let go of the ball and it is now rolling.</summary>
        public void BallReleased()
        {
            Require(GamePhase.Ready, nameof(BallReleased));
            SetPhase(GamePhase.Throwing);
        }

        /// <summary>The ball reached the pins (or finished rolling) and the pins are settling.</summary>
        public void ThrowReachedPins()
        {
            Require(GamePhase.Throwing, nameof(ThrowReachedPins));
            SetPhase(GamePhase.Settling);
        }

        /// <summary>The pins came to rest. Records the throw and moves to showing the result.</summary>
        public void PinsSettled(int pinsFallen)
        {
            Require(GamePhase.Settling, nameof(PinsSettled));
            Session.RecordThrow(pinsFallen);
            LastPinsFallen = pinsFallen;
            SetPhase(GamePhase.ShowingResult);
        }

        /// <summary>The result has been on screen long enough; reset pins and ball.</summary>
        public void ResultShown()
        {
            Require(GamePhase.ShowingResult, nameof(ResultShown));
            SetPhase(GamePhase.Resetting);
        }

        /// <summary>Pins and ball are back in place; the next throw may start.</summary>
        public void ResetComplete()
        {
            Require(GamePhase.Resetting, nameof(ResetComplete));
            SetPhase(GamePhase.Ready);
        }

        /// <summary>Clear the record and go back to level selection from any phase.</summary>
        public void Restart()
        {
            Session.Reset();
            LastPinsFallen = 0;
            SelectedLevel = ThrowAssist.Default;
            SetPhase(GamePhase.SelectingLevel);
        }

        void Require(GamePhase expected, string action)
        {
            if (Phase != expected)
                throw new InvalidOperationException($"{action} is not allowed in phase {Phase} (expected {expected}).");
        }

        void SetPhase(GamePhase next)
        {
            var previous = Phase;
            Phase = next;
            PhaseChanged?.Invoke(previous, next);
        }
    }
}
