using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using DG.Tweening;

namespace ProjectM.Map
{
    /// <summary>
    /// Điều khiển Panel UI cho Event nhận thưởng thẻ (Card Reward).
    /// Gắn script này vào GameObject "CardRewardPanel" trong Unity.
    /// </summary>
    public class CardRewardPanel : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════
        // Inspector References
        // ════════════════════════════════════════════════════════════════

        [Header("Layout")]
        [Tooltip("Kéo Container GameObject chứa các nút thẻ vào đây (HorizontalLayoutGroup)")]
        public Transform cardChoiceContainer;

        [Tooltip("Kéo Prefab nút thẻ (Card_Prefab) vào đây")]
        public GameObject cardChoicePrefab;

        [Tooltip("Scale của thẻ khi sinh ra (nếu 1.0 quá to, chỉnh về 0.6)")]
        public float cardSpawnScale = 0.6f;

        [Header("Fly Animation")]
        [Tooltip("Kéo icon/vị trí cái túi đồ của bạn vào đây (để thẻ bài bay về đó)")]
        public RectTransform bagIconTarget;
        
        [Tooltip("Scale của thẻ trong lúc bay (vd: 0.4)")]
        public float flyingScale = 0.4f;

        [Tooltip("Thời gian thẻ bay vào túi (giây)")]
        public float flyDuration = 0.5f;

        [Header("UI Elements")]
        [Tooltip("Text hiển thị tiêu đề (VD: 'Chọn 1 thẻ để thêm vào bộ bài')")]
        public TextMeshProUGUI titleText;

        [Tooltip("Nút 'Quay lại' (để tạm ẩn panel và xem Map)")]
        public Button peekMapButton;

        [Header("Animation")]
        [Tooltip("Thời gian panel fade-in (giây)")]
        public float fadeInDuration = 0.3f;

        [Header("Card Layout")]
        [Tooltip("Khoảng cách giữa các thẻ theo chiều ngang")]
        public float cardSpacing = 400f;
        
        [Tooltip("Độ nghiêng của 2 thẻ ở 2 bên (độ)")]
        public float fanAngle = 15f;

        [Tooltip("Độ nhô cao của thẻ giữa so với thẻ 2 bên")]
        public float yOffsetAmount = 30f;

        // ── Private ────────────────────────────────────────────────────
        private CanvasGroup _canvasGroup;
        private List<ScriptableObject> _currentChoices = new();
        private System.Action<ScriptableObject> _onCardChosen;  // Callback khi người chơi chọn 1 thẻ
        private System.Action _onPeekMap;               // Callback khi bấm xem lén Map (nếu cần)

