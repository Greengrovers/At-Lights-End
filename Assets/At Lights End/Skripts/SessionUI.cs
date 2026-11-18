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
        [Header("Paper Intro")]
        [SerializeField] private TMP_FontAsset introFont;
        [SerializeField] private string paperTitle = "STUDENT PROFILE";
        [SerializeField] private string namePrompt = "Enter your name";
        [SerializeField] private string namePlaceholder = "Your name";
        [SerializeField] private string confirmLabel = "Confirm";
        [SerializeField] private string invalidNameMessage = "Enter 1–24 characters without < or >.";
        [SerializeField] private string profileHeading = "About me";
        [SerializeField, TextArea(3, 8)] private string profileFacts = "15 years old\nLikes ghost stories";
        [SerializeField] private string continueLabel = "Continue";
        [SerializeField] private AudioClip schoolBell;

        private static readonly Vector2 PaperSize = new Vector2(760, 890);
        private static readonly Color Ink = new Color(0.20f, 0.16f, 0.15f);
        private RectTransform paper;
        private GameObject nameForm, profile;
        private CanvasGroup profileFade;
        private UnityEngine.UI.Button continueButton;
        private TMP_Text profileName, factsText;
        private AudioSource introAudio;
        private string pendingName;
        private float reveal;

        private Transform canvas;
        private GameObject namePanel, inventoryPanel;
        private GameObject notebookPanel;
        private UnityEngine.UI.Button notebookButton;
        private UnityEngine.UI.ScrollRect notebookScroll;
        private TMP_Text notebookText;
        private TMP_Text itemAddedText;
        private float itemAddedUntil;
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
            timeButton = Button("Progress Button", canvas, context.IsNight ? "Weiter zu Tag " + (PlayerSession.Day + 1) : "Weiter zur Nacht", new Vector2(0.78f, 0.91f), new Vector2(0.98f, 0.98f), context.SwitchTimeOfDay);
            BuildInventory();
            BuildNotebook();
            BuildNamePrompt();
            BuildItemAddedNotice();
            inventoryPanel.SetActive(false);
            namePanel.SetActive(!PlayerSession.HasPlayerName);
            if (NamePromptVisible) UpdatePaperZoom();
            inventoryButton.gameObject.SetActive(context.IsNight && PlayerSession.HasPlayerName);
            PlayerSession.InventoryChanged += RefreshInventory;
            PlayerSession.ItemAdded += ShowItemAddedNotice;
        }
        private void Start() { if (NamePromptVisible) nameInput.ActivateInputField(); }
        private void Update()
        {
            bool ready = context != null && context.CanExplore && !PlayerSession.InventoryOpen && !PlayerSession.NotebookOpen;
            timeButton.gameObject.SetActive(context.CanSwitchTime);
            notebookButton.gameObject.SetActive(ready);
            inventoryButton.gameObject.SetActive(context.IsNight && PlayerSession.HasPlayerName);
            inventoryButton.interactable = ready;
            if (NamePromptVisible) UpdatePaperZoom();
            if (itemAddedText.transform.parent.gameObject.activeSelf && Time.unscaledTime >= itemAddedUntil)
                itemAddedText.transform.parent.gameObject.SetActive(false);
            if (InventoryVisible && (!context.IsNight || !context.CanExplore)) CloseInventory();
#if ENABLE_INPUT_SYSTEM
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool escape = Input.GetKeyDown(KeyCode.Escape);
#else
            bool escape = false;
#endif
            if (escape && InventoryVisible) CloseInventory();
            if (escape && PlayerSession.NotebookOpen) CloseNotebook();
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
            namePanel = Image("Paper Intro", canvas, Vector2.zero, Vector2.one, new Color(0.055f, 0.063f, 0.078f)).gameObject;
            var mask = namePanel.AddComponent<UnityEngine.UI.RectMask2D>();
            paper = Image("Student Profile Paper", mask.transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Color(0.94f, 0.89f, 0.78f)).rectTransform;
            paper.sizeDelta = PaperSize;

            for (int row = 1; row <= 17; row++)
            {
                float y = row * 0.05f;
                Image("Paper Rule", paper, new Vector2(0.04f, y), new Vector2(0.96f, y + 0.0014f),
                    new Color(0.34f, 0.45f, 0.52f, 0.22f)).raycastTarget = false;
            }
            Image("Margin Rule", paper, new Vector2(0.085f, 0.05f), new Vector2(0.088f, 0.9f),
                new Color(0.68f, 0.32f, 0.30f, 0.35f)).raycastTarget = false;
            IntroText("Paper Title", paper, paperTitle, new Vector2(0.12f, 0.88f), new Vector2(0.88f, 0.96f), 26);

            nameForm = Rect("Name Form", paper, Vector2.zero, Vector2.one).gameObject;
            var form = nameForm.transform;
            IntroText("Prompt", form, namePrompt, new Vector2(0.14f, 0.81f), new Vector2(0.86f, 0.88f), 36);
            var input = Image("Player Name Input", form, new Vector2(0.15f, 0.74f), new Vector2(0.85f, 0.81f),
                new Color(1f, 0.97f, 0.90f));
            var viewport = Rect("Text Area", input.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = IntroText("Input Text", viewport, "", Vector2.zero, Vector2.one, 34);
            text.enableAutoSizing = false;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var placeholder = IntroText("Placeholder", viewport, namePlaceholder, Vector2.zero, Vector2.one, 30);
            placeholder.color = new Color(0.41f, 0.35f, 0.31f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            nameInput = input.gameObject.AddComponent<TMP_InputField>();
            nameInput.textViewport = viewport;
            nameInput.textComponent = text;
            nameInput.placeholder = placeholder;
            nameInput.targetGraphic = input;
            nameInput.characterLimit = 24;
            nameInput.lineType = TMP_InputField.LineType.SingleLine;
            nameInput.onSubmit.AddListener(_ => ConfirmName());
            error = IntroText("Validation", form, "", new Vector2(0.12f, 0.685f), new Vector2(0.88f, 0.74f), 22);
            error.color = new Color(0.55f, 0.17f, 0.14f);
            IntroButton("Confirm Name", form, confirmLabel, new Vector2(0.31f, 0.61f), new Vector2(0.69f, 0.68f), ConfirmName);

            profile = Rect("Profile Details", paper, Vector2.zero, Vector2.one).gameObject;
            profileFade = profile.AddComponent<CanvasGroup>();
            profileFade.alpha = 0;
            profileName = IntroText("Player Name", profile.transform, "", new Vector2(0.12f, 0.73f), new Vector2(0.88f, 0.87f), 58);
            profileName.richText = false;
            IntroText("Profile Heading", profile.transform, profileHeading, new Vector2(0.14f, 0.60f), new Vector2(0.86f, 0.70f), 34)
                .alignment = TextAlignmentOptions.MidlineLeft;
            factsText = IntroText("Profile Facts", profile.transform, "", new Vector2(0.14f, 0.24f), new Vector2(0.86f, 0.58f), 31);
            factsText.alignment = TextAlignmentOptions.TopLeft;
            continueButton = IntroButton("Continue", profile.transform, continueLabel,
                new Vector2(0.29f, 0.09f), new Vector2(0.71f, 0.17f), FinishIntro);
            continueButton.interactable = false;
            profile.SetActive(false);

            if (schoolBell != null)
            {
                introAudio = gameObject.AddComponent<AudioSource>();
                introAudio.playOnAwake = false;
            }
        }

        private TextMeshProUGUI IntroText(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
        {
            var text = Text(name, parent, value, min, max, size);
            text.font = introFont != null ? introFont : font;
            text.color = Ink;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.55f;
            text.fontSizeMax = size;
            return text;
        }

        private UnityEngine.UI.Button IntroButton(string name, Transform parent, string value, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction action)
        {
            var button = Button(name, parent, value, min, max, action);
            button.targetGraphic.color = new Color(0.31f, 0.23f, 0.22f);
            var label = button.GetComponentInChildren<TMP_Text>();
            if (introFont != null) label.font = introFont;
            label.enableAutoSizing = true;
            label.fontSizeMin = 15;
            label.fontSizeMax = 26;
            return button;
        }

        private void UpdatePaperZoom()
        {
            if (pendingName != null && reveal < 1f)
            {
                reveal = Mathf.MoveTowards(reveal, 1f, Time.unscaledDeltaTime / 0.8f);
                profileFade.alpha = Mathf.InverseLerp(0.2f, 0.9f, reveal);
                continueButton.interactable = reveal >= 1f;
            }
            Vector2 viewport = ((RectTransform)namePanel.transform).rect.size;
            if (viewport.x <= 0 || viewport.y <= 0) return;
            float fit = 0.9f * Mathf.Min(viewport.x / PaperSize.x, viewport.y / PaperSize.y);
            float close = Mathf.Min(1.24f * viewport.x / PaperSize.x, 2.25f * viewport.y / PaperSize.y);
            float progress = Mathf.SmoothStep(0, 1, reveal);
            float scale = Mathf.Lerp(close, fit, progress);
            paper.localScale = new Vector3(scale, scale, 1);
            paper.anchoredPosition = Vector2.down * (PaperSize.y * 0.25f * scale * (1 - progress));
        }

        public void ConfirmName()
        {
            if (!NamePromptVisible || pendingName != null) return;
            if (!PlayerSession.IsValidPlayerName(nameInput.text))
            {
                error.text = invalidNameMessage;
                return;
            }
            pendingName = nameInput.text.Trim();
            profileName.text = pendingName;
            factsText.text = (profileFacts ?? "").Replace("{playerName}", pendingName);
            nameInput.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            nameForm.SetActive(false);
            profile.SetActive(true);
        }

        private void FinishIntro()
        {
            if (!NamePromptVisible || reveal < 1f || !PlayerSession.SetPlayerName(pendingName)) return;
            if (introAudio != null) introAudio.PlayOneShot(schoolBell);
            namePanel.SetActive(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private void BuildItemAddedNotice()
        {
            var panel = Image("Item Added Notice", canvas, new Vector2(0.26f, 0.88f), new Vector2(0.74f, 0.96f),
                new Color(0.08f, 0.13f, 0.20f, 0.96f));
            panel.raycastTarget = false;
            itemAddedText = Text("Message", panel.transform, "", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), 30);
            itemAddedText.enableAutoSizing = true;
            itemAddedText.fontSizeMin = 17;
            itemAddedText.fontSizeMax = 30;
            itemAddedText.richText = false;
            panel.gameObject.SetActive(false);
        }

        private void ShowItemAddedNotice(InventoryItem item)
        {
            string itemName = string.IsNullOrWhiteSpace(item.displayName) ? item.name : item.displayName;
            itemAddedText.text = itemName + " added to inventory";
            itemAddedUntil = Time.unscaledTime + 2f;
            itemAddedText.transform.parent.gameObject.SetActive(true);
        }

        private void BuildNotebook()
        {
            notebookButton = Button("Notebook Button", canvas, "Notizbuch",
                new Vector2(0.02f, 0.91f), new Vector2(0.17f, 0.98f), OpenNotebook);
            var overlay = Image("Notebook Screen", canvas, Vector2.zero, Vector2.one, new Color(0.04f, 0.04f, 0.06f, 0.88f));
            notebookPanel = overlay.gameObject;
            var page = Image("Notebook Page", overlay.transform, new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.90f),
                new Color(0.94f, 0.89f, 0.78f));
            var heading = Text("Notebook Title", page.transform, "Notizbuch", new Vector2(0.07f, 0.83f), new Vector2(0.66f, 0.97f), 42);
            heading.alignment = TextAlignmentOptions.MidlineLeft;
            heading.color = Ink;
            Button("Close Notebook", page.transform, "Schließen ×", new Vector2(0.72f, 0.86f), new Vector2(0.95f, 0.96f), CloseNotebook);

            var scrollRoot = Rect("Notebook Scroll", page.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.81f));
            notebookScroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            notebookScroll.horizontal = false;
            notebookScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Image("Viewport", scrollRoot, Vector2.zero, Vector2.one, Color.white);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var entries = Rect("Notes", viewport.transform, new Vector2(0, 1), Vector2.one);
            entries.pivot = new Vector2(0.5f, 1);
            var layout = entries.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            entries.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            notebookText = Text("Notebook Hints", entries, "", Vector2.zero, Vector2.one, 30);
            notebookText.alignment = TextAlignmentOptions.TopLeft;
            notebookText.color = Ink;
            notebookText.richText = false;
            notebookScroll.viewport = viewport.rectTransform;
            notebookScroll.content = entries;
            notebookPanel.SetActive(false);
        }

        private void OpenNotebook()
        {
            if (!context.CanExplore || PlayerSession.BlocksWorldInput) return;
            var hints = PlayerSession.NotebookHints;
            notebookText.text = hints.Count == 0 ? "Noch keine Hinweise." : "• " + string.Join("\n\n• ", hints);
            PlayerSession.NotebookOpen = true;
            notebookPanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            notebookScroll.verticalNormalizedPosition = 1f;
        }

        private void CloseNotebook()
        {
            notebookPanel.SetActive(false);
            PlayerSession.NotebookOpen = false;
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
            PlayerSession.ItemAdded -= ShowItemAddedNotice;
            PlayerSession.InventoryOpen = false;
            PlayerSession.NotebookOpen = false;
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
