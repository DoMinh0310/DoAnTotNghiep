using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using ProjectM.Skills;
using ProjectM.Managers;

namespace ProjectM.Inventory
{
    /// <summary>
    /// Gắn vào mỗi slot Relic trong Inventory.
    /// Dùng cho cả:
    ///   - Sidebar pool (championIndex = -1) : hiển thị relic sở hữu
    ///   - Champion equipped slot (championIndex >= 0) : relic đang trang bị cho tướng đó
    ///
    /// UNITY SETUP: Prefab cần có:
    ///   [Root] RelicSlotUI + CanvasGroup + Image (background slot)
    ///     ├── RelicIcon        : Image (ảnh relic)
    ///     ├── GlowBorder       : Image (ring viền phát sáng, tắt mặc định)
    ///     ├── EquippedBadge    : Image/Text (hiện nếu đang được equip, chỉ dùng trên sidebar)
    ///     ├── EmptyIcon        : Image (icon "trống", hiện khi không có relic)
    ///     └── RelicNameText    : TMP_Text (tên relic, có thể null)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class RelicSlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler
    {
        // ── Data ──────────────────────────────────────────────────────────
        public RelicData  currentRelic;
        /// <summary>-1 = sidebar pool; >= 0 = index tướng trong ChampionSetup.champions</summary>
        public int        championIndex = -1;

        // ── UI References ────────────────────────────────────────────────
        [Header("UI References")]
        public Image      relicIconImage;
        public Image      glowBorderImage;
        public Image      equippedBadgeImage;   // chỉ hiện trên sidebar khi relic đang được equip
        public GameObject emptyIconObj;
        public TMP_Text   relicNameText;

        // ── Tooltip (optional) ────────────────────────────────────────────
        [Header("Tooltip (Optional)")]
        public GameObject tooltipPanel;
        public TMP_Text   tooltipText;

        // ── Private state ─────────────────────────────────────────────────
        private CanvasGroup   _canvasGroup;
        private Canvas        _rootCanvas;
        private RectTransform _rootRect;
        private Tweener       _glowTween;

        private GameObject    _ghost;           // ghost image theo con trỏ khi drag
        private Vector3       _originalTooltipLocalPos = Vector3.zero;

        // ── Static drag state ─────────────────────────────────────────────
        public static RelicSlotUI Dragging { get; private set; }

        // ══════════════════════════════════════════════════════════════════
        private void Awake()
        {
            if (tooltipPanel != null) _originalTooltipLocalPos = tooltipPanel.transform.localPosition;

            // Tự thêm CanvasGroup nếu chưa có (RequireComponent backup)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _rootCanvas  = GetComponentInParent<Canvas>();
            _rootRect    = _rootCanvas?.GetComponent<RectTransform>();

            // Xóa background nếu có (nếu RelicIcon là root thì nó tự xóa viền trắng)
            var allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img == relicIconImage) continue;
                if (img == glowBorderImage) continue;
                if (tooltipPanel != null && (img.gameObject == tooltipPanel || img.transform.IsChildOf(tooltipPanel.transform))) continue; // BỎ QUA TOOLTIP ĐỂ KHÔNG BỊ TÀNG HÌNH NỀN
                img.color = Color.clear;
            }

