using UnityEngine;
using UnityEngine.Events;

namespace AtLightsEnd
{
    /// <summary>Place on the root of a prop. Its clickable parts need non-trigger 3D colliders.</summary>
    [DisallowMultipleComponent]
    public sealed class InspectableObject : MonoBehaviour
    {
        [SerializeField] private bool allowRotation = true;
        [Tooltip("Optional pivot inside this object; otherwise the renderer bounds centre is used.")]
        [SerializeField] private Transform inspectionPivot;
        [SerializeField] private UnityEvent onInspectionStarted = new UnityEvent();
        [SerializeField] private UnityEvent onInspectionEnded = new UnityEvent();

        public bool AllowRotation => allowRotation;
        public UnityEvent OnInspectionStarted => onInspectionStarted;
        public UnityEvent OnInspectionEnded => onInspectionEnded;

        public Bounds GetBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool found = false;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        public Vector3 GetPivot()
        {
            return inspectionPivot != null && inspectionPivot.IsChildOf(transform)
                ? inspectionPivot.position : GetBounds().center;
        }
    }
}
