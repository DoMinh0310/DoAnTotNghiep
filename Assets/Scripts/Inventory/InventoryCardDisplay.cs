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
    public class InventoryCardDisplay : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private CardDisplay _display;
        private CardData _champData;
        private SkillData _skillData;
        private string _extraAbilities = "";

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
            _champData = data;
            if (_display == null) _display = GetComponent<CardDisplay>();
            _display.LoadData(data);
        }

        /// <summary>Hiển thị tướng với chỉ số đã cộng bonus từ Smith Event và skill thêm từ Trinket/Relic.</summary>
        public void InitChampion(CardData data, int atkBonus, int hpBonus, string extraAbilities = "")
        {
            _champData = data;
            _extraAbilities = extraAbilities;
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
            _skillData = data;
            if (_display == null) _display = GetComponent<CardDisplay>();
            _display.LoadSkillData(data);
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (UI.TooltipManager.Instance == null) return;

            string title = "";
            string desc = "";

            if (_champData != null)
            {
                title = _champData.cardName;
                desc = _champData.abilities;
                if (!string.IsNullOrEmpty(_extraAbilities))
                {
                    desc += "\n\n<color=#A0FFA0>" + _extraAbilities + "</color>";
                }
            }
            else if (_skillData != null)
            {
                title = _skillData.skillName;
                desc = _skillData.description;
            }
            else return;

            // Xử lý giải thích nguyên tố (Keyword)
            string keywordExplanations = "";
            if (_display != null && _display.keywordDatabase != null && _display.keywordDatabase.keywords != null)
            {
                foreach (var kw in _display.keywordDatabase.keywords)
                {
                    if (string.IsNullOrEmpty(kw.explanation)) continue;

                    bool contains = kw.caseInsensitive 
                        ? desc.IndexOf(kw.keyword, System.StringComparison.OrdinalIgnoreCase) >= 0
                        : desc.Contains(kw.keyword);

                    if (contains)
                    {
                        if (!string.IsNullOrEmpty(keywordExplanations)) keywordExplanations += "\n\n";
                        string colorHex = ColorUtility.ToHtmlStringRGBA(kw.color);
                        keywordExplanations += $"<b><color=#{colorHex}>{kw.keyword}</color></b>\n{kw.explanation}";
                    }
                }
            }

            UI.TooltipManager.Instance.ShowTooltip(title, desc, keywordExplanations);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (UI.TooltipManager.Instance != null)
            {
                UI.TooltipManager.Instance.HideTooltip();
            }
        }
    }
}
