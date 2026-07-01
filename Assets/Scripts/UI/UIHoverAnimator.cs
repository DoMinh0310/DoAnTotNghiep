using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ProjectM.UI
{
    /// <summary>
    /// Script gắn vào các UI elements (như nút Chuông, Túi đồ) để tạo hiệu ứng hover.
    /// Khi chuột đưa vào: Phóng to nhẹ và đung đưa.
    /// Khi chuột rời đi: Trở về trạng thái ban đầu.
    /// </summary>
    public class UIHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Animation Settings")]
        [Tooltip("Độ phóng to khi giữ chuột")]
        public float hoverMaxScale = 1.08f;
        [Tooltip("Góc lắc lư (độ)")]
        public float swingAngle = 3f;
        [Tooltip("Tốc độ lắc lư (giây/lượt)")]
        public float swingDuration = 7.5f;

        private Vector3 _originalScale;
        private Quaternion _originalRotation;
        private Tween _scaleTween;
        private Sequence _swingSequence;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _originalRotation = transform.localRotation;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Managers.BattleManager.IsInputBlocked) return;

            KillTweens();

            // Phóng to nhanh hơn (0.25s)
            _scaleTween = transform.DOScale(_originalScale * hoverMaxScale, 0.25f).SetEase(Ease.OutQuad);

            _swingSequence = DOTween.Sequence();
            
            // Lắc sang trái (0.2s)
            _swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, swingAngle), 0.2f).SetEase(Ease.OutQuad));
            // Lắc sang phải (0.4s)
            _swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -swingAngle), 0.4f).SetEase(Ease.InOutSine));
            // Về lại vị trí cân bằng (0.2s)
            _swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, 0), 0.2f).SetEase(Ease.InQuad));
            
            // Đợi 1.3 giây
            _swingSequence.AppendInterval(1.3f);
            
            _swingSequence.SetLoops(-1);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            KillTweens();

            // Trở về trạng thái gốc mượt mà
            _scaleTween = transform.DOScale(_originalScale, 0.4f).SetEase(Ease.OutQuad);
            transform.DOLocalRotateQuaternion(_originalRotation, 0.4f).SetEase(Ease.OutQuad);
        }

        private void OnDisable()
        {
            KillTweens();
            transform.localScale = _originalScale;
            transform.localRotation = _originalRotation;
        }

        private void KillTweens()
        {
            if (_scaleTween != null && _scaleTween.IsActive()) _scaleTween.Kill();
            if (_swingSequence != null && _swingSequence.IsActive()) _swingSequence.Kill();
            transform.DOKill();
        }
    }
}
