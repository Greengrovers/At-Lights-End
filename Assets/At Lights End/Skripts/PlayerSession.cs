using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtLightsEnd
{
    // Scene-independent, deliberately memory-only. A fresh Play/player run starts empty.
    public static class PlayerSession
    {
        public sealed class InventoryEntry
        {
            public InventoryItem Item { get; internal set; }
            public int Quantity { get; internal set; }
        }
        private static readonly List<InventoryEntry> inventory = new List<InventoryEntry>();
        private static readonly HashSet<string> collected = new HashSet<string>();
        private static readonly HashSet<string> visited = new HashSet<string>();
        public static IReadOnlyList<InventoryEntry> Inventory => inventory;
        public static string PlayerName { get; private set; } = "";
        public static bool HasPlayerName => !string.IsNullOrWhiteSpace(PlayerName);
        public static bool IsNight { get; internal set; }
        public static bool InventoryOpen { get; internal set; }
        public static bool IsLoading { get; internal set; }
        public static bool CinematicPlaying { get; internal set; }
        public static bool BlocksWorldInput => !HasPlayerName || InventoryOpen || IsLoading || CinematicPlaying;
        public static event Action InventoryChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRun()
        {
            inventory.Clear(); collected.Clear(); visited.Clear();
            PlayerName = ""; IsNight = InventoryOpen = IsLoading = false;
            CinematicPlaying = false;
            InventoryChanged = null;
        }
        public static bool SetPlayerName(string value)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0 || value.Length > 24 || value.IndexOfAny(new[] { '<', '>', '\n', '\r' }) >= 0) return false;
            PlayerName = value;
            return true;
        }
        public static string ResolveText(string value) => (value ?? "").Replace("{playerName}", PlayerName);
        public static bool HasCollected(string id) => collected.Contains(id);
        public static bool HasVisited(string id) => visited.Contains(id);
        public static void MarkVisited(string id) => visited.Add(id);

        public static bool Collect(InventoryItem item, int quantity, string pickupId)
        {
            if (!IsNight || BlocksWorldInput || item == null || string.IsNullOrWhiteSpace(item.Id) ||
                quantity <= 0 || string.IsNullOrWhiteSpace(pickupId) || collected.Contains(pickupId)) return false;
            var entry = inventory.Find(e => e.Item.Id == item.Id);
            if (entry != null && entry.Quantity > int.MaxValue - quantity) return false;
            collected.Add(pickupId);
            if (entry == null) inventory.Add(new InventoryEntry { Item = item, Quantity = quantity });
            else entry.Quantity += quantity;
            InventoryChanged?.Invoke();
            return true;
        }
    }
}
