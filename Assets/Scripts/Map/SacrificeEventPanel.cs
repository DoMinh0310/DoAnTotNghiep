using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using DG.Tweening;

namespace ProjectM.Map
{
    // ════════════════════════════════════════════════════════════════════
    // SACRIFICE CARD SLOT — Hiển thị 1 thẻ trong list, xử lý click chọn
    // ════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Gắn runtime vào mỗi thẻ Utility được sinh ra trong SacrificeEventPanel.
    /// Xử lý click chọn, hover effect, và drag & drop vào vùng huỷ.
    /// </summary>
    public class SacrificeCardSlot : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Dữ liệu ──────────────────────────────────────────────────────
        public ProjectM.Skills.SkillData SkillData { get; private set; }
        public bool IsSelected { get; private set; }

        // ── Callback ──────────────────────────────────────────────────────
        public System.Action<SacrificeCardSlot> onSelected;

        // ── Internal ─────────────────────────────────────────────────────
        private RectTransform _rt;
        private float _baseScale;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Canvas _rootCanvas;

        private bool _isDragging = false;
        private GameObject _dragProxy;   // Bản sao để drag, giữ nguyên thẻ gốc tại chỗ

        // ── Glow border khi được chọn ─────────────────────────────────────
        [HideInInspector] public Image selectionGlow;

        // ═════════════════════════════════════════════════════════════════
        public void Setup(ProjectM.Skills.SkillData data, float scale)
        {
            SkillData = data;
            _baseScale = scale;
            _rt = GetComponent<RectTransform>();
            _rootCanvas = GetComponentInParent<Canvas>();

            transform.localScale = Vector3.zero;
            transform.DOScale(_baseScale, 0.35f).SetEase(Ease.OutBack);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionGlow != null)
                selectionGlow.enabled = selected;

