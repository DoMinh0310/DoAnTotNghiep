using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Coffee.UIEffects;

namespace ProjectM.Cards
{
    [RequireComponent(typeof(RectTransform))]
    public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover Settings")]
        public float hoverScaleMultiplier = 1.2f;
        [Tooltip("Thời gian animation hover (giây)")]
        public float hoverDuration = 0.15f;
        [Tooltip("Độ cao thẻ bài nhấc lên khi hover (pixel)")]
        public float hoverYOffset = 50f;

        private RectTransform rectTransform;

        // Nguồn chân lý duy nhất — chỉ được set bởi PlayerHand hoặc SetNewParent
        private Vector3    baseScale    = Vector3.zero;
        private Quaternion baseRotation = Quaternion.identity;
        private Vector2    basePosition = Vector2.zero;

        private bool isHovering = false;
        public  bool IsHovering => isHovering;

        // Các tween đang chạy — cần Kill trước khi tạo tween mới
        private Tween _scaleTween;
        private Tween _posTween;
        private Tween _rotTween;

        // Canvas override CHỈ dùng khi hover để nổi lên trên các lá khác
        private Canvas _overrideCanvas;
        private Canvas overrideCanvas
        {
            get
            {
                if (_overrideCanvas == null)
                {
                    _overrideCanvas = GetComponent<Canvas>();
                    if (_overrideCanvas == null) _overrideCanvas = gameObject.AddComponent<Canvas>();
                    if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
                }
                return _overrideCanvas;
            }
        }

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // Ép Anchor về tâm để anchoredPosition luôn = offset từ tâm của parent
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        }

        void Start()
        {
            // Pre-warm: Kích hoạt UIEffect material ngay từ đầu bằng cách bật/tắt overrideSorting
            // trong vòng 2 frame. Việc này buộc Unity build sẵn Material cho context Canvas mới,
            // tránh hoàn toàn hiện tượng thẻ tàng hình đúng 1 frame ở lần hover đầu tiên.
            StartCoroutine(PreWarmUIEffect());
        }

        private System.Collections.IEnumerator PreWarmUIEffect()
        {
            // Frame 1: Bật Canvas override lên (giống như hover)
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 0;
            // Force tất cả UIEffect rebuild material
            foreach (var fx in GetComponentsInChildren<UIEffect>(true))
                fx.SetMaterialDirty();

            yield return null; // Đợi 1 frame để Unity build xong Material

            // Frame 2: Tắt lại về trạng thái bình thường
            overrideCanvas.overrideSorting = false;
            foreach (var fx in GetComponentsInChildren<UIEffect>(true))
                fx.SetVerticesDirty();
        }

        void Update()
        {
            // Nếu bất kỳ lá bài nào đang bị kéo → buộc reset hover ngay lập tức
            if (CardDragHandler.isAnyCardDragging)
            {
                if (isHovering || overrideCanvas.overrideSorting)
                {
                    CardDragHandler dragHandler = GetComponent<CardDragHandler>();
                    bool isThisCardDragging = dragHandler != null && dragHandler.isDragging;
                    
                    if (isThisCardDragging)
                    {
                        // Thẻ này đang được kéo, drag handler sẽ quản lý transform, không cần tween về
                        isHovering = false;
                        overrideCanvas.overrideSorting = false;
                        KillAllTweens();
                    }
                    else
                    {
                        // Thẻ khác đang được kéo nhưng thẻ này đang dính hover -> Force tween về để không bị kẹt
                        ForceStopHover();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        /// <summary>
        /// PlayerHand và SetNewParent gọi để nạp trạng thái gốc chuẩn xác.
        /// </summary>
        public void UpdateBaseState(Vector2 pos, Quaternion rot, Vector3 scale)
        {
            basePosition = pos;
            baseRotation = rot;
            baseScale    = scale;

            // Nếu đang lơ lửng (hover) mà bị đổi Base State (do PlayerHand hoặc trả về từ Drag thất bại),
            // ta phải tính toán lại vị trí Hover để thẻ không bị kẹt ở tọa độ màn hình cũ.
            if (isHovering && !CardDragHandler.isAnyCardDragging)
            {
                KillAllTweens();
                _scaleTween = transform.DOScale(baseScale * hoverScaleMultiplier, hoverDuration).SetEase(DG.Tweening.Ease.OutQuad);
                _rotTween = transform.DOLocalRotateQuaternion(Quaternion.identity, hoverDuration).SetEase(DG.Tweening.Ease.OutQuad);
                _posTween = rectTransform.DOAnchorPos(basePosition + new Vector2(0, hoverYOffset), hoverDuration).SetEase(DG.Tweening.Ease.OutQuad);
            }
        }

        // ─────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CardDragHandler.isAnyCardDragging) return;
            if (ProjectM.Skills.SkillDragHandler.isAnySkillTargeting) return;
            if (baseScale == Vector3.zero) return;
            if (Managers.BattleManager.IsInputBlocked) return;

            isHovering = true;
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder    = 100;

            KillAllTweens();

            // Phóng to mượt mà không bị nảy (giật cục)
            _scaleTween = transform
                .DOScale(baseScale * hoverScaleMultiplier, hoverDuration)
                .SetEase(Ease.OutQuad);

            // Dựng thẳng
            _rotTween = transform
                .DOLocalRotateQuaternion(Quaternion.identity, hoverDuration)
                .SetEase(Ease.OutQuad);

            // Nhích lên trên mượt mà
            _posTween = rectTransform
                .DOAnchorPos(basePosition + new Vector2(0, hoverYOffset), hoverDuration)
                .SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopHover();
        }

        // ─────────────────────────────────────────────────
        public void ForceStopHover()
        {
            StopHover(true);
        }

        private void StopHover(bool force = false)
        {
            if (!isHovering && !force) return;

            // Nếu đang kéo bài thì KHÔNG tween về.
            // NẾU đang targeting và KHÔNG PHẢI gọi ép buộc (force), giữ nguyên trạng thái hover cho thẻ này!
            if (!force && (CardDragHandler.isAnyCardDragging || ProjectM.Skills.SkillDragHandler.isAnySkillTargeting)) return;

            isHovering = false;
            overrideCanvas.overrideSorting = false;

            // Sau khi tắt overrideSorting, force UIEffect redraw để tránh bị tàng hình
            foreach (var fx in GetComponentsInChildren<UIEffect>(true))
                fx.SetVerticesDirty();

            if (baseScale == Vector3.zero) return;

            KillAllTweens();

            // Luôn trả về đầy đủ cả 3: scale + rotation + position
            // PlayerHand chỉ tween lại khi count thay đổi, nên ta phải tự lo rotation
            _scaleTween = transform
                .DOScale(baseScale, hoverDuration)
                .SetEase(Ease.OutQuad);

            _rotTween = transform
                .DOLocalRotateQuaternion(baseRotation, hoverDuration)
                .SetEase(Ease.OutQuad);

            _posTween = rectTransform
                .DOAnchorPos(basePosition, hoverDuration)
                .SetEase(Ease.OutQuad);
        }

        private void KillAllTweens()
        {
            _scaleTween?.Kill();
            _posTween?.Kill();
            _rotTween?.Kill();
        }
    }
}
