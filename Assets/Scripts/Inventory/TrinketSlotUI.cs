using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ProjectM.Skills;

namespace ProjectM.Inventory
{
    /// <summary>
    /// Hiển thị 1 Trinket trong Inventory (read-only — chỉ đổi được tại Shop).
    ///
    /// UNITY SETUP: Prefab cần có:
    ///   [Root] TrinketSlotUI + Image (background)
    ///     ├── TrinketIcon     : Image
    ///     ├── EmptyIcon       : Image/GameObject (khi không có trinket)
    ///     └── TrinketNameText : TMP_Text (tên trinket)
    /// </summary>
    public class TrinketSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ── Data ──────────────────────────────────────────────────────────
        public TrinketData currentTrinket;
        /// <summary>-1 = sidebar pool; >= 0 = index tướng trong ChampionSetup.champions</summary>
        public int         championIndex = -1;

        // ── UI References ────────────────────────────────────────────────
        [Header("UI References")]
        public Image      trinketIconImage;
        public Image      glowBorderImage;   // GlowBorder child — tương tự RelicSlotUI
        public GameObject emptyIconObj;
        public TMP_Text   trinketNameText;

        // ── Tooltip (optional) ────────────────────────────────────────────
        [Header("Tooltip (Optional)")]
        public GameObject tooltipPanel;
        public TMP_Text   tooltipText;

        // ══════════════════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Auto-find theo tên nếu chưa kéo vào Inspector
            if (trinketIconImage == null)
            {
                var t = transform.Find("ItemImage");
                if (t != null) trinketIconImage = t.GetComponent<Image>();
                else           trinketIconImage = GetComponent<Image>(); // fallback: dùng root Image
            }
            if (glowBorderImage == null)
            {
                var t = transform.Find("GlowBorder");
                if (t != null) glowBorderImage = t.GetComponent<Image>();
            }

            // Ẩn GlowBorder mặc định — chỉ bật khi kéo trong Inventory
            if (glowBorderImage != null) glowBorderImage.gameObject.SetActive(false);

            RefreshDisplay();
        }

        public void Setup(TrinketData trinket, int champIndex)
        {
            currentTrinket = trinket;
            championIndex  = champIndex;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            bool hasTrinket = currentTrinket != null;

            // Nếu không có trinket VÀ không có icon "ô trống" → ẩn toàn bộ slot
            if (!hasTrinket && emptyIconObj == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (emptyIconObj != null) emptyIconObj.SetActive(!hasTrinket);

            // Dùng color thay vì SetActive để tránh ẩn cả slot khi root Image là icon
            if (trinketIconImage != null)
            {
                bool showIcon = hasTrinket && currentTrinket.icon != null;
                trinketIconImage.sprite = showIcon ? currentTrinket.icon : null;
                trinketIconImage.color  = showIcon ? Color.white : Color.clear;
            }

            if (trinketNameText != null) trinketNameText.text = "";

            if (tooltipPanel != null) tooltipPanel.SetActive(false);

            // Cập nhật mô tả nội tại trên thẻ tướng
            var cardDisplay = GetComponentInParent<ProjectM.Cards.CardDisplay>();
            if (cardDisplay != null)
                cardDisplay.UpdateTrinketDescription(currentTrinket);
        }

        // ══════════════════════════════════════════════════════════════════
        // GLOW — Giống RelicSlotUI
        // ══════════════════════════════════════════════════════════════════

        public void ShowGlow(bool show)
        {
            if (glowBorderImage != null)
                glowBorderImage.gameObject.SetActive(show);
        }

        // ══════════════════════════════════════════════════════════════════
        // HOVER — Hiển thị tooltip mô tả
        // ══════════════════════════════════════════════════════════════════

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentTrinket == null || tooltipPanel == null) return;
            tooltipPanel.SetActive(true);
            if (tooltipText != null)
                tooltipText.text = $"<b>{currentTrinket.trinketName}</b>\n{currentTrinket.description}";
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }
    }
}
