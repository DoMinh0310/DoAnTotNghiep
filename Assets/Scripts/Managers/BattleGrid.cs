using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;

namespace ProjectM.Managers
{
    public class BattleGrid : MonoBehaviour
    {
        public static BattleGrid Instance { get; private set; }

        [Header("Player Slots (Center → Outward)")]
        [Tooltip("Thứ tự: slot3, slot2, slot1, slot0 (tối đa 4 tướng)")]
        public CardDropZone[] playerSlotsTop = new CardDropZone[4];
        [Tooltip("Thứ tự: slot9, slot8, slot7, slot6 (tối đa 4 tướng)")]
        public CardDropZone[] playerSlotsBot = new CardDropZone[4];

        [Header("Enemy Slots (Center → Outward)")]
        [Tooltip("Thứ tự: slot4, slot5, slot6")]
        public CardDropZone[] enemySlotsTop = new CardDropZone[3];
        [Tooltip("Thứ tự: slot10, slot11, slot12")]
        public CardDropZone[] enemySlotsBot = new CardDropZone[3];

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ══════════════════════════════════════════
        // PLACEMENT VALIDATION
        // ══════════════════════════════════════════

        /// <summary>
        /// Slot này có được phép đặt bài không?
        /// Quy tắc: Slot[i] chỉ mở khi Slot[i-1] (cùng hàng, gần TT hơn) đã có thẻ.
        /// </summary>
        public bool CanPlaceInSlot(CardDropZone slot)
        {
            int idx = GetIndexInRow(slot, playerSlotsTop);
            if (idx >= 0) return idx == 0 || GetCardInSlot(playerSlotsTop[idx - 1]) != null;

            idx = GetIndexInRow(slot, playerSlotsBot);
            if (idx >= 0) return idx == 0 || GetCardInSlot(playerSlotsBot[idx - 1]) != null;

            return false;
        }

        /// <summary>
        /// Thay vì chặn, tìm slot gần TT nhất còn trống trong cùng hàng với slot được chỉ định.
        /// Người chơi thả bài ở bất kỳ ô nào trong hàng đó, bài sẽ tự động vào ô đúng nhất.
        /// movingCard: thẻ đang được di chuyển (nếu có) — không tính vào giới hạn 4 tướng.
        /// </summary>
        public CardDropZone GetBestAvailableSlotInRow(CardDropZone targetSlot, CardBattle movingCard = null)
        {
            // Kiểm tra giới hạn tổng 4 tướng người chơi
            // Nếu đang di chuyển thẻ đã có trên sàn (Data != null) → không tính thẻ đó vào limit
            int currentCount = GetAllPlayerCards().Count;
            bool isMovingExisting = movingCard != null && movingCard.Data != null;
            if (isMovingExisting) currentCount--;

            if (currentCount >= 4)
            {
                BattleDebugger.Warn("⚠️ Đã đạt giới hạn tối đa 4 tướng trên sàn!");
                return null;
            }

            CardDropZone[] row = GetPlayerRowOf(targetSlot);
            if (row == null) return null;

            foreach (var slot in row)
            {
                // Bỏ qua slot đang chứa chính thẻ đang kéo (để không tự block chính mình)
                CardBattle inSlot = GetCardInSlot(slot);
                if (inSlot == null || inSlot == movingCard) return slot;
            }
            return null; // Hàng đầy
        }

        private CardDropZone[] GetPlayerRowOf(CardDropZone slot)
        {
            if (GetIndexInRow(slot, playerSlotsTop) >= 0) return playerSlotsTop;
            if (GetIndexInRow(slot, playerSlotsBot) >= 0) return playerSlotsBot;
            return null;
        }

        // ══════════════════════════════════════════
        // CARD REGISTRATION
        // ══════════════════════════════════════════

        /// <summary>
        /// Đăng ký thẻ vừa được đặt lên sàn. Gọi Initialize() với CardData và phe.
        /// </summary>
        public void RegisterCard(CardBattle card, bool isPlayer)
        {
            CardDisplay display = card.GetComponent<CardDisplay>();
            if (display == null || display.cardData == null)
            {
                Debug.LogError($"[BattleGrid] {card.name} thiếu CardData, không thể Initialize!");
                return;
            }
            card.Initialize(display.cardData, isPlayer);
            Debug.Log($"[BattleGrid] Đã đăng ký {display.cardData.cardName} | isPlayer={isPlayer}");
        }

        // ══════════════════════════════════════════
        // ATTACK TARGETING
        // ══════════════════════════════════════════

