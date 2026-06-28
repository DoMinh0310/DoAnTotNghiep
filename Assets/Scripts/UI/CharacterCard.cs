using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectM.Cards;
using DG.Tweening;

namespace ProjectM.UI
{
    /// <summary>
    /// Đại diện cho 1 ô nhân vật trong màn hình chọn nhân vật.
    /// Gắn script này lên từng card object.
    /// </summary>
    public class CharacterCard : MonoBehaviour
    {
        [Header("UI References")]
        public Image characterPortrait;
        public TextMeshProUGUI championNameText;
        public Image cardBorder;            // Image viền của card
        public Button selectButton;

        [Header("Border Colors")]
        public Color normalBorderColor   = new Color(0.4f, 0.4f, 0.4f, 1f);  // Xám
        public Color selectedBorderColor = new Color(1f, 0.85f, 0.1f, 1f);   // Vàng sáng

        [Header("Scale Animation")]
        public float selectedScale  = 1.08f;
        public float normalScale    = 1.0f;
        public float animDuration   = 0.2f;

        private System.Action _onClick;
        private bool _isSelected;

        public void Setup(CardData data, System.Action onClick)
        {
            _onClick = onClick;
            _isSelected = false;

            if (data != null)
            {
                if (characterPortrait != null && data.characterArt != null)
                    characterPortrait.sprite = data.characterArt;

                if (championNameText != null)
                    championNameText.text = data.cardName;
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => _onClick?.Invoke());
            }

            // Đặt về trạng thái bình thường
            if (cardBorder != null) cardBorder.color = normalBorderColor;
            transform.localScale = Vector3.one * normalScale;
        }

        /// <summary>
        /// Bật/tắt trạng thái được chọn của card.
        /// Đổi màu viền và scale để phân biệt rõ.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (cardBorder != null)
            {
                cardBorder.DOColor(selected ? selectedBorderColor : normalBorderColor, animDuration);
            }

            float targetScale = selected ? selectedScale : normalScale;
            transform.DOScale(targetScale, animDuration).SetEase(Ease.OutBack);
        }

        public bool IsSelected => _isSelected;
    }
}
