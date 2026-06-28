using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace ProjectM.Map
{
    /// <summary>
    /// Quản lý 1 node đơn lẻ trên Map: state machine, animation pop-up, mũi tên bounce, click.
    /// Gắn vào prefab Node (UI Button).
    /// </summary>
    public class MapNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ══════════════════════════════════════════
        // STATE (NodeState enum nằm trong NodeState.cs)
        // ══════════════════════════════════════════
        public NodeState State { get; private set; } = NodeState.Hidden;

        // ══════════════════════════════════════════
        // DATA
        // ══════════════════════════════════════════
        public NodeType NodeType    { get; private set; }
        public int SegmentIndex     { get; private set; }
        public int PathIndex        { get; private set; } // 0=Left, 1=Center, 2=Right, -1=combat
        public int NodeIndex        { get; private set; } // vị trí trong path
        public int SlotIndex        { get; private set; } // index trong slots[] của MapManager

        // ══════════════════════════════════════════
        // UI REFERENCES
        // ══════════════════════════════════════════
        [Header("UI References")]
        [Tooltip("Image hiển thị icon của event")]
        public Image iconImage;

        [Tooltip("GameObject hiện dấu tick khi Completed")]
        public GameObject completedOverlay;

        [Tooltip("Mũi tên bounce phía trên node (loop khi Active)")]
        public RectTransform arrowIndicator;

        [Tooltip("Button để nhận click")]
        public Button button;

        [Tooltip("CanvasGroup để điều chỉnh alpha")]
        public CanvasGroup canvasGroup;

        // ══════════════════════════════════════════
        // ANIMATION SETTINGS
        // ══════════════════════════════════════════
        [Header("Animation")]
        [Tooltip("Thời gian pop-up khi reveal (giây)")]
        public float popUpDuration = 0.35f;

        [Tooltip("Biên độ mũi tên bounce (pixel)")]
        public float arrowBounceAmplitude = 12f;

        [Tooltip("Tốc độ mũi tên bounce")]
        public float arrowBounceSpeed = 2.5f;

        [Tooltip("Alpha khi bị Dimmed")]
        public float dimmedAlpha = 0.3f;

        [Header("Click Pulse")]
        [Tooltip("Tỷ lệ phóng to khi click (1.08 = +8%)")]
        public float clickPulseScale = 1.08f;

        [Tooltip("Tổng thời gian animation click (giây)")]
        public float clickPulseDuration = 0.18f;

        [Header("Hover Animation")]
        [Tooltip("Tỷ lệ scale icon khi hover (1.1 = +10%)")]
        public float hoverScale = 1.1f;
        [Tooltip("Thời gian chuyển đổi (giây)")]
        public float hoverDuration = 0.15f;

        // ── Private ──────────────────────────────
        private Coroutine arrowCoroutine;
        private Vector2 arrowBasePosition;
        private Coroutine hoverCoroutine;
        
        // Kích thước chuẩn của node (Boss = 1.1, Thường = 1.0)
        private float _baseScale = 1f;

        // ══════════════════════════════════════════
        // KHỞI TẠO
        // ══════════════════════════════════════════
        private void Awake()
        {
            // Tự tìm Button và CanvasGroup trên chính GameObject này nếu chưa gán tay
            if (button == null)
                button = GetComponent<Button>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            // Nếu chưa có CanvasGroup thì tự tạo
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (button != null)
                button.onClick.AddListener(OnClicked);

            // Map V2 hiển thị toàn bộ node ngay lập tức (không cần anim pop-up)
            transform.localScale = Vector3.one;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (arrowIndicator != null)
            {
                arrowBasePosition = arrowIndicator.anchoredPosition;
                arrowIndicator.gameObject.SetActive(false);
            }

            if (completedOverlay != null)
                completedOverlay.SetActive(false);
        }

        /// <summary>Gọi bởi MapManager để khởi tạo node với data cụ thể.</summary>
        public void Init(NodeType type, MapNodeData data, int slotIndex, int pathIndex, int nodeIndex)
        {
            NodeType     = type;
            SlotIndex    = slotIndex;
            PathIndex    = pathIndex;
            NodeIndex    = nodeIndex;
            SegmentIndex = slotIndex; // giữ tương thích ngược

            // Nếu là Boss nhưng chưa có data (do chưa gắn icon Boss), mượn tạm icon của Battle
            if (type == NodeType.Boss && data == null)
            {
                data = MapManager.Instance?.GetNodeData(NodeType.Battle);
            }

            if (iconImage != null && data != null)
                iconImage.sprite = data.icon;

            // Boss sẽ to hơn các node thường 10%
            _baseScale = (type == NodeType.Boss) ? 1.1f : 1f;
            transform.localScale = Vector3.one * _baseScale;
        }

        // ══════════════════════════════════════════
        // STATE TRANSITIONS
        // ══════════════════════════════════════════

        /// <summary>Pop-up animation: scale 0 → 1.2 → 1</summary>
        public IEnumerator RevealRoutine()
        {
            State = NodeState.Revealed;

            float elapsed = 0f;
            float growPhase   = popUpDuration * 0.7f;
            float shrinkPhase = popUpDuration * 0.3f;

            // Phase 1: scale 0 → 1.2
            while (elapsed < growPhase)
            {
                elapsed += Time.deltaTime;
                float t     = elapsed / growPhase;
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
                transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f * _baseScale, eased);
                yield return null;
            }

            // Phase 2: scale 1.2 → 1
            elapsed = 0f;
            while (elapsed < shrinkPhase)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shrinkPhase;
                transform.localScale = Vector3.one * Mathf.Lerp(1.2f * _baseScale, _baseScale, t);
                yield return null;
            }

            transform.localScale = Vector3.one * _baseScale;
        }

        /// <summary>Kích hoạt node: có thể click, hiện mũi tên bounce.</summary>
        public void Activate()
        {
            State = NodeState.Active;

            if (canvasGroup != null)
            {
                canvasGroup.interactable    = true;
                canvasGroup.blocksRaycasts  = true;
            }

            if (arrowIndicator != null)
            {
                arrowIndicator.gameObject.SetActive(true);
                if (arrowCoroutine != null) StopCoroutine(arrowCoroutine);
                arrowCoroutine = StartCoroutine(ArrowBounceRoutine());
            }
        }

        /// <summary>Đánh dấu đã hoàn thành.</summary>
        public void SetCompleted()
        {
            State = NodeState.Completed;
            StopArrow();

            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0.7f;
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (completedOverlay != null)
                completedOverlay.SetActive(true);
        }

        /// <summary>Làm mờ node.
        /// skipped=true: node cùng hàng bị bỏ qua → mờ rõ (dùng dimmedAlpha).
        /// skipped=false: node tương lai chưa tới → hiển bình thường.
        /// </summary>
        public void SetDimmed(bool skipped = false)
        {
            State = NodeState.Dimmed;
            StopArrow();

            if (canvasGroup != null)
            {
                canvasGroup.alpha          = skipped ? dimmedAlpha : 1f;
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        // ══════════════════════════════════════════
        // ARROW BOUNCE ANIMATION
        // ══════════════════════════════════════════
        private IEnumerator ArrowBounceRoutine()
        {
            while (true)
            {
                float y = Mathf.Sin(Time.time * arrowBounceSpeed) * arrowBounceAmplitude;
                arrowIndicator.anchoredPosition = arrowBasePosition + new Vector2(0f, y);
                yield return null;
            }
        }

        private void StopArrow()
        {
            if (arrowCoroutine != null)
            {
                StopCoroutine(arrowCoroutine);
                arrowCoroutine = null;
            }
            if (arrowIndicator != null)
                arrowIndicator.gameObject.SetActive(false);
        }

        // ══════════════════════════════════════════
        // CLICK HANDLER
        // ══════════════════════════════════════════
        private void OnClicked()
        {
            if (State != NodeState.Active) return;
            StartCoroutine(PlayClickPulse());
            MapManager.Instance?.OnNodeClicked(this);
        }

        /// <summary>Animation phóng to nhẹ khi click: scale 1 → clickPulseScale → 1.</summary>
        private IEnumerator PlayClickPulse()
        {
            float half    = clickPulseDuration * 0.5f;
            float elapsed = 0f;

            // Phase 1: phóng to
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                transform.localScale = Vector3.one * Mathf.Lerp(_baseScale, clickPulseScale * _baseScale, t);
                yield return null;
            }

            // Phase 2: thu nhỏ lại
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                transform.localScale = Vector3.one * Mathf.Lerp(clickPulseScale * _baseScale, _baseScale, t);
                yield return null;
            }

            transform.localScale = Vector3.one * _baseScale;
        }

        // ══════════════════════════════════════════
        // HOVER HANDLERS
        // ══════════════════════════════════════════
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Chỉ chạy hiệu ứng khi node đang sáng (Active)
            if (State != NodeState.Active || iconImage == null) return;

            if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(AnimateHoverScale(hoverScale));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (State != NodeState.Active || iconImage == null) return;

            if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(AnimateHoverScale(1f));
        }

        private IEnumerator AnimateHoverScale(float targetScale)
        {
            float elapsed = 0f;
            Vector3 startScale = iconImage.transform.localScale;
            Vector3 targetVec  = Vector3.one * targetScale;

            while (elapsed < hoverDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hoverDuration);
                // Dùng ease-out để mượt hơn
                float easeT = 1f - Mathf.Pow(1f - t, 3f); 
                iconImage.transform.localScale = Vector3.Lerp(startScale, targetVec, easeT);
                yield return null;
            }
            iconImage.transform.localScale = targetVec;
        }
    }
}
