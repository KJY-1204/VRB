using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRB
{
    /// <summary>
    /// A chair the player can sit on to rest. Pointing the ray at it and pressing the trigger moves the rig so the
    /// head is over the seat, and doing it again returns the player to where they stood. The tracking origin stays
    /// on the floor, so the real sitting height carries over.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class SeatSpot : MonoBehaviour
    {
        [SerializeField] Transform m_Rig;
        [SerializeField] Camera m_Head;

        XRSimpleInteractable m_Interactable;
        Vector3 m_StandingPosition;

        public bool IsSeated { get; private set; }

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
        }

        void OnEnable()
        {
            m_Interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args) => Toggle();

        public void Toggle()
        {
            if (IsSeated) Stand();
            else Sit();
        }

        public void Sit()
        {
            if (IsSeated) return;

            m_StandingPosition = m_Rig.position;
            var toSeat = transform.position - m_Head.transform.position;
            m_Rig.position += new Vector3(toSeat.x, 0f, toSeat.z);
            IsSeated = true;
        }

        public void Stand()
        {
            if (!IsSeated) return;

            m_Rig.position = m_StandingPosition;
            IsSeated = false;
        }
    }
}
