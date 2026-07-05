using UnityEngine;
using DG.Tweening;

namespace ProjectM.Cards
{
    /// <summary>
    /// Overlay icon nhấp nháy trên CardBattle khi đang bị chọn làm mục tiêu.
    ///
    /// SETUP:
    ///   1. Tạo child GameObject trong Card_Prefab tên "TargetHighlight".
    ///   2. Gắn script này vào đó.
    ///   3. Gán iconTransform = RectTransform của Image icon đỏ (ảnh 2).
    ///   4. Mặc định ẩn (SetActive false hoặc alpha 0).
    /// </summary>
    public class TargetHighlight : MonoBehaviour
    {
        [Tooltip("Transform của icon xoay (thường là chính GameObject này hoặc Image con)")]
        public RectTransform iconTransform;

        [Header("Animation")]
        [SerializeField] private float rotateAngle = 15f;
        [SerializeField] private float pulseScale  = 1.15f;
        [SerializeField] private float speed       = 0.4f;
        [SerializeField] private float cardHoverScale = 1.2f; // Khớp với CardHoverHandler

        // ── Runtime ──────────────────────────────────────────────────────
        private CanvasGroup _cg;
        private Tween _rotateTween;
        private Tween _pulseTween;
        private Tween _fadeTween;
        private Tween _cardScaleTween;

        // ════════════════════════════════════════════════════════════════
        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

            // ── Tự động thêm Canvas để render nổi lên trên cả đường Bezier (order 500) ──
            var myCanvas = GetComponent<Canvas>();
            if (myCanvas == null) myCanvas = gameObject.AddComponent<Canvas>();
            myCanvas.overrideSorting = true;
            myCanvas.sortingOrder    = 600; // Đảm bảo luôn nổi lên trên cả đường Bezier Line (order 500)

            // Bắt đầu ẩn
            _cg.alpha          = 0f;
            _cg.blocksRaycasts = false;
            _cg.interactable   = false;
            gameObject.SetActive(false);
        }

        // ════════════════════════════════════════════════════════════════
        /// <summary>Hiện icon với animation. Gọi bởi SkillDragHandler.</summary>
        public void Show()
        {
            gameObject.SetActive(true);

            // Đảm bảo tâm động luôn nằm chính giữa tâm của lá bài để khớp chuẩn 100% với đường Bezier
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            // Phóng to toàn bộ thẻ bài (để giống với scale khi hover bình thường)
            _cardScaleTween?.Kill();
            if (transform.parent != null)
                _cardScaleTween = transform.parent.DOScale(cardHoverScale, 0.15f).SetEase(Ease.OutQuad);

            // Fade in
            _fadeTween?.Kill();
            _fadeTween = _cg.DOFade(1f, 0.15f).SetEase(Ease.OutQuad);

            // Reset rồi loop
            if (iconTransform != null)
            {
                iconTransform.localRotation = Quaternion.identity;
                iconTransform.localScale    = Vector3.one;

                _rotateTween?.Kill();
                _rotateTween = iconTransform
                    .DOLocalRotate(new Vector3(0f, 0f, rotateAngle), speed)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                _pulseTween?.Kill();
                _pulseTween = iconTransform
                    .DOScale(pulseScale, speed * 1.2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        // ════════════════════════════════════════════════════════════════
        /// <summary>Ẩn icon và dừng animation. Gọi bởi SkillDragHandler.</summary>
        public void Hide()
        {
            _rotateTween?.Kill();
            _pulseTween?.Kill();
            _fadeTween?.Kill();
            
            _cardScaleTween?.Kill();
            if (transform.parent != null)
                transform.parent.localScale = Vector3.one;

            // Kill mọi tween DOTween đang chạy trên canvasGroup (kể cả fade-in chưa xong)
            if (_cg != null)
            {
                _cg.DOKill();
                _cg.alpha = 0f;
            }

            if (iconTransform != null)
            {
                // Kill mọi tween trên icon để tránh scale/rotation còn sót lại
                iconTransform.DOKill();
                iconTransform.localRotation = Quaternion.identity;
                iconTransform.localScale    = Vector3.one;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _rotateTween?.Kill();
            _pulseTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
