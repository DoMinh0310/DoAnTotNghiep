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
        public List<string> playerRelicIDs = new List<string>();

        /// <summary>
        /// Lưu bonus chỉ số tích lũy cho từng tướng từ Smith Event.
        /// Key = tên asset CardData (champion.name), Value = struct lưu bonus.
        /// Dùng 2 List song song để serializable.
        /// </summary>
        public List<string> championBonusIDs = new List<string>();
        public List<ChampionStatBonus> championBonuses = new List<ChampionStatBonus>();

        /// <summary>Lấy bonus stat của 1 tướng theo tên asset.</summary>
        public ChampionStatBonus GetChampionBonus(string championID)
        {
            int idx = championBonusIDs.IndexOf(championID);
            return idx >= 0 ? championBonuses[idx] : new ChampionStatBonus();
        }

        /// <summary>Cộng thêm bonus stat cho 1 tướng.</summary>
        public void AddChampionBonus(string championID, int atkBonus, int hpBonus)
        {
            int idx = championBonusIDs.IndexOf(championID);
            if (idx >= 0)
            {
                var b = championBonuses[idx];
                b.attackBonus += atkBonus;
                b.healthBonus += hpBonus;
                championBonuses[idx] = b;
            }
            else
            {
                championBonusIDs.Add(championID);
                championBonuses.Add(new ChampionStatBonus { attackBonus = atkBonus, healthBonus = hpBonus });
            }
        }

        /// <summary>Danh sách tên asset các Relic đã nhặt được trong run.</summary>
        public List<string> ownedRelicIDs = new List<string>();

        /// <summary>Danh sách tên asset các Trinket đã nhặt được trong run.</summary>
        public List<string> ownedTrinketIDs = new List<string>();

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

        // ── Thống kê Run (Statistics) ───────────────────────────────────
        public int enemiesKilled = 0;
        public int totalDamageDealt = 0;
        public int goldEarned = 0;
        public int goldSpent = 0;

        // ════════════════════════════════════════════════════════════════
        /// <summary>Khởi tạo run mới hoàn toàn.</summary>
        public void InitNewRun(int seed)
        {
            mapSeed = seed;
            currentSlotIndex = 0; // Bắt đầu ở Slot 0 (Start)
            nonCombatSinceLastCombat = 0;

            visitedSlots      = new List<int> { 0 }; // Đã đi qua điểm xuất phát (node 0)
            randomSlotIndices = new List<int>();
            randomSlotTypes   = new List<NodeType>();
            shopCountPerRoute = new List<int> { 0, 0, 0 };

            // Run state khởi tạo
            gold              = 10; // Vàng ban đầu
            playerDeckIDs     = new List<string>();
            playerTrinketIDs  = new List<string>();
            playerRelicIDs    = new List<string>();
            ownedRelicIDs     = new List<string>();
            ownedTrinketIDs   = new List<string>();
            currentCombatStageID = "";
            championSetup     = null; // Sẽ được gán từ MapTestBootstrap hoặc MenuManager

            // Reset thống kê
            enemiesKilled = 0;
            totalDamageDealt = 0;
            goldEarned = 0;
            goldSpent = 0;
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
