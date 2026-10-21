using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>
    /// The colors shared by every in-game screen, so the menu, the boards and the TV look like one set. Panels, cards
    /// and buttons are see-through, like frosted glass; only text and the chosen/primary controls are (nearly) solid.
    /// </summary>
    public static class UiPalette
    {
        public static readonly Color Panel = new Color(0.04f, 0.06f, 0.15f, 0.62f);
        public static readonly Color Card = new Color(0.12f, 0.16f, 0.34f, 0.50f);
        public static readonly Color CardHighlight = new Color(0.10f, 0.40f, 0.78f, 0.60f);
        public static readonly Color Button = new Color(0.16f, 0.22f, 0.42f, 0.55f);
        public static readonly Color Selected = new Color(0.10f, 0.58f, 1f, 0.88f);
        public static readonly Color Accent = new Color(0.16f, 0.78f, 1f, 1f);
        public static readonly Color Edge = new Color(0.30f, 0.85f, 1f, 0.85f);
        public static readonly Color Go = new Color(0.14f, 0.78f, 0.45f, 0.90f);
        public static readonly Color Muted = new Color(0.72f, 0.78f, 0.92f, 1f);
        public static readonly Color Amber = new Color(1f, 0.82f, 0.30f, 1f);
        public static readonly Color Strike = new Color(1f, 0.38f, 0.66f, 1f);

        /// <summary>Amber for ordinary messages, hot pink for a strike and cyan for a spare.</summary>
        public static Color StatusColor(GamePhase phase, Match match)
        {
            if (phase != GamePhase.ShowingResult) return Amber;

            // The turn has not moved on yet, so the latest throw is the current player's.
            var mark = (match.LastThrower ?? match.Current).Session.Card.LastMark;
            return mark == "X" ? Strike : mark == "/" ? Accent : Amber;
        }
    }
}
