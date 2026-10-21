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
        GameOver,
    }

    /// <summary>
    /// Turn flow of one match: select level and the number of players, then repeat ready -> throw -> settle -> show
    /// result -> reset, with the players taking turns, until everyone's tenth frame is done and the game is over. Pure
    /// logic; the scene drives it by reporting what happened (ball released, pins settled, ...).
    /// </summary>
    public sealed class GameFlow
    {
        public Match Match { get; private set; }

        /// <summary>The player whose turn it is. During the result of a throw it is still the one who threw.</summary>
        public Player CurrentPlayer => Match.Current;

        /// <summary>The current player's game; with a single player, simply the game.</summary>
        public GameSession Session => Match.Current.Session;

        public GamePhase Phase { get; private set; } = GamePhase.SelectingLevel;
        public ThrowAssistLevel SelectedLevel { get; private set; } = ThrowAssist.Default;
        public int LastPinsFallen { get; private set; }

        /// <summary>Raised on every phase change with (previous, current).</summary>
        public event Action<GamePhase, GamePhase> PhaseChanged;

        /// <summary>Raised when the turn passes to another player, with (previous, current).</summary>
        public event Action<Player, Player> TurnChanged;

        public GameFlow() : this(new Match()) { }

        public GameFlow(Match match)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
        }

        /// <summary>Set how many people play. Only allowed before the game starts; every scorecard starts empty.</summary>
        public void SetPlayerCount(int playerCount)
        {
            Require(GamePhase.SelectingLevel, nameof(SetPlayerCount));
            Match = new Match(playerCount);
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
            Match.RecordThrow(pinsFallen);
            LastPinsFallen = pinsFallen;
            SetPhase(GamePhase.ShowingResult);
        }

        /// <summary>
        /// True when the pins about to be reset should all be set up again (a new frame for whoever is up next, or a
        /// fresh rack in the tenth); false when the standing ones stay and only the fallen are cleared (second ball of
        /// a frame). Read it after <see cref="ResultShown"/>, which is when the turn moves on.
        /// </summary>
        public bool NeedsFreshRack => Session.PinsStanding == GameSession.PinCount;

        /// <summary>
        /// The result has been on screen long enough. The turn passes to the next player if the thrower's frame is
        /// over; then it goes on to reset pins and ball, or, if that was the last throw of the match, to the game-over
        /// screen.
        /// </summary>
        public void ResultShown()
        {
            Require(GamePhase.ShowingResult, nameof(ResultShown));

            var previous = Match.Current;
            if (Match.AdvanceTurn()) TurnChanged?.Invoke(previous, Match.Current);

            SetPhase(Match.IsComplete ? GamePhase.GameOver : GamePhase.Resetting);
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
            Match.Reset();
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
