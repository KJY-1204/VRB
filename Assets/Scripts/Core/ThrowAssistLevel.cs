namespace VRB.Core
{
    /// <summary>Throw correction levels the player picks before a game starts.</summary>
    public enum ThrowAssistLevel
    {
        RawPhysics = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3,
    }

    public static class ThrowAssist
    {
        public const ThrowAssistLevel Default = ThrowAssistLevel.Weak;
    }
}
