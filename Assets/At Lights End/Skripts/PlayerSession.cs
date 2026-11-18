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
        private static readonly List<string> notebookHints = new List<string>();
        private static readonly HashSet<DialogueAsset> completedDialogues = new HashSet<DialogueAsset>();
        private static readonly HashSet<string> collected = new HashSet<string>();
        private static readonly HashSet<string> visited = new HashSet<string>();
        private static readonly Dictionary<string, int> roomVisits = new Dictionary<string, int>();
        public static int RecordRoomEntry(string id)
        {
            roomVisits.TryGetValue(id, out int count);
            roomVisits[id] = count + 1;
            return count + 1;
        }
        public static int RoomVisits(string id) => roomVisits.TryGetValue(id, out int count) ? count : 0;
        public static IReadOnlyList<InventoryEntry> Inventory => inventory;
        public static IReadOnlyList<string> NotebookHints => notebookHints;
        public static string PlayerName { get; private set; } = "";
        public static bool HasPlayerName => !string.IsNullOrWhiteSpace(PlayerName);
        public static bool IsNight { get; internal set; }
        public static bool InventoryOpen { get; internal set; }
        public static bool NotebookOpen { get; internal set; }
        public static bool IsLoading { get; internal set; }
        public static bool CinematicPlaying { get; internal set; }
        public static bool OpeningComplete { get; internal set; }
        public static int Day { get; private set; } = 1;
        public static bool BlocksWorldInput => !HasPlayerName || InventoryOpen || NotebookOpen || IsLoading || CinematicPlaying;
        public static event Action InventoryChanged;
        public static event Action<InventoryItem> ItemAdded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRun()
        {
            inventory.Clear(); collected.Clear(); visited.Clear();
            notebookHints.Clear();
            completedDialogues.Clear();
            Day = 1;
            roomVisits.Clear();
            PlayerName = ""; IsNight = InventoryOpen = IsLoading = false;
            CinematicPlaying = false;
            NotebookOpen = false;
            OpeningComplete = false;
            InventoryChanged = null;
            ItemAdded = null;
        }
        public static bool SetPlayerName(string value)
        {
            value = (value ?? "").Trim();
            if (!IsValidPlayerName(value)) return false;
            PlayerName = value;
            return true;
        }
        public static bool IsValidPlayerName(string value)
        {
            value = (value ?? "").Trim();
            return value.Length > 0 && value.Length <= 24 && value.IndexOfAny(new[] { '<', '>', '\n', '\r' }) < 0;
        }
        public static string ResolveText(string value) => (value ?? "").Replace("{playerName}", PlayerName);
        internal static bool RecordHint(string value)
        {
            string hint = ResolveText(value).Trim();
            if (hint.Length == 0 || notebookHints.Contains(hint)) return false;
            notebookHints.Add(hint);
            return true;
        }
        internal static void CompleteDialogue(DialogueAsset dialogue) => completedDialogues.Add(dialogue);
        public static bool HasCompletedDialogue(DialogueAsset dialogue) => completedDialogues.Contains(dialogue);
        internal static void AdvanceDay()
        {
            Day++;
            completedDialogues.Clear();
        }
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
            ItemAdded?.Invoke(item);
            return true;
        }
    }
}
