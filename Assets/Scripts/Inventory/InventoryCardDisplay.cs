using UnityEngine;
using ProjectM.Cards;
using ProjectM.Skills;

namespace ProjectM.Inventory
{
    /// <summary>
    /// Wrapper gắn vào thẻ trong Inventory (tướng hoặc skill).
    /// Dùng để quản lý các tương tác riêng biệt trong Inventory (ví dụ click để xem chi tiết)
    /// mà không ảnh hưởng tới logic Drag-Drop của combat.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public class InventoryCardDisplay : MonoBehaviour
    {
        private CardDisplay _display;

        private void Awake()
        {
            _display = GetComponent<CardDisplay>();
            
            // Xoá component kéo thả của combat để tránh tương tác lỗi khi đang mở Inventory
            var cardDrag = GetComponent<CardDragHandler>();
            if (cardDrag != null) Destroy(cardDrag);

            var skillDrag = GetComponent<SkillDragHandler>();
            if (skillDrag != null) Destroy(skillDrag);
        }

        public void InitChampion(CardData data)
        {
            if (_display == null) _display = GetComponent<CardDisplay>();
            _display.LoadData(data);
        }

        public void InitSkill(SkillData data)
        {
            if (_display == null) _display = GetComponent<CardDisplay>();
            _display.LoadSkillData(data);
        }
    }
}
