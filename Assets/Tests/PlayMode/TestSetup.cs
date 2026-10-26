using UnityEngine;

namespace VRB.Tests
{
    /// <summary>
    /// The game opens on the main screen, but nearly every test wants the start screen (lane, players, throw assist)
    /// straight away, so it is switched off for all of them. <see cref="TitleTests"/> turns it back on for itself.
    /// This runs every time Play mode starts, because entering it resets static values.
    /// </summary>
    static class TestSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SkipTheMainScreen() => TitleView.ShowAtStart = false;
    }
}
