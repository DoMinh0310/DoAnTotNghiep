using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectM.Inventory
{
    /// <summary>
    /// Gắn vào container RelicSideBar.
    /// Cho phép kéo Relic từ slot tướng thả vào vùng sidebar trống → tháo Relic.
    /// Khi thả lên một RelicSlotUI cụ thể, Unity gọi RelicSlotUI.OnDrop trước (ưu tiên cao hơn).
    /// Script này chỉ bắt những drop rơi vào vùng trống của sidebar (không trúng slot nào).
    /// </summary>
    public class RelicSidebarDropZone : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            var source = RelicSlotUI.Dragging;
            if (source == null) return;

            // Chỉ xử lý khi kéo từ slot tướng (không phải từ sidebar)
            if (source.championIndex < 0) return;

            // Tháo relic khỏi tướng → tự động quay về sidebar pool sau khi PopulateInventory()
            InventoryManager.Instance?.HandleRelicUnequip(source);
        }
    }
}