        /// <summary>
        /// Tìm mục tiêu cho attacker. Ưu tiên cùng hàng, fallback chéo hàng.
        /// </summary>
        public CardBattle GetAttackTarget(CardBattle attacker)
        {
            bool isTop = IsCardInTopRow(attacker);
            CardBattle target;

            if (attacker.IsPlayerCard)
            {
                target = GetFrontCard(isTop ? enemySlotsTop : enemySlotsBot)
                      ?? GetFrontCard(isTop ? enemySlotsBot : enemySlotsTop);
            }
            else
            {
                target = GetFrontCard(isTop ? playerSlotsTop : playerSlotsBot)
                      ?? GetFrontCard(isTop ? playerSlotsBot : playerSlotsTop);
            }

            if (target != null)
                BattleDebugger.Log($"🗡️ {attacker.Data?.cardName} → {target.Data?.cardName}");
            else
                BattleDebugger.Warn($"{attacker.Data?.cardName} không tìm thấy mục tiêu!");

            return target;
        }

        // Lấy thẻ gần TT nhất trong hàng (slot[0] là gần TT nhất)
        private CardBattle GetFrontCard(CardDropZone[] row)
        {
            foreach (var slot in row)
            {
                CardBattle c = GetCardInSlot(slot);
                if (c != null && !c.IsDead) return c;
            }
            return null;
        }

        // ══════════════════════════════════════════
        // SHIFTING (DỒN HÀNG)
        // ══════════════════════════════════════════

        /// <summary>Được gọi khi thẻ chết. Dồn hàng để lấp chỗ trống.</summary>
        public void OnCardDied(CardBattle deadCard)
        {
            CardDropZone[] row = FindRowOfCard(deadCard);
            if (row != null) StartCoroutine(ShiftAfterFrame(row));
        }

        private IEnumerator ShiftAfterFrame(CardDropZone[] row)
        {
            yield return null; // Đợi Destroy() hoàn tất
            ShiftRow(row);
        }

        /// <summary>Dồn hàng chứa slot được chỉ định. Gọi khi thẻ bị di chuyển (không phải chết).</summary>
        public void ShiftRowOf(CardDropZone slot)
        {
            // Tìm hàng player có chứa slot này
            if (GetIndexInRow(slot, playerSlotsTop) >= 0) { ShiftRow(playerSlotsTop); return; }
            if (GetIndexInRow(slot, playerSlotsBot) >= 0) { ShiftRow(playerSlotsBot); return; }
            // Cũng xử lý cho hàng địch nếu cần sau này
            if (GetIndexInRow(slot, enemySlotsTop) >= 0) { ShiftRow(enemySlotsTop); return; }
            if (GetIndexInRow(slot, enemySlotsBot) >= 0) { ShiftRow(enemySlotsBot); }
        }

        private void ShiftRow(CardDropZone[] row)
        {
            // Duyệt từ slot gần TT nhất ra ngoài
            // Nếu slot[i] trống mà slot[i+1] có thẻ → dồn vào
            for (int i = 0; i < row.Length - 1; i++)
            {
                if (GetCardInSlot(row[i]) == null)
                {
                    CardBattle next = GetCardInSlot(row[i + 1]);
                    if (next != null) MoveCardToSlot(next, row[i]);
                }
            }
        }

        private void MoveCardToSlot(CardBattle card, CardDropZone newSlot)
        {
            card.transform.SetParent(newSlot.transform);

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                card.transform.localRotation = Quaternion.identity;
                cardRect.anchoredPosition = Vector2.zero;
                // Scale giữ nguyên vì các slot cùng hàng đều cùng kích thước
            }

            // Cập nhật HoverHandler để animation hover vẫn đúng sau khi dời
            CardHoverHandler hover = card.GetComponent<CardHoverHandler>();
            if (hover != null)
                hover.UpdateBaseState(Vector2.zero, Quaternion.identity, card.transform.localScale);

            Debug.Log($"[BattleGrid] Dồn {card.Data.cardName} → {newSlot.name}");
        }

        // ══════════════════════════════════════════
        // GET ALL CARDS (BattleManager dùng để tick)
        // ══════════════════════════════════════════

        public List<CardBattle> GetAllPlayerCards() =>
            CollectCards(playerSlotsTop, playerSlotsBot);

        public List<CardBattle> GetAllEnemyCards() =>
            CollectCards(enemySlotsTop, enemySlotsBot);

        /// <summary>
        /// Trả về tất cả kẻ địch còn sống trong cùng hàng với target.
        /// Dùng bởi SkillExecutor khi skill có TargetType = EnemyRow.
        /// </summary>
        public List<CardBattle> GetEnemiesInSameRow(CardBattle target)
        {
            var result = new List<CardBattle>();
            if (target == null) return result;

            // Xác định hàng địch chứa target
            CardDropZone[] row = null;
            if (target.transform.parent != null)
            {
                var slot = target.transform.parent.GetComponent<CardDropZone>();
                if (slot != null)
                {
                    if (GetIndexInRow(slot, enemySlotsTop) >= 0) row = enemySlotsTop;
                    else if (GetIndexInRow(slot, enemySlotsBot) >= 0) row = enemySlotsBot;
                }
            }

            if (row == null) return result;

            // Thu thập tất cả thẻ địch còn sống trong hàng đó
            foreach (var slot in row)
            {
                CardBattle card = GetCardInSlot(slot);
                if (card != null && !card.IsDead)
                    result.Add(card);
            }
            return result;
        }

