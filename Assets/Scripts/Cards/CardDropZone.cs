using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectM.Cards
{
    public class CardDropZone : MonoBehaviour, IDropHandler
    {
        [Header("Zone Settings")]
        [Tooltip("Tick nếu đây là ô của người chơi (cho phép thả bài vào). Bỏ tick = ô của địch (chặn thả bài).")]
        public bool isPlayerZone = true;

        [Tooltip("Số lượng bài tối đa được phép đặt vào ô này (thường là 1)")]
        public int capacity = 1;

        [Header("Enemy Spawn")]
        [Tooltip("Kéo Prefab thẻ bài địch vào đây để tự động spawn khi game bắt đầu.")]
        public GameObject enemyCardPrefab;

        [Tooltip("Kéo CardData của thẻ địch vào đây.")]
        public CardData enemyCardData;

        private void Start()
        {
            // Nếu là ô địch và được cấu hình sẵn prefab + data -> tự động spawn
            if (!isPlayerZone && enemyCardPrefab != null && enemyCardData != null)
            {
                SpawnEnemyCard(enemyCardPrefab, enemyCardData);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Không cho phép thực hiện thao tác thả bài nếu đang bị block input (đang trong combat)
            if (Managers.BattleManager.IsInputBlocked) return;

            if (!isPlayerZone)
            {
                Debug.Log("[CardDropZone] Ô này là của địch, không thể đặt bài vào!");
                return;
            }

            if (eventData.pointerDrag == null) return;

            // Không cho phép đặt Skill vào ô của tướng
            if (eventData.pointerDrag.GetComponent<Skills.SkillDragHandler>() != null) return;

            CardDragHandler draggableCard = eventData.pointerDrag.GetComponent<CardDragHandler>();
            // Phải có CardDragHandler và nó phải đang được bật (enabled)
            if (draggableCard == null || !draggableCard.enabled) return;

            // --- KIỂM TRA TÍNH NĂNG HOÁN ĐỔI VỊ TRÍ (SWAP / REORDER) ---
            bool isFromBoard = draggableCard.previousParent != null && draggableCard.previousParent.GetComponent<CardDropZone>() != null;
            if (isFromBoard)
            {
                Transform oldZone = draggableCard.previousParent;
                Managers.BattleGrid gridInstance = Managers.BattleGrid.Instance;
                CardBattle movingCard = draggableCard.GetComponent<CardBattle>();

                // Thử Reorder trong cùng hàng trước, dù slot đích có trống hay không
                if (gridInstance != null && gridInstance.TryReorderInSameRow(oldZone.GetComponent<CardDropZone>(), this, movingCard))
                {
                    return; // Xử lý đẩy hàng xong, ngắt
                }
                
                // Nếu khác hàng (TryReorderInSameRow trả về false) VÀ slot đích có bài, ta thực hiện Swap (đổi chỗ)
                if (transform.childCount > 0)
                {
                    CardDragHandler targetCard = transform.GetChild(0).GetComponent<CardDragHandler>();
                    if (targetCard != null && targetCard != draggableCard)
                    {
                        draggableCard.SetNewParent(this.transform);
                        targetCard.SetNewParent(oldZone);
                        
                        Debug.Log($"[CardDropZone] Đã Swap vị trí của {draggableCard.name} và {targetCard.name}");
                        return; // Hoàn tất Swap
                    }
                }
            }
            // ------------------------------------------------

            Managers.BattleGrid grid = Managers.BattleGrid.Instance;

            // Truyền thẻ đang được kéo vào để BattleGrid không tính nó vào giới hạn 4 tướng khi di chuyển
            CardBattle battle = draggableCard.GetComponent<CardBattle>();
            CardDropZone bestSlot = grid != null ? grid.GetBestAvailableSlotInRow(this, battle) : null;

            if (bestSlot == null)
            {
                Debug.Log($"[CardDropZone] Hàng này đã đầy, không thể đặt thêm!");
                return;
            }

            // Đặt bài vào slot tốt nhất (có thể khác slot người chơi thả vào)
            draggableCard.SetNewParent(bestSlot.transform);

            // Chỉ đăng ký (Initialize) khi thẻ chưa từng được khởi tạo (tức là lần đầu tiên đặt từ tay xuống sàn)
            // Nếu battle.Data != null thì thẻ đến từ một slot khác trên sàn — không reset HP!
            if (grid != null && battle != null && battle.Data == null)
                grid.RegisterCard(battle, true);
        }

        /// <summary>
        /// Spawn một thẻ địch vào slot này với dữ liệu cho trước.
        /// Có thể gọi từ code bên ngoài (VD: EnemyWaveManager) để điều khiển thủ công.
        /// Trả về CardBattle của thẻ vừa spawn, null nếu thất bại.
        /// </summary>
        public CardBattle SpawnEnemyCard(GameObject prefab, CardData data)
        {
            if (transform.childCount >= capacity)
            {
                Debug.LogWarning($"[CardDropZone] Slot {name} đã đầy, không spawn được!");
                return null;
            }

            GameObject newCard = Instantiate(prefab, transform);

            // Snap về trung tâm slot
            RectTransform slotRect = GetComponent<RectTransform>();
            RectTransform cardRect = newCard.GetComponent<RectTransform>();
            Vector3 finalScale = Vector3.one;
            if (slotRect != null && cardRect != null)
            {
                float scaleFactor = slotRect.rect.width / cardRect.rect.width;
                finalScale = new Vector3(scaleFactor, scaleFactor, 1f);
                newCard.transform.localScale = finalScale;
                newCard.transform.localRotation = Quaternion.identity;
                cardRect.anchoredPosition = Vector2.zero;
            }

            // Nạp dữ liệu thẻ
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.LoadData(data);
            }

            // Vô hiệu hóa kéo thả cho thẻ địch, NHƯNG GIỮ hover để người chơi xem chỉ số
            CardDragHandler drag = newCard.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.enabled = false;
            }

            // Báo cho CardHoverHandler biết trạng thái gốc để hiệu ứng hover hoạt động đúng
            CardHoverHandler hover = newCard.GetComponent<CardHoverHandler>();
            if (hover != null)
            {
                hover.UpdateBaseState(Vector2.zero, Quaternion.identity, finalScale);
            }

            // Đăng ký với BattleGrid để khởi tạo trạng thái chiến đấu (isPlayer = false)
            CardBattle battle = newCard.GetComponent<CardBattle>();
            if (battle != null)
            {
                Managers.BattleGrid grid = Managers.BattleGrid.Instance;
                if (grid != null) grid.RegisterCard(battle, false);
                else Debug.LogWarning("[CardDropZone] BattleGrid chưa tồn tại, RegisterCard bị bỏ qua.");
            }

            Debug.Log($"[CardDropZone] Đã spawn {data.cardName} vào slot {name}.");
            return battle;
        }
    }
}
