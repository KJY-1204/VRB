using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRB
{
    /// <summary>
    /// Lets the ball be grabbed only when the hand is close to it, and only while the game allows throwing.
    /// The rig's far ray is kept for menus, so reach is limited here instead of turning the ray off.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class NearGrabFilter : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] float m_MaxReach = 0.35f;

        XRGrabInteractable m_Grab;

        /// <summary>Extra gate set by the game: return false to forbid grabbing (e.g. before the game starts).</summary>
        public Func<bool> CanGrab { get; set; }

        public bool canProcess => isActiveAndEnabled;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            m_Grab.selectFilters.Add(this);
        }

        void OnDisable()
        {
            m_Grab.selectFilters.Remove(this);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // Never interrupt a grab that is already in progress.
            if (interactable.interactorsSelecting.Contains(interactor)) return true;

            if (CanGrab != null && !CanGrab()) return false;

            var attach = interactor.GetAttachTransform(interactable);
            return Vector3.Distance(attach.position, transform.position) <= m_MaxReach;
        }
    }
}