            if (selected)
            {
                transform.DOKill();
                transform.DOScale(_baseScale * 1.08f, 0.15f).SetEase(Ease.OutQuad);
            }
            else
            {
                transform.DOKill();
                transform.DOScale(_baseScale, 0.15f).SetEase(Ease.OutQuad);
            }
        }

        // ── Hover ────────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData e)
        {
            if (IsSelected || _isDragging) return;
            _originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            transform.DOKill();
            transform.DOScale(_baseScale * 1.12f, 0.15f).SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (IsSelected || _isDragging) return;
            transform.SetSiblingIndex(_originalSiblingIndex);
            transform.DOKill();
            transform.DOScale(_baseScale, 0.15f).SetEase(Ease.OutQuad);
        }

        // ── Click chọn ───────────────────────────────────────────────────
        public void OnPointerClick(PointerEventData e)
        {
            onSelected?.Invoke(this);
        }

        // ── Drag & Drop ──────────────────────────────────────────────────
        public void OnBeginDrag(PointerEventData e)
        {
            // Chỉ cho drag nếu đã được chọn trước
            if (!IsSelected) { e.pointerDrag = null; return; }

            _isDragging = true;
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // Tạo drag proxy trong Canvas root (không ảnh hưởng layout gốc)
            _dragProxy = Instantiate(gameObject, _rootCanvas.transform);
            _dragProxy.transform.localScale = transform.lossyScale;
            // Tắt SacrificeCardSlot trên proxy để tránh double event
            var slot = _dragProxy.GetComponent<SacrificeCardSlot>();
            if (slot != null) Destroy(slot);

            // Ẩn glow trên proxy
            var proxyGlow = _dragProxy.GetComponentInChildren<Image>();
            // Chỉnh alpha thẻ gốc cho có cảm giác "cầm lên"
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;
            cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_isDragging || _dragProxy == null) return;
            // Di chuyển proxy theo con trỏ chuột
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                e.position, _rootCanvas.worldCamera,
                out Vector2 localPos);
            (_dragProxy.transform as RectTransform).anchoredPosition = localPos;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (_dragProxy != null) Destroy(_dragProxy);

            // Khôi phục alpha thẻ gốc
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // SACRIFICE DROP ZONE — Vùng thả thẻ để huỷ
    // ════════════════════════════════════════════════════════════════════
    public class SacrificeDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Action<SacrificeCardSlot> onCardDropped;
        private Image _image;
        private Color _normalColor;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (_image != null) _normalColor = _image.color;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (e.pointerDrag == null) return;
            if (_image != null)
                _image.DOColor(new Color(1f, 0.3f, 0.3f, 0.8f), 0.15f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (_image != null)
                _image.DOColor(_normalColor, 0.15f);
        }

        public void OnDrop(PointerEventData e)
        {
            if (_image != null)
                _image.DOColor(_normalColor, 0.15f);

            // Chỉ chấp nhận thả từ SacrificeCardSlot
            var slot = e.pointerDrag?.GetComponent<SacrificeCardSlot>();
            if (slot != null)
            {
                onCardDropped?.Invoke(slot);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // SACRIFICE EVENT PANEL — Panel chính
    // ════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Điều khiển UI cho Sacrifice Event.
    /// Flow:
    ///   1. Panel mở ra — hiện danh sách thẻ Utilities của người chơi.
    ///   2. Người chơi click vào 1 thẻ để chọn → nút "Sacrifice" và vùng thả sáng lên.
    ///   3. Bấm nút "Sacrifice" hoặc kéo thả thẻ vào ảnh để xác nhận huỷ.
    ///   4. Thẻ bị xoá khỏi deck, panel đóng.
    /// </summary>
    public class SacrificeEventPanel : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ══════════════════════════════════════════════════════════════

        [Header("Core References")]
        [Tooltip("CanvasGroup bọc ngoài cùng panel (dùng cho fade in/out)")]
        public CanvasGroup panelCanvasGroup;

        [Tooltip("Container chứa các thẻ Utility (có HorizontalLayoutGroup hoặc GridLayoutGroup)")]
        public Transform cardContainer;

        [Tooltip("Prefab thẻ bài (Card_Prefab có CardDisplay)")]
        public GameObject cardPrefab;

        [Tooltip("Scale của thẻ bài khi sinh ra trong danh sách")]
        public float cardScale = 0.5f;

        [Header("Sacrifice Target (Right Side)")]
        [Tooltip("GameObject hình ảnh / vùng thả bên phải để huỷ thẻ (tự thêm SacrificeDropZone vào đây)")]
        public GameObject sacrificeTarget;

        [Tooltip("Hiệu ứng Scale pulse của sacrificeTarget khi có thẻ được chọn")]
        public bool pulseTargetWhenReady = true;

        [Header("Buttons")]
        [Tooltip("Nút Sacrifice bên cạnh ảnh (chỉ interactable khi đã chọn thẻ)")]
        public Button sacrificeButton;

        [Tooltip("Nút quay lại map (Peek Map / Back)")]
        public Button backButton;

        [Header("Texts")]
        public TextMeshProUGUI headerText;
        public TextMeshProUGUI hintText;

        [Header("Animation")]
        public float fadeInDuration = 0.3f;

        // ══════════════════════════════════════════════════════════════
        // RUNTIME
        // ══════════════════════════════════════════════════════════════
        private System.Action _onComplete;
        private SacrificeCardSlot _selectedSlot;
        private readonly List<SacrificeCardSlot> _spawnedSlots = new List<SacrificeCardSlot>();
        private Tweener _pulseTween;
        private GameObject _slottedCardVisual; // Thẻ ảo nằm trên nhà máy

        // ══════════════════════════════════════════════════════════════
        // UNITY
        // ══════════════════════════════════════════════════════════════
        private void Awake()
        {
            if (panelCanvasGroup == null)
                panelCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            // Gắn SacrificeDropZone runtime vào ảnh bên phải
            if (sacrificeTarget != null)
            {
                var dz = sacrificeTarget.GetComponent<SacrificeDropZone>()
                         ?? sacrificeTarget.AddComponent<SacrificeDropZone>();
                dz.onCardDropped = OnCardDroppedIntoMachine;

                // Đảm bảo có Graphic để raycast bắt được
                if (sacrificeTarget.GetComponent<Graphic>() == null)
                    sacrificeTarget.AddComponent<Image>().color = Color.clear;
            }

            if (sacrificeButton != null)
            {
                sacrificeButton.onClick.AddListener(ConfirmSacrifice);
                sacrificeButton.interactable = false;
            }

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            gameObject.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Gọi từ MapManager để mở Sacrifice Event.
        /// </summary>
        public void Open(System.Action onComplete)
        {
            _onComplete = onComplete;
            _selectedSlot = null;
            if (_slottedCardVisual != null) Destroy(_slottedCardVisual);

            if (sacrificeButton != null) sacrificeButton.interactable = false;
            if (hintText != null) hintText.text = "Chọn 1 thẻ Hỗ trợ để huỷ";

            PopulateUtilityCards();

            gameObject.SetActive(true);
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.DOFade(1f, fadeInDuration);

            MapManager.Instance?.SetEventInProgress(true, Reopen);
        }

        public void Reopen()
        {
            gameObject.SetActive(true);
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.DOFade(1f, fadeInDuration);
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE — Populate cards
        // ══════════════════════════════════════════════════════════════
        private void PopulateUtilityCards()
        {
            // Xoá thẻ cũ
            foreach (var s in _spawnedSlots)
                if (s != null) Destroy(s.gameObject);
            _spawnedSlots.Clear();

            // Lấy danh sách thẻ Utilities từ RunData
            var utilityCards = GetPlayerUtilityCards();

            if (utilityCards.Count == 0)
            {
                if (hintText != null) hintText.text = "Bạn không có thẻ Hỗ trợ nào!";
                return;
            }

            foreach (var card in utilityCards)
            {
                // 1. Tạo Wrapper rỗng để Grid Layout Group điều khiển (không bị bóp méo thẻ)
                GameObject wrapper = new GameObject("CardWrapper", typeof(RectTransform));
                wrapper.transform.SetParent(cardContainer, false);
                
                // 2. Sinh thẻ bài làm con của Wrapper
                GameObject go = Instantiate(cardPrefab, wrapper.transform);
                go.transform.localPosition = Vector3.zero;

                // Load dữ liệu lên CardDisplay
                var display = go.GetComponentInChildren<CardDisplay>();
                if (display != null) display.LoadSkillData(card);

                // Thêm SacrificeCardSlot vào THẺ (không phải wrapper)
                var slot = go.AddComponent<SacrificeCardSlot>();

                // Thêm glow border (Image con) để đánh dấu "selected"
                var glowGO = new GameObject("SelectionGlow", typeof(RectTransform), typeof(Image));
                glowGO.transform.SetParent(go.transform, false);
                var glowRT = glowGO.GetComponent<RectTransform>();
                glowRT.anchorMin = Vector2.zero;
                glowRT.anchorMax = Vector2.one;
                glowRT.offsetMin = new Vector2(-6f, -6f);
                glowRT.offsetMax = new Vector2(6f, 6f);
                var glowImg = glowGO.GetComponent<Image>();
                glowImg.color = new Color(1f, 0.9f, 0.3f, 0.6f);
                glowImg.enabled = false;
                glowGO.transform.SetSiblingIndex(0); // Để glow ở phía dưới
                slot.selectionGlow = glowImg;

                slot.onSelected = OnCardSelected;
                slot.Setup(card, cardScale);

                _spawnedSlots.Add(slot);
            }
        }

        private List<ProjectM.Skills.SkillData> GetPlayerUtilityCards()
        {
            var result = new List<ProjectM.Skills.SkillData>();
            var runData = GameManager.Instance?.RunData;
            if (runData == null) return result;

            var allCards = ProjectM.Inventory.InventoryManager.Instance?.allSkillAssets;
            if (allCards == null)
            {
                Debug.LogWarning("[SacrificeEvent] Không tìm thấy InventoryManager hoặc allSkillAssets trống!");
                return result;
            }

            // Dùng lookup để tra nhanh theo name, nhưng duyệt qua playerDeckIDs
            // để GIỮ NGUYÊN SỐ LƯỢNG thẻ trùng tên (ví dụ 4 Treebark = 4 slot riêng biệt)
            var lookup = new Dictionary<string, ProjectM.Skills.SkillData>();
            foreach (var c in allCards)
                if (c != null && !lookup.ContainsKey(c.name)) lookup[c.name] = c;

            foreach (var id in runData.playerDeckIDs)
            {
                // Chỉ lấy SkillData (bỏ qua champion CardData)
                if (lookup.TryGetValue(id, out var card))
                    result.Add(card); // Mỗi entry trong playerDeckIDs → 1 slot riêng
            }
            return result;
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE — Selection logic
        // ══════════════════════════════════════════════════════════════
        private void OnCardSelected(SacrificeCardSlot clickedSlot)
        {
            // Phát tiếng chọn thẻ
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardSelectClip);

            // Bỏ chọn slot cũ
            if (_selectedSlot != null && _selectedSlot != clickedSlot)
                _selectedSlot.SetSelected(false);

            bool wasSameSlot = (_selectedSlot == clickedSlot);

            _selectedSlot = wasSameSlot ? null : clickedSlot;
            clickedSlot.SetSelected(!wasSameSlot);

            bool hasSelection = (_selectedSlot != null);

            // Cập nhật nút và hint
            if (sacrificeButton != null) sacrificeButton.interactable = hasSelection;
            if (hintText != null)
                hintText.text = hasSelection
                    ? $"Huỷ <b>{_selectedSlot.SkillData.skillName}</b>? Bấm nút Recycle để xác nhận."
                    : "Chọn hoặc kéo thả 1 thẻ Hỗ trợ vào máy để huỷ";

            // Đồng bộ thẻ ảo trên nhà máy
            if (hasSelection)
            {
                CreateSlottedVisual(_selectedSlot);
            }
            else
            {
                if (_slottedCardVisual != null) Destroy(_slottedCardVisual);
            }

            // Pulse target khi có lựa chọn
            if (sacrificeTarget != null && pulseTargetWhenReady)
            {
                _pulseTween?.Kill();
                if (hasSelection)
                {
                    _pulseTween = sacrificeTarget.transform
                        .DOScale(1.05f, 0.5f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
                else
                {
                    sacrificeTarget.transform.DOScale(1f, 0.15f);
                }
            }
        }

        private void OnCardDroppedIntoMachine(SacrificeCardSlot slot)
        {
            // Tự động chọn thẻ vừa thả vào máy
            if (_selectedSlot != slot)
            {
                OnCardSelected(slot);
            }
        }

        private void CreateSlottedVisual(SacrificeCardSlot slot)
        {
            if (_slottedCardVisual != null) Destroy(_slottedCardVisual);
            if (sacrificeTarget == null) return;
            
            // Đặt thẻ làm "anh em" (sibling) của nhà máy thay vì làm "con" (child) để không bị dính hiệu ứng scale
            _slottedCardVisual = Instantiate(cardPrefab, sacrificeTarget.transform.parent);
            
            // Cập nhật vị trí đè lên đúng tâm của nhà máy
            _slottedCardVisual.transform.position = sacrificeTarget.transform.position;
            
            // Xoay thẻ thẳng đứng hoặc nghiêng nhẹ cho tự nhiên tuỳ ý
            _slottedCardVisual.transform.localRotation = Quaternion.identity; 
            _slottedCardVisual.transform.localScale = Vector3.one * cardScale * 1.5f; // Hiện to hơn 1 chút trên máy
            
            // Đảm bảo thẻ nằm đè lên trên cùng của giao diện
            _slottedCardVisual.transform.SetAsLastSibling();
            
            var display = _slottedCardVisual.GetComponentInChildren<CardDisplay>();
            if (display != null) display.LoadSkillData(slot.SkillData);
            
            // Xoá script tương tác trên thẻ ảo
            var uiSlot = _slottedCardVisual.GetComponent<SacrificeCardSlot>();
            if (uiSlot != null) Destroy(uiSlot);
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE — Confirm sacrifice
        // ══════════════════════════════════════════════════════════════
        private void ConfirmSacrifice()
        {
            if (_selectedSlot == null) return;
            
            // Phát tiếng nghiền thẻ
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.recycleClip);
            
            var card = _selectedSlot.SkillData;

            // Xoá thẻ khỏi RunData
            var runData = GameManager.Instance?.RunData;
            if (runData != null)
            {
                // Xoá 1 lần đầu tiên tìm thấy ID (name) trùng
                int idx = runData.playerDeckIDs.IndexOf(card.name);
                if (idx >= 0) runData.playerDeckIDs.RemoveAt(idx);

                // Xoá khỏi RAM (championSetup.supportDeck)
                if (runData.championSetup != null)
                {
                    runData.championSetup.supportDeck.Remove(card);
                }

                Debug.Log($"[SacrificeEvent] Đã huỷ thẻ: {card.skillName} (id={card.name})");
            }

            // Xoá bản gốc ở danh sách bên trái ngay lập tức để người chơi thấy nó biến mất
            _selectedSlot.gameObject.SetActive(false);

            // Animation thẻ ảo bị nghiền nát/hút vào máy
            if (_slottedCardVisual != null)
            {
                _pulseTween?.Kill();
                if (sacrificeTarget != null) sacrificeTarget.transform.DOScale(1f, 0.1f);

                _slottedCardVisual.transform.DOKill();
                _slottedCardVisual.transform.DOScale(0f, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    Destroy(_slottedCardVisual);
                    CloseAndComplete();
                });
            }
            else
            {
                CloseAndComplete();
            }
        }

        private void CloseAndComplete()
        {
            panelCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                _onComplete?.Invoke();
                MapManager.Instance?.SetEventInProgress(false, null);
            });
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE — Back button
        // ══════════════════════════════════════════════════════════════
        private void OnBackClicked()
        {
            // Ẩn panel tạm để xem map, event vẫn còn
            _pulseTween?.Kill();
            if (sacrificeTarget != null) sacrificeTarget.transform.DOScale(1f, 0.1f);

            panelCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                MapManager.Instance?.SetEventInProgress(true, Reopen);
            });
        }
    }
}
