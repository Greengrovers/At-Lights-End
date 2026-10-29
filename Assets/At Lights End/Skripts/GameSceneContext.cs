using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtLightsEnd
{
    [DefaultExecutionOrder(-500)]
    public sealed class GameSceneContext : MonoBehaviour
    {
        [SerializeField] private bool night;
        [SerializeField] private string dayScenePath = "Assets/At Lights End/Scenes/Shool Day.unity";
        [SerializeField] private string nightScenePath = "Assets/At Lights End/Scenes/Prototype Nacht.unity";
        [SerializeField] private DialogueController dialogue;
        [SerializeField] private RoomNavigationController navigation;
        [SerializeField] private ObjectInspectionController inspection;
        public static GameSceneContext Current { get; private set; }
        public bool IsNight => night;
        public bool CanExplore => PlayerSession.HasPlayerName && !PlayerSession.IsLoading && !PlayerSession.CinematicPlaying &&
            (dialogue == null || !dialogue.IsOpen) && (navigation == null || !navigation.IsTransitioning) &&
            (inspection == null || !inspection.IsInspecting);

        public bool InspectInventoryItem(InventoryItem item, System.Action onReturn) =>
            CanExplore && inspection != null && inspection.BeginInventoryInspection(item, onReturn);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCurrent() => Current = null;
        private void Awake()
        {
            Current = this;
            PlayerSession.IsNight = night;
            PlayerSession.InventoryOpen = false;
            PlayerSession.IsLoading = false;
        }
        public void EndInspection() { if (inspection != null) inspection.EndInspection(); }
        public void SwitchTimeOfDay()
        {
            if (!CanExplore || PlayerSession.InventoryOpen) return;
            string destination = night ? dayScenePath : nightScenePath;
            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogError("Add the destination scene to Build Profiles: " + destination, this);
                return;
            }
            EndInspection();
            PlayerSession.IsLoading = true;
            StartCoroutine(Load(destination));
        }
        private IEnumerator Load(string destination)
        {
            var operation = SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single);
            if (operation == null) { PlayerSession.IsLoading = false; yield break; }
            while (!operation.isDone) yield return null;
        }
        private void OnDestroy() { if (Current == this) Current = null; }
    }
}
