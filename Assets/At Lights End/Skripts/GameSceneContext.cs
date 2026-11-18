using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private DialogueActor dayOneCluesThought;
        [SerializeField] private DialogueActor dayTwoCafeteriaFollowUp;
        [SerializeField] private DialogueAsset[] dayTwoCafeteriaConversations = new DialogueAsset[0];
        [SerializeField] private DialogueActor dayThreeCafeteriaFollowUp;
        public static GameSceneContext Current { get; private set; }
        public bool IsNight => night;
        public bool IsOpening => navigation != null && navigation.IsOpening;
        private int startedOpeningRoom = -1;
        private HashSet<DialogueAsset> requiredHints;
        public bool CanExplore => !IsOpening && PlayerSession.HasPlayerName && !PlayerSession.IsLoading && !PlayerSession.CinematicPlaying &&
            (dialogue == null || !dialogue.IsOpen) && (navigation == null || !navigation.IsTransitioning) &&
            (inspection == null || !inspection.IsInspecting);
        public bool CanSwitchTime
        {
            get
            {
                if (!CanExplore || PlayerSession.BlocksWorldInput || navigation == null) return false;
                if (night) return PlayerSession.Day < 3 && navigation.CanFinishNight;
                var departure = DepartureDialogue();
                return departure != null && AllHintsCompleted() && PlayerSession.HasCompletedDialogue(departure.conversation);
            }
        }

        private DialogueActor DepartureDialogue()
        {
            switch (PlayerSession.Day)
            {
                case 1: return dayOneCluesThought;
                case 2: return dayTwoCafeteriaFollowUp;
                case 3: return dayThreeCafeteriaFollowUp;
                default: return null;
            }
        }

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
        private void LateUpdate()
        {
            if (IsOpening)
            {
                StartOpeningDialogue();
                return;
            }
            if (!CanExplore || PlayerSession.BlocksWorldInput || navigation == null || navigation.ActiveRoomIndex < 0) return;
            var entry = navigation.ActiveRoomFirstVisitDialogue;
            if (entry != null && !PlayerSession.HasVisited(RoomFirstVisitDialogue.VisitId(gameObject.scene.path, navigation.ActiveRoomIndex))) return;
            if (night)
            {
                if (navigation.ActiveGroupCollected) TryPlayOnce(navigation.ActiveRoomCompletionDialogue);
                return;
            }
            if (!AllHintsCompleted()) return;

            if (PlayerSession.Day == 1)
                TryPlayOnce(dayOneCluesThought);
            else if (PlayerSession.Day == 2 && dayTwoCafeteriaFollowUp != null && dayTwoCafeteriaFollowUp.isActiveAndEnabled)
            {
                foreach (var conversation in dayTwoCafeteriaConversations)
                    if (!PlayerSession.HasCompletedDialogue(conversation)) return;
                TryPlayOnce(dayTwoCafeteriaFollowUp);
            }
            else if (PlayerSession.Day == 3 && dayThreeCafeteriaFollowUp != null && dayThreeCafeteriaFollowUp.isActiveAndEnabled)
                TryPlayOnce(dayThreeCafeteriaFollowUp);
        }

        private bool AllHintsCompleted()
        {
            if (navigation == null || navigation.ActiveRoomIndex < 0) return false;
            if (requiredHints == null)
            {
                requiredHints = navigation.FindHintDialogues();
                if (dayOneCluesThought != null) requiredHints.Remove(dayOneCluesThought.conversation);
                if (dayTwoCafeteriaFollowUp != null) requiredHints.Remove(dayTwoCafeteriaFollowUp.conversation);
                if (dayThreeCafeteriaFollowUp != null) requiredHints.Remove(dayThreeCafeteriaFollowUp.conversation);
            }
            foreach (var hint in requiredHints)
                if (!PlayerSession.HasCompletedDialogue(hint)) return false;
            return true;
        }

        private void TryPlayOnce(DialogueActor source)
        {
            if (source == null || source.conversation == null || PlayerSession.HasCompletedDialogue(source.conversation)) return;
            dialogue.TryBeginConversation(source.conversation, completed: () => { });
        }

        private void StartOpeningDialogue()
        {
            if (PlayerSession.BlocksWorldInput || !navigation.isActiveAndEnabled ||
                navigation.IsTransitioning || navigation.ActiveRoomIndex < 0 || startedOpeningRoom == navigation.ActiveRoomIndex)
                return;

            var source = navigation.ActiveOpeningDialogue;
            if (source == null || source.conversation == null || !source.conversation.IsValid || dialogue == null)
            {
                startedOpeningRoom = navigation.ActiveRoomIndex;
                Debug.LogError("The opening room needs a valid dialogue on its Dialog empty.", this);
                return;
            }
            if (dialogue.TryBeginConversation(source.conversation, source, CompleteOpeningRoom))
                startedOpeningRoom = navigation.ActiveRoomIndex;
        }

        private void CompleteOpeningRoom()
        {
            if (navigation.HasNextOpeningRoom)
            {
                navigation.AdvanceOpeningRoom();
                return;
            }
            if (TryLoadOtherScene() && night) PlayerSession.OpeningComplete = true;
        }

        public void SwitchTimeOfDay()
        {
            if (!CanSwitchTime) return;
            if (TryLoadOtherScene() && night) PlayerSession.AdvanceDay();
        }

        private bool TryLoadOtherScene()
        {
            string destination = night ? dayScenePath : nightScenePath;
            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogError("Add the destination scene to Build Profiles: " + destination, this);
                return false;
            }
            EndInspection();
            PlayerSession.IsLoading = true;
            StartCoroutine(Load(destination));
            return true;
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
