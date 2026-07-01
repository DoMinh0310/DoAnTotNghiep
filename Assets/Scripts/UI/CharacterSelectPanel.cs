using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ProjectM.Cards;
using ProjectM.Skills;
using DG.Tweening;

namespace ProjectM.UI
{
    /// <summary>
    /// Panel chọn 2 trong 4 nhân vật khởi đầu.
    /// Dùng trực tiếp card_prefab của hệ thống chiến đấu.
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        public CanvasGroup canvasGroup;

        [Header("Prefabs & Containers")]
        public GameObject cardPrefab;
        public Transform characterGrid;

        [Header("Let's Go Button")]
        public Button letsGoButton;
        public CanvasGroup letsGoCanvasGroup;

        [Header("Back Button")]
        [Tooltip("Nút quay về Main Menu")]
        public Button backButton;

        [Header("Background Click Area")]
        [Tooltip("Kéo cái Image nền đen (root của panel) vào đây để click vùng trống = bỏ chọn hết")]
        public Button bgClickArea;

        [Header("Settings")]
        [Tooltip("Số nhân vật tối đa được chọn")]
        public int maxSelection = 2;

        [Header("Champion Setup Nguồn (Scriptable Object)")]
        [Tooltip("Kéo file Starter Deck_1 (ChampionSetup) vào đây. Tất cả tướng trong file này sẽ hiện ra để chọn.")]
        public ChampionSetup baseStarterData;

        private List<int> _selectedIndices = new List<int>();
        private List<SelectableCharacterCard> _spawnedCards = new List<SelectableCharacterCard>();

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            SetPanelVisible(false, instant: true);

            if (letsGoCanvasGroup != null)
            {
                letsGoCanvasGroup.alpha = 0f;
                letsGoCanvasGroup.interactable = false;
                letsGoCanvasGroup.blocksRaycasts = false;
            }
            if (letsGoButton != null)
                letsGoButton.onClick.AddListener(OnLetsGoClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            // Click vùng trống (bg) → bỏ chọn tất cả, cho chọn lại
            if (bgClickArea != null)
                bgClickArea.onClick.AddListener(OnBackgroundClicked);
        }

        public void Open()
        {
            _selectedIndices.Clear();
            ClearSpawnedCards();

            // Sinh card từ prefab
            if (baseStarterData != null && baseStarterData.champions != null)
            {
                for (int i = 0; i < baseStarterData.champions.Count; i++)
                {
                    if (cardPrefab == null || characterGrid == null) break;

                    GameObject cardObj = Instantiate(cardPrefab, characterGrid);
                    
                    // Lấy CardDisplay để hiển thị data tướng
                    CardDisplay display = cardObj.GetComponentInChildren<CardDisplay>();
                    if (display != null && baseStarterData.champions[i] != null)
                    {
                        display.LoadData(baseStarterData.champions[i].championData);
                    }

                    // Gắn component tương tác
                    SelectableCharacterCard selectable = cardObj.AddComponent<SelectableCharacterCard>();
                    int capturedIndex = i;
                    selectable.Setup(() => OnCardClicked(capturedIndex));
                    
                    // --- ĐỔ BÓNG TỪ TRÁI SANG PHẢI CHỈ RIÊNG CHO MENU ---
                    // Tìm component Shadow của thẻ bài (hoặc background của thẻ)
                    Shadow[] shadows = cardObj.GetComponentsInChildren<Shadow>(true);
                    foreach(Shadow shadow in shadows)
                    {
                        // Giữ nguyên trục Y (đổ xuống dưới), đổi trục X sang dương (đổ từ trái sang phải)
                        Vector2 currentDist = shadow.effectDistance;
                        shadow.effectDistance = new Vector2(Mathf.Abs(currentDist.x), currentDist.y);
                    }

                    // --- VÔ HIỆU HÓA KÉO THẢ (CHỈ CHO CHỌN) ---
                    CardDragHandler dragHandler = cardObj.GetComponentInChildren<CardDragHandler>(true);
                    if (dragHandler != null) dragHandler.enabled = false;

                    _spawnedCards.Add(selectable);
                }
            }

            if (letsGoCanvasGroup != null)
            {
                letsGoCanvasGroup.alpha = 0f;
                letsGoCanvasGroup.interactable = false;
                letsGoCanvasGroup.blocksRaycasts = false;
            }

            SetPanelVisible(true, instant: false);
        }

