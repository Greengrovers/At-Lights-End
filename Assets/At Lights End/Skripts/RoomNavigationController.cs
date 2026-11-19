using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AtLightsEnd
{
    [DisallowMultipleComponent]
    public sealed class RoomNavigationController : MonoBehaviour
    {
        [Serializable]
        public sealed class Room
        {
            public GameObject root;
            [Tooltip("The background used to fit the camera, excluding props.")]
            public Renderer background;
            [Tooltip("Automatically played once when this room is first entered. Leave empty for no intro.")]
            public DialogueAsset firstVisitDialogue;
            public DialogueActor openingDialogue;
            public DialogueActor firstVisitSource;
            public DialogueActor completionSource;
            [Min(0)] public int progressGroup;
            [NonSerialized] public InventoryPickup[] requiredPickups = Array.Empty<InventoryPickup>();
        }

        [SerializeField] private Camera roomCamera;
        [SerializeField] private Room[] rooms = new Room[0];
        [SerializeField] private Room[] openingRooms = new Room[0];
        [SerializeField] private Room[] dayTwoRooms = new Room[0];
        [SerializeField] private Room[] dayThreeRooms = new Room[0];
        [SerializeField] private int startingRoom;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.65f;
        [SerializeField, Min(1f)] private float framingPadding = 1.02f;
        [SerializeField, Min(1f)] private float cameraDistance = 10f;
        [SerializeField] private UnityEngine.UI.Button previousButton;
        [SerializeField] private UnityEngine.UI.Button nextButton;
        [SerializeField] private CanvasGroup transitionCover;
        [SerializeField] private ObjectInspectionController inspection;

        public int ActiveRoomIndex { get; private set; } = -1;
        public bool IsTransitioning { get; private set; }
        public bool IsOpening => !PlayerSession.OpeningComplete && openingRooms.Length > 0;
        public DialogueActor ActiveOpeningDialogue => IsOpening && ActiveRoomIndex >= 0
            ? rooms[ActiveRoomIndex].openingDialogue : null;
        public bool HasNextOpeningRoom => IsOpening && ActiveRoomIndex + 1 < rooms.Length;
        public DialogueAsset ActiveRoomFirstVisitDialogue => !IsOpening && ActiveRoomIndex >= 0 && ActiveRoomIndex < rooms.Length
            ? rooms[ActiveRoomIndex].firstVisitSource != null
                ? rooms[ActiveRoomIndex].firstVisitSource.conversation : rooms[ActiveRoomIndex].firstVisitDialogue
            : null;
        private bool GateNightRooms => PlayerSession.IsNight && !IsOpening;
        private bool HasActiveRoom => ActiveRoomIndex >= 0 && ActiveRoomIndex < rooms.Length;
        public DialogueActor ActiveRoomCompletionDialogue => HasActiveRoom ? rooms[ActiveRoomIndex].completionSource : null;
        public bool ActiveGroupCollected => GateNightRooms && HasActiveRoom &&
            GroupCollected(rooms[ActiveRoomIndex].progressGroup);
        public bool CanFinishNight => GateNightRooms && HasActiveRoom &&
            rooms[ActiveRoomIndex].progressGroup == rooms[rooms.Length - 1].progressGroup &&
            GroupComplete(rooms[ActiveRoomIndex].progressGroup);
        private Coroutine transition;
        private float previousAspect;
        private bool resumeInspection;

        private void OnEnable()
        {
            if (previousButton != null) previousButton.onClick.AddListener(PreviousRoom);
            if (nextButton != null) nextButton.onClick.AddListener(NextRoom);
        }

        private void Start()
        {
            if (roomCamera == null) roomCamera = Camera.main;
            Room[] activeRooms = GetRoomsForRun();
            foreach (var list in new[] { rooms, openingRooms, dayTwoRooms, dayThreeRooms })
                if (list != activeRooms) HideRooms(list);
            rooms = activeRooms;
            if (roomCamera == null || rooms.Length == 0 ||
                Array.Exists(rooms, r => r == null || r.root == null || r.background == null))
            {
                Debug.LogError("Room navigation needs a camera and a root/background for every room.", this);
                enabled = false;
                return;
            }
            if (GateNightRooms)
                foreach (var room in rooms)
                    room.requiredPickups = Array.FindAll(room.root.GetComponentsInChildren<InventoryPickup>(true),
                        pickup => pickup.CountsForProgress && IsEnabledInsideRoom(pickup.transform, room.root.transform));
            ShowRoomImmediately(IsOpening || GateNightRooms ? 0 : Mathf.Clamp(startingRoom, 0, rooms.Length - 1));
        }

        private Room[] GetRoomsForRun()
        {
            if (IsOpening) return openingRooms;
            if (PlayerSession.Day == 2 && dayTwoRooms.Length > 0) return dayTwoRooms;
            if (PlayerSession.Day == 3 && dayThreeRooms.Length > 0) return dayThreeRooms;
            return rooms;
        }

        private static void HideRooms(IEnumerable<Room> list)
        {
            foreach (var room in list)
                if (room?.root != null) room.root.SetActive(false);
        }

        private static bool IsEnabledInsideRoom(Transform child, Transform root)
        {
            for (var current = child; current != root; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }

        internal HashSet<DialogueAsset> FindHintDialogues()
        {
            var hints = new HashSet<DialogueAsset>();
            foreach (var room in rooms)
            {
                if (room.firstVisitDialogue != null && room.firstVisitDialogue.hintDialog) hints.Add(room.firstVisitDialogue);
                foreach (var actor in room.root.GetComponentsInChildren<DialogueActor>(true))
                    if (actor.enabled && actor.conversation != null && actor.conversation.hintDialog)
                        hints.Add(actor.conversation);
            }
            return hints;
        }

        private void LateUpdate()
        {
            if (ActiveRoomIndex < 0 || roomCamera == null || IsTransitioning) return;
            if (GateNightRooms) RefreshButtons();
            if (Mathf.Abs(previousAspect - roomCamera.aspect) > 0.0001f)
                ApplyFraming();
        }

        public void PreviousRoom() => GoToRoom(ActiveRoomIndex - 1);
        public void NextRoom() => GoToRoom(ActiveRoomIndex + 1);

        public void GoToRoom(int index)
        {
            if (IsOpening || PlayerSession.BlocksWorldInput || !CanGoTo(index)) return;
            StartRoomTransition(index);
        }

        private bool CanGoTo(int index)
        {
            if (index < 0 || index >= rooms.Length || ActiveRoomIndex < 0) return false;
            if (!GateNightRooms) return true;
            if (Mathf.Abs(index - ActiveRoomIndex) != 1) return false;
            int current = rooms[ActiveRoomIndex].progressGroup;
            int target = rooms[index].progressGroup;
            return target == current || (target == current + 1 && GroupComplete(current));
        }

        private bool GroupComplete(int group)
        {
            if (!GroupCollected(group)) return false;
            foreach (var room in rooms)
            {
                if (room.progressGroup != group || room.completionSource == null) continue;
                if (!PlayerSession.HasCompletedDialogue(room.completionSource.conversation)) return false;
            }
            return true;
        }

        private bool GroupCollected(int group)
        {
            foreach (var room in rooms)
            {
                if (room.progressGroup != group) continue;
                foreach (var pickup in room.requiredPickups)
                    if (pickup != null && !pickup.IsCollected) return false;
            }
            return true;
        }

        internal void AdvanceOpeningRoom()
        {
            if (HasNextOpeningRoom) StartRoomTransition(ActiveRoomIndex + 1);
        }

        private void StartRoomTransition(int index)
        {
            if (!isActiveAndEnabled || IsTransitioning || roomCamera == null ||
                index < 0 || index >= rooms.Length || index == ActiveRoomIndex) return;
            transition = StartCoroutine(ChangeRoom(index));
        }

        public void ShowRoomImmediately(int index)
        {
            if (roomCamera == null || index < 0 || index >= rooms.Length) return;
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (inspection != null) inspection.EndInspection();
            RestoreInspection();
            IsTransitioning = false;
            ActivateOnly(index);
            ApplyFraming();
            SetCover(0f);
            RefreshButtons();
        }

        private IEnumerator ChangeRoom(int index)
        {
            IsTransitioning = true;
            RefreshButtons();
            if (inspection != null)
            {
                inspection.EndInspection();
                resumeInspection = inspection.enabled;
                inspection.enabled = false;
            }
            float fadeOutDuration = transitionDuration * 0.3f;
            for (float elapsed = 0; elapsed < fadeOutDuration; elapsed += Time.unscaledDeltaTime)
            {
                SetCover(Mathf.SmoothStep(0, 1, elapsed / fadeOutDuration));
                yield return null;
            }
            SetCover(1f);
            Vector3 fromPosition = roomCamera.transform.position;
            float fromSize = roomCamera.orthographicSize;
            // Disable the old room before activating the new one; never show both.
            ActivateOnly(index);
            float moveDuration = Mathf.Max(0.01f, transitionDuration - fadeOutDuration);
            for (float elapsed = 0; elapsed < moveDuration; elapsed += Time.unscaledDeltaTime)
            {
                CalculateFraming(out Vector3 position, out float size);
                float t = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
                roomCamera.transform.position = Vector3.Lerp(fromPosition, position, t);
                roomCamera.orthographicSize = Mathf.Lerp(fromSize, size, t);
                SetCover(1f - t);
                yield return null;
            }
            ApplyFraming();
            IsTransitioning = false;
            SetCover(0f);
            RestoreInspection();
            transition = null;
            RefreshButtons();
        }

        private void ActivateOnly(int index)
        {
            if (!IsOpening)
            {
                string visitKey = gameObject.scene.path + ":room-entry:" + PlayerSession.Day + ":" + index;
                int visits = ActiveRoomIndex != index ? PlayerSession.RecordRoomEntry(visitKey) : PlayerSession.RoomVisits(visitKey);
                var cast = rooms[index].root.GetComponent<RoomVisitCast>();
                if (cast != null) cast.ApplyVisit(visits);
            }
            for (int i = 0; i < rooms.Length; i++) rooms[i].root.SetActive(false);
            rooms[index].root.SetActive(true);
            ActiveRoomIndex = index;
        }

        private void CalculateFraming(out Vector3 position, out float size)
        {
            CalculateFraming(rooms[ActiveRoomIndex].background, roomCamera, out position, out size);
        }

        public void CalculateFraming(Renderer background, Camera camera, out Vector3 position, out float size)
        {
            Bounds bounds = background.bounds;
            Quaternion inverse = Quaternion.Inverse(camera.transform.rotation);
            Vector3 extents = bounds.extents;
            float maxX = 0, maxY = 0, distance = cameraDistance;
            float aspect = Mathf.Max(0.01f, camera.aspect);
            float tanVertical = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 local = inverse * Vector3.Scale(extents, new Vector3(x, y, z));
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
                distance = Mathf.Max(distance, -local.z + camera.nearClipPlane + 0.5f);
                if (!camera.orthographic)
                {
                    distance = Mathf.Max(distance, Mathf.Abs(local.y) * framingPadding / tanVertical - local.z);
                    distance = Mathf.Max(distance, Mathf.Abs(local.x) * framingPadding / (tanVertical * aspect) - local.z);
                }
            }
            size = Mathf.Max(0.01f, maxY, maxX / aspect) * framingPadding;
            position = bounds.center - camera.transform.forward * distance;
        }

        private void ApplyFraming()
        {
            CalculateFraming(out Vector3 position, out float size);
            roomCamera.transform.position = position;
            if (roomCamera.orthographic) roomCamera.orthographicSize = size;
            previousAspect = roomCamera.aspect;
        }

        private void SetCover(float alpha)
        {
            if (transitionCover == null) return;
            transitionCover.alpha = alpha;
            transitionCover.blocksRaycasts = IsTransitioning;
        }

        private void RefreshButtons()
        {
            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(!IsOpening && ActiveRoomIndex > 0 &&
                    (!GateNightRooms || rooms[ActiveRoomIndex - 1].progressGroup == rooms[ActiveRoomIndex].progressGroup));
                previousButton.interactable = !IsTransitioning && CanGoTo(ActiveRoomIndex - 1);
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(!IsOpening && ActiveRoomIndex >= 0 && ActiveRoomIndex < rooms.Length - 1);
                nextButton.interactable = !IsTransitioning && CanGoTo(ActiveRoomIndex + 1);
            }
        }

        private void RestoreInspection()
        {
            if (resumeInspection && inspection != null) inspection.enabled = true;
            resumeInspection = false;
        }

        private void OnDisable()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(PreviousRoom);
            if (nextButton != null) nextButton.onClick.RemoveListener(NextRoom);
            if (transition != null) StopCoroutine(transition);
            transition = null;
            IsTransitioning = false;
            RestoreInspection();
            SetCover(0f);
            if (ActiveRoomIndex >= 0 && ActiveRoomIndex < rooms.Length && roomCamera != null &&
                rooms[ActiveRoomIndex] != null && rooms[ActiveRoomIndex].background != null)
                ApplyFraming();
        }
    }
}
