using TMPro;
using UnityEngine;

namespace AtLightsEnd
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite paperSprite;
        [SerializeField] private Color paperColor = new Color(0.91f, 0.86f, 0.76f, 1);
        [SerializeField, Range(0.2f, 0.9f)] private float inactiveBrightness = 0.52f;
        private GameObject root;
        private UnityEngine.UI.RawImage left, right;
        private TMP_Text speaker, body, counter, nextLabel;
        private bool speakingLeft;
        public Color LeftTint => left.color;
        public Color RightTint => right.color;

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
            Image("Backdrop", root.transform, Vector2.zero, Vector2.one, new Color(0.04f, 0.03f, 0.06f, 0.7f), true);
            left = Portrait("Left Character", new Vector2(0.04f, 0.20f), new Vector2(0.48f, 0.98f));
            right = Portrait("Right Character", new Vector2(0.52f, 0.20f), new Vector2(0.96f, 0.98f));
            Image("Paper Shadow", root.transform, new Vector2(0.065f, 0.035f), new Vector2(0.945f, 0.255f), new Color(0.08f, 0.06f, 0.06f, 0.9f), false);
            var paper = Image("Dialogue Paper", root.transform, new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.265f), paperColor, true);
            if (paperSprite != null) { paper.sprite = paperSprite; paper.type = UnityEngine.UI.Image.Type.Sliced; }
            for (int i = 1; i <= 3; i++)
            {
                var rule = Image("Paper Rule", paper.transform, new Vector2(0.055f, i * 0.23f), new Vector2(0.98f, i * 0.23f), new Color(0.48f, 0.4f, 0.36f, 0.22f), false);
                rule.rectTransform.sizeDelta = new Vector2(0, 1);
            }
            var margin = Image("Paper Margin", paper.transform, new Vector2(0.045f, 0.03f), new Vector2(0.045f, 0.97f), new Color(0.65f, 0.35f, 0.33f, 0.3f), false);
            margin.rectTransform.sizeDelta = new Vector2(2, 0);
            for (int i = 0; i < 3; i++) Text("Binding", paper.transform, "•", new Vector2(0.006f, 0.18f + i * 0.25f), new Vector2(0.04f, 0.38f + i * 0.25f), 32, TextAlignmentOptions.Center);
            var tab = Image("Name Tab", root.transform, new Vector2(0.08f, 0.265f), new Vector2(0.32f, 0.315f), new Color(0.64f, 0.53f, 0.5f, 1), false);
            speaker = Text("Speaker", tab.transform, "", new Vector2(0.06f, 0), new Vector2(0.96f, 1), 29, TextAlignmentOptions.MidlineLeft);
            speaker.color = Color.white;
            body = Text("Dialogue Text", paper.transform, "", new Vector2(0.07f, 0.27f), new Vector2(0.96f, 0.90f), 30, TextAlignmentOptions.TopLeft);
            body.overflowMode = TextOverflowModes.Page;
            counter = Text("Progress", paper.transform, "", new Vector2(0.07f, 0.04f), new Vector2(0.3f, 0.22f), 20, TextAlignmentOptions.MidlineLeft);
            nextLabel = Button("Next", paper.transform, "Weiter  ›", new Vector2(0.77f, 0.035f), new Vector2(0.97f, 0.23f), controller.Advance);
            Button("Close", root.transform, "Schließen ×", new Vector2(0.81f, 0.92f), new Vector2(0.96f, 0.98f), controller.End);
            root.SetActive(false);
        }
        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            Color dim = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness, 1);
            float t = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            left.color = Color.Lerp(left.color, speakingLeft ? Color.white : dim, t);
            right.color = Color.Lerp(right.color, speakingLeft ? dim : Color.white, t);
        }
        public void Show(DialogueAsset dialogue, int index)
        {
            SetPortrait(left, dialogue.leftCharacter);
            SetPortrait(right, dialogue.rightCharacter);
            var line = dialogue.lines[index]; speakingLeft = line.speaker == DialogueAsset.Speaker.Left;
            speaker.text = speakingLeft ? dialogue.leftCharacter.displayName : dialogue.rightCharacter.displayName;
            body.text = line.text; body.pageToDisplay = 1;
            counter.text = (index + 1) + " / " + dialogue.lines.Length;
            nextLabel.text = index == dialogue.lines.Length - 1 ? "Ende  ›" : "Weiter  ›";
            if (!root.activeSelf)
            {
                left.color = speakingLeft ? Color.white : new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness, 1);
                right.color = !speakingLeft ? Color.white : new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness, 1);
            }
            root.SetActive(true);
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
            if (character == null)
            {
                image.texture = null;
                image.enabled = false;
                return;
            }
            image.texture = character.portrait == null ? null : character.portrait.texture;
            image.enabled = image.texture != null;
            Rect crop = character.portraitRegion;
            // Upper-body close-up, using UV cropping rather than modifying the original image.
            crop.y += crop.height * 0.30f; crop.height *= 0.70f;
            image.uvRect = crop;
            if (image.texture != null)
                image.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio =
                    image.texture.width * crop.width / Mathf.Max(1, image.texture.height * crop.height);
        }
        private UnityEngine.UI.RawImage Portrait(string name, Vector2 min, Vector2 max)
        {
            var holder = Rect(name + " Frame", root.transform, min, max);
            holder.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var rect = Rect(name, holder, Vector2.zero, Vector2.one);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>(); image.raycastTarget = false;
            rect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
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