            // Auto-find theo tên nếu chưa kéo vào Inspector
            if (relicIconImage == null)
            {
                var t = transform.Find("ItemImage");
                if (t != null)
                {
                    relicIconImage = t.GetComponent<Image>();
                }
                else
                {
                    // Tự động tạo child ItemImage nếu prefab thiếu, KHÔNG dùng root Image (tránh đè background)
                    var iconGo = new GameObject("ItemImage");
                    iconGo.transform.SetParent(transform, false);
                    iconGo.transform.SetAsFirstSibling(); // Đưa xuống dưới cùng layer để text đè lên trên
                    relicIconImage = iconGo.AddComponent<Image>();
                    var rt = relicIconImage.GetComponent<RectTransform>();
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

        // ══════════════════════════════════════════════════════════════════
        // SETUP
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Khởi tạo slot với dữ liệu.</summary>
        public void Setup(RelicData relic, int champIndex)
        {
            currentRelic   = relic;
            championIndex  = champIndex;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            bool hasRelic = currentRelic != null;

            // Slot của tướng (championIndex >= 0) LUÔN hiển thị — kể cả khi trống,
            // vì nó cần là drop target cho relic từ sidebar.
            // Sidebar slot (championIndex = -1) mới ẩn khi rỗng.
            if (!hasRelic && emptyIconObj == null && championIndex < 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (emptyIconObj != null) emptyIconObj.SetActive(!hasRelic);

            if (relicIconImage != null)
            {
                bool showIcon = hasRelic && currentRelic.icon != null;
                relicIconImage.sprite = showIcon ? currentRelic.icon : null;
                // Slot tướng rỗng: hiện nền mờ nhạt thay vì trong suốt hoàn toàn
                relicIconImage.color  = showIcon  ? Color.white
                                      : (championIndex >= 0 ? new Color(1f, 1f, 1f, 0.15f)
                                                            : Color.clear);
            }

            if (relicNameText != null) relicNameText.text = hasRelic ? currentRelic.cycleSpeed.ToString() : "";

            if (equippedBadgeImage != null)
            {
                bool showBadge = false;
                if (championIndex == -1 && hasRelic)
                {
                    var setup = SkillHandManager.Instance?.championSetup;
                    if (setup != null)
                        showBadge = setup.champions.Exists(c => c.equippedRelic == currentRelic);
                }
                equippedBadgeImage.gameObject.SetActive(showBadge);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // GLOW
        // ══════════════════════════════════════════════════════════════════

        public void ShowGlow(bool show)
        {
            // Bật/tắt object chứa UIEffect của bạn
            if (glowBorderImage != null)
            {
                glowBorderImage.gameObject.SetActive(show);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DRAG
        // ══════════════════════════════════════════════════════════════════

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Không cho drag khi đang combat hoặc không có relic
            if (IsCombatLocked() || currentRelic == null) return;

            Dragging = this;
            InventoryManager.Instance?.OnRelicDragStart(this);

            // Ghost image: theo con trỏ
            _ghost = new GameObject("RelicGhost");
            _ghost.transform.SetParent(_rootCanvas.transform, false);
            _ghost.transform.SetAsLastSibling();

            var img = _ghost.AddComponent<Image>();
            img.sprite        = relicIconImage?.sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;

            var rt = _ghost.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72f, 72f);
            MoveGhostTo(eventData.position);

            // Mờ slot gốc trong lúc drag
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
            InventoryManager.Instance?.OnRelicDragEnd();
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
            InventoryManager.Instance?.HandleRelicDrop(Dragging, this);
        }

        // ══════════════════════════════════════════════════════════════════
        // CLICK — Right-click để tháo Relic khỏi tướng
        // ══════════════════════════════════════════════════════════════════

        public void OnPointerClick(PointerEventData eventData)
        {
            // Chỉ xử lý right-click trên slot của tướng (không phải sidebar)
            if (eventData.button != PointerEventData.InputButton.Right) return;
            if (championIndex < 0 || currentRelic == null) return;
            if (IsCombatLocked()) return;

            InventoryManager.Instance?.HandleRelicUnequip(this);
        }

        // ══════════════════════════════════════════════════════════════════
        // HOVER (highlight khi drag đang diễn ra)
        // ══════════════════════════════════════════════════════════════════

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Dragging != null && Dragging != this)
            {
                // Pulse mạnh hơn khi hover
                _glowTween?.Kill();
                if (glowBorderImage != null)
                {
                    glowBorderImage.gameObject.SetActive(true);
                    glowBorderImage.color = new Color(1f, 1f, 0.3f, 1f);
                    _glowTween = glowBorderImage
                        .DOColor(new Color(1f, 0.7f, 0f, 1f), 0.2f)
                        .SetLoops(-1, LoopType.Yoyo);
                }
                transform.DOScale(1.08f, 0.15f);
            }

            if (currentRelic == null || tooltipPanel == null) return;
            
            // Nếu tooltip đang bị quăng đi đâu đó, kéo nó về để sửa position
            if (tooltipPanel.transform.parent != this.transform)
                tooltipPanel.transform.SetParent(this.transform, false);

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
                // Sidebar (championIndex < 0) giữ nguyên scale chuẩn
                if (championIndex >= 0)
                {
                    tooltipPanel.transform.localScale *= 1.5f;
                }
            }

            tooltipPanel.SetActive(true);

            if (tooltipText != null)
                tooltipText.text = $"<b>{currentRelic.relicName}</b>\n{currentRelic.description}";
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Dragging != null && Dragging != this)
            {
                transform.DOScale(1f, 0.1f);
                // Trả về glow bình thường
                ShowGlow(true);
            }

            if (tooltipPanel != null) 
            {
                tooltipPanel.SetActive(false);
                // Trả về chỗ cũ để dọn dẹp
                tooltipPanel.transform.SetParent(this.transform, true);
                // Reset lại đúng bằng 1 để không bị cộng dồn ở lần bật sau
                tooltipPanel.transform.localScale = Vector3.one;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════

        private static bool IsCombatLocked()
        {
            var bm = BattleManager.Instance;
            if (bm == null) return false; // Không ở BattleScene = tự do edit
            return !bm.IsPreparationPhase; // Đang combat = khoá
        }
    }
}
