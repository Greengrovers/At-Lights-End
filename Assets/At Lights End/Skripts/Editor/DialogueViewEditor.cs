using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtLightsEnd.Editor
{
    [CustomEditor(typeof(DialogueView))]
    public sealed class DialogueViewEditor : UnityEditor.Editor
    {
        private DialogueAsset conversation;
        private VisualElement preview;
        private float aspect = 16f / 9f;
        private int lineIndex;
        private readonly List<System.Action> syncSliders = new List<System.Action>();
        private DialogueView View => (DialogueView)target;

        public override VisualElement CreateInspectorGUI()
        {
            syncSliders.Clear();
            var root = new VisualElement();
            root.Add(new HelpBox("Sprecherpositionen hier einstellen – kein Play-Modus nötig. Position und Größe werden nur im ausgewählten Dialog-Asset gespeichert. Strg+Z macht Änderungen rückgängig.", HelpBoxMessageType.Info));
            var field = new ObjectField("Dialog bearbeiten") { objectType = typeof(DialogueAsset), allowSceneObjects = false };
            if (conversation == null)
                conversation = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/At Lights End/Dialogues/Cafeteria/Day 1/Chris_Sean_Marcie_Cafeteria_Day1_Visit1.asset");
            field.SetValueWithoutNotify(conversation);
            root.Add(field);
            var line = new SliderInt("Dialogzeile", 1, Mathf.Max(1, conversation == null ? 1 : conversation.lines.Length)) { showInputField = true, value = 1 };
            root.Add(line);
            field.RegisterValueChangedCallback(e =>
            {
                conversation = (DialogueAsset)e.newValue; lineIndex = 0;
                line.highValue = Mathf.Max(1, conversation == null ? 1 : conversation.lines.Length);
                line.SetValueWithoutNotify(1); View.ClearPortraitPreviewCache(); RefreshSliders();
            });
            line.RegisterValueChangedCallback(e => { lineIndex = e.newValue - 1; DrawPreview(); });
            var ratios = new PopupField<string>("Bildformat", new List<string> { "16:9", "4:3", "21:9" }, 0);
            ratios.RegisterValueChangedCallback(e => { aspect = e.newValue == "4:3" ? 4f / 3f : e.newValue == "21:9" ? 21f / 9f : 16f / 9f; DrawPreview(); });
            root.Add(ratios);
            preview = new VisualElement { name = "dialogue-layout-preview" };
            preview.style.height = 230; preview.style.backgroundColor = new Color(0.09f, 0.11f, 0.15f);
            preview.style.overflow = Overflow.Hidden;
            preview.RegisterCallback<GeometryChangedEvent>(_ => DrawPreview());
            root.Add(preview);
            root.Add(new Button(() => { View.ClearPortraitPreviewCache(); DrawPreview(); }) { text = "Vorschau aktualisieren" });
            root.Add(new HelpBox("X/Y verschieben den Platz. Breite/Höhe bestimmen die gemeinsame Ausgangsgröße. Sprite-Größe skaliert nur diese Figur: 1 = 100 %. Der untere Mittelpunkt bleibt fest. Große Figuren können sich überlappen oder über den Bildschirmrand reichen.", HelpBoxMessageType.None));
            AddSlot(root, "Zweierdialog – Links", "twoPersonLeft");
            AddSlot(root, "Zweierdialog – Rechts", "twoPersonRight");
            AddSlot(root, "Dreierdialog – Links", "threePersonLeft");
            AddSlot(root, "Dreierdialog – Mitte", "threePersonMiddle");
            AddSlot(root, "Dreierdialog – Rechts", "threePersonRight");
            var other = new Foldout { text = "Farben, Schrift und Grafiken", value = false };
            foreach (string name in new[] { "font", "paperSprite", "thoughtSymbol", "paperColor", "inactiveBrightness" })
                other.Add(new PropertyField(serializedObject.FindProperty(name)));
            root.Add(other);
            root.TrackSerializedObjectValue(serializedObject, _ => { foreach (var sync in syncSliders) sync(); DrawPreview(); });
            root.RegisterCallback<AttachToPanelEvent>(_ => { Undo.undoRedoPerformed += RefreshSliders; DrawPreview(); });
            root.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= RefreshSliders);
            return root;
        }

        private void RefreshSliders()
        {
            foreach (var sync in syncSliders) sync();
            DrawPreview();
        }

        private SerializedProperty ReadLayoutProperty(string name)
        {
            if (conversation != null && conversation.hasPortraitLayout)
                return new SerializedObject(conversation).FindProperty("portraitLayout." + name);
            serializedObject.Update();
            return serializedObject.FindProperty(name);
        }

        private SerializedObject EditableLayout()
        {
            var data = new SerializedObject(conversation);
            if (!conversation.hasPortraitLayout)
            {
                serializedObject.Update();
                foreach (string name in new[] { "twoPersonLeft", "twoPersonRight", "threePersonLeft", "threePersonMiddle", "threePersonRight" })
                {
                    data.FindProperty("portraitLayout." + name).rectValue = serializedObject.FindProperty(name).rectValue;
                    data.FindProperty("portraitLayout." + name + "Scale").floatValue = serializedObject.FindProperty(name + "Scale").floatValue;
                }
                data.FindProperty("hasPortraitLayout").boolValue = true;
            }
            return data;
        }

        private void AddSlot(VisualElement parent, string title, string propertyName)
        {
            var foldout = new Foldout { text = title, value = propertyName.StartsWith("three") };
            parent.Add(foldout);
            for (int component = 0; component < 4; component++)
            {
                int part = component;
                var slider = new Slider(new[] { "X – links / rechts", "Y – unten / oben", "Breite", "Höhe" }[part], part < 2 ? 0 : 0.05f, 1) { showInputField = true };
                System.Action sync = () =>
                {
                    if (target == null) return;
                    var r = ReadLayoutProperty(propertyName).rectValue;
                    slider.SetValueWithoutNotify(part == 0 ? r.x : part == 1 ? r.y : part == 2 ? r.width : r.height);
                };
                syncSliders.Add(sync); sync();
                slider.RegisterValueChangedCallback(e =>
                {
                    if (conversation == null) return; var data = EditableLayout(); var prop = data.FindProperty("portraitLayout." + propertyName); var r = prop.rectValue;
                    if (part == 0) r.x = Mathf.Clamp(e.newValue, 0, 1 - r.width);
                    else if (part == 1) r.y = Mathf.Clamp(e.newValue, 0, 1 - r.height);
                    else if (part == 2) r.width = Mathf.Clamp(e.newValue, 0.05f, 1 - r.x);
                    else r.height = Mathf.Clamp(e.newValue, 0.05f, 1 - r.y);
                    prop.rectValue = r; data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(conversation);
                    sync(); DrawPreview();
                });
                foldout.Add(slider);
            }
            string scaleProperty = propertyName + "Scale";
            var size = new Slider("Sprite-Größe", 0.25f, 2f) { name = scaleProperty, showInputField = true };
            System.Action syncSize = () =>
            {
                if (target == null) return;
                serializedObject.Update();
                size.SetValueWithoutNotify(ReadLayoutProperty(scaleProperty).floatValue);
            };
            syncSliders.Add(syncSize); syncSize();
            size.RegisterValueChangedCallback(e =>
            {
                serializedObject.Update();
                if (conversation == null) return; var data = EditableLayout(); data.FindProperty("portraitLayout." + scaleProperty).floatValue = Mathf.Clamp(e.newValue, 0.25f, 2f);
                data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(conversation);
                syncSize(); DrawPreview();
            });
            foldout.Add(size);
        }

        private void Place(VisualElement element, Rect normalized, float width, float height)
        {
            element.style.position = Position.Absolute;
            element.style.left = normalized.x * width; element.style.top = (1 - normalized.yMax) * height;
            element.style.width = normalized.width * width; element.style.height = normalized.height * height;
        }

        private void DrawPreview()
        {
            if (preview == null || target == null) return;
            float width = preview.resolvedStyle.width;
            if (float.IsNaN(width) || width < 1) return;
            float height = width / aspect;
            preview.style.height = height; preview.Clear();
            if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
            { preview.Add(new Label("Einen Dialog für die Vorschau auswählen.")); return; }
            var line = conversation.lines[Mathf.Clamp(lineIndex, 0, conversation.lines.Length - 1)];
            bool thoughts = conversation.IsThought(line), narrator = conversation.IsNarrator(line);
            bool three = conversation.middleCharacter != null;
            var sides = new[] { DialogueAsset.Speaker.Left, DialogueAsset.Speaker.Middle, DialogueAsset.Speaker.Right };
            float sharedHeight = float.MaxValue;
            if (!thoughts && !narrator)
            {
                foreach (var side in sides)
                {
                    var c = conversation.CharacterFor(side);
                    if (c == null || c.isPlayerCharacter || c.portrait == null) continue;
                    Rect frame = View.GetPortraitFrame(side, three, conversation), crop = View.GetPortraitCrop(c);
                    float ratio = c.portrait.texture.width * crop.width / Mathf.Max(1, c.portrait.texture.height * crop.height);
                    sharedHeight = Mathf.Min(sharedHeight, frame.height * height, frame.width * width / ratio);
                }
                foreach (var side in sides)
                {
                    var c = conversation.CharacterFor(side);
                    if (c == null || c.isPlayerCharacter || c.portrait == null) continue;
                    Rect frame = View.GetPortraitFrame(side, three, conversation), crop = View.GetPortraitCrop(c);
                    float ratio = c.portrait.texture.width * crop.width / Mathf.Max(1, c.portrait.texture.height * crop.height);
                    var guide = new VisualElement(); Place(guide, frame, width, height);
                    guide.style.borderLeftWidth = guide.style.borderRightWidth = guide.style.borderTopWidth = guide.style.borderBottomWidth = 1;
                    guide.style.borderLeftColor = guide.style.borderRightColor = guide.style.borderTopColor = guide.style.borderBottomColor = new Color(0.4f, 0.6f, 0.8f, 0.6f);
                    preview.Add(guide);
                    var image = new Image { image = c.portrait.texture, uv = crop, scaleMode = ScaleMode.StretchToFill };
                    float brightness = line.speaker == side ? 1 : serializedObject.FindProperty("inactiveBrightness").floatValue;
                    image.tintColor = new Color(brightness, brightness, brightness, 1);
                    float scaledHeight = sharedHeight * View.GetPortraitScale(side, three, conversation);
                    var rect = new Rect(frame.center.x - scaledHeight * ratio / width / 2, frame.y, scaledHeight * ratio / width, scaledHeight / height);
                    Place(image, rect, width, height); preview.Add(image);
                }
            }
            var paper = new VisualElement();
            paper.style.backgroundColor = thoughts ? new Color(0.73f, 0.85f, 0.95f) : serializedObject.FindProperty("paperColor").colorValue;
            Place(paper, new Rect(0.06f, 0.045f, 0.88f, thoughts ? 0.265f : 0.22f), width, height);
            var text = new Label(PlayerSession.ResolveText(line.text)); text.style.fontSize = Mathf.Max(8, width / 64);
            text.style.whiteSpace = WhiteSpace.Normal; text.style.color = thoughts ? new Color(0.39f, 0.20f, 0.55f) : new Color(0.2f, 0.15f, 0.14f);
            text.style.marginLeft = width * 0.025f; text.style.marginTop = 5; text.style.marginRight = 8;
            paper.style.overflow = Overflow.Hidden; paper.Add(text); preview.Add(paper);
            string speaker = narrator ? "Narrator" : thoughts ? "Spielername" : conversation.CharacterFor(line.speaker)?.DisplayName ?? "Kein Sprecher zugewiesen";
            var name = new Label(speaker); name.style.backgroundColor = new Color(0.45f, 0.36f, 0.35f); name.style.color = Color.white;
            name.style.fontSize = Mathf.Max(8, width / 64);
            Place(name, new Rect(0.08f, thoughts ? 0.31f : 0.265f, 0.24f, 0.05f), width, height); preview.Add(name);
        }
    }
}
