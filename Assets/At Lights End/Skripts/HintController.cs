using System;
using UnityEngine;

namespace AtLightsEnd
{
    /// <summary>Two-stage hints. Explicit targets keep this independent of future puzzle progression.</summary>
    public sealed class HintController : MonoBehaviour
    {
        [Serializable]
        public sealed class Hint
        {
            public GameObject roomRoot;
            public InspectableObject target;
            [TextArea] public string message = "Der Pflanztopf soll in der Nähe des Globus sein, hat Mia gesagt.";
            [NonSerialized] public bool textShown;
        }

        [SerializeField] private Hint[] hints = new Hint[0];
        [SerializeField] private RoomNavigationController navigation;
        [SerializeField] private ObjectInspectionController inspection;
        [SerializeField] private UnityEngine.UI.Button hintButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMPro.TMP_Text dialogueText;
        [SerializeField] private HintOutline outline;
        [SerializeField, Min(0.1f)] private float outlineDuration = 6f;
        private Hint currentHint;
        private float hideOutlineAt;
        public bool IsDialogueVisible => dialoguePanel != null && dialoguePanel.activeSelf;
        public bool IsOutlineVisible => outline != null && outline.Target != null;

        private void OnEnable()
        {
            if (hintButton != null) hintButton.onClick.AddListener(RequestHint);
            if (closeButton != null) closeButton.onClick.AddListener(CloseDialogue);
        }

        private Hint GetAvailableHint()
        {
            if (navigation != null && navigation.IsTransitioning) return null;
            if (inspection != null && inspection.IsInspecting) return null;
            foreach (Hint hint in hints)
                if (hint != null && hint.roomRoot != null && hint.roomRoot.activeInHierarchy &&
                    hint.target != null && hint.target.isActiveAndEnabled) return hint;
            return null;
        }

        private void Update()
        {
            Hint available = GetAvailableHint();
            if (available != currentHint)
            {
                CloseDialogue();
                ClearOutline();
                currentHint = available;
            }
            if (hintButton != null) hintButton.interactable = available != null;
            if (IsOutlineVisible && Time.unscaledTime >= hideOutlineAt) ClearOutline();
        }

        public void RequestHint()
        {
            Hint hint = GetAvailableHint();
            if (hint == null) return;
            if (hint != currentHint)
            {
                CloseDialogue();
                ClearOutline();
                currentHint = hint;
            }
            if (!hint.textShown)
            {
                hint.textShown = true;
                dialogueText.text = hint.message;
                dialoguePanel.SetActive(true);
                ClearOutline();
            }
            else
            {
                CloseDialogue();
                outline.SetTarget(hint.target);
                hideOutlineAt = Time.unscaledTime + outlineDuration;
            }
        }

        public void CloseDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private void ClearOutline()
        {
            if (outline != null) outline.SetTarget(null);
        }

        private void OnDisable()
        {
            if (hintButton != null) hintButton.onClick.RemoveListener(RequestHint);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseDialogue);
            CloseDialogue();
            ClearOutline();
        }
    }
}