        // ════════════════════════════════════════════════════════════════
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Ẩn panel ngay từ đầu
            gameObject.SetActive(false);
        }

        // ════════════════════════════════════════════════════════════════
        // PUBLIC API — Gọi từ EventManager
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Mở Panel, điền thẻ vào và chờ người chơi chọn.
        /// </summary>
        /// <param name="choices">Danh sách thẻ để người chơi lựa chọn.</param>
        /// <param name="allowPeekMap">Cho phép bấm nút để xem lén Map.</param>
        /// <param name="onCardChosen">Callback khi người chơi chọn 1 thẻ.</param>
        public void Show(List<ScriptableObject> choices, bool allowPeekMap,
                         System.Action<ScriptableObject> onCardChosen)
        {
            _currentChoices = choices;
            _onCardChosen   = onCardChosen;

            // Cập nhật tiêu đề
            if (titleText != null)
                titleText.text = "Pick a new item!";

            // Hiện/ẩn nút Xem lén Map
            if (peekMapButton != null)
            {
                peekMapButton.gameObject.SetActive(allowPeekMap);
                peekMapButton.onClick.RemoveAllListeners();
                peekMapButton.onClick.AddListener(OnPeekMapClicked);
            }

            // Xóa các nút thẻ cũ (nếu còn từ lần mở trước)
            foreach (Transform child in cardChoiceContainer)
                Destroy(child.gameObject);

            // Tạo nút cho từng thẻ với góc xoay xòe quạt
            for (int i = 0; i < choices.Count; i++)
            {
                SpawnCardChoiceButton(choices[i], i, choices.Count);
            }

            // Hiện panel với animation
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Ẩn Panel sau khi người chơi đã chọn.
        /// </summary>
        public void Hide()
        {
            StartCoroutine(FadeOutAndDeactivate());
        }

        // ════════════════════════════════════════════════════════════════
        // Private Methods
        // ════════════════════════════════════════════════════════════════

        private void SpawnCardChoiceButton(ScriptableObject cardObj, int index, int totalCards)
        {
            if (cardChoicePrefab == null || cardChoiceContainer == null)
            {
                Debug.LogError("[CardRewardPanel] Chưa gán Card_Prefab hoặc cardChoiceContainer trong Inspector!");
                return;
            }

            // Tạo Prefab thẻ của bạn
            var go = Instantiate(cardChoicePrefab, cardChoiceContainer);

            // Lấy CardDisplay để hiển thị data
            var display = go.GetComponentInChildren<CardDisplay>();
            if (display != null)
            {
                if (cardObj is CardData cd)
                {
                    display.cardData = cd;
                    display.LoadData(cd);
                }
                else if (cardObj is ProjectM.Skills.SkillData sd)
                {
                    display.LoadSkillData(sd);
                }
            }
            else
            {
                Debug.LogError("[CardRewardPanel] Prefab không có script CardDisplay!");
            }

            // Thêm component xử lý Animation (Hover, Fly)
            var interactor = go.AddComponent<RewardCardInteraction>();

            // Tính toán góc xòe quạt và vị trí thủ công để tránh lỗi LayoutGroup
            float zRot = 0f;
            float xOffset = 0f;
            float yOffset = 0f;

            if (totalCards == 3)
            {
                if (index == 0) { zRot = fanAngle; xOffset = -cardSpacing; yOffset = -yOffsetAmount; }
                else if (index == 1) { zRot = 0f; xOffset = 0f; yOffset = yOffsetAmount; } // Thẻ giữa hơi nhô cao
                else if (index == 2) { zRot = -fanAngle; xOffset = cardSpacing; yOffset = -yOffsetAmount; }
            }

            // Gọi Setup để chạy Anim hiện ra và thiết lập vị trí
            interactor.Setup(cardSpawnScale, zRot, xOffset, yOffset);

            // Gắn Button vào thẻ để click được (nếu chưa có)
            var btn = go.GetComponent<Button>();
            if (btn == null)
                btn = go.AddComponent<Button>();

            // Khi người chơi click vào lá bài này
            btn.onClick.AddListener(() => OnCardClicked(cardObj, interactor));
        }

        private void OnCardClicked(ScriptableObject chosen, RewardCardInteraction interactor)
        {
            // Thêm thẻ vào Deck trong RunData
            if (GameManager.Instance?.RunData != null)
            {
                var runData = GameManager.Instance.RunData;

                // 1. Lưu ID vào RunData (cho Save/Load)
                string itemID = chosen.name;
                runData.playerDeckIDs.Add(itemID);

                // 2. Sync vào championSetup.supportDeck trong RAM
                // (Inventory và SkillHandManager đọc từ đây, cho phép lấy trùng thẻ có sẵn)
                if (runData.championSetup != null && chosen is ProjectM.Skills.SkillData sd)
                {
                    runData.championSetup.supportDeck.Add(sd);
                    Debug.Log($"[CardReward] Synced '{sd.skillName}' vào championSetup.supportDeck " +
                              $"(total: {runData.championSetup.supportDeck.Count}).");
                }

                GameManager.Instance.SaveGame();
                Debug.Log($"[CardReward] Đã thêm thẻ '{itemID}' vào Deck (playerDeckIDs count: {runData.playerDeckIDs.Count}).");
            }
            else
            {
                Debug.LogWarning("[CardRewardPanel] GameManager hoặc RunData là null! Thẻ không được lưu.");
            }


            // Vô hiệu hóa raycast của Panel để người chơi không click thêm thẻ khác
            _canvasGroup.blocksRaycasts = false;

            // Nếu chưa cấu hình túi đồ thì đóng luôn
            if (bagIconTarget == null)
            {
                Debug.LogWarning("[CardRewardPanel] Chưa gán bagIconTarget! Thẻ sẽ không bay mà đóng Panel luôn.");
                _onCardChosen?.Invoke(chosen);
                Hide();
            }
            else
            {
                // Cho thẻ bay về túi, bay xong mới Hide Panel
                interactor.AnimateFlyToBag(bagIconTarget, flyingScale, flyDuration, () => 
                {
                    _onCardChosen?.Invoke(chosen);
                    Hide();
                });

                // Các thẻ còn lại mờ dần biến mất
                foreach (Transform child in cardChoiceContainer)
                {
                    if (child.gameObject != interactor.gameObject)
                    {
                        var cg = child.gameObject.GetComponent<CanvasGroup>();
                        if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
                        cg.DOFade(0f, 0.3f);
                    }
                }
            }
        }

        private void OnPeekMapClicked()
        {
            Debug.Log("[CardReward] Tạm ẩn Panel để xem Map.");
            _onPeekMap?.Invoke();
            // Tạm ẩn bằng cách giảm Alpha và tắt chặn chuột
            StartCoroutine(FadeTo(0f, false));
            
            // Báo cho MapManager biết là Event vẫn đang dở dang
            if (MapManager.Instance != null)
            {
                MapManager.Instance.SetEventInProgress(true, Reopen);
            }
        }

        /// <summary>
        /// Được MapManager gọi khi người chơi bấm lại vào Node hiện tại trên Map
        /// </summary>
        public void Reopen()
        {
            Debug.Log("[CardReward] Mở lại Panel.");
            gameObject.SetActive(true);
            StartCoroutine(FadeTo(1f, true));
            
            if (MapManager.Instance != null)
            {
                // Giữ nguyên trạng thái khóa Map vì Event vẫn đang diễn ra
                MapManager.Instance.SetEventInProgress(true, Reopen);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Animations
        // ════════════════════════════════════════════════════════════════

        private IEnumerator FadeIn()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndDeactivate()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            float t = fadeInDuration;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeTo(float targetAlpha, bool blocksRaycasts)
        {
            _canvasGroup.interactable = blocksRaycasts;
            _canvasGroup.blocksRaycasts = blocksRaycasts;
            float startAlpha = _canvasGroup.alpha;
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = targetAlpha;
        }
    }
}
