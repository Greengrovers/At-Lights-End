using UnityEngine;

namespace AtLightsEnd
{
    /// <summary>Session-local room intros; progress is intentionally not stored in dialogue assets.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class RoomFirstVisitDialogue : MonoBehaviour
    {
        [SerializeField] private RoomNavigationController navigation;
        [SerializeField] private DialogueController dialogue;

        private string VisitId(int roomIndex) => gameObject.scene.path + ":room:" + roomIndex;
        public bool HasPlayed(int roomIndex) => PlayerSession.HasVisited(VisitId(roomIndex));

        private void LateUpdate()
        {
            // Start also covers the initially visible room. Waiting until LateUpdate lets
            // Start, camera framing and the transition coroutine finish first.
            if (PlayerSession.BlocksWorldInput || navigation == null || dialogue == null || !navigation.isActiveAndEnabled ||
                navigation.IsTransitioning || dialogue.IsOpen || navigation.ActiveRoomIndex < 0) return;
            int roomIndex = navigation.ActiveRoomIndex;
            if (HasPlayed(roomIndex)) return;
            DialogueAsset intro = navigation.ActiveRoomFirstVisitDialogue;
            if (intro == null) return;
            // Only consume the intro after the controller actually accepts it.
            if (dialogue.TryBeginConversation(intro)) PlayerSession.MarkVisited(VisitId(roomIndex));
        }
    }
}
