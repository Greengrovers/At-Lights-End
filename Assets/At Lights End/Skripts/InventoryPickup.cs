using UnityEngine;

namespace AtLightsEnd
{
    [DisallowMultipleComponent]
    public sealed class InventoryPickup : MonoBehaviour
    {
        [SerializeField] private InventoryItem item;
        [SerializeField, Min(1)] private int quantity = 1;
        [Tooltip("Unique per placed pickup; do not reuse this ID for a second copy.")]
        [SerializeField] private string pickupId;
        private string SessionId => gameObject.scene.path + ":" + pickupId;
        [ContextMenu("Generate New Pickup ID")]
        private void GenerateNewPickupId()
        {
            pickupId = System.Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(pickupId)) pickupId = System.Guid.NewGuid().ToString("N");
        }
        private void OnEnable()
        {
            if (Application.isPlaying && PlayerSession.HasCollected(SessionId)) gameObject.SetActive(false);
        }
        public bool TryCollect()
        {
            if (!isActiveAndEnabled || string.IsNullOrEmpty(pickupId)) return false;
            var context = GameSceneContext.Current;
            if (context == null || !context.CanExplore) return false;
            if (!PlayerSession.Collect(item, quantity, SessionId)) return false;
            gameObject.SetActive(false);
            return true;
        }
    }
}
