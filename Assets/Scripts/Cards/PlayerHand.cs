using UnityEngine;
using DG.Tweening;

namespace ProjectM.Cards
{
    public class PlayerHand : MonoBehaviour
    {
        [Header("Fanning Settings")]
        public float cardSpacing = 120f;
        [Tooltip("Độ nghiêng mỗi thẻ. Số âm để xòe đều ra hai bên theo kiểu quạt chuẩn.")]
        public float rotationPerCard = -5f;
        [Tooltip("Độ chúi xuống của các thẻ ở viền để tạo hình vòng cung cong")]
        public float heightArc = 15f;
        [Tooltip("Thời gian DOTween animation xếp lại bài (giây)")]
        public float layoutDuration = 0.25f;

        [Header("Scale Settings")]
        [Tooltip("Tỷ lệ thu nhỏ của bài khi nằm trên tay")]
        public float cardScale = 0.6f;

        // Theo dõi số thẻ lần trước để chỉ re-tween khi có thay đổi
        private int _lastChildCount = -1;

        void Update()
        {
            int current = transform.childCount;
            if (current != _lastChildCount)
            {
                _lastChildCount = current;
                UpdateHandLayout();
            }
        }

        public void UpdateHandLayout()
        {
            int childCount = transform.childCount;
            if (childCount == 0) return;

            float centerIndex = (childCount - 1) / 2f;

            for (int i = 0; i < childCount; i++)
            {
                Transform card = transform.GetChild(i);
                float offset = i - centerIndex;

                Vector2    targetPos   = new Vector2(offset * cardSpacing, -Mathf.Pow(offset, 2) * heightArc);
                Quaternion targetRot   = Quaternion.Euler(0, 0, offset * rotationPerCard);
                Vector3    targetScale = new Vector3(cardScale, cardScale, 1f);

                CardHoverHandler hover = card.GetComponent<CardHoverHandler>();
                if (hover != null)
                {
                    // Luôn cập nhật base state để hover biết đường về nhà
                    hover.UpdateBaseState(targetPos, targetRot, targetScale);

                    // Nếu đang hover → để CardHoverHandler tự lo, không can thiệp
                    if (hover.IsHovering) continue;
                }

                RectTransform rect = card.GetComponent<RectTransform>();
                if (rect == null) continue;

                if (!Application.isPlaying)
                {
                    // Edit Mode: snap ngay, không tween
                    rect.anchoredPosition = targetPos;
                    rect.localRotation    = targetRot;
                    rect.localScale       = targetScale;
                    continue;
                }

                // Play Mode: tween mượt về vị trí đúng
                // Kill tween cũ trên cùng object trước khi tạo mới (tránh conflict)
                int id = card.GetHashCode();
                DOTween.Kill($"hp{id}");
                DOTween.Kill($"hr{id}");
                DOTween.Kill($"hs{id}");

                rect.DOAnchorPos(targetPos, layoutDuration)
                    .SetEase(Ease.OutBack)
                    .SetId($"hp{id}");

                card.DOLocalRotateQuaternion(targetRot, layoutDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId($"hr{id}");

                card.DOScale(targetScale, layoutDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId($"hs{id}");
            }
        }
    }
}
