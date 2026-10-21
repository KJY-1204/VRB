using UnityEngine;
using VRB.Core;

namespace VRB
{
    /// <summary>The colors shared by every in-game screen, so the menu, the boards and the TV look like one set.</summary>
    public static class UiPalette
    {
        public static readonly Color Panel = new Color(0.05f, 0.06f, 0.13f, 0.94f);
        public static readonly Color Card = new Color(0.10f, 0.12f, 0.24f, 1f);
        public static readonly Color Button = new Color(0.15f, 0.18f, 0.32f, 1f);
        public static readonly Color Selected = new Color(0.08f, 0.55f, 0.95f, 1f);
        public static readonly Color Accent = new Color(0.16f, 0.78f, 1f, 1f);
        public static readonly Color Go = new Color(0.14f, 0.74f, 0.42f, 1f);
        public static readonly Color Muted = new Color(0.62f, 0.68f, 0.85f, 1f);
        public static readonly Color Amber = new Color(1f, 0.80f, 0.28f, 1f);
        public static readonly Color Strike = new Color(1f, 0.34f, 0.62f, 1f);

        /// <summary>Amber for ordinary messages, hot pink when the last throw was a strike.</summary>
        public static Color StatusColor(GamePhase phase, int lastPins) =>
            phase == GamePhase.ShowingResult && lastPins == GameSession.PinCount ? Strike : Amber;
    }
}
