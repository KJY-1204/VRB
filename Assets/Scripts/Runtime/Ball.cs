using UnityEngine;

namespace VRB
{
    /// <summary>Marks the bowling ball and gives other systems quick access to its rigidbody.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Ball : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }
    }
}
