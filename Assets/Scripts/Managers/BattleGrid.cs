using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using DG.Tweening;

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

        public bool IsEnemyZone(CardDropZone slot)
        {
            foreach (var s in enemySlotsTop) if (s == slot) return true;
            foreach (var s in enemySlotsBot) if (s == slot) return true;
            return false;
        }

        public bool IsPlayerZone(CardDropZone slot)
        {
            foreach (var s in playerSlotsTop) if (s == slot) return true;
            foreach (var s in playerSlotsBot) if (s == slot) return true;
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
            int currentChampionCount = 0;
            foreach (var card in GetAllPlayerCards())
            {
                if (card.Data != null && card.Data.cardType == Cards.CardType.Champion)
                    currentChampionCount++;
            }

            // Nếu thẻ đang di chuyển LÀ MỘT TƯỚNG (đã đăng ký), thì trừ đi 1 để không tự đếm chính nó
            bool isMovingExistingChampion = movingCard != null && movingCard.Data != null && movingCard.Data.cardType == Cards.CardType.Champion;
            if (isMovingExistingChampion) currentChampionCount--;

            // Kiểm tra xem thẻ SẮP ĐƯỢC ĐẶT XUỐNG có phải là Champion hay không.
            bool isAttemptingToPlaceChampion = true; // Mặc định là true nếu không rõ
            if (movingCard != null)
            {
                if (movingCard.Data != null) 
                    isAttemptingToPlaceChampion = movingCard.Data.cardType == Cards.CardType.Champion;
                else
                {
                    // Lấy dữ liệu tạm từ CardDisplay (vì thẻ chưa Register nên Data chưa được gán)
                    Cards.CardDisplay disp = movingCard.GetComponentInChildren<Cards.CardDisplay>(true);
                    if (disp != null && disp.cardData != null)
                        isAttemptingToPlaceChampion = disp.cardData.cardType == Cards.CardType.Champion;
                }
            }

            // Chỉ block nếu đang cố đặt thêm một TƯỚNG, còn Building thì thả ga
            if (currentChampionCount >= 4 && isAttemptingToPlaceChampion)
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

            int atkBonus = 0;
            int hpBonus = 0;
            if (isPlayer && GameManager.Instance?.RunData != null)
            {
                var bonus = GameManager.Instance.RunData.GetChampionBonus(display.cardData.name);
                atkBonus = bonus.attackBonus;
                hpBonus = bonus.healthBonus;
            }

            card.Initialize(display.cardData, isPlayer, atkBonus, hpBonus);
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
                // Tướng của người chơi luôn đánh thằng đứng đầu
                target = GetFrontCard(isTop ? enemySlotsTop : enemySlotsBot)
                      ?? GetFrontCard(isTop ? enemySlotsBot : enemySlotsTop);
            }
            else
            {
                // Nếu quái có nội tại đánh random
                if (attacker.Data != null && attacker.Data.attacksRandomTarget)
                {
                    target = GetRandomCard(isTop ? playerSlotsTop : playerSlotsBot)
                          ?? GetRandomCard(isTop ? playerSlotsBot : playerSlotsTop);
                }
                else
                {
                    // Quái bình thường đánh mục tiêu đứng đầu
                    target = GetFrontCard(isTop ? playerSlotsTop : playerSlotsBot)
                          ?? GetFrontCard(isTop ? playerSlotsBot : playerSlotsTop);
                }
            }

            if (target != null)
                BattleDebugger.Log($"🗡️ {attacker.Data?.cardName} → {target.Data?.cardName}");
            else
                BattleDebugger.Warn($"{attacker.Data?.cardName} không tìm thấy mục tiêu!");

            return target;
        }

        // Lấy ngẫu nhiên 1 thẻ còn sống trong hàng
        private CardBattle GetRandomCard(CardDropZone[] row)
        {
            List<CardBattle> aliveCards = new List<CardBattle>();
            foreach (var slot in row)
            {
                CardBattle c = GetCardInSlot(slot);
                if (c != null && !c.IsDead) aliveCards.Add(c);
            }
            if (aliveCards.Count > 0)
            {
                return aliveCards[UnityEngine.Random.Range(0, aliveCards.Count)];
            }
            return null;
        }

        // Lấy thẻ gần TT nhất trong hàng (slot[0] là Front của cả 2 phe)
        private CardBattle GetFrontCard(CardDropZone[] row)
        {
            foreach (var slot in row)
            {
                CardBattle c = GetCardInSlot(slot);
                if (c != null && !c.IsDead) return c;
            }
            return null;
        }

        /// <summary>
        /// Tìm đồng minh đứng ngay phía trước của thẻ này trong cùng hàng.
        /// Nếu thẻ đang đứng đầu hàng, trả về chính nó.
        /// </summary>
        public CardBattle GetFrontAllyInRow(CardBattle card)
        {
            CardDropZone[] row = FindRowOfCard(card);
            if (row == null) return null;
            int idx = -1;
            for (int i = 0; i < row.Length; i++)
            {
                if (GetCardInSlot(row[i]) == card) { idx = i; break; }
            }
            
            // Front là index 0, đằng trước là index < idx
            if (idx > 0)
            {
                for (int i = idx - 1; i >= 0; i--)
                {
                    CardBattle c = GetCardInSlot(row[i]);
                    if (c != null && !c.IsDead) return c;
                }
            }
            
            return card;
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
            // Tìm hàng có chứa slot này
            if (GetIndexInRow(slot, playerSlotsTop) >= 0) { ShiftRow(playerSlotsTop); return; }
            if (GetIndexInRow(slot, playerSlotsBot) >= 0) { ShiftRow(playerSlotsBot); return; }
            if (GetIndexInRow(slot, enemySlotsTop) >= 0) { ShiftRow(enemySlotsTop); return; }
            if (GetIndexInRow(slot, enemySlotsBot) >= 0) { ShiftRow(enemySlotsBot); }
        }

        private void ShiftRow(CardDropZone[] row)
        {
            // Cả Player và Enemy đều dồn về Front = index 0
            for (int i = 0; i < row.Length - 1; i++)
            {
                if (GetCardInSlot(row[i]) == null)
                {
                    for (int j = i + 1; j < row.Length; j++)
                    {
                        CardBattle next = GetCardInSlot(row[j]);
                        if (next != null)
                        {
                            MoveCardToSlot(next, row[i]);
                            break;
                        }
                    }
                }
            }
        }

        private void MoveCardToSlot(CardBattle card, CardDropZone newSlot)
        {
            CardDragHandler drag = card.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.SetNewParent(newSlot.transform);
            }
            else
            {
                Vector3 worldPos = card.transform.position;
                card.transform.SetParent(newSlot.transform);

                RectTransform cardRect = card.GetComponent<RectTransform>();
                RectTransform slotRect = newSlot.GetComponent<RectTransform>();
                if (cardRect != null && slotRect != null)
                {
                    card.transform.DOKill();
                    card.transform.position = worldPos;

                    float scaleFactor = slotRect.rect.width > 0 && cardRect.rect.width > 0 
                        ? slotRect.rect.width / cardRect.rect.width : 1f;
                    Vector3 newScale = new Vector3(scaleFactor, scaleFactor, 1f);

                    cardRect.DOAnchorPos(Vector2.zero, 0.2f).SetEase(DG.Tweening.Ease.OutQuad);
                    card.transform.DOScale(newScale, 0.2f).SetEase(DG.Tweening.Ease.OutQuad);
                    card.transform.DOLocalRotateQuaternion(Quaternion.identity, 0.2f).SetEase(DG.Tweening.Ease.OutQuad);
                }
            }

            Debug.Log($"[BattleGrid] Dồn {card.Data?.cardName ?? card.name} → {newSlot.name}");
        }

        // ══════════════════════════════════════════
        // REORDERING (ĐẢO VỊ TRÍ TRONG CÙNG HÀNG)
        // ══════════════════════════════════════════

        /// <summary>
        /// Di chuyển một thẻ đến slot mới trong cùng hàng, các thẻ khác tự động đẩy (shift) ra sau/trước.
        /// Trả về true nếu thành công (do cùng hàng), false nếu khác hàng (sẽ dùng Swap thay thế).
        /// </summary>
        public bool TryReorderInSameRow(CardDropZone sourceSlot, CardDropZone targetSlot, CardBattle movingCard)
        {
            CardDropZone[] row = GetPlayerRowOf(targetSlot);
            if (row == null) return false;

            int sourceIdx = GetIndexInRow(sourceSlot, row);
            int targetIdx = GetIndexInRow(targetSlot, row);

            // Nếu không cùng nằm trong 1 hàng thì không dùng Shift, trả về false để CardDropZone dùng Swap
            if (sourceIdx < 0 || targetIdx < 0) return false;

            // Xây dựng lại danh sách thẻ trong hàng CHƯA tính thẻ đang bị nhấc lên
            CardBattle[] currentCards = new CardBattle[row.Length];
            for (int i = 0; i < row.Length; i++)
            {
                if (i == sourceIdx) currentCards[i] = movingCard; // Khôi phục thẻ đang nhấc về slot cũ
                else currentCards[i] = GetCardInSlot(row[i]);
            }

            var list = new System.Collections.Generic.List<CardBattle>(currentCards);

            // Rút thẻ đang nhấc ra khỏi vị trí cũ
            list.RemoveAt(sourceIdx);
            // Chèn vào vị trí mới
            list.Insert(targetIdx, movingCard);

            // Gán lại Parent cho tất cả thẻ trong hàng theo danh sách mới
            for (int i = 0; i < row.Length; i++)
            {
                if (list[i] != null)
                {
                    CardDragHandler drag = list[i].GetComponent<CardDragHandler>();
                    if (drag != null)
                    {
                        drag.SetNewParent(row[i].transform);
                    }
                    else
                    {
                        list[i].transform.SetParent(row[i].transform);
                        RectTransform cardRect = list[i].GetComponent<RectTransform>();
                        if (cardRect != null)
                        {
                            list[i].transform.localRotation = Quaternion.identity;
                            cardRect.anchoredPosition = Vector2.zero;
                            // Để an toàn không lưu đè scale bị béo phì nếu không có CardDragHandler
                        }
                    }
                }
            }

            BattleDebugger.Log($"🔄 [BattleGrid] Đảo vị trí trong hàng: {movingCard?.Data?.cardName} chuyển đến {targetSlot.name}");
            return true;
        }

        // ══════════════════════════════════════════
        // GET ALL CARDS (BattleManager dùng để tick)
        // ══════════════════════════════════════════

        /// <summary>
        /// Lấy tất cả thẻ bài trên bàn đấu theo thứ tự đánh do thiết kế quy định (Địch ưu tiên đánh trước).
        /// Thứ tự: 4, 3, 5, 2, 6, 1, 10, 9, 11, 8, 12, 7
        /// </summary>
        public List<CardBattle> GetAllCardsInAttackOrder()
        {
            var attackOrder = new int[] { 4, 3, 5, 2, 6, 1, 10, 9, 11, 8, 12, 7 };
            var list = new List<CardBattle>();
            
            var allSlots = new List<CardDropZone>();
            if (playerSlotsTop != null) allSlots.AddRange(playerSlotsTop);
            if (playerSlotsBot != null) allSlots.AddRange(playerSlotsBot);
            if (enemySlotsTop != null) allSlots.AddRange(enemySlotsTop);
            if (enemySlotsBot != null) allSlots.AddRange(enemySlotsBot);

            foreach (int slotNum in attackOrder)
            {
                // Tìm slot có tên kết thúc bằng số tương ứng (VD: Slot_Card_3)
                CardDropZone slot = allSlots.Find(s => s != null && s.name.EndsWith($"_{slotNum}"));
                if (slot != null)
                {
                    CardBattle c = GetCardInSlot(slot);
                    if (c != null && !c.IsDead) list.Add(c);
                }
            }
            return list;
        }

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


        public CardBattle GetCardInSlot(CardDropZone slot)
        {
            if (slot == null) return null;
            if (slot.transform.childCount == 0) return null;

            foreach (var card in slot.GetComponentsInChildren<CardBattle>())
            {
                if (card != null && !card.IsDead)
                    return card;
            }
            return null;
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