        private List<CardBattle> CollectCards(CardDropZone[] row1, CardDropZone[] row2)
        {
            var list = new List<CardBattle>();
            // Lọc bỏ thẻ đã chết (IsDead=true) — chúng vẫn còn trên scene trong lúc animation dissolve chạy
            foreach (var s in row1) { var c = GetCardInSlot(s); if (c != null && !c.IsDead) list.Add(c); }
            foreach (var s in row2) { var c = GetCardInSlot(s); if (c != null && !c.IsDead) list.Add(c); }
            return list;
        }

        // ══════════════════════════════════════════
        // ENEMY SLOT HELPERS (dùng bởi EnemyWaveManager)
        // ══════════════════════════════════════════

        /// <summary>Lấy CardDropZone tương ứng với EnemySlotId.</summary>
        public CardDropZone GetEnemySlotById(EnemySlotId id)
        {
            return id switch
            {
                EnemySlotId.Top0 => enemySlotsTop.Length > 0 ? enemySlotsTop[0] : null,
                EnemySlotId.Top1 => enemySlotsTop.Length > 1 ? enemySlotsTop[1] : null,
                EnemySlotId.Top2 => enemySlotsTop.Length > 2 ? enemySlotsTop[2] : null,
                EnemySlotId.Bot0 => enemySlotsBot.Length > 0 ? enemySlotsBot[0] : null,
                EnemySlotId.Bot1 => enemySlotsBot.Length > 1 ? enemySlotsBot[1] : null,
                EnemySlotId.Bot2 => enemySlotsBot.Length > 2 ? enemySlotsBot[2] : null,
                _ => null
            };
        }

        /// <summary>Tìm EnemySlotId của một CardDropZone, null nếu không tìm thấy.</summary>
        public EnemySlotId? GetSlotIdOf(CardDropZone slot)
        {
            for (int i = 0; i < enemySlotsTop.Length; i++)
                if (enemySlotsTop[i] == slot) return (EnemySlotId)i;
            for (int i = 0; i < enemySlotsBot.Length; i++)
                if (enemySlotsBot[i] == slot) return (EnemySlotId)(i + 3);
            return null;
        }

        /// <summary>Trả về danh sách slot địch đang trống (kèm EnemySlotId).</summary>
        public List<(EnemySlotId id, CardDropZone zone)> GetEmptyEnemySlots()
        {
            var result = new List<(EnemySlotId, CardDropZone)>();
            for (int i = 0; i < enemySlotsTop.Length; i++)
                if (enemySlotsTop[i] != null && GetCardInSlot(enemySlotsTop[i]) == null)
                    result.Add(((EnemySlotId)i, enemySlotsTop[i]));
            for (int i = 0; i < enemySlotsBot.Length; i++)
                if (enemySlotsBot[i] != null && GetCardInSlot(enemySlotsBot[i]) == null)
                    result.Add(((EnemySlotId)(i + 3), enemySlotsBot[i]));
            return result;
        }


        private CardBattle GetCardInSlot(CardDropZone slot)
        {
            if (slot == null) return null;
            return slot.GetComponentInChildren<CardBattle>();
        }

        private int GetIndexInRow(CardDropZone slot, CardDropZone[] row)
        {
            for (int i = 0; i < row.Length; i++)
                if (row[i] == slot) return i;
            return -1;
        }

        private bool IsCardInTopRow(CardBattle card)
        {
            if (card.transform.parent == null) return true;
            CardDropZone slot = card.transform.parent.GetComponent<CardDropZone>();
            if (slot == null) return true;
            return GetIndexInRow(slot, playerSlotsTop) >= 0
                || GetIndexInRow(slot, enemySlotsTop) >= 0;
        }

        private CardDropZone[] FindRowOfCard(CardBattle card)
        {
            if (card.transform.parent == null) return null;
            CardDropZone slot = card.transform.parent.GetComponent<CardDropZone>();
            if (slot == null) return null;

            if (GetIndexInRow(slot, playerSlotsTop) >= 0) return playerSlotsTop;
            if (GetIndexInRow(slot, playerSlotsBot) >= 0) return playerSlotsBot;
            if (GetIndexInRow(slot, enemySlotsTop) >= 0) return enemySlotsTop;
            if (GetIndexInRow(slot, enemySlotsBot) >= 0) return enemySlotsBot;
            return null;
        }
    }
}
