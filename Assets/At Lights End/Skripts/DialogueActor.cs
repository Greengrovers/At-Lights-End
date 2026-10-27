using UnityEngine;

namespace AtLightsEnd
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DialogueActor : MonoBehaviour
    {
        public DialogueAsset conversation;
        public DialogueController controller;
        [SerializeField] private Shader highlightShader;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Material originalMaterial, highlightMaterial;
        private bool highlighted;
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer.color;
            originalMaterial = spriteRenderer.sharedMaterial;
            if (highlightShader != null)
            {
                highlightMaterial = new Material(highlightShader);
                spriteRenderer.sharedMaterial = highlightMaterial;
            }
        }
        public void SetHighlighted(bool value)
        {
            if (spriteRenderer == null || highlighted == value) return;
            highlighted = value;
            spriteRenderer.color = value ? new Color(1f, 0.86f, 0.55f, originalColor.a) : originalColor;
            if (highlightMaterial != null) highlightMaterial.SetFloat("_Highlight", value ? 1f : 0f);
        }
        public void Interact()
        {
            if (controller != null && conversation != null) controller.Begin(this);
        }
        private void OnDisable() => SetHighlighted(false);
        private void OnDestroy()
        {
            if (spriteRenderer != null) spriteRenderer.sharedMaterial = originalMaterial;
            if (highlightMaterial != null) Destroy(highlightMaterial);
        }
    }
}
