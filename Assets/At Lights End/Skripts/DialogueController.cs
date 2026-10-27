using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AtLightsEnd
{
    [DefaultExecutionOrder(-100)]
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private DialogueView view;
        [SerializeField] private RoomNavigationController navigation;
        [SerializeField] private ObjectInspectionController inspection;
        [SerializeField] private HintController hints;
        [SerializeField] private CanvasGroup explorationUI;
        private bool inspectionWasEnabled, navigationWasEnabled, hintsWereEnabled;
        private bool uiInteractable, uiBlocks;
        private float uiAlpha;
        private DialogueActor hovered, actor;
        private DialogueAsset conversation;
        private bool requiresActor;
        private int lineIndex;
        private readonly List<UnityEngine.EventSystems.RaycastResult> uiHits = new List<UnityEngine.EventSystems.RaycastResult>();
        private readonly List<SpriteRenderer> hiddenActors = new List<SpriteRenderer>();
        public bool IsOpen => conversation != null;
        public int LineIndex => lineIndex;

        private void Awake()
        {
            if (sceneCamera == null) sceneCamera = Camera.main;
            view.Initialize(this);
        }
        private void Update()
        {
            if (PlayerSession.BlocksWorldInput) return;
            ReadInput(out Vector2 pointer, out bool clicked, out bool next, out bool close);
            if (IsOpen)
            {
                if (requiresActor && (actor == null || !actor.isActiveAndEnabled)) { End(); return; }
                if (close) End(); else if (next) Advance();
                return;
            }
            DialogueActor candidate = null;
            if (sceneCamera != null && (navigation == null || !navigation.IsTransitioning) &&
                (inspection == null || !inspection.IsInspecting) && !OverUI(pointer))
            {
                Ray ray = sceneCamera.ScreenPointToRay(pointer);
                float shift = Vector3.Dot(sceneCamera.transform.position - ray.origin, ray.direction);
                ray.origin = sceneCamera.orthographic ? ray.GetPoint(shift) : sceneCamera.transform.position;
                if (Physics.Raycast(ray, out RaycastHit hit, sceneCamera.farClipPlane, ~0, QueryTriggerInteraction.Ignore))
                    candidate = hit.collider.GetComponentInParent<DialogueActor>();
            }
            if (candidate != hovered)
            {
                if (hovered != null) hovered.SetHighlighted(false);
                hovered = candidate;
                if (hovered != null) hovered.SetHighlighted(true);
            }
            if (clicked && hovered != null) hovered.Interact();
        }
        private bool OverUI(Vector2 pointer)
        {
            var system = UnityEngine.EventSystems.EventSystem.current;
            if (system == null) return false;
            uiHits.Clear();
            system.RaycastAll(new UnityEngine.EventSystems.PointerEventData(system) { position = pointer }, uiHits);
            return uiHits.Exists(h => h.module is UnityEngine.UI.GraphicRaycaster);
        }
        public void Begin(DialogueActor source)
        {
            if (source == null || !source.isActiveAndEnabled) return;
            TryBeginConversation(source.conversation, source);
        }

        // Allows room intros and other scripted events without a clickable scene actor.
        public bool TryBeginConversation(DialogueAsset dialogue, DialogueActor source = null)
        {
            if (PlayerSession.BlocksWorldInput || !isActiveAndEnabled || IsOpen || dialogue == null || !dialogue.IsValid ||
                (source != null && !source.isActiveAndEnabled) ||
                (navigation != null && navigation.IsTransitioning)) return false;
            actor = source;
            requiresActor = source != null;
            conversation = dialogue;
            lineIndex = 0;
            if (hovered != null) hovered.SetHighlighted(false);
            hovered = null;
            if (!dialogue.playerThoughts)
            foreach (var sceneActor in FindObjectsByType<DialogueActor>(FindObjectsSortMode.None))
            {
                var renderer = sceneActor.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.enabled) { hiddenActors.Add(renderer); renderer.enabled = false; }
            }
            if (inspection != null) { inspection.EndInspection(); inspectionWasEnabled = inspection.enabled; inspection.enabled = false; }
            if (navigation != null) { navigationWasEnabled = navigation.enabled; navigation.enabled = false; }
            if (hints != null) { hintsWereEnabled = hints.enabled; hints.enabled = false; }
            if (explorationUI != null)
            {
                uiAlpha = explorationUI.alpha; uiInteractable = explorationUI.interactable; uiBlocks = explorationUI.blocksRaycasts;
                explorationUI.alpha = 0; explorationUI.interactable = false; explorationUI.blocksRaycasts = false;
            }
            view.Show(conversation, lineIndex);
            return true;
        }
        public void Advance()
        {
            if (!IsOpen) return;
            if (view.TryAdvancePage()) return;
            if (++lineIndex >= conversation.lines.Length) End();
            else view.Show(conversation, lineIndex);
        }
        public void End()
        {
            if (!IsOpen) return;
            conversation = null; actor = null;
            requiresActor = false;
            view.Hide();
            foreach (var renderer in hiddenActors) if (renderer != null) renderer.enabled = true;
            hiddenActors.Clear();
            if (inspection != null) inspection.enabled = inspectionWasEnabled;
            if (navigation != null) navigation.enabled = navigationWasEnabled;
            if (hints != null) hints.enabled = hintsWereEnabled;
            if (explorationUI != null)
            { explorationUI.alpha = uiAlpha; explorationUI.interactable = uiInteractable; explorationUI.blocksRaycasts = uiBlocks; }
        }
        private void OnDisable()
        {
            End();
            if (hovered != null) hovered.SetHighlighted(false);
        }
        private static void ReadInput(out Vector2 position, out bool click, out bool next, out bool close)
        {
#if ENABLE_INPUT_SYSTEM
            position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            next = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
            close = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition; click = Input.GetMouseButtonDown(0);
            next = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return); close = Input.GetKeyDown(KeyCode.Escape);
#else
            position = Vector2.zero; click = next = close = false;
#endif
        }
    }
}
