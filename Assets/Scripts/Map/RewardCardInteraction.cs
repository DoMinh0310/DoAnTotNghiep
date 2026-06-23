using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ProjectM.Map
{
    /// <summary>
    /// Xử lý animation cho thẻ bài khi xuất hiện ở Event: xoè quạt, hover focus, và bay vào túi đồ.
    /// </summary>
    public class RewardCardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private float _baseScale;
        private float _baseZRotation;
        private Vector2 _basePosition;
        private int _originalSiblingIndex;
        private bool _isChosen = false;

        /// <summary>
        /// Khởi tạo thông số cho animation xoè quạt
        /// </summary>
        public void Setup(float scale, float zRotation, float xOffset, float yOffset)
        {
            _baseScale = scale;
            _baseZRotation = zRotation;
            _originalSiblingIndex = transform.GetSiblingIndex();

            var rt = GetComponent<RectTransform>();
            _basePosition = new Vector2(xOffset, yOffset);
            rt.anchoredPosition = _basePosition;

            // Đặt kích thước ban đầu = 0 để làm hiệu ứng pop-up
            transform.localScale = Vector3.zero;
            
            // Xoay thẻ theo góc (trái, giữa, phải)
            transform.localEulerAngles = new Vector3(0, 0, _baseZRotation);

            // Animation Pop-up hiện ra
            transform.DOScale(_baseScale, 0.4f).SetEase(Ease.OutBack);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isChosen) return;
            
            // Hủy các animation cũ đang chạy để tránh xung đột
            transform.DOKill();

            // Lưu lại index gốc trước khi đẩy lên trên cùng
            _originalSiblingIndex = transform.GetSiblingIndex();

            // Phóng to lên một chút, làm thẳng thẻ lại và hơi nhấc lên trên
            transform.DOScale(_baseScale * 1.15f, 0.2f).SetEase(Ease.OutQuad);
            transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
            GetComponent<RectTransform>().DOAnchorPosY(_basePosition.y + 30f, 0.2f).SetEase(Ease.OutQuad);

            // Đưa thẻ lên lớp trên cùng để không bị thẻ khác đè lên
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isChosen) return;

            transform.DOKill();

            // Trả về kích thước, góc xoay, và vị trí ban đầu
            transform.DOScale(_baseScale, 0.2f).SetEase(Ease.OutQuad);
            transform.DOLocalRotate(new Vector3(0, 0, _baseZRotation), 0.2f).SetEase(Ease.OutQuad);
            GetComponent<RectTransform>().DOAnchorPosY(_basePosition.y, 0.2f).SetEase(Ease.OutQuad);

            // Trả về đúng vị trí layer cũ để không bị lộn xộn thứ tự
            transform.SetSiblingIndex(_originalSiblingIndex);
        }

        /// <summary>
        /// Gọi khi người chơi click chọn lá bài này
        /// </summary>
        public void AnimateFlyToBag(RectTransform bagIcon, float targetScale, float flyDuration, System.Action onComplete)
        {
            _isChosen = true;
            transform.DOKill();

            // Nhấc thẻ ra khỏi Layout Group (đưa ra Canvas ngoài cùng) để có thể bay tự do không bị ép vị trí
            Canvas canvas = GetComponentInParent<Canvas>();
            transform.SetParent(canvas.transform, true);

            // Bay vòng cung (Jump) đến vị trí túi đồ
            Vector3 targetPos = bagIcon.position;
            float jumpHeight = 200f; // Độ cao của vòng cung bay

            // Hiệu ứng 1: Xoay đúng 1 nửa vòng ngược chiều kim đồng hồ (LocalAxisAdd để cộng thêm vào góc hiện tại)
            transform.DOLocalRotate(new Vector3(0, 0, 180), flyDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad);
            
            // Hiệu ứng 2: Thu nhỏ dần lại về 0 (biến mất hoàn toàn khi bay xong)
            transform.DOScale(0f, flyDuration).SetEase(Ease.InCubic);

            // Hiệu ứng 3: Bay vòng cung
            transform.DOJump(targetPos, jumpHeight, 1, flyDuration).SetEase(Ease.InCubic).OnComplete(() =>
            {
                // Khi bay xong thì gọi callback (để đóng Panel) và tự hủy
                onComplete?.Invoke();
                Destroy(gameObject);
            });
        }
    }
}
