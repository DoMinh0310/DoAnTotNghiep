using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ProjectM.UI
{
    public class ButtonScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Settings")]
        [Tooltip("Độ phóng to khi rê chuột vào (1.1 = to hơn 10%)")]
        public float hoverScale = 1.1f;
        
        [Tooltip("Độ thu nhỏ khi click chuột (0.95 = nhỏ hơn 5%)")]
        public float clickScale = 0.95f;
        
        [Tooltip("Tốc độ phóng to/thu nhỏ (giây)")]
        public float animationDuration = 0.2f;

        private Vector3 originalScale;
        private bool isHovering = false;

        private void Awake()
        {
            // Lưu lại kích thước gốc lúc mới sinh ra
            originalScale = transform.localScale;
        }

        private void OnDisable()
        {
            // Khi Panel ẩn đi (hoặc nút bị disable), lập tức reset kích thước về gốc để tránh lỗi kẹt animation
            transform.DOKill();
            transform.localScale = originalScale;
            isHovering = false;
        }

        // KHI CHUỘT CHỈ VÀO
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            transform.DOKill();
            transform.DOScale(originalScale * hoverScale, animationDuration).SetEase(Ease.OutBack);
        }

        // KHI CHUỘT ĐI RA
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            transform.DOKill();
            transform.DOScale(originalScale, animationDuration).SetEase(Ease.OutQuad);
        }

        // KHI CLICK CHUỘT XUỐNG
        public void OnPointerDown(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(originalScale * clickScale, animationDuration / 2f).SetEase(Ease.OutQuad);
        }

        // KHI NHẢ CHUỘT RA
        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOKill();
            // Nếu nhả chuột ra mà vẫn đang trỏ vào nút thì phóng to lại (Hover), nếu đã lỡ kéo ra ngoài thì về gốc
            Vector3 targetScale = isHovering ? (originalScale * hoverScale) : originalScale;
            transform.DOScale(targetScale, animationDuration).SetEase(Ease.OutBack);
        }
    }
}
