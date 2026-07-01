using UnityEngine;
using System.Collections;
using ProjectM.Cards;
using ProjectM.Managers;

namespace ProjectM.Skills
{
    [CreateAssetMenu(fileName = "SkillOverride_Summon", menuName = "Project M/Skills/Overrides/Summon")]
    public class SkillOverride_Summon : SkillOverrideBase
    {
        [Header("Summon Settings")]
        [Tooltip("CardData của công trình muốn gọi ra (vd: HP_generator)")]
        public CardData buildingData;
        
        [Tooltip("Prefab của công trình (Kéo file Building_Prefab vào đây)")]
        public GameObject buildingPrefab;

        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (buildingData == null || buildingPrefab == null)
            {
                Debug.LogWarning("[SkillOverride_Summon] Chưa gán CardData hoặc Prefab!");
                yield break;
            }

            BattleGrid grid = FindAnyObjectByType<BattleGrid>();
            if (grid == null) yield break;

            CardDropZone emptySlot = null;

            // 1. Ưu tiên lấy ô mà người chơi thả thẻ vào (từ Drag-and-Drop)
            if (SkillDragHandler.targetDropZoneForSummon != null)
            {
                emptySlot = SkillDragHandler.targetDropZoneForSummon;
                SkillDragHandler.targetDropZoneForSummon = null; // Reset sau khi dùng
            }
            else
            {
                // 2. Fallback: Nếu không dùng Drag-and-Drop mà click thì tự động tìm ô trống đầu tiên
                emptySlot = FindEmptySlot(grid.playerSlotsBot) ?? FindEmptySlot(grid.playerSlotsTop);
            }

            if (emptySlot == null)
            {
                Debug.LogWarning("[SkillOverride_Summon] Hết chỗ trống trên bàn cờ! Thẻ bài không có tác dụng.");
                yield break;
            }

            // Sinh ra Prefab
            GameObject newObj = Instantiate(buildingPrefab, emptySlot.transform);
            
            // ── Bổ sung logic chỉnh lại tỷ lệ, toạ độ, góc xoay cho thẻ Building ──
            RectTransform slotRect = emptySlot.GetComponent<RectTransform>();
            RectTransform cardRect = newObj.GetComponent<RectTransform>();
            if (slotRect != null && cardRect != null)
            {
                float scaleFactor = slotRect.rect.width / cardRect.rect.width;
                newObj.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
                newObj.transform.localRotation = Quaternion.identity;
                cardRect.anchoredPosition = Vector2.zero;
            }

            // Lấy CardDisplay và nhét data vào 
            CardDisplay display = newObj.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.cardData = buildingData;
                // Force load nếu Display có LoadData/Init UI
                // Do prefab này đã được gán sẵn CardBattle nên nó sẽ tự lấy Data qua GetComponent.
            }

            CardBattle newCard = newObj.GetComponent<CardBattle>();
            if (newCard != null)
            {
                // Đăng ký vào grid (với tư cách là Player Card)
                grid.RegisterCard(newCard, true);
            }

            Debug.Log($"[SkillOverride_Summon] Gọi thành công {buildingData.cardName} ra bàn cờ!");
            
            // Tạo một chút độ trễ cho mượt
            yield return new WaitForSeconds(0.3f);
        }

        private CardDropZone FindEmptySlot(CardDropZone[] row)
        {
            foreach (var slot in row)
            {
                if (slot != null && slot.transform.childCount == 0) // Kiểm tra xem slot có GameObject con nào chưa
                {
                    // Chắc chắn slot này chưa có CardBattle nào đăng ký
                    BattleGrid grid = FindAnyObjectByType<BattleGrid>();
                    if (grid != null && grid.GetCardInSlot(slot) == null)
                    {
                        return slot;
                    }
                }
            }
            return null;
        }
    }
}
