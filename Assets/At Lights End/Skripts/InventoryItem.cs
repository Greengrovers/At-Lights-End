using UnityEngine;

namespace AtLightsEnd
{
    [CreateAssetMenu(menuName = "At Lights End/Inventory/Item", fileName = "New Item")]
    public sealed class InventoryItem : ScriptableObject
    {
        [SerializeField] private string itemId;
        public string displayName;
        [TextArea(2, 8)] public string description;
        public Sprite icon;
        public InspectableObject inspectionPrefab;
        public string Id => itemId;
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId)) itemId = System.Guid.NewGuid().ToString("N");
        }
    }
}
