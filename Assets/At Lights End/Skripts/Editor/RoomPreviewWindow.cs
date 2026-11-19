using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AtLightsEnd.Editor
{
    public sealed class RoomPreviewWindow : EditorWindow
    {
        private sealed class PreviewRoom
        {
            public GameObject Root;
            public Renderer Background;
            public string Label;
            public bool IsOpening;
        }

        private RoomNavigationController navigation;
        private Camera roomCamera;
        private readonly List<PreviewRoom> rooms = new List<PreviewRoom>();
        private PopupField<string> roomPicker;
        private PopupField<string> visitPicker;
        private Label status;
        private Button showButton;
        private Button alignButton;
        private Button stopButton;
        private AnimationModeDriver driver;
        private AnimationClip activate;
        private AnimationClip deactivate;
        private AnimationClip frame;
        private PreviewRoom activeRoom;
        private float lastAspect;

        [MenuItem("Tools/At Lights End/Raumvorschau")]
        private static void Open() => GetWindow<RoomPreviewWindow>("Raumvorschau");

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            var controller = new ObjectField("Raumnavigation") { objectType = typeof(RoomNavigationController) };
            root.Add(controller);
            roomPicker = new PopupField<string>("Raum", new List<string> { "–" }, 0);
            visitPicker = new PopupField<string>("Besuch", new List<string> { "Erster Besuch", "Späterer Besuch" }, 0);
            root.Add(roomPicker);
            root.Add(visitPicker);
            showButton = new Button(ShowSelected) { text = "Im Game View anzeigen" };
            alignButton = new Button(AlignSceneView) { text = "Scene View für die Platzierung ausrichten" };
            stopButton = new Button(StopPreview) { text = "Vorschau beenden" };
            root.Add(showButton);
            root.Add(alignButton);
            root.Add(stopButton);
            status = new Label();
            status.style.whiteSpace = WhiteSpace.Normal;
            root.Add(status);

            controller.RegisterValueChangedCallback(e => LoadRooms(e.newValue as RoomNavigationController));
            roomPicker.RegisterValueChangedCallback(_ => { if (activeRoom != null) ShowSelected(); });
            visitPicker.RegisterValueChangedCallback(_ => { if (activeRoom != null) ShowSelected(); });
            RoomNavigationController selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<RoomNavigationController>()
                : null;
            if (selected == null)
            {
                var scene = SceneManager.GetActiveScene();
                foreach (var candidate in FindObjectsByType<RoomNavigationController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.gameObject.scene == scene) { selected = candidate; break; }
            }
            controller.value = selected;
            if (selected == null) LoadRooms(null);
        }

        private void OnEnable()
        {
            EditorApplication.update += WatchPreview;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneSaving += BeforeSave;
            EditorSceneManager.sceneClosing += BeforeClose;
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        }

        private void OnDisable()
        {
            StopPreview();
            EditorApplication.update -= WatchPreview;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneSaving -= BeforeSave;
            EditorSceneManager.sceneClosing -= BeforeClose;
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            if (driver != null) DestroyImmediate(driver);
            if (activate != null) DestroyImmediate(activate);
            if (deactivate != null) DestroyImmediate(deactivate);
            if (frame != null) DestroyImmediate(frame);
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) StopPreview();
        }

        private void BeforeSave(Scene scene, string path) => StopPreview();
        private void BeforeClose(Scene scene, bool removingScene) => StopPreview();

        private void LoadRooms(RoomNavigationController controller)
        {
            StopPreview();
            navigation = controller;
            roomCamera = null;
            rooms.Clear();
            var labels = new List<string>();
            if (navigation != null)
            {
                var data = new SerializedObject(navigation);
                roomCamera = data.FindProperty("roomCamera").objectReferenceValue as Camera;
                foreach (var (field, prefix) in new[]
                {
                    ("openingRooms", "Day 0"), ("rooms", "Day 1"), ("dayTwoRooms", "Day 2"), ("dayThreeRooms", "Day 3")
                })
                {
                    var array = data.FindProperty(field);
                    for (int i = 0; i < array.arraySize; i++)
                    {
                        var entry = array.GetArrayElementAtIndex(i);
                        var room = entry.FindPropertyRelative("root").objectReferenceValue as GameObject;
                        if (room == null) continue;
                        var row = new PreviewRoom
                        {
                            Root = room,
                            Background = entry.FindPropertyRelative("background").objectReferenceValue as Renderer,
                            Label = $"{prefix} / {i + 1} / {room.name.Trim()}",
                            IsOpening = field == "openingRooms"
                        };
                        rooms.Add(row);
                        labels.Add(row.Label);
                    }
                }
            }
            roomPicker.choices = labels.Count > 0 ? labels : new List<string> { "–" };
            roomPicker.index = 0;
            showButton.SetEnabled(rooms.Count > 0 && roomCamera != null);
            status.text = roomCamera == null ? "Wähle eine Raumnavigation mit zugewiesener Kamera." : "";
        }

        private void ShowSelected()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || navigation == null || roomCamera == null ||
                roomPicker.index < 0 || roomPicker.index >= rooms.Count) return;
            if (SceneManager.loadedSceneCount != 1)
            {
                status.text = "Öffne die gewünschte Szene einzeln, damit ihre Kamera das Game View rendert.";
                return;
            }
            var selected = rooms[roomPicker.index];
            if (selected.Background == null)
            {
                status.text = "Diesem Raum ist kein Hintergrund zugewiesen.";
                return;
            }
            if (AnimationMode.InAnimationMode() && (driver == null || !AnimationMode.InAnimationMode(driver)))
            {
                status.text = "Beende zuerst die laufende Animationsvorschau in Unity.";
                return;
            }
            if (driver == null)
            {
                driver = CreateInstance<AnimationModeDriver>();
                driver.hideFlags = HideFlags.HideAndDontSave;
            }
            if (activate == null)
            {
                activate = MakeActiveClip(true);
                deactivate = MakeActiveClip(false);
                frame = new AnimationClip { hideFlags = HideFlags.HideAndDontSave };
            }
            if (!AnimationMode.InAnimationMode(driver)) AnimationMode.StartAnimationMode(driver);
            activeRoom = selected;
            try
            {
                ApplyPreview();
                status.text = "Aktiv. Das Game View zeigt den Raum mit der Spielkamera. Speichern oder Play beendet die Vorschau.";
                alignButton.SetEnabled(true);
                stopButton.SetEnabled(true);
                GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            }
            catch (Exception exception)
            {
                StopPreview();
                status.text = "Die Vorschau konnte nicht gestartet werden.";
                Debug.LogException(exception, navigation);
            }
        }

        private static AnimationClip MakeActiveClip(bool visible)
        {
            var clip = new AnimationClip { hideFlags = HideFlags.HideAndDontSave };
            clip.SetCurve("", typeof(GameObject), "m_IsActive", Constant(visible ? 1f : 0f));
            return clip;
        }

        private static AnimationCurve Constant(float value) => AnimationCurve.Constant(0f, 1f, value);

        private void ApplyPreview()
        {
            AnimationMode.BeginSampling();
            try
            {
                foreach (var room in rooms)
                    if (room.Root != null && room.Root != activeRoom.Root) SampleActive(room.Root, false);
                SampleActive(activeRoom.Root, true);
                SampleCast();

                navigation.CalculateFraming(activeRoom.Background, roomCamera, out var position, out float size);
                Transform parent = roomCamera.transform.parent;
                Vector3 local = parent == null ? position : parent.InverseTransformPoint(position);
                frame.SetCurve("", typeof(Transform), "m_LocalPosition.x", Constant(local.x));
                frame.SetCurve("", typeof(Transform), "m_LocalPosition.y", Constant(local.y));
                frame.SetCurve("", typeof(Transform), "m_LocalPosition.z", Constant(local.z));
                if (roomCamera.orthographic) frame.SetCurve("", typeof(Camera), "orthographic size", Constant(size));
                AnimationMode.SampleAnimationClip(roomCamera.gameObject, frame, 0f);
                lastAspect = roomCamera.aspect;
            }
            finally
            {
                AnimationMode.EndSampling();
            }
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void SampleActive(GameObject target, bool visible)
        {
            AnimationMode.SampleAnimationClip(target, visible ? activate : deactivate, 0f);
        }

        private void SampleCast()
        {
            if (activeRoom.IsOpening) return;
            var cast = activeRoom.Root.GetComponent<RoomVisitCast>();
            if (cast == null) return;
            var data = new SerializedObject(cast);
            var first = data.FindProperty("firstVisitCast").objectReferenceValue as GameObject;
            var later = data.FindProperty("laterVisitCast").objectReferenceValue as GameObject;
            if (first != null) SampleActive(first, visitPicker.index == 0);
            if (later != null) SampleActive(later, visitPicker.index != 0);
        }

        private void AlignSceneView()
        {
            if (activeRoom == null || roomCamera == null) return;
            var view = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
            view.in2DMode = false;
            view.LookAt(activeRoom.Background.bounds.center, roomCamera.transform.rotation,
                roomCamera.orthographicSize * Mathf.Sqrt(1f + roomCamera.aspect * roomCamera.aspect), roomCamera.orthographic, true);
            view.Focus();
        }

        private void WatchPreview()
        {
            if (activeRoom == null) return;
            if (roomCamera == null || activeRoom.Background == null || activeRoom.Root == null ||
                SceneManager.loadedSceneCount != 1 || !AnimationMode.InAnimationMode(driver))
            {
                StopPreview();
                return;
            }
            if (Mathf.Abs(roomCamera.aspect - lastAspect) > 0.0001f) ApplyPreview();
        }

        private void StopPreview()
        {
            bool wasActive = activeRoom != null;
            activeRoom = null;
            if (driver != null && AnimationMode.InAnimationMode(driver)) AnimationMode.StopAnimationMode(driver);
            alignButton?.SetEnabled(false);
            stopButton?.SetEnabled(false);
            if (wasActive)
            {
                if (status != null) status.text = "Vorschau beendet.";
                SceneView.RepaintAll();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}
