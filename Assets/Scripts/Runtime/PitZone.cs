using UnityEngine;

namespace VRB
{
    /// <summary>
    /// Volume over the pit behind the pin deck. Pins ask it whether they are inside, which is more robust
    /// than counting trigger enter/exit events (those can go stale when bodies bounce or sleep).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PitZone : MonoBehaviour
    {
        Collider m_Volume;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void Awake()
        {
            m_Volume = GetComponent<Collider>();
        }

        public bool Contains(Vector3 worldPoint)
        {
            return m_Volume.bounds.Contains(worldPoint);
        }
    }
}
