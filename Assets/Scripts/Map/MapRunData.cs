using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectM.Skills;

namespace ProjectM.Map
{
    /// <summary>
    /// Lưu toàn bộ trạng thái của 1 run trên Map V2 (vertical scroll, 3 routes, 3 combats).
    /// Giữ xuyên scene bởi GameManager.
    /// </summary>
    [Serializable]
    public class MapRunData
    {
        // ── Vị trí hiện tại ─────────────────────────────────────────────
        /// <summary>Slot index player đang đứng. -1 = chưa di chuyển (ở Start).</summary>
        public int currentSlotIndex = -1;

        // ── Lịch sử ─────────────────────────────────────────────────────
        /// <summary>Danh sách các slot đã visit (để vẽ đường đã đi).</summary>
        public List<int> visitedSlots = new List<int>();

        // ── Counter combat ───────────────────────────────────────────────
        /// <summary>
        /// Số event không phải combat đã qua kể từ lần combat gần nhất.
        /// Khi đạt 2 → bắt buộc phải đến combat tiếp theo.
        /// (Được enforce qua cấu trúc graph, biến này để track/UI.)
        /// </summary>
        public int nonCombatSinceLastCombat = 0;

        // ── Random event types ───────────────────────────────────────────
        // Dùng 2 List song song thay cho Dictionary vì Dictionary không serializable.
        public List<int> randomSlotIndices = new List<int>();
        public List<NodeType> randomSlotTypes = new List<NodeType>();

        // ── Shop quota tracking ──────────────────────────────────────────
        // Đếm số lượng Shop đã được assign cho từng route để enforce giới hạn max 2 shop/route.
        public List<int> shopCountPerRoute = new List<int> { 0, 0, 0 };

        // ── Seed ─────────────────────────────────────────────────────────
        public int mapSeed = 0;

        // ══════════════════════════════════════════════════════════════
        // RUN STATE — Tài nguyên của người chơi xuyên suốt 1 run
        // ══════════════════════════════════════════════════════════════

        /// <summary>Số Vàng hiện tại của người chơi.</summary>
        public int gold = 0;

        /// <summary>
        /// Danh sách ID (CardData.id) các thẻ trong bộ bài hiện tại.
        /// Được dùng làm nguồn sự thật cho Inventory và mọi UI liên quan đến Deck.
        /// </summary>
        public List<string> playerDeckIDs = new List<string>();

        /// <summary>
        /// Danh sách ID các Trinket/Relic đang được trang bị.
        /// </summary>
        public List<string> playerTrinketIDs = new List<string>();

        /// <summary>
        /// Stage data ID sẽ được load khi vào Combat Scene.
        /// Ví dụ: "stage_data1.1". MapManager ghi vào đây trước khi chuyển scene.
        /// </summary>
        public string currentCombatStageID = "";

        /// <summary>
        /// ChampionSetup của run hiện tại.
        /// Lưu tham chiếu đến asset StarterDeck để InventoryManager ở MapScene
        /// có thể dùng y hệt InventoryManager ở BattleScene.
        /// </summary>
        [NonSerialized] // ScriptableObject không serialize được qua JSON — chỉ giữ trong RAM trong 1 session.
        public ChampionSetup championSetup;

        // ════════════════════════════════════════════════════════════════
        /// <summary>Khởi tạo run mới hoàn toàn.</summary>
        public void InitNewRun(int seed)
        {
            mapSeed = seed;
            currentSlotIndex = 0; // Bắt đầu ở Slot 0 (Start)
            nonCombatSinceLastCombat = 0;

            visitedSlots      = new List<int>();
            randomSlotIndices = new List<int>();
            randomSlotTypes   = new List<NodeType>();
            shopCountPerRoute = new List<int> { 0, 0, 0 };

            // Run state khởi tạo
            gold              = 10; // Vàng ban đầu
            playerDeckIDs     = new List<string>();
            playerTrinketIDs  = new List<string>();
            currentCombatStageID = "";
            championSetup     = null; // Sẽ được gán từ MapTestBootstrap hoặc MenuManager
        }

        // ════════════════════════════════════════════════════════════════
        // ACCESSORS
        // ════════════════════════════════════════════════════════════════
        public bool HasVisited(int slotIndex) =>
            visitedSlots.Contains(slotIndex);

        public NodeType GetSlotType(int slotIndex)
        {
            int idx = randomSlotIndices.IndexOf(slotIndex);
            return idx >= 0 ? randomSlotTypes[idx] : NodeType.Battle;
        }

        public void SetSlotType(int slotIndex, NodeType type)
        {
            int idx = randomSlotIndices.IndexOf(slotIndex);
            if (idx >= 0)
                randomSlotTypes[idx] = type;
            else
            {
                randomSlotIndices.Add(slotIndex);
                randomSlotTypes.Add(type);
            }
        }
    }
}
