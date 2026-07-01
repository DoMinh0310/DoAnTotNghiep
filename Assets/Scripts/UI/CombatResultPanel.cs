using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using ProjectM.Managers;

namespace ProjectM.UI
{
    /// <summary>
    /// Hiển thị màn hình Victory/Loss.
    /// Có thể click đúp để tiếp tục (ra Map nếu thắng, về Menu nếu thua).
    /// </summary>
    public class CombatResultPanel : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        public CanvasGroup canvasGroup;
        
        [Tooltip("Kéo object chứa ảnh chữ Victory vào đây")]
        public GameObject victoryObject;
        
        [Tooltip("Kéo object chứa ảnh chữ Consumed vào đây")]
        public GameObject lossObject;

        [Tooltip("Kéo object chứa thông điệp Thank you for playing vào đây (hiện khi thắng boss cuối)")]
        public GameObject endGameObject;

        [Header("Stats UI (Only for Loss)")]
        public TMPro.TextMeshProUGUI enemiesKilledText;
        public TMPro.TextMeshProUGUI totalDamageText;
        public TMPro.TextMeshProUGUI goldEarnedText;
        public TMPro.TextMeshProUGUI goldSpentText;

        private bool _isVictory;
        private bool _isShown;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // Ẩn cả 3 chữ lúc ban đầu
            if (victoryObject != null) victoryObject.SetActive(false);
            if (lossObject != null) lossObject.SetActive(false);
            if (endGameObject != null) endGameObject.SetActive(false);
        }

        public void ShowVictory()
        {
            if (_isShown) return;
            _isShown = true;
            _isVictory = true;

            bool isFinalBoss = false;
            var runData = GameManager.Instance?.RunData;
            if (runData != null && runData.currentSlotIndex == 19)
            {
                isFinalBoss = true;
            }

            if (victoryObject != null) victoryObject.SetActive(true); // LUÔN HIỆN chữ Victory
            if (endGameObject != null) endGameObject.SetActive(isFinalBoss); // Hiện THÊM lời cảm ơn nếu là Boss
            if (lossObject != null) lossObject.SetActive(false);
            
            // Ẩn bảng thống kê khi Victory
            Transform statContainer = transform.Find("StatContainer");
            if (statContainer != null) statContainer.gameObject.SetActive(false);
            
            ShowPanel();
        }

        public void ShowLoss()
        {
            if (_isShown) return;
            _isShown = true;
            _isVictory = false;

            if (victoryObject != null) victoryObject.SetActive(false);
            if (endGameObject != null) endGameObject.SetActive(false);
            if (lossObject != null) lossObject.SetActive(true);
            
            // Hiện bảng thống kê khi Loss
            Transform statContainer = transform.Find("StatContainer");
            if (statContainer != null) statContainer.gameObject.SetActive(true);
            
            // Cập nhật text thống kê từ RunData
            var runData = GameManager.Instance?.RunData;
            if (runData != null)
            {
                // Label màu trắng, số màu riêng theo từng loại
                if (enemiesKilledText != null)
                    enemiesKilledText.text = $"Enemies Killed: <color=#AAAAAA>{runData.enemiesKilled}</color>";

                if (totalDamageText != null)
                    totalDamageText.text = $"Total Damage Dealt: <color=#E74C3C>{runData.totalDamageDealt}</color>";

                if (goldEarnedText != null)
                    goldEarnedText.text = $"Gold Earned: <color=#F1C40F>{runData.goldEarned}</color>";

                if (goldSpentText != null)
                    goldSpentText.text = $"Gold Spent: <color=#F1C40F>{runData.goldSpent}</color>";
            }

            ShowPanel();
        }

        private void ShowPanel()
        {
            if (canvasGroup == null) return;

            // Make sure it renders on top
            transform.SetAsLastSibling();
            
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1f, 1f).SetEase(Ease.InOutQuad);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isShown) return;

            if (eventData.clickCount == 2)
            {
                // Ngăn người chơi bấm đúp nhiều lần
                canvasGroup.interactable = false;

                if (_isVictory)
                {
                    var runData = GameManager.Instance?.RunData;
                    if (runData != null && runData.currentSlotIndex == 19)
                    {
                        // Thắng Boss cuối -> Hoàn thành Demo -> Xóa save và về Menu
                        GameManager.Instance?.DeleteSave();
                        GameManager.Instance?.LoadMenuScene();
                    }
                    else
                    {
                        // Thắng combat bình thường -> Về Map
                        GameManager.Instance?.OnCombatWon();
                    }
                }
                else
                {
                    // Thua -> Xóa save và về Menu để bắt đầu lại
                    GameManager.Instance?.DeleteSave();
                    GameManager.Instance?.LoadMenuScene();
                }
            }
        }
    }
}
