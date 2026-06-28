using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectM.Cards;
using ProjectM.Skills;
using DG.Tweening;

namespace ProjectM.UI
{
    /// <summary>
    /// Gắn động vào card_prefab khi được sinh ra ở màn hình chọn tướng.
    /// Dùng để quản lý thao tác Click và hiệu ứng Scale/Glow khi được chọn.
    /// </summary>
    public class SelectableCharacterCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Animation")]
        public float selectedScale = 0.65f;
        public float normalScale = 0.6f;
        public float hoverScale = 0.62f;
        public float animDuration = 0.2f;

        private System.Action _onClick;
        private bool _isSelected;

        // Image viền sáng (nếu có thể lấy từ CardDisplay, hoặc spawn thêm 1 lớp viền)
        // Trong trường hợp dùng card_prefab gốc, ta chỉ cần dùng Scale là đủ xịn xò rồi!
        
        public void Setup(System.Action onClick)
        {
            _onClick = onClick;
            _isSelected = false;
            transform.localScale = Vector3.one * normalScale;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            float targetScale = selected ? selectedScale : normalScale;
            transform.DOScale(targetScale, animDuration).SetEase(Ease.OutBack);
        }

        public bool IsSelected => _isSelected;

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelected)
                transform.DOScale(hoverScale, animDuration).SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isSelected)
                transform.DOScale(normalScale, animDuration).SetEase(Ease.OutQuad);
        }
    }
}
