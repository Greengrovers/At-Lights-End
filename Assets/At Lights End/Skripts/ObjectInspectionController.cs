using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AtLightsEnd
{
    /// <summary>One controller per scene, normally attached to the gameplay camera.</summary>
    [DisallowMultipleComponent]
    public sealed class ObjectInspectionController : MonoBehaviour
    {
        [SerializeField] private Camera inspectionCamera;
        [SerializeField, Min(0.1f)] private float interactionRange = 20f;
        [Tooltip("Include scenery too, so walls block clicks on hidden props.")]
        [SerializeField] private LayerMask raycastLayers = ~0;
        [SerializeField, Min(0.1f)] private float minimumInspectionDistance = 1f;
        [SerializeField, Range(0.1f, 0.9f)] private float screenFill = 0.65f;
        [SerializeField, Min(0.01f)] private float rotationDegreesPerPixel = 0.25f;
        [Tooltip("Optional movement/look scripts to pause during inspection. Do not include this controller.")]
        [SerializeField] private Behaviour[] pauseDuringInspection = new Behaviour[0];

        public InspectableObject CurrentObject { get; private set; }
        public bool IsInspecting => CurrentObject != null;

        private Transform originalParent;
        private Vector3 originalLocalPosition, originalLocalScale;
        private Quaternion originalLocalRotation;
        private int originalSibling;
        private Transform inspectionRig;
        private float inspectionDistance;
        private Quaternion previousCameraRotation;
        private Vector2 previousPointer;
        private bool dragging;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private readonly List<BodyState> bodies = new List<BodyState>();
        private readonly List<Behaviour> pausedBehaviours = new List<Behaviour>();

        private struct BodyState
        {
            public Rigidbody Body;
            public bool Kinematic, DetectCollisions;
            public Vector3 Velocity, AngularVelocity;
        }

        private void Awake()
        {
            if (inspectionCamera == null) inspectionCamera = GetComponent<Camera>();
            if (inspectionCamera == null) inspectionCamera = Camera.main;
            if (inspectionCamera == null)
            {
                Debug.LogError("Object inspection needs a Camera assigned in the Inspector.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (inspectionRig != null && CurrentObject == null) EndInspection();
            if (inspectionCamera == null) { EndInspection(); return; }
            ReadInput(out Vector2 pointer, out bool pressed, out bool held, out bool escape);
            ProcessInput(pointer, pressed, held, escape);
        }

        private void ProcessInput(Vector2 pointer, bool pressed, bool held, bool escape)
        {
            // Screen-space navigation must never click through to a scene prop.
            if (pressed && IsPointerOverUI(pointer)) pressed = false;
            if (IsInspecting)
            {
                if (escape) { EndInspection(); return; }
                if (pressed) { dragging = true; previousPointer = pointer; }
                if (!held) dragging = false;
                if (dragging && CurrentObject.AllowRotation)
                {
                    Vector2 delta = pointer - previousPointer;
                    inspectionRig.Rotate(inspectionCamera.transform.up, -delta.x * rotationDegreesPerPixel, Space.World);
                    inspectionRig.Rotate(inspectionCamera.transform.right, delta.y * rotationDegreesPerPixel, Space.World);
                }
                previousPointer = pointer;
                return;
            }

            if (!pressed || !inspectionCamera.pixelRect.Contains(pointer)) return;
            Ray ray = CreateInteractionRay(pointer);
            // Only the nearest surface counts: hidden props cannot be clicked through walls.
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                InspectableObject target = hit.collider.GetComponentInParent<InspectableObject>();
                if (target != null && target.isActiveAndEnabled) BeginInspection(target);
            }
        }

        private Ray CreateInteractionRay(Vector2 pointer)
        {
            Ray ray = inspectionCamera.ScreenPointToRay(pointer);
            // ScreenPointToRay starts on the near clipping plane, which may be negative
            // for orthographic backgrounds. Measure interaction range from the camera.
            if (!inspectionCamera.orthographic)
                return new Ray(inspectionCamera.transform.position, ray.direction);
            float offset = Vector3.Dot(
                inspectionCamera.transform.position - ray.origin,
                inspectionCamera.transform.forward);
            float forward = Vector3.Dot(ray.direction, inspectionCamera.transform.forward);
            return new Ray(ray.GetPoint(offset / forward), ray.direction);
        }

        private void LateUpdate()
        {
            if (inspectionRig == null || inspectionCamera == null) return;
            Transform view = inspectionCamera.transform;
            inspectionRig.position = view.position + view.forward * inspectionDistance;
            inspectionRig.rotation = view.rotation * Quaternion.Inverse(previousCameraRotation) * inspectionRig.rotation;
            previousCameraRotation = view.rotation;
        }

        private static bool IsPointerOverUI(Vector2 pointer)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;
            var data = new UnityEngine.EventSystems.PointerEventData(eventSystem) { position = pointer };
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(data, hits);
            return hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
        }

        public void BeginInspection(InspectableObject target)
        {
            if (!isActiveAndEnabled || target == null || !target.isActiveAndEnabled || IsInspecting || inspectionCamera == null) return;
            // Moving an ancestor of the camera would move the camera itself.
            if (inspectionCamera.transform.IsChildOf(target.transform)) return;
            CurrentObject = target;
            Transform prop = target.transform;
            originalParent = prop.parent;
            originalSibling = prop.GetSiblingIndex();
            originalLocalPosition = prop.localPosition;
            originalLocalRotation = prop.localRotation;
            originalLocalScale = prop.localScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            foreach (Behaviour behaviour in pauseDuringInspection)
            {
                if (behaviour == null || behaviour == this || behaviour is Camera || !behaviour.enabled) continue;
                pausedBehaviours.Add(behaviour);
                behaviour.enabled = false;
            }
            foreach (Rigidbody body in target.GetComponentsInChildren<Rigidbody>(true))
            {
                bodies.Add(new BodyState { Body = body, Kinematic = body.isKinematic,
                    DetectCollisions = body.detectCollisions,
                    Velocity = body.isKinematic ? Vector3.zero : body.linearVelocity,
                    AngularVelocity = body.isKinematic ? Vector3.zero : body.angularVelocity });
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            Bounds bounds = target.GetBounds();
            Vector3 pivot = target.GetPivot();
            float radius = Mathf.Max(0.05f, bounds.extents.magnitude + Vector3.Distance(bounds.center, pivot));
            inspectionRig = new GameObject("Object Inspection Pivot (Runtime)").transform;
            inspectionRig.position = pivot;
            prop.SetParent(inspectionRig, true);
            float distance = Mathf.Max(minimumInspectionDistance, radius + inspectionCamera.nearClipPlane + 0.1f);
            if (inspectionCamera.orthographic)
            {
                float available = inspectionCamera.orthographicSize * Mathf.Min(1f, inspectionCamera.aspect) * screenFill;
                // Moving closer does not enlarge objects in an orthographic camera.
                inspectionRig.localScale = Vector3.one * (available / radius);
                distance = Mathf.Max(distance, available + Mathf.Max(0f, inspectionCamera.nearClipPlane) + 0.1f);
            }
            else
            {
                float halfVertical = inspectionCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
                float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * inspectionCamera.aspect);
                float halfAngle = Mathf.Atan(Mathf.Tan(Mathf.Min(halfVertical, halfHorizontal)) * screenFill);
                distance = Mathf.Max(distance, radius / Mathf.Sin(halfAngle));
            }
            inspectionRig.position = inspectionCamera.transform.position + inspectionCamera.transform.forward * distance;
            // Follow camera pose without inheriting its scale (which can even contain zero).
            inspectionDistance = distance;
            previousCameraRotation = inspectionCamera.transform.rotation;
            dragging = false; // The click that selects a prop must not also rotate it.
            target.OnInspectionStarted.Invoke();
        }

        public void EndInspection()
        {
            if (inspectionRig == null && CurrentObject == null) return;
            InspectableObject target = CurrentObject;
            CurrentObject = null;
            dragging = false;
            if (target != null)
            {
                Transform prop = target.transform;
                prop.SetParent(originalParent, false);
                prop.localPosition = originalLocalPosition;
                prop.localRotation = originalLocalRotation;
                prop.localScale = originalLocalScale;
                prop.SetSiblingIndex(originalSibling);
            }
            foreach (BodyState state in bodies)
            {
                if (state.Body == null) continue;
                state.Body.isKinematic = state.Kinematic;
                state.Body.detectCollisions = state.DetectCollisions;
                if (!state.Kinematic)
                {
                    state.Body.linearVelocity = state.Velocity;
                    state.Body.angularVelocity = state.AngularVelocity;
                }
            }
            bodies.Clear();
            foreach (Behaviour behaviour in pausedBehaviours)
                if (behaviour != null) behaviour.enabled = true;
            pausedBehaviours.Clear();
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            if (inspectionRig != null) Destroy(inspectionRig.gameObject);
            inspectionRig = null;
            if (target != null) target.OnInspectionEnded.Invoke();
        }

        private void OnDisable() => EndInspection();

        private static void ReadInput(out Vector2 pointer, out bool pressed, out bool held, out bool escape)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            pointer = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            pressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            held = mouse != null && mouse.leftButton.isPressed;
            escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pointer = Input.mousePosition;
            pressed = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            escape = Input.GetKeyDown(KeyCode.Escape);
#else
            pointer = Vector2.zero;
            pressed = held = escape = false;
#endif
        }
    }
}
