using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AtLightsEnd
{
    public sealed class SessionUI : MonoBehaviour
    {
        [SerializeField] private GameSceneContext context;
        [SerializeField] private TMP_FontAsset font;
        private Transform canvas;
        private GameObject namePanel, inventoryPanel;
        private UnityEngine.UI.Button inventoryButton, timeButton, confirmButton;
        private TMP_InputField nameInput;
        private TMP_Text error, details, empty;
        private RectTransform content;
        private UnityEngine.UI.ScrollRect scroll;
        private UnityEngine.UI.GridLayoutGroup inventoryGrid;
        public bool NamePromptVisible => namePanel != null && namePanel.activeSelf;
        public bool InventoryVisible => inventoryPanel != null && inventoryPanel.activeSelf;

        private void Awake()
        {
            if (font == null) font = TMP_Settings.defaultFontAsset;
            var go = new GameObject("Session UI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            go.transform.SetParent(transform, false); canvas = go.transform;
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = 500;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            inventoryButton = Button("Equipment Button", canvas, "Ghost Hunting Equipment", new Vector2(0.02f, 0.025f), new Vector2(0.27f, 0.095f), OpenInventory);
            timeButton = Button("Time Test Button", canvas, context.IsNight ? "Test: Zum Tag" : "Test: Zur Nacht", new Vector2(0.78f, 0.91f), new Vector2(0.98f, 0.98f), context.SwitchTimeOfDay);
            BuildInventory(); BuildNamePrompt();
            inventoryPanel.SetActive(false);
            namePanel.SetActive(!PlayerSession.HasPlayerName);
            inventoryButton.gameObject.SetActive(context.IsNight && PlayerSession.HasPlayerName);
            PlayerSession.InventoryChanged += RefreshInventory;
        }
        private void Start() { if (NamePromptVisible) nameInput.ActivateInputField(); }
        private void Update()
        {
            bool ready = context != null && context.CanExplore && !PlayerSession.InventoryOpen;
            timeButton.interactable = ready;
            timeButton.gameObject.SetActive(context != null && context.CanExplore && !PlayerSession.InventoryOpen);
            inventoryButton.gameObject.SetActive(context.IsNight && PlayerSession.HasPlayerName);
            inventoryButton.interactable = ready;
            if (InventoryVisible && (!context.IsNight || !context.CanExplore)) CloseInventory();
#if ENABLE_INPUT_SYSTEM
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool escape = Input.GetKeyDown(KeyCode.Escape);
#else
            bool escape = false;
#endif
            if (escape && InventoryVisible) CloseInventory();
            if (InventoryVisible)
            {
                float width = scroll.viewport.rect.width;
                int columns = Mathf.Max(1, Mathf.FloorToInt((width - 32 + 18) / 258f));
                inventoryGrid.constraintCount = columns;
                float size = Mathf.Max(1, (width - 32 - (columns - 1) * 18) / columns);
                inventoryGrid.cellSize = new Vector2(size, size + 44);
            }
        }
        private void BuildNamePrompt()
        {
            namePanel = Image("Player Name Screen", canvas, Vector2.zero, Vector2.one, new Color(0.025f, 0.04f, 0.08f, 0.97f)).gameObject;
            var panel = Image("Name Card", namePanel.transform, new Vector2(0.23f, 0.27f), new Vector2(0.77f, 0.74f), new Color(0.10f, 0.18f, 0.29f, 1));
            Text("Title", panel.transform, "Wie heißt du?", new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.94f), 44);
            Text("Explanation", panel.transform, "Dieser Name erscheint bei deinen Gedanken und in Dialogen.", new Vector2(0.06f, 0.57f), new Vector2(0.94f, 0.73f), 25);
            var input = Image("Player Name Input", panel.transform, new Vector2(0.10f, 0.36f), new Vector2(0.90f, 0.53f), new Color(0.94f, 0.97f, 1, 1));
            var viewport = Rect("Text Area", input.transform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = Text("Input Text", viewport, "", Vector2.zero, Vector2.one, 32); text.color = new Color(0.04f, 0.10f, 0.18f); text.alignment = TextAlignmentOptions.MidlineLeft;
            var placeholder = Text("Placeholder", viewport, "Dein Name", Vector2.zero, Vector2.one, 30); placeholder.color = new Color(0.35f, 0.43f, 0.52f); placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            nameInput = input.gameObject.AddComponent<TMP_InputField>();
            nameInput.textViewport = viewport; nameInput.textComponent = text; nameInput.placeholder = placeholder;
            nameInput.targetGraphic = input; nameInput.characterLimit = 24; nameInput.lineType = TMP_InputField.LineType.SingleLine;
            nameInput.onSubmit.AddListener(_ => ConfirmName());
            error = Text("Validation", panel.transform, "", new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.34f), 23);
            error.color = new Color(1, 0.72f, 0.59f);
            confirmButton = Button("Begin Game", panel.transform, "Spiel starten", new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.19f), ConfirmName);
        }
        public void ConfirmName()
        {
            if (!NamePromptVisible) return;
            if (!PlayerSession.SetPlayerName(nameInput.text)) { error.text = "Bitte einen Namen mit 1–24 Zeichen ohne < oder > eingeben."; return; }
            nameInput.DeactivateInputField();
            namePanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
        private void BuildInventory()
        {
            inventoryPanel = Image("Inventory Screen", canvas, Vector2.zero, Vector2.one, new Color(0.015f, 0.025f, 0.05f, 0.94f)).gameObject;
            var panel = Image("Equipment Panel", inventoryPanel.transform, new Vector2(0.09f, 0.10f), new Vector2(0.91f, 0.90f), new Color(0.08f, 0.13f, 0.20f, 1));
            Text("Equipment Title", panel.transform, "Ghost Hunting Equipment", new Vector2(0.04f, 0.86f), new Vector2(0.80f, 0.98f), 38).alignment = TextAlignmentOptions.MidlineLeft;
            Button("Close Inventory", panel.transform, "Schließen ×", new Vector2(0.79f, 0.88f), new Vector2(0.97f, 0.97f), CloseInventory);
            var scrollRoot = Rect("Equipment Scroll", panel.transform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.84f));
            scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Image("Viewport", scrollRoot, Vector2.zero, Vector2.one, new Color(0.04f, 0.08f, 0.13f, 1));
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
            content = Rect("Content", viewport.transform, new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(0.5f, 1);
            inventoryGrid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            inventoryGrid.padding = new RectOffset(16, 16, 16, 16);
            inventoryGrid.spacing = new Vector2(18, 18);
            inventoryGrid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            inventoryGrid.constraintCount = 4;
            inventoryGrid.cellSize = new Vector2(260, 304);
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform; scroll.content = content;
            empty = Text("Empty Inventory", viewport.transform, "Noch keine Ausrüstung.\nSammle Gegenstände im Raum ein.", new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.70f), 30);
            details = Text("Item Details", panel.transform, "", new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.21f), 26);
            details.alignment = TextAlignmentOptions.TopLeft;
        }
        public void OpenInventory()
        {
            if (!context.IsNight || !context.CanExplore || PlayerSession.BlocksWorldInput) return;
            context.EndInspection();
            PlayerSession.InventoryOpen = true;
            RefreshInventory();
            inventoryPanel.SetActive(true);
            scroll.verticalNormalizedPosition = 1;
        }
        public void CloseInventory()
        {
            PlayerSession.InventoryOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }
        private void RefreshInventory()
        {
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            empty.gameObject.SetActive(PlayerSession.Inventory.Count == 0);
            details.text = "Wähle einen Gegenstand zum Untersuchen. Mit Escape legst du ihn zurück.";
            foreach (var entry in PlayerSession.Inventory)
            {
                var item = entry.Item;
                var row = Button("Item " + item.Id, content, item.displayName, Vector2.zero, Vector2.one,
                    () => InspectItem(item));
                row.targetGraphic.color = new Color(0.12f, 0.22f, 0.32f, 1);
                var label = row.GetComponentInChildren<TMP_Text>();
                label.rectTransform.anchorMin = new Vector2(0.04f, 0.025f);
                label.rectTransform.anchorMax = new Vector2(0.96f, 0.18f);
                label.fontSize = 24;
                if (item.icon != null)
                {
                    var icon = Image("Object Preview", row.transform, new Vector2(0.05f, 0.21f), new Vector2(0.95f, 0.96f), Color.white);
                    icon.sprite = item.icon; icon.preserveAspect = true; icon.raycastTarget = false;
                }
                else Text("Missing Preview", row.transform, "?", new Vector2(0.05f, 0.21f), new Vector2(0.95f, 0.96f), 80);
                if (entry.Quantity > 1)
                    Text("Quantity", row.transform, "×" + entry.Quantity, new Vector2(0.70f, 0.79f), new Vector2(0.96f, 0.96f), 26);
            }
        }
        private void OnDestroy()
        {
            PlayerSession.InventoryChanged -= RefreshInventory;
            PlayerSession.InventoryOpen = false;
        }
        private void InspectItem(InventoryItem item)
        {
            if (item.inspectionPrefab == null)
            {
                details.text = item.displayName + "\n" + item.description;
                return;
            }
            CloseInventory();
            if (!context.InspectInventoryItem(item, () => StartCoroutine(ReturnAfterEscape()))) OpenInventory();
        }
        private System.Collections.IEnumerator ReturnAfterEscape()
        {
            // Wait until the Escape press has passed through this frame's UI Update.
            yield return null;
            OpenInventory();
        }
        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        private UnityEngine.UI.Image Image(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var image = Rect(name, parent, min, max).gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; return image;
        }
        private TextMeshProUGUI Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private UnityEngine.UI.Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var image = Image(name, parent, min, max, new Color(0.17f, 0.32f, 0.48f, 1));
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var nav = button.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = nav;
            button.onClick.AddListener(action); Text("Label", image.transform, value, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), 26);
            return button;
        }
    }
}
