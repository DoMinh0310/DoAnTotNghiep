using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// Quản lý hiển thị bảng mô tả chi tiết (Tooltip) khi người chơi hover vào thẻ hoặc đồ vật.
    /// Gắn script này vào một GameObject chứa Panel Tooltip trong Canvas.
    /// </summary>
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        [Header("UI References")]
        public RectTransform tooltipPanel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI mainDescriptionText;
        public TextMeshProUGUI keywordExplanationText; // Phần mô tả các nguyên tố (Bleed, Frost...)
        public LayoutElement layoutElement;

        [Header("Settings")]
        public int characterWrapLimit = 40;
        public Vector2 offset = new Vector2(20f, -20f);

        private RectTransform _canvasRectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRectTransform = _canvas.GetComponent<RectTransform>();
            }

            if (tooltipPanel != null)
            {
                tooltipPanel.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (tooltipPanel != null && tooltipPanel.gameObject.activeSelf)
            {
                UpdatePosition();
            }
        }

        public void ShowTooltip(string title, string mainDesc, string keywordDesc = "")
        {
            if (tooltipPanel == null) return;

            // Set text
            if (titleText != null) titleText.text = title;
            if (mainDescriptionText != null) mainDescriptionText.text = mainDesc;
            
            if (keywordExplanationText != null)
            {
                keywordExplanationText.text = keywordDesc;
                keywordExplanationText.gameObject.SetActive(!string.IsNullOrEmpty(keywordDesc));
            }

            // Bật layout element (Wrap text nếu quá dài)
            if (layoutElement != null)
            {
                int titleLength = titleText != null ? titleText.text.Length : 0;
                int descLength = mainDescriptionText != null ? mainDescriptionText.text.Length : 0;
                int keywordLength = keywordExplanationText != null ? keywordExplanationText.text.Length : 0;

                layoutElement.enabled = (titleLength > characterWrapLimit || descLength > characterWrapLimit || keywordLength > characterWrapLimit);
            }

            // Ép Anchor và Pivot về Top-Left để tránh lỗi Panel bị Stretch (kéo dãn) bay ra khỏi màn hình
            tooltipPanel.anchorMin = new Vector2(0, 1);
            tooltipPanel.anchorMax = new Vector2(0, 1);
            tooltipPanel.pivot = new Vector2(0, 1);

            // Đưa panel lên trên cùng để không bị thẻ bài đè lên
            tooltipPanel.SetAsLastSibling();
            UpdatePosition();
            tooltipPanel.gameObject.SetActive(true);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.gameObject.SetActive(false);
            }
        }

        private void UpdatePosition()
        {
            if (_canvas == null || _canvasRectTransform == null) return;

            Vector2 anchoredPosition;
            Vector2 mousePos;
#if ENABLE_INPUT_SYSTEM
            mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            // Fallback for old input system or if Both are enabled but the user uses new Input System anyway
            try {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            } catch {
                mousePos = Input.mousePosition;
            }
#endif

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                mousePos,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out anchoredPosition);

            // Cộng thêm khoảng cách để chuột không bị đè lên bảng
            anchoredPosition += offset;

            // Đảm bảo tooltip không bị tràn ra khỏi màn hình
            float rightEdgeToScreenEdgeDistance = _canvasRectTransform.rect.width * 0.5f - (anchoredPosition.x + tooltipPanel.rect.width * tooltipPanel.pivot.x);
            if (rightEdgeToScreenEdgeDistance < 0)
            {
                anchoredPosition.x += rightEdgeToScreenEdgeDistance;
            }

            float leftEdgeToScreenEdgeDistance = 0 - (anchoredPosition.x - tooltipPanel.rect.width * tooltipPanel.pivot.x) + _canvasRectTransform.rect.width * -0.5f;
            if (leftEdgeToScreenEdgeDistance > 0)
            {
                anchoredPosition.x += leftEdgeToScreenEdgeDistance;
            }

            float topEdgeToScreenEdgeDistance = _canvasRectTransform.rect.height * 0.5f - (anchoredPosition.y + tooltipPanel.rect.height * tooltipPanel.pivot.y);
            if (topEdgeToScreenEdgeDistance < 0)
            {
                anchoredPosition.y += topEdgeToScreenEdgeDistance;
            }

            float bottomEdgeToScreenEdgeDistance = 0 - (anchoredPosition.y - tooltipPanel.rect.height * tooltipPanel.pivot.y) + _canvasRectTransform.rect.height * -0.5f;
            if (bottomEdgeToScreenEdgeDistance > 0)
            {
                anchoredPosition.y += bottomEdgeToScreenEdgeDistance;
            }

            tooltipPanel.anchoredPosition = anchoredPosition;
        }
    }
}
