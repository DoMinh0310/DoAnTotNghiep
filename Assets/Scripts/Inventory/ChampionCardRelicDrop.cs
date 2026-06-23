using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectM.Inventory
{
    /// <summary>
    /// Gắn vào root của Card_Prefab khi hiển thị trong Inventory.
    /// Biến toàn bộ thẻ tướng thành vùng drop target cho Relic —
    /// người dùng chỉ cần thả relic vào BẤT KỲ chỗ nào trên thẻ,
    /// hệ thống sẽ tự tìm RelicSlotUI bên trong và thực hiện swap.
    /// </summary>
    public class ChampionCardRelicDrop : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            var dragging = RelicSlotUI.Dragging;
            if (dragging == null) return;

            // Tìm RelicSlotUI của chính thẻ này (bao gồm object đang ẩn)
            var targetSlot = GetComponentInChildren<RelicSlotUI>(true);
            if (targetSlot == null) return;

            // Không drop lên chính mình
            if (targetSlot == dragging) return;

            InventoryManager.Instance?.HandleRelicDrop(dragging, targetSlot);
        }
    }
}
