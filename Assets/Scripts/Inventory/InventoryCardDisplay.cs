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

        /// <summary>Hiển thị tướng với chỉ số đã cộng bonus từ Smith Event và skill thêm từ Trinket/Relic.</summary>
        public void InitChampion(CardData data, int atkBonus, int hpBonus, string extraAbilities = "")
        {
            if (_display == null) _display = GetComponent<CardDisplay>();
            _display.LoadData(data);

            // Override chỉ số nếu có bonus
            if (atkBonus != 0 && _display.attackText != null)
                _display.attackText.text = (data.attack + atkBonus).ToString();
            if (hpBonus != 0 && _display.healthText != null)
                _display.healthText.text = (data.health + hpBonus).ToString();

            // Append thêm text của Trinket/Relic (nếu có)
            if (!string.IsNullOrEmpty(extraAbilities) && _display.abilitiesText != null)
            {
                // Thêm màu hoặc format tùy ý, ở đây thêm màu xanh lục nhạt để phân biệt
                _display.abilitiesText.text += "\n\n<color=#A0FFA0>" + extraAbilities + "</color>";
            }
        }

        public void InitSkill(SkillData data)
        {
            if (_display == null) _display = GetComponent<CardDisplay>();
            _display.LoadSkillData(data);
        }
    }
}
