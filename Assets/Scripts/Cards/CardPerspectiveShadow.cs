using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.Cards
{
    /// <summary>
    /// Tạo hiệu ứng bóng đổ có chiều sâu (Perspective Shadow).
    /// Bóng của thẻ ở giữa sẽ đổ thẳng xuống, thẻ ở xa hai bên sẽ đổ lệch ra ngoài.
    /// Gắn script này vào Card_Prefab và Skill_Prefab.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CardPerspectiveShadow : MonoBehaviour
    {
        [Header("Shadow Settings")]
        [Tooltip("Màu của bóng (đen trong suốt)")]
        public Color shadowColor = new Color(0f, 0f, 0f, 0.65f);
        
        [Tooltip("Độ lệch xuống dưới mặc định của bóng")]
        public float baseYOffset = -20f; 
        
        [Tooltip("Giới hạn độ lệch X tối đa sang 2 bên")]
        public float maxShadowXOffset = 45f; 
        
        [Tooltip("Mức độ lệch của bóng so với khoảng cách từ tâm màn hình. Càng lớn bóng đổ càng mạnh.")]
        public float perspectiveMultiplier = 0.05f; 

        private Image shadowImage;
        private RectTransform shadowRect;
        private Canvas parentCanvas;

        private void Start()
        {
            parentCanvas = GetComponentInParent<Canvas>();

            // Tìm Image gốc để lấy hình dáng thẻ (Ưu tiên CardFrame, nếu không có lấy CardBack)
            Image sourceImage = null;
            Transform frame = transform.Find("CardFront/CardFrame");
            if (frame != null) sourceImage = frame.GetComponent<Image>();
            
            if (sourceImage == null)
            {
                Transform back = transform.Find("CardBack");
                if (back != null) sourceImage = back.GetComponent<Image>();
            }
            
            if (sourceImage == null) sourceImage = GetComponentInChildren<Image>();

            // Tạo GameObject làm bóng
            GameObject shadowObj = new GameObject("PerspectiveShadow");
            shadowRect = shadowObj.AddComponent<RectTransform>();
            
            // Gắn làm con của thẻ hiện tại và đẩy xuống dưới cùng (phía sau các layer khác)
            shadowObj.transform.SetParent(transform, false);
            shadowObj.transform.SetAsFirstSibling();

            // Khớp kích thước với thẻ
            RectTransform myRect = GetComponent<RectTransform>();
            shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRect.pivot = new Vector2(0.5f, 0.5f);
            shadowRect.sizeDelta = myRect.sizeDelta;

            // Cấu hình Image để vẽ bóng
            shadowImage = shadowObj.AddComponent<Image>();
            shadowImage.color = shadowColor;
            shadowImage.raycastTarget = false; // Xuyên click, không cản trở kéo thả
            
            if (sourceImage != null)
            {
                shadowImage.sprite = sourceImage.sprite;
                shadowImage.type = sourceImage.type;
            }
        }

        private void LateUpdate()
        {
            if (parentCanvas == null || shadowRect == null) return;

            // Tính vị trí X của thẻ trên màn hình
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, transform.position);
            
            // Tính độ lệch so với tâm màn hình (Center X)
            float screenCenterX = Screen.width / 2f;
            float distanceFromCenter = screenPos.x - screenCenterX;

            // Thẻ bên phải tâm (distance > 0) -> bóng lệch phải (X > 0)
            // Thẻ bên trái tâm (distance < 0) -> bóng lệch trái (X < 0)
            float targetXOffset = distanceFromCenter * perspectiveMultiplier;
            targetXOffset = Mathf.Clamp(targetXOffset, -maxShadowXOffset, maxShadowXOffset);

            // Cập nhật vị trí bóng (kết hợp X lệch và Y cố định xuống dưới)
            shadowRect.anchoredPosition = new Vector2(targetXOffset, baseYOffset);
        }
    }
}
