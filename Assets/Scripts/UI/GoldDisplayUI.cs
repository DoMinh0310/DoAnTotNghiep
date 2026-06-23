using UnityEngine;
using TMPro;
using DG.Tweening;
using ProjectM.Managers;
using ProjectM.Inventory;

namespace ProjectM.UI
{
    /// <summary>
    /// Hiển thị số tiền (Vàng) hiện tại của người chơi.
    /// Script này sẽ tự động cập nhật text mỗi frame và tự ẩn đi khi mở túi đồ.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class GoldDisplayUI : MonoBehaviour
    {
        [Tooltip("Kéo TextMeshProUGUI dùng để hiển thị số vàng vào đây")]
        public TextMeshProUGUI goldText;

        [Tooltip("Tiền tố chữ đằng trước (nếu có, VD: 'x ')")]
        public string prefix = "";

        [Header("Animation Settings")]
        [Tooltip("Màu hiển thị của số tiền cộng thêm")]
        public string addedColorHex = "#FFFF00";
        [Tooltip("Thời gian chạy đếm số khi cộng vàng")]
        public float countDuration = 1.0f;
        [Tooltip("Transform để làm hiệu ứng phóng to/thu nhỏ (Mặc định là cả cụm Currency)")]
        public Transform scaleTarget;

        private int _visualGold = -1;
        private int _lastActualGold = -1;
        private CanvasGroup _canvasGroup;
        private Coroutine _countCoroutine;
        private Tweener _pulseTween;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (scaleTarget == null) scaleTarget = transform;
        }

        private void OnEnable()
        {
            InventoryManager.OnInventoryToggled += HandleInventoryToggled;
        }

        private void OnDisable()
        {
            InventoryManager.OnInventoryToggled -= HandleInventoryToggled;
            StopPulse();
        }

        private void HandleInventoryToggled(bool isInventoryOpen)
        {
            float targetAlpha = isInventoryOpen ? 0f : 1f;
            _canvasGroup.DOFade(targetAlpha, 0.2f).SetEase(Ease.InOutSine);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.RunData != null)
            {
                int currentGold = GameManager.Instance.RunData.gold;
                
                if (_visualGold == -1)
                {
                    // Lần đầu tiên set up
                    _visualGold = currentGold;
                    _lastActualGold = currentGold;
                    UpdateText(_visualGold);
                }
                else if (currentGold != _lastActualGold)
                {
                    if (currentGold > _lastActualGold)
                    {
                        // Được cộng vàng -> Chạy animation
                        if (_countCoroutine != null) StopCoroutine(_countCoroutine);
                        _countCoroutine = StartCoroutine(AnimateGoldCount(currentGold));
                    }
                    else
                    {
                        // Bị trừ vàng (mua đồ, v.v) -> Có thể xử lý tương tự sau, tạm thời skip màu vàng
                        _visualGold = currentGold;
                        UpdateText(_visualGold);
                        if (_countCoroutine != null) StopCoroutine(_countCoroutine);
                        StopPulse();
                    }
                    _lastActualGold = currentGold;
                }
            }
            else
            {
                if (_visualGold != 0 && goldText != null)
                {
                    _visualGold = 0;
                    _lastActualGold = 0;
                    UpdateText(0);
                }
            }
        }

        private System.Collections.IEnumerator AnimateGoldCount(int targetGold)
        {
            int startBase = _visualGold;
            float elapsed = 0f;

            // Bật hiệu ứng đập nhịp tim (Ping Pong Scale)
            if (_pulseTween == null || !_pulseTween.IsActive())
            {
                _pulseTween = scaleTarget.DOScale(1.18f, 0.12f)
                                         .SetLoops(-1, LoopType.Yoyo)
                                         .SetEase(Ease.InOutSine);
            }

            while (elapsed < countDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / countDuration);
                
                // Nội suy vàng gốc từ start đến target
                _visualGold = Mathf.RoundToInt(Mathf.Lerp(startBase, targetGold, t));
                
                // Số vàng còn thừa (chưa đếm xong)
                int currentAdded = targetGold - _visualGold;

                if (currentAdded > 0)
                {
                    goldText.text = $"{prefix}{_visualGold} <color={addedColorHex}>+{currentAdded}</color>";
                }
                else
                {
                    UpdateText(_visualGold);
                }

                yield return null;
            }

            // Đảm bảo chính xác con số cuối
            _visualGold = targetGold;
            UpdateText(_visualGold);

            StopPulse();
        }

        private void StopPulse()
        {
            if (_pulseTween != null)
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }
            if (scaleTarget != null)
            {
                scaleTarget.DOScale(1f, 0.15f); // Trả về size gốc mượt mà
            }
        }

        private void UpdateText(int amount)
        {
            if (goldText != null)
                goldText.text = prefix + amount.ToString();
        }
    }
}

