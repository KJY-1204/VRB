namespace VRB.Core
{
    /// <summary>
    /// Watches a thrown ball and decides when it has "arrived": it reached the pin deck, stopped rolling
    /// short of it, or took too long. Judging the pins only starts after this, so pins standing still while
    /// the ball is still on its way are not mistaken for a finished throw.
    /// </summary>
    public sealed class ThrowWatcher
    {
        public float PinDeckZ { get; }
        public float StopSpeed { get; }
        public float StopSeconds { get; }
        public float TimeoutSeconds { get; }

        float m_Elapsed;
        float m_StoppedFor;

        public bool HasArrived { get; private set; }

        public ThrowWatcher(float pinDeckZ, float stopSpeed = 0.05f, float stopSeconds = 1f, float timeoutSeconds = 20f)
        {
            PinDeckZ = pinDeckZ;
            StopSpeed = stopSpeed;
            StopSeconds = stopSeconds;
            TimeoutSeconds = timeoutSeconds;
        }

        public void Reset()
        {
            m_Elapsed = 0f;
            m_StoppedFor = 0f;
            HasArrived = false;
        }

        /// <summary>Returns true only on the update where the ball is first considered arrived.</summary>
        public bool Update(float deltaTime, float ballZ, float ballSpeed)
        {
            if (HasArrived) return false;

            m_Elapsed += deltaTime;
            m_StoppedFor = ballSpeed < StopSpeed ? m_StoppedFor + deltaTime : 0f;

            if (ballZ >= PinDeckZ || m_StoppedFor >= StopSeconds || m_Elapsed >= TimeoutSeconds)
            {
                HasArrived = true;
                return true;
            }

            return false;
        }
    }
}