        private void ClearSpawnedCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
        }

        private void OnCardClicked(int index)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardSelectClip);
            
            if (_selectedIndices.Contains(index))
            {
                _selectedIndices.Remove(index);
                _spawnedCards[index].SetSelected(false);
            }
            else
            {
                if (_selectedIndices.Count >= maxSelection)
                {
                    int oldest = _selectedIndices[0];
                    _selectedIndices.RemoveAt(0);
                    _spawnedCards[oldest].SetSelected(false);
                }
                _selectedIndices.Add(index);
                _spawnedCards[index].SetSelected(true);
            }

            RefreshLetsGoButton();
        }

        private void RefreshLetsGoButton()
        {
            bool ready = _selectedIndices.Count == maxSelection;
            if (letsGoCanvasGroup == null) return;

            if (ready && letsGoCanvasGroup.alpha < 0.5f)
            {
                letsGoCanvasGroup.blocksRaycasts = true;
                letsGoCanvasGroup.interactable = true;
                letsGoCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            }
            else if (!ready)
            {
                letsGoCanvasGroup.interactable = false;
                letsGoCanvasGroup.blocksRaycasts = false;
                letsGoCanvasGroup.DOFade(0f, 0.2f);
            }
        }

        private void OnLetsGoClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.genericButtonClip);
            if (_selectedIndices.Count < maxSelection || baseStarterData == null) return;

            var setup = ScriptableObject.CreateInstance<ChampionSetup>();
            setup.champions = new List<ChampionEntry>();

            foreach (int idx in _selectedIndices)
            {
                if (idx < baseStarterData.champions.Count && baseStarterData.champions[idx] != null)
                {
                    // Copy nguyên si cả CardData và Trinket/Relic khởi đầu
                    setup.champions.Add(new ChampionEntry
                    {
                        championData = baseStarterData.champions[idx].championData,
                        equippedRelic = baseStarterData.champions[idx].equippedRelic,
                        equippedTrinket = baseStarterData.champions[idx].equippedTrinket
                    });
                }
            }

            // Lưu các tướng chưa được chọn vào unchosenChampions
            setup.unchosenChampions = new List<ChampionEntry>();
            for (int i = 0; i < baseStarterData.champions.Count; i++)
            {
                if (!_selectedIndices.Contains(i) && baseStarterData.champions[i] != null)
                {
                    setup.unchosenChampions.Add(new ChampionEntry
                    {
                        championData = baseStarterData.champions[i].championData,
                        equippedRelic = baseStarterData.champions[i].equippedRelic,
                        equippedTrinket = baseStarterData.champions[i].equippedTrinket
                    });
                }
            }

            // Copy toàn bộ Support Deck và thư viện ngọc sở hữu từ cục gốc
            setup.supportDeck = new List<SkillData>(baseStarterData.supportDeck);
            setup.ownedRelics = new List<RelicData>(baseStarterData.ownedRelics);
            setup.ownedTrinkets = new List<TrinketData>(baseStarterData.ownedTrinkets);

            SetPanelVisible(false, instant: false, onComplete: () =>
            {
                ProjectM.GameManager.Instance?.StartNewRun(setup);
            });
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.genericButtonClip);
            SetPanelVisible(false, instant: false);
        }

        /// <summary>Click vùng nền trống → bỏ chọn toàn bộ tướng, cho người chơi chọn lại.</summary>
        private void OnBackgroundClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.genericButtonClip);
            // Bỏ chọn tất cả
            foreach (int idx in _selectedIndices)
                _spawnedCards[idx].SetSelected(false);
            _selectedIndices.Clear();

            // Ẩn nút Let's Go
            RefreshLetsGoButton();
        }

        private void SetPanelVisible(bool visible, bool instant, System.Action onComplete = null)
        {
            if (canvasGroup == null) { onComplete?.Invoke(); return; }

            if (instant)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                onComplete?.Invoke();
            }
            else
            {
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
                float target = visible ? 1f : 0f;
                canvasGroup.DOFade(target, 0.4f).SetEase(Ease.InOutQuad).OnComplete(() => onComplete?.Invoke());
            }
        }
    }
}
