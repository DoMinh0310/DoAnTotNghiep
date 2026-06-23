using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ProjectM.Cards;
using ProjectM.Skills;

namespace ProjectM.UI
{
    /// <summary>
    /// Quản lý Panel hiển thị Discard Pile (đống bài đã dùng).
    ///
    /// UNITY SETUP:
    ///   1. Gắn script này vào root DiscardPilePanel (có CanvasGroup).
    ///   2. Gán discardPanelCanvas → CanvasGroup của Panel (để animate).
    ///   3. Gán gridContainer     → Transform có GridLayoutGroup (chứa các thẻ hiển thị).
    ///   4. Gán skillPrefab       → Skill_Prefab (prefab thẻ kỹ năng).
    ///   5. Gán closeButton       → Nút X đóng Panel.
    ///   6. Nút Discard Pile ở combat UI → OnClick → gọi OpenDiscardPile().
    /// </summary>
    public class DiscardPileUI : MonoBehaviour
    {
        // ══════════════════════════════════════════
        [Header("UI References")]
        [Tooltip("CanvasGroup của panel này (để fade in/out).")]
        public CanvasGroup discardPanelCanvas;

        [Tooltip("Icon/Nút mở Discard Pile ở Combat UI (tương đương bagIcon của Inventory). Tùy chọn.")]
        public RectTransform openIcon;

        [Tooltip("Transform có GridLayoutGroup — chứa các thẻ hiển thị.")]
        public Transform gridContainer;

        [Tooltip("Prefab thẻ skill (Skill_Prefab với SkillExecutor + SkillDragHandler).")]
        public GameObject skillPrefab;

        [Tooltip("Nút đóng Panel.")]
        public Button closeButton;

        [Tooltip("Root GameObject của Panel cần ẩn/hiện (thường chính là DiscardPilePanel). Để trống = dùng gameObject chứa script.")]
        public GameObject panelRoot;

        [Header("Animation")]
        [Tooltip("Thời gian fade in/out (giây).")]
        public float fadeDuration = 0.25f;

        // ── Runtime ────────────────────────────────────────────────────────
        private bool _isOpen = false;
        private readonly List<GameObject> _spawnedCards = new();

        // Helper: trả về đúng root cần show/hide
        // Nếu bạn gán panelRoot trong Inspector → dùng cái đó; nếu không → dùng gameObject chứa script
        private GameObject Root => panelRoot != null ? panelRoot : gameObject;

        // ══════════════════════════════════════════
        private void Awake()
        {
            if (discardPanelCanvas == null)
                discardPanelCanvas = GetComponent<CanvasGroup>();

            // Bắt đầu ẩn panel
            if (discardPanelCanvas != null)
            {
                discardPanelCanvas.alpha = 0f;
                discardPanelCanvas.interactable = false;
                discardPanelCanvas.blocksRaycasts = false;
            }
            gameObject.SetActive(false); // tắt gameObject chứa script trước (dùng Root ở Open/Close)

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseDiscardPile);
        }

        // ══════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════

        /// <summary>Gọi từ nút Discard Pile ở Combat UI để mở Panel.</summary>
        public void OpenDiscardPile()
        {
            if (_isOpen) return;
            _isOpen = true;

            Root.SetActive(true);
            PopulateGrid();

            if (discardPanelCanvas != null)
            {
                discardPanelCanvas.interactable   = true;
                discardPanelCanvas.blocksRaycasts = true;
                discardPanelCanvas.DOKill();
                discardPanelCanvas.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
            }

            Debug.Log("[DiscardPileUI] Panel đã mở.");
        }

        /// <summary>Gọi từ nút X để đóng Panel. Luôn hoạt động dù Panel được mở bằng cách nào.</summary>
        public void CloseDiscardPile()
        {
            Debug.Log("[DiscardPileUI] CloseDiscardPile() được gọi! Root = " + Root.name);
            _isOpen = false;

            if (discardPanelCanvas != null)
            {
                discardPanelCanvas.interactable   = false;
                discardPanelCanvas.blocksRaycasts = false;
                discardPanelCanvas.DOKill();
                discardPanelCanvas.alpha = 0f;
            }

            ClearGrid();
            Root.SetActive(false);
            Debug.Log("[DiscardPileUI] Panel đã đóng.");
        }

        // ══════════════════════════════════════════
        // POPULATE
        // ══════════════════════════════════════════

        private void PopulateGrid()
        {
            ClearGrid();

            var skillHand = SkillHandManager.Instance;
            if (skillHand == null)
            {
                Debug.LogWarning("[DiscardPileUI] Không tìm thấy SkillHandManager!");
                return;
            }

            if (skillPrefab == null || gridContainer == null)
            {
                Debug.LogError("[DiscardPileUI] skillPrefab hoặc gridContainer chưa được gán trong Inspector!");
                return;
            }

            var discardList = skillHand.GetDiscardPile();

            if (discardList.Count == 0)
            {
                Debug.Log("[DiscardPileUI] Discard pile đang trống.");
                return;
            }

            foreach (var skillData in discardList)
            {
                if (skillData == null) continue;

                // Spawn thẻ vào grid
                GameObject cardGO = Instantiate(skillPrefab, gridContainer);
                cardGO.name = $"Discard_{skillData.skillName}";

                // Nạp dữ liệu lên CardDisplay
                var executor = cardGO.GetComponentInChildren<SkillExecutor>(includeInactive: true);
                if (executor != null)
                    executor.Init(skillData);
                else
                {
                    // Fallback: nạp thẳng vào CardDisplay nếu không có SkillExecutor
                    var display = cardGO.GetComponentInChildren<CardDisplay>(includeInactive: true);
                    display?.LoadSkillData(skillData);
                }

                // ── Vô hiệu hóa hoàn toàn kéo thả (view-only) ──
                foreach (var sdh in cardGO.GetComponentsInChildren<SkillDragHandler>(includeInactive: true))
                    sdh.enabled = false;

                foreach (var cdh in cardGO.GetComponentsInChildren<CardDragHandler>(includeInactive: true))
                    cdh.enabled = false;

                // Vô hiệu hóa CanvasGroup blocksRaycasts để tooltip vẫn hoạt động nếu có
                var cg = cardGO.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }

                // Reset scale & rotation (phòng khi prefab có giá trị lạ)
                cardGO.transform.localScale    = Vector3.one;
                cardGO.transform.localRotation = Quaternion.identity;

                _spawnedCards.Add(cardGO);
            }

            Debug.Log($"[DiscardPileUI] Hiển thị {_spawnedCards.Count} thẻ trong Discard Pile.");
        }

        private void ClearGrid()
        {
            foreach (var go in _spawnedCards)
            {
                if (go != null) Destroy(go);
            }
            _spawnedCards.Clear();
        }

        private void OnDestroy()
        {
            discardPanelCanvas?.DOKill();
        }
    }
}
