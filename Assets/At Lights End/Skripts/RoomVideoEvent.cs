using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace AtLightsEnd
{
    // Runs before RoomFirstVisitDialogue, so an entrance movie finishes before room thoughts.
    [DefaultExecutionOrder(50)]
    public sealed class RoomVideoEvent : MonoBehaviour
    {
        [SerializeField] private string eventId = "cat-shadow";
        [SerializeField] private bool playOnFirstEntry = true;
        [SerializeField, Min(0)] private int roomIndex = 1;
        [SerializeField] private RoomNavigationController navigation;
        [SerializeField] private VideoClip video;
        [SerializeField] private AudioClip sound;
        [SerializeField, Range(0, 1)] private float volume = 0.8f;
        [SerializeField] private UnityEvent onFinished = new UnityEvent();
        private VideoPlayer player;
        private AudioSource audioSource;
        private GameObject overlay;
        private UnityEngine.UI.RawImage picture;
        private bool failed, firstFrame, finished, attemptedEntry;
        private Coroutine playback;
        public bool IsPlaying { get; private set; }
        public string EventId => eventId;
        private string VisitKey => "cinematic:" + gameObject.scene.path + ":" + eventId;
        public bool HasPlayed => PlayerSession.HasVisited(VisitKey);

        private void Awake()
        {
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false; player.isLooping = false;
            player.source = VideoSource.VideoClip; player.clip = video;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.APIOnly;
            player.waitForFirstFrame = true; player.sendFrameReadyEvents = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.errorReceived += OnVideoError;
            player.frameReady += OnFirstFrame;
            player.loopPointReached += OnVideoFinished;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.loop = false;
            audioSource.spatialBlend = 0; audioSource.volume = volume; audioSource.clip = sound;

            overlay = new GameObject("Video Event Overlay", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1000;
            var background = new GameObject("Letterbox", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            background.transform.SetParent(overlay.transform, false);
            var rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            background.GetComponent<UnityEngine.UI.Image>().color = Color.black;
            var image = new GameObject("Video", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
            image.transform.SetParent(background.transform, false);
            rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            picture = image.GetComponent<UnityEngine.UI.RawImage>(); picture.raycastTarget = false;
            var fitter = image.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = video != null && video.height > 0 ? (float)video.width / video.height : 16f / 9f;
            overlay.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!playOnFirstEntry || attemptedEntry || HasPlayed || PlayerSession.BlocksWorldInput ||
                navigation == null || navigation.IsTransitioning || navigation.ActiveRoomIndex != roomIndex) return;
            if (GameSceneContext.Current == null || !GameSceneContext.Current.CanExplore) return;
            attemptedEntry = true;
            PlayOnce();
        }

        // Callable from a UnityEvent or animation event; dialogue lines can use EventId.
        public void PlayOnce()
        {
            if (!isActiveAndEnabled || IsPlaying || HasPlayed || video == null ||
                PlayerSession.BlocksWorldInput || (navigation != null && navigation.IsTransitioning)) return;
            GameSceneContext.Current?.EndInspection();
            IsPlaying = true; PlayerSession.CinematicPlaying = true;
            playback = StartCoroutine(PlayRoutine());
        }

        public static void Trigger(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            foreach (var sceneEvent in FindObjectsByType<RoomVideoEvent>(FindObjectsSortMode.None))
                if (sceneEvent.eventId == id) { sceneEvent.PlayOnce(); return; }
            Debug.LogWarning("No RoomVideoEvent found for dialogue event: " + id);
        }

        private IEnumerator PlayRoutine()
        {
            failed = firstFrame = finished = false;
            player.Prepare();
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!player.isPrepared && !failed && Time.realtimeSinceStartup < deadline) yield return null;
            if (!player.isPrepared || failed) { Finish(false); yield break; }
            picture.texture = player.texture;
            player.Play();
            deadline = Time.realtimeSinceStartup + 10f;
            while (!firstFrame && !failed && Time.realtimeSinceStartup < deadline) yield return null;
            if (!firstFrame || failed) { Finish(false); yield break; }
            // Start the separate thunder on the first decoded video frame, not during preparation.
            overlay.SetActive(true);
            if (sound != null) audioSource.Play();
            PlayerSession.MarkVisited(VisitKey);
            deadline = Time.realtimeSinceStartup + Mathf.Max(10f, (float)player.length + 5f);
            while (!finished && !failed && Time.realtimeSinceStartup < deadline) yield return null;
            // Allow the thunder tail to finish naturally after the short movie.
            while (!failed && audioSource.isPlaying) yield return null;
            Finish(!failed && finished);
        }

        private void OnFirstFrame(VideoPlayer source, long frame) => firstFrame = true;
        private void OnVideoFinished(VideoPlayer source) => finished = true;
        private void OnVideoError(VideoPlayer source, string message)
        {
            failed = true;
            Debug.LogError("Video event '" + eventId + "': " + message, this);
        }
        private void Finish(bool success)
        {
            player.Stop(); audioSource.Stop(); overlay.SetActive(false);
            IsPlaying = false; PlayerSession.CinematicPlaying = false; playback = null;
            if (success) onFinished.Invoke();
            else Debug.LogWarning("Video event could not finish; gameplay has been restored.", this);
        }
        private void OnDisable()
        {
            if (!IsPlaying) return;
            if (playback != null) StopCoroutine(playback);
            Finish(false);
        }
    }
}
