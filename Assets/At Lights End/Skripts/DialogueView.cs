using TMPro;
using UnityEngine;

namespace AtLightsEnd
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite paperSprite;
        [SerializeField] private Sprite thoughtSymbol;
        [SerializeField] private Color paperColor = new Color(0.91f, 0.86f, 0.76f, 1);
        [SerializeField, Range(0.2f, 0.9f)] private float inactiveBrightness = 0.52f;
        [Header("Dialogue Portrait Positions (normalized screen coordinates)")]
        [SerializeField] private Rect twoPersonLeft = new Rect(0.04f, 0.20f, 0.44f, 0.78f);
        [SerializeField] private Rect twoPersonRight = new Rect(0.52f, 0.20f, 0.44f, 0.78f);
        [SerializeField] private Rect threePersonLeft = new Rect(0.025f, 0.20f, 0.31f, 0.78f);
        [SerializeField] private Rect threePersonMiddle = new Rect(0.345f, 0.20f, 0.31f, 0.78f);
        [SerializeField] private Rect threePersonRight = new Rect(0.665f, 0.20f, 0.31f, 0.78f);
        [SerializeField, Range(0.25f, 2f)] private float twoPersonLeftScale = 1f;
        [SerializeField, Range(0.25f, 2f)] private float twoPersonRightScale = 1f;
        [SerializeField, Range(0.25f, 2f)] private float threePersonLeftScale = 1f;
        [SerializeField, Range(0.25f, 2f)] private float threePersonMiddleScale = 1f;
        [SerializeField, Range(0.25f, 2f)] private float threePersonRightScale = 1f;
        private bool threePeople;
        private DialogueAsset currentDialogue;
        private readonly System.Collections.Generic.Dictionary<Sprite, Rect> portraitBounds = new System.Collections.Generic.Dictionary<Sprite, Rect>();
        private GameObject root;
        private UnityEngine.UI.RawImage left, right, middle;
        private TMP_Text speaker, body, counter, nextLabel;
        private DialogueAsset.Speaker speakingSide;
        private Sprite roundedSprite;
        private Transform paperRoot, thoughtRoot;
        private UnityEngine.UI.Image backdrop;
        private TMP_Text paperBody, paperSpeaker, paperCounter, paperNext;
        private TMP_Text thoughtBody, thoughtSpeaker, thoughtCounter, thoughtNext;
        public Color LeftTint => left.color;
        public Color RightTint => right.color;
        public Rect GetPortraitFrame(DialogueAsset.Speaker side, bool threePeople, DialogueAsset dialogue = null)
        {
            if (dialogue != null && dialogue.hasPortraitLayout) return dialogue.portraitLayout.Frame(side, threePeople);
            if (side == DialogueAsset.Speaker.Middle) return threePersonMiddle;
            return side == DialogueAsset.Speaker.Left
                ? (threePeople ? threePersonLeft : twoPersonLeft)
                : (threePeople ? threePersonRight : twoPersonRight);
        }
        public Rect GetPortraitCrop(DialogueCharacter character)
        {
            Rect crop = character.autoPortraitRegion && character.portrait != null
                ? VisiblePortraitBounds(character.portrait) : character.portraitRegion;
            float trim = Mathf.Clamp(character.closeUpBottomCrop, 0, 0.6f);
            crop.y += crop.height * trim; crop.height *= 1 - trim;
            return crop;
        }
        public void ClearPortraitPreviewCache() => portraitBounds.Clear();
        public float GetPortraitScale(DialogueAsset.Speaker side, bool three, DialogueAsset dialogue = null)
        {
            if (dialogue != null && dialogue.hasPortraitLayout) return dialogue.portraitLayout.Scale(side, three);
            if (side == DialogueAsset.Speaker.Middle) return threePersonMiddleScale;
            return side == DialogueAsset.Speaker.Left
                ? (three ? threePersonLeftScale : twoPersonLeftScale)
                : (three ? threePersonRightScale : twoPersonRightScale);
        }

        public void Initialize(DialogueController controller)
        {
            if (root != null) return;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            root = new GameObject("Dialogue Overlay", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            backdrop = Image("Backdrop", root.transform, Vector2.zero, Vector2.one, new Color(0.04f, 0.03f, 0.06f, 0.7f), true);
            left = Portrait("Left Character", new Vector2(0.04f, 0.20f), new Vector2(0.48f, 0.98f));
            right = Portrait("Right Character", new Vector2(0.52f, 0.20f), new Vector2(0.96f, 0.98f));
            middle = Portrait("Middle Character", new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.98f));
            paperRoot = Rect("Conversation Presentation", root.transform, Vector2.zero, Vector2.one);
            Image("Paper Shadow", paperRoot, new Vector2(0.065f, 0.035f), new Vector2(0.945f, 0.255f), new Color(0.08f, 0.06f, 0.06f, 0.9f), false);
            var paper = Image("Dialogue Paper", paperRoot, new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.265f), paperColor, true);
            if (paperSprite != null) { paper.sprite = paperSprite; paper.type = UnityEngine.UI.Image.Type.Sliced; }
            for (int i = 1; i <= 3; i++)
            {
                var rule = Image("Paper Rule", paper.transform, new Vector2(0.055f, i * 0.23f), new Vector2(0.98f, i * 0.23f), new Color(0.48f, 0.4f, 0.36f, 0.22f), false);
                rule.rectTransform.sizeDelta = new Vector2(0, 1);
            }
            var margin = Image("Paper Margin", paper.transform, new Vector2(0.045f, 0.03f), new Vector2(0.045f, 0.97f), new Color(0.65f, 0.35f, 0.33f, 0.3f), false);
            margin.rectTransform.sizeDelta = new Vector2(2, 0);
            for (int i = 0; i < 3; i++) Text("Binding", paper.transform, "•", new Vector2(0.006f, 0.18f + i * 0.25f), new Vector2(0.04f, 0.38f + i * 0.25f), 32, TextAlignmentOptions.Center);
            var tab = Image("Name Tab", paperRoot, new Vector2(0.08f, 0.265f), new Vector2(0.32f, 0.315f), new Color(0.64f, 0.53f, 0.5f, 1), false);
            speaker = Text("Speaker", tab.transform, "", new Vector2(0.06f, 0), new Vector2(0.96f, 1), 29, TextAlignmentOptions.MidlineLeft);
            speaker.color = Color.white;
            body = Text("Dialogue Text", paper.transform, "", new Vector2(0.07f, 0.27f), new Vector2(0.96f, 0.90f), 30, TextAlignmentOptions.TopLeft);
            body.overflowMode = TextOverflowModes.Page;
            counter = Text("Progress", paper.transform, "", new Vector2(0.07f, 0.04f), new Vector2(0.3f, 0.22f), 20, TextAlignmentOptions.MidlineLeft);
            nextLabel = Button("Next", paper.transform, "Weiter  ›", new Vector2(0.77f, 0.035f), new Vector2(0.97f, 0.23f), controller.Advance);
            paperBody = body; paperSpeaker = speaker; paperCounter = counter; paperNext = nextLabel;
            InitializeThoughtBubble(controller);
            Button("Close", root.transform, "Schließen ×", new Vector2(0.81f, 0.92f), new Vector2(0.96f, 0.98f), controller.End);
            root.SetActive(false);
        }
        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            Color dim = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness, 1);
            float t = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            left.color = Color.Lerp(left.color, speakingSide == DialogueAsset.Speaker.Left ? Color.white : dim, t);
            right.color = Color.Lerp(right.color, speakingSide == DialogueAsset.Speaker.Right ? Color.white : dim, t);
            middle.color = Color.Lerp(middle.color, speakingSide == DialogueAsset.Speaker.Middle ? Color.white : dim, t);
        }
        private void LateUpdate()
        {
            if (root == null || !root.activeSelf) return;
            // A shared height that fits even the widest pose avoids both cropped arms
            // and one character floating smaller and higher than the other speakers.
            float height = float.MaxValue;
            foreach (var portrait in new[] { left, middle, right })
            {
                if (!portrait.isActiveAndEnabled || portrait.texture == null) continue;
                var frame = ((RectTransform)portrait.transform.parent).rect;
                float aspect = portrait.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio;
                height = Mathf.Min(height, frame.height, frame.width / Mathf.Max(0.01f, aspect));
            }
            if (height == float.MaxValue || height <= 0) return;
            foreach (var portrait in new[] { left, middle, right })
            {
                if (!portrait.isActiveAndEnabled || portrait.texture == null) continue;
                float aspect = portrait.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio;
                var side = portrait == left ? DialogueAsset.Speaker.Left : portrait == middle ? DialogueAsset.Speaker.Middle : DialogueAsset.Speaker.Right;
                float scaledHeight = height * GetPortraitScale(side, threePeople, currentDialogue);
                portrait.rectTransform.sizeDelta = new Vector2(scaledHeight * aspect, scaledHeight);
                portrait.rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        public void Show(DialogueAsset dialogue, int index)
        {
            currentDialogue = dialogue;
            var line = dialogue.lines[index];
            bool thoughts = dialogue.IsThought(line);
            bool narration = dialogue.IsNarrator(line);
            bool portraits = !thoughts && !narration;
            paperRoot.gameObject.SetActive(!thoughts);
            thoughtRoot.gameObject.SetActive(thoughts);
            left.transform.parent.gameObject.SetActive(portraits);
            right.transform.parent.gameObject.SetActive(portraits);
            middle.transform.parent.gameObject.SetActive(portraits && dialogue.middleCharacter != null);
            backdrop.color = portraits ? new Color(0.04f, 0.03f, 0.06f, 0.7f) : Color.clear;
            if (portraits)
            {
                SetPortrait(left, dialogue.leftCharacter);
                SetPortrait(right, dialogue.rightCharacter);
                SetPortrait(middle, dialogue.middleCharacter);
                bool three = dialogue.middleCharacter != null;
                threePeople = three;
                SetFrame(left, GetPortraitFrame(DialogueAsset.Speaker.Left, three, dialogue));
                SetFrame(right, GetPortraitFrame(DialogueAsset.Speaker.Right, three, dialogue));
                SetFrame(middle, GetPortraitFrame(DialogueAsset.Speaker.Middle, three, dialogue));
            }
            body = thoughts ? thoughtBody : paperBody;
            speaker = thoughts ? thoughtSpeaker : paperSpeaker;
            counter = thoughts ? thoughtCounter : paperCounter;
            nextLabel = thoughts ? thoughtNext : paperNext;
            speakingSide = line.speaker;
            speaker.text = narration ? "Narrator" : thoughts ? PlayerSession.PlayerName : dialogue.CharacterFor(line.speaker).DisplayName;
            string resolvedText = PlayerSession.ResolveText(line.text);
            body.text = resolvedText;
            body.pageToDisplay = 1;
            counter.text = (index + 1) + " / " + dialogue.lines.Length;
            nextLabel.text = index == dialogue.lines.Length - 1 ? "Ende  ›" : "Weiter  ›";
            if (!root.activeSelf)
            {
                Color dim = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness, 1);
                left.color = speakingSide == DialogueAsset.Speaker.Left ? Color.white : dim;
                right.color = speakingSide == DialogueAsset.Speaker.Right ? Color.white : dim;
                middle.color = speakingSide == DialogueAsset.Speaker.Middle ? Color.white : dim;
            }
            root.SetActive(true);
        }
        private void InitializeThoughtBubble(DialogueController controller)
        {
            Color purple = new Color(0.39f, 0.20f, 0.55f, 1);
            Color blue = new Color(0.73f, 0.85f, 0.95f, 1);
            Color tabColor = new Color(0.52f, 0.63f, 0.85f, 1);
            thoughtRoot = Rect("Player Thoughts", root.transform, Vector2.zero, Vector2.one);
            var paper = Image("Blue Notebook Paper", thoughtRoot, new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.31f), blue, true);
            for (int i = 1; i <= 3; i++)
            {
                var rule = Image("Notebook Rule", paper.transform, new Vector2(0.065f, i * 0.23f), new Vector2(0.98f, i * 0.23f), new Color(0.46f, 0.53f, 0.77f, 0.55f), false);
                rule.rectTransform.sizeDelta = new Vector2(0, 1.5f);
                var symbol = Image("Thought Symbol", paper.transform, new Vector2(0.012f, 0.12f + (i - 1) * 0.28f), new Vector2(0.043f, 0.33f + (i - 1) * 0.28f), Color.white, false);
                symbol.sprite = thoughtSymbol; symbol.preserveAspect = true;
                symbol.enabled = thoughtSymbol != null;
            }
            var margin = Image("Notebook Margin", paper.transform, new Vector2(0.052f, 0.04f), new Vector2(0.052f, 0.96f), new Color(0.5f, 0.45f, 0.75f, 0.6f), false);
            margin.rectTransform.sizeDelta = new Vector2(2, 0);
            var tab = Image("Thought Name Tab", thoughtRoot, new Vector2(0.08f, 0.31f), new Vector2(0.32f, 0.36f), tabColor, false);
            thoughtSpeaker = Text("Thought Speaker", tab.transform, "", new Vector2(0.06f, 0), new Vector2(0.96f, 1), 29, TextAlignmentOptions.MidlineLeft);
            thoughtBody = Text("Thought Text", paper.transform, "", new Vector2(0.075f, 0.25f), new Vector2(0.96f, 0.92f), 29, TextAlignmentOptions.TopLeft);
            thoughtBody.overflowMode = TextOverflowModes.Page;
            thoughtCounter = Text("Thought Progress", paper.transform, "", new Vector2(0.075f, 0.03f), new Vector2(0.3f, 0.20f), 20, TextAlignmentOptions.MidlineLeft);
            thoughtNext = Button("Next Thought", paper.transform, "Weiter  ›", new Vector2(0.79f, 0.045f), new Vector2(0.97f, 0.22f), controller.Advance);
            var nextBackground = thoughtNext.transform.parent.GetComponent<UnityEngine.UI.Image>();
            nextBackground.color = tabColor;
            roundedSprite = MakeRoundedSprite(); nextBackground.sprite = roundedSprite;
            nextBackground.type = UnityEngine.UI.Image.Type.Sliced;
            thoughtNext.fontStyle = FontStyles.Bold;
            thoughtSpeaker.color = thoughtBody.color = thoughtCounter.color = thoughtNext.color = purple;
            thoughtRoot.gameObject.SetActive(false);
        }
        private void SetFrame(UnityEngine.UI.RawImage portrait, Rect bounds)
        {
            var frame = (RectTransform)portrait.transform.parent;
            frame.anchorMin = bounds.min; frame.anchorMax = bounds.max;
            frame.offsetMin = frame.offsetMax = Vector2.zero;
        }
        private Sprite MakeRoundedSprite()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Max(8 - x, 0, x - 23), dy = Mathf.Max(8 - y, 0, y - 23);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(8.5f - Mathf.Sqrt(dx * dx + dy * dy))));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        }
        private void OnDestroy()
        {
            if (roundedSprite != null) { Destroy(roundedSprite.texture); Destroy(roundedSprite); }
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        public bool TryAdvancePage()
        {
            body.ForceMeshUpdate();
            if (body.pageToDisplay >= body.textInfo.pageCount) return false;
            body.pageToDisplay++;
            return true;
        }
        private void SetPortrait(UnityEngine.UI.RawImage image, DialogueCharacter character)
        {
            if (character == null || character.isPlayerCharacter)
            {
                image.texture = null;
                image.enabled = false;
                return;
            }
            image.texture = character.portrait == null ? null : character.portrait.texture;
            image.enabled = image.texture != null;
            Rect crop = GetPortraitCrop(character);
            image.uvRect = crop;
            if (image.texture != null)
                image.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio =
                    image.texture.width * crop.width / Mathf.Max(1, image.texture.height * crop.height);
        }
        private Rect VisiblePortraitBounds(Sprite sprite)
        {
            if (portraitBounds.TryGetValue(sprite, out Rect cached)) return cached;
            var texture = sprite.texture;
            Rect source = sprite.textureRect;
            Rect uv = new Rect(source.x / texture.width, source.y / texture.height,
                source.width / texture.width, source.height / texture.height);
            // Read a small temporary copy: source textures do not need Read/Write enabled.
            const int resolution = 512;
            var target = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(texture, target, uv.size, uv.position);
                RenderTexture.active = target;
                readable = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                readable.Apply();
                var pixels = readable.GetPixels32();
                int minX = resolution, minY = resolution, maxX = -1, maxY = -1;
                // Exported character sheets can contain detached colour swatches or stray marks.
                // Frame the largest connected silhouette instead of including every opaque pixel.
                var seen = new bool[pixels.Length];
                var queue = new int[pixels.Length];
                int largest = 0;
                for (int start = 0; start < pixels.Length; start++)
                {
                    if (seen[start] || pixels[start].a < 26) continue;
                    int head = 0, tail = 0, x0 = resolution, y0 = resolution, x1 = -1, y1 = -1;
                    queue[tail++] = start; seen[start] = true;
                    while (head < tail)
                    {
                        int point = queue[head++], x = point % resolution, y = point / resolution;
                        x0 = Mathf.Min(x0, x); y0 = Mathf.Min(y0, y);
                        x1 = Mathf.Max(x1, x); y1 = Mathf.Max(y1, y);
                        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= resolution || ny >= resolution) continue;
                            int next = ny * resolution + nx;
                            if (seen[next] || pixels[next].a < 26) continue;
                            seen[next] = true; queue[tail++] = next;
                        }
                    }
                    if (tail > largest)
                    { largest = tail; minX = x0; minY = y0; maxX = x1; maxY = y1; }
                }
                if (maxX >= minX && maxY >= minY)
                {
                    minX = Mathf.Max(0, minX - 2); minY = Mathf.Max(0, minY - 2);
                    maxX = Mathf.Min(resolution - 1, maxX + 2); maxY = Mathf.Min(resolution - 1, maxY + 2);
                    uv = new Rect(uv.x + uv.width * minX / resolution, uv.y + uv.height * minY / resolution,
                        uv.width * (maxX - minX + 1) / resolution, uv.height * (maxY - minY + 1) / resolution);
                }
                portraitBounds[sprite] = uv;
                return uv;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (readable != null)
                {
                    if (Application.isPlaying) Destroy(readable);
                    else DestroyImmediate(readable);
                }
            }
        }
        private UnityEngine.UI.RawImage Portrait(string name, Vector2 min, Vector2 max)
        {
            var holder = Rect(name + " Frame", root.transform, min, max);
            // Size all visible portraits together in LateUpdate, anchored at the baseline.
            var rect = Rect(name, holder, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            rect.pivot = new Vector2(0.5f, 0);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget = false;
            var fitter = rect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.enabled = false; // Stores the aspect ratio; shared sizing is handled above.
            return image;
        }
        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private UnityEngine.UI.Image Image(string name, Transform parent, Vector2 min, Vector2 max, Color color, bool raycast)
        {
            var image = Rect(name, parent, min, max).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = raycast; return image;
        }
        private TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size, TextAlignmentOptions align)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.text = value; text.alignment = align;
            text.color = new Color(0.20f, 0.15f, 0.14f, 1); text.raycastTarget = false; return text;
        }
        private TMP_Text Button(string name, Transform parent, string value, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var bg = Image(name, parent, min, max, new Color(0.45f, 0.36f, 0.35f, 1), true);
            var button = bg.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = bg;
            var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(action);
            var label = Text("Label", bg.transform, value, Vector2.zero, Vector2.one, 23, TextAlignmentOptions.Center); label.color = paperColor;
            return label;
        }
    }
}
