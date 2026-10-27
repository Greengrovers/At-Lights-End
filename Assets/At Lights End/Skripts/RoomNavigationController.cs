using System;
using System.Collections;
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
        }

        [SerializeField] private Camera roomCamera;
        [SerializeField] private Room[] rooms = new Room[0];
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
        public DialogueAsset ActiveRoomFirstVisitDialogue => ActiveRoomIndex >= 0 && ActiveRoomIndex < rooms.Length
            ? rooms[ActiveRoomIndex].firstVisitDialogue : null;
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
            if (roomCamera == null || rooms.Length == 0 ||
                Array.Exists(rooms, r => r == null || r.root == null || r.background == null))
            {
                Debug.LogError("Room navigation needs a camera and a root/background for every room.", this);
                enabled = false;
                return;
            }
            ShowRoomImmediately(Mathf.Clamp(startingRoom, 0, rooms.Length - 1));
        }

        private void LateUpdate()
        {
            if (ActiveRoomIndex < 0 || roomCamera == null || IsTransitioning) return;
            if (Mathf.Abs(previousAspect - roomCamera.aspect) > 0.0001f)
                ApplyFraming();
        }

        public void PreviousRoom() => GoToRoom(ActiveRoomIndex - 1);
        public void NextRoom() => GoToRoom(ActiveRoomIndex + 1);

        public void GoToRoom(int index)
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
            for (int i = 0; i < rooms.Length; i++) rooms[i].root.SetActive(false);
            rooms[index].root.SetActive(true);
            ActiveRoomIndex = index;
        }

        private void CalculateFraming(out Vector3 position, out float size)
        {
            Bounds bounds = rooms[ActiveRoomIndex].background.bounds;
            Quaternion inverse = Quaternion.Inverse(roomCamera.transform.rotation);
            Vector3 extents = bounds.extents;
            float maxX = 0, maxY = 0, distance = cameraDistance;
            float aspect = Mathf.Max(0.01f, roomCamera.aspect);
            float tanVertical = Mathf.Tan(roomCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 local = inverse * Vector3.Scale(extents, new Vector3(x, y, z));
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
                distance = Mathf.Max(distance, -local.z + roomCamera.nearClipPlane + 0.5f);
                if (!roomCamera.orthographic)
                {
                    distance = Mathf.Max(distance, Mathf.Abs(local.y) * framingPadding / tanVertical - local.z);
                    distance = Mathf.Max(distance, Mathf.Abs(local.x) * framingPadding / (tanVertical * aspect) - local.z);
                }
            }
            size = Mathf.Max(0.01f, maxY, maxX / aspect) * framingPadding;
            position = bounds.center - roomCamera.transform.forward * distance;
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
                previousButton.gameObject.SetActive(ActiveRoomIndex > 0);
                previousButton.interactable = !IsTransitioning;
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(ActiveRoomIndex >= 0 && ActiveRoomIndex < rooms.Length - 1);
                nextButton.interactable = !IsTransitioning;
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
