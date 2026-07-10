using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
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
    public class TrinketSlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler
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

        // ── Private state ─────────────────────────────────────────────────
        private CanvasGroup   _canvasGroup;
        private Canvas        _rootCanvas;
        private RectTransform _rootRect;
        private Tweener       _glowTween;

        private GameObject    _ghost;
        private Vector3       _originalTooltipLocalPos = Vector3.zero;

        // ── Static drag state ─────────────────────────────────────────────
        public static TrinketSlotUI Dragging { get; private set; }

        // ── Static tooltip state (chỉ có 1 tooltip hiển tại một thời điểm) ────
        private static TrinketSlotUI _activeTooltipSlot;

        // ══════════════════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (tooltipPanel != null) _originalTooltipLocalPos = tooltipPanel.transform.localPosition;

            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _rootCanvas  = GetComponentInParent<Canvas>();
            _rootRect    = _rootCanvas?.GetComponent<RectTransform>();

            // Auto-find theo tên nếu chưa kéo vào Inspector
            if (trinketIconImage == null)
            {
                var t = transform.Find("ItemImage");
                if (t != null)
                {
                    trinketIconImage = t.GetComponent<Image>();
                }
                else
                {
                    // Tự động tạo child ItemImage nếu prefab thiếu, KHÔNG dùng root Image (tránh đè background)
                    var iconGo = new GameObject("ItemImage");
                    iconGo.transform.SetParent(transform, false);
                    iconGo.transform.SetAsFirstSibling(); // Đưa xuống lớp dưới cùng để text đè lên trên
                    trinketIconImage = iconGo.AddComponent<Image>();
                    var rt = trinketIconImage.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                }
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

        private void OnDestroy()
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
                _ghost = null;
            }
            
            _glowTween?.Kill();
            transform.DOKill();

            if (Dragging == this)
                Dragging = null;
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

            // Slot của tướng (championIndex >= 0) LUÔN hiển thị — cần là drop target.
            // Sidebar slot (championIndex = -1) mới ẩn khi rỗng và không có emptyIconObj.
            if (!hasTrinket && emptyIconObj == null && championIndex < 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            // emptyIconObj chỉ hiện trên sidebar (championIndex < 0)
            // Champion card slot không dùng placeholder — giống RelicSlotUI trên champion card luôn có emptyIconObj = null
            if (emptyIconObj != null) emptyIconObj.SetActive(!hasTrinket && championIndex < 0);

            // Xóa TOÀN BỘ Image nền (kể cả child) trừ icon, glow, và TOOLTIP
            // Tránh trường hợp trinketIconImage == root Image bị clear xong lại set trắng
            var allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img == trinketIconImage) continue;        // bỏ qua icon
                if (img == glowBorderImage)  continue;        // bỏ qua glow
                if (tooltipPanel != null && (img.gameObject == tooltipPanel || img.transform.IsChildOf(tooltipPanel.transform))) continue; // BỎ QUA TOOLTIP ĐỂ KHÔNG BỊ TÀNG HÌNH NỀN
                img.color = Color.clear;                      // ẩn mọi background
            }

            if (trinketIconImage != null)
            {
                bool showIcon = hasTrinket && currentTrinket.icon != null;
                trinketIconImage.sprite = showIcon ? currentTrinket.icon : null;
                trinketIconImage.color  = showIcon  ? Color.white
                                        : (championIndex >= 0 ? new Color(1f, 1f, 1f, 0.15f)
                                                              : Color.clear);
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
        // DRAG
        // ══════════════════════════════════════════════════════════════════

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsCombatLocked() || currentTrinket == null) return;

            Dragging = this;
            InventoryManager.Instance?.OnTrinketDragStart(this);

            _ghost = new GameObject("TrinketGhost");
            _ghost.transform.SetParent(_rootCanvas.transform, false);
            _ghost.transform.SetAsLastSibling();

            var img = _ghost.AddComponent<Image>();
            img.sprite        = trinketIconImage?.sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;

            var rt = _ghost.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72f, 72f);
            MoveGhostTo(eventData.position);

            _canvasGroup.alpha = 0.4f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost == null) return;
            MoveGhostTo(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }
            _canvasGroup.alpha = 1f;

            Dragging = null;
            InventoryManager.Instance?.OnTrinketDragEnd();
        }

        private void MoveGhostTo(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootRect, screenPos, null, out var local);
            _ghost.GetComponent<RectTransform>().anchoredPosition = local;
        }

        // ══════════════════════════════════════════════════════════════════
        // DROP TARGET
        // ══════════════════════════════════════════════════════════════════

        public void OnDrop(PointerEventData eventData)
        {
            if (Dragging == null || Dragging == this) return;
            InventoryManager.Instance?.HandleTrinketDrop(Dragging, this);
        }

        // ══════════════════════════════════════════════════════════════════
        // CLICK — Right-click để tháo
        // ══════════════════════════════════════════════════════════════════

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;
            if (championIndex < 0 || currentTrinket == null) return;
            if (IsCombatLocked()) return;

            InventoryManager.Instance?.HandleTrinketUnequip(this);
        }

        // ══════════════════════════════════════════════════════════════════
        // HOVER — Hiển thị tooltip mô tả & highlight drag
        // ══════════════════════════════════════════════════════════════════

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Dragging != null && Dragging != this)
            {
                _glowTween?.Kill();
                if (glowBorderImage != null)
                {
                    glowBorderImage.gameObject.SetActive(true);
                    glowBorderImage.color = new Color(1f, 1f, 0.3f, 1f);
                    _glowTween = glowBorderImage
                        .DOColor(new Color(1f, 0.7f, 0f, 1f), 0.2f)
                        .SetLoops(-1, DG.Tweening.LoopType.Yoyo);
                }
                transform.DOScale(1.08f, 0.15f);
            }

            // Nếu đang trong quá trình Drag & Drop thì KHÔNG mở Tooltip
            if (eventData.dragging || eventData.pointerDrag != null || Dragging != null) return;

            if (currentTrinket == null || tooltipPanel == null) return;
            
            // ── Ẩn tooltip cũ (nếu có slot khác đang mở) trước khi mở cái mới ──
            if (_activeTooltipSlot != null && _activeTooltipSlot != this)
                _activeTooltipSlot.HideTooltip();

            // Tự động đảo ngược hướng hiển thị (trái/phải)
            Vector3 basePos = _originalTooltipLocalPos;
            if (championIndex < 0)
                basePos.x = -Mathf.Abs(basePos.x); // Sidebar: Ép văng sang trái
            else
                basePos.x = Mathf.Abs(basePos.x);  // Tướng: Ép văng sang phải
            
            tooltipPanel.transform.localPosition = basePos;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null && !tooltipPanel.activeSelf)
            {
                tooltipPanel.transform.SetParent(rootCanvas.rootCanvas.transform, true);
                tooltipPanel.transform.SetAsLastSibling();
                
                // CHỈ phóng to nếu đang ở trên thẻ tướng (championIndex >= 0)
                if (championIndex >= 0)
                    tooltipPanel.transform.localScale *= 1.5f;
            }

            tooltipPanel.SetActive(true);
            _activeTooltipSlot = this;

            if (tooltipText != null)
                tooltipText.text = $"<b>{currentTrinket.trinketName}</b>\n{currentTrinket.description}";
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Dragging != null && Dragging != this)
            {
                transform.DOScale(1f, 0.1f);
                ShowGlow(true);
            }

            HideTooltip();
        }

        /// <summary>Dọn tooltip và trả về vị trí gốc an toàn. Có thể gọi từ bên ngoài.</summary>
        public void HideTooltip()
        {
            if (tooltipPanel == null) return;
            tooltipPanel.SetActive(false);
            tooltipPanel.transform.SetParent(this.transform, true);
            tooltipPanel.transform.localScale = Vector3.one;
            if (_activeTooltipSlot == this)
                _activeTooltipSlot = null;
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════

        private static bool IsCombatLocked()
        {
            var bm = ProjectM.Managers.BattleManager.Instance;
            if (bm == null) return false;
            return !bm.IsPreparationPhase;
        }
    }
}
