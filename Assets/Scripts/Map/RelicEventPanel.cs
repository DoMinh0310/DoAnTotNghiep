using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using DG.Tweening;
using ProjectM.Skills;

namespace ProjectM.Map
{
    /// <summary>
    /// Bắt sự kiện hover chuột vào Relic Icon.
    /// </summary>
    public class RelicHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Action<bool> onHover;
        public void OnPointerEnter(PointerEventData eventData) => onHover?.Invoke(true);
        public void OnPointerExit(PointerEventData eventData) => onHover?.Invoke(false);
    }

    /// <summary>
    /// Điều khiển toàn bộ UI cho Relic Event.
    /// Flow mới:
    /// 1. Vào event: Header Text và Item Name Text hiện ra ngay. Relic lơ lửng.
    /// 2. Hover chuột vào Relic: Info Panels (Skill preview bên trái, Description bên phải) hiện ra.
    /// 3. Click vào Relic: Các nút (Equip, Save) hiện ra ở dưới.
    /// </summary>
    public class RelicEventPanel : MonoBehaviour
    {
        [Header("Phase 1 – Orbit & Core")]
        public Image relicIconImage;
        public Image glowImage;
        public float floatAmplitude = 18f;
        public float floatSpeed = 0.6f;
        public Color glowColorA = new Color(1f, 0.85f, 0.3f, 0.6f);
        public Color glowColorB = new Color(1f, 0.5f, 0.1f, 1f);
        public float glowPulseDuration = 0.9f;

        [Header("Always Visible Texts (Top)")]
        public TextMeshProUGUI headerText;
        public TextMeshProUGUI itemNameText;
        public Color relicNameColor = new Color(1f, 0.85f, 0.3f);
        public Color trinketNameColor = new Color(0.5f, 0.9f, 1f);

        [Header("Hover Info Panels (Left & Right)")]
        public Skills.KeywordDatabase keywordDatabase;
        [Tooltip("Root chứa cả 2 khung thông tin trái/phải, sẽ hiện khi hover")]
        public CanvasGroup infoPanelsGroup;
        [Tooltip("Root chứa thẻ bài preview (chỉ cho Relic)")]
        public GameObject skillPreviewRoot;
        [Tooltip("Prefab thẻ (VD: Card_Prefab) để hiển thị chi tiết kĩ năng")]
        public GameObject cardPrefab;
        [Tooltip("Panel description (chỉ cho Trinket)")]
        public GameObject descriptionBox;
        public TextMeshProUGUI descriptionText;

        [Header("Click Buttons (Bottom)")]
        [Tooltip("Root chứa 2 nút bấm, sẽ hiện khi click")]
        public CanvasGroup buttonsGroup;
        public Button equipNowButton;
        public Button saveForLaterButton;

        [Header("Peek Map")]
        public Button peekMapButton;

        [Header("Animation")]
        public float panelFadeInDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private Coroutine   _floatCoroutine;
        private Tweener     _glowTween;
        private Tweener     _infoFadeTween;
        private Vector3     _orbitBasePos;
        private Vector3     _glowBasePos;

        private RelicData   _currentRelic;
        private TrinketData _currentTrinket;
        private System.Action _onComplete;
        private bool        _isButtonsShown = false;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            if (relicIconImage != null)
            {
                var btn = relicIconImage.GetComponent<Button>() ?? relicIconImage.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(OnRelicClicked);

                var hover = relicIconImage.gameObject.AddComponent<RelicHoverTrigger>();
                hover.onHover = OnHoverRelic;
            }

            if (equipNowButton != null) equipNowButton.onClick.AddListener(OnEquipNow);
            if (saveForLaterButton != null) saveForLaterButton.onClick.AddListener(OnSaveForLater);
            if (peekMapButton != null) peekMapButton.onClick.AddListener(OnPeekMap);

            if (infoPanelsGroup != null)
            {
                infoPanelsGroup.alpha = 0f;
                infoPanelsGroup.gameObject.SetActive(false);
            }

            if (buttonsGroup != null)
            {
                buttonsGroup.alpha = 0f;
                buttonsGroup.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _glowTween?.Kill();
            _infoFadeTween?.Kill();
            DOTween.Kill(relicIconImage?.transform);
        }

        public void ShowRelic(RelicData relic, System.Action onComplete)
        {
            _currentRelic = relic;
            _currentTrinket = null;
            _onComplete = onComplete;
            OpenPanel();
        }

        public void ShowTrinket(TrinketData trinket, System.Action onComplete)
        {
            _currentRelic = null;
            _currentTrinket = trinket;
            _onComplete = onComplete;
            OpenPanel();
        }

        public void Hide()
        {
            StopFloat();
            _glowTween?.Kill();
            _infoFadeTween?.Kill();
            _canvasGroup.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
        }

        private void OpenPanel()
        {
            bool isRelic = _currentRelic != null;
            Sprite icon = isRelic ? _currentRelic.icon : _currentTrinket?.icon;
            string displayName = isRelic ? _currentRelic.relicName : _currentTrinket?.trinketName;

            if (relicIconImage != null)
            {
                relicIconImage.sprite = icon;
                relicIconImage.color = Color.white;
            }

            // Thiết lập Header và Tên Item ngay từ đầu
            if (headerText != null)
                headerText.text = isRelic ? "Relic Received!" : "Trinket Received!";

            if (itemNameText != null)
            {
                itemNameText.text = displayName;
                itemNameText.color = isRelic ? relicNameColor : trinketNameColor;
            }

            // Reset trạng thái hover/click
            _isButtonsShown = false;
            if (infoPanelsGroup != null)
            {
                infoPanelsGroup.alpha = 0f;
                infoPanelsGroup.gameObject.SetActive(false);
            }
            if (buttonsGroup != null)
            {
                buttonsGroup.alpha = 0f;
                buttonsGroup.gameObject.SetActive(false);
                buttonsGroup.interactable = false;
                buttonsGroup.blocksRaycasts = false;
            }

            // Thiết lập dữ liệu cho Info Panels sẵn
            PopulateInfoPanels(isRelic);

            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, panelFadeInDuration);

            if (relicIconImage != null)
            {
                _orbitBasePos = relicIconImage.transform.localPosition;
                if (glowImage != null) _glowBasePos = glowImage.transform.localPosition;
            }

            StartFloat();
            StartGlow();

            MapManager.Instance?.SetEventInProgress(true, Reopen);
        }

        public void Reopen()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, panelFadeInDuration);
        }

        private void PopulateInfoPanels(bool isRelic)
        {
            if (skillPreviewRoot != null)
            {
                bool showSkill = isRelic && _currentRelic.possibleSkills != null && _currentRelic.possibleSkills.Count > 0;
                skillPreviewRoot.SetActive(showSkill);
                if (showSkill && cardPrefab != null)
                {
                    // Xóa các thẻ cũ
                    foreach (Transform child in skillPreviewRoot.transform)
                    {
                        Destroy(child.gameObject);
                    }
                    
                    // Tạo ra thẻ hoàn chỉnh cho mỗi skill
                    for (int i = 0; i < _currentRelic.possibleSkills.Count; i++)
                    {
                        var skill = _currentRelic.possibleSkills[i];
                        if (skill == null) continue;
                        
                        GameObject go = Instantiate(cardPrefab, skillPreviewRoot.transform);
                        go.transform.localScale = Vector3.one * 0.4f; // Thu nhỏ thẻ lại cho vừa
                        go.transform.localPosition = Vector3.zero;
                        
                        var display = go.GetComponentInChildren<ProjectM.Cards.CardDisplay>();
                        if (display != null)
                        {
                            display.LoadSkillData(skill);
                        }
                    }
                }
            }

            if (descriptionBox != null)
            {
                descriptionBox.SetActive(true);
                if (descriptionText != null)
                {
                    string rawDesc = isRelic ? _currentRelic.description : _currentTrinket.description;
                    descriptionText.text = Skills.TextFormatter.Process(rawDesc, keywordDatabase);
                }
            }
        }

        private void OnHoverRelic(bool isHovering)
        {
            if (infoPanelsGroup == null) return;

            _infoFadeTween?.Kill();

            if (isHovering)
            {
                infoPanelsGroup.gameObject.SetActive(true);
                _infoFadeTween = infoPanelsGroup.DOFade(1f, 0.2f);
            }
            else
            {
                _infoFadeTween = infoPanelsGroup.DOFade(0f, 0.2f).OnComplete(() =>
                {
                    if (infoPanelsGroup != null) infoPanelsGroup.gameObject.SetActive(false);
                });
            }
        }

        private void OnRelicClicked()
        {
            if (_isButtonsShown || buttonsGroup == null) return;
            _isButtonsShown = true;

            buttonsGroup.gameObject.SetActive(true);
            buttonsGroup.DOFade(1f, 0.3f);
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;

            // Nảy nhẹ nút bấm
            buttonsGroup.transform.localScale = Vector3.one * 0.8f;
            buttonsGroup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        private void StartFloat()
        {
            StopFloat();
            _floatCoroutine = StartCoroutine(FloatRoutine());
        }

        private void StopFloat()
        {
            if (_floatCoroutine != null)
            {
                StopCoroutine(_floatCoroutine);
                _floatCoroutine = null;
            }
        }

        private IEnumerator FloatRoutine()
        {
            while (true)
            {
                if (relicIconImage == null) yield break;
                float y = Mathf.Sin(Time.time * floatSpeed * Mathf.PI * 2f) * floatAmplitude;
                relicIconImage.transform.localPosition = _orbitBasePos + new Vector3(0f, y, 0f);
                if (glowImage != null) glowImage.transform.localPosition = _glowBasePos + new Vector3(0f, y, 0f);
                yield return null;
            }
        }

        private void StartGlow()
        {
            _glowTween?.Kill();
            if (glowImage == null) return;

            glowImage.color = glowColorA;
            _glowTween = glowImage
                .DOColor(glowColorB, glowPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void OnEquipNow()
        {
            AudioManager.Instance?.PlayAcquireItemCombo();
            SaveToRunData();
            // Mở Inventory ngay lập tức
            if (ProjectM.Inventory.InventoryManager.Instance != null)
            {
                ProjectM.Inventory.InventoryManager.Instance.OpenInventory();
            }
            FinishEvent();
        }

        private void OnSaveForLater()
        {
            AudioManager.Instance?.PlayAcquireItemCombo();
            SaveToRunData();
            FinishEvent();
        }

        private void SaveToRunData()
        {
            ProjectM.Inventory.InventoryManager.Instance?.EnsureChampionSetup();
            var runData = GameManager.Instance?.RunData;
            if (runData == null) return;

            if (_currentRelic != null)
            {
                // Lưu ID (cho Save/Load JSON)
                if (!runData.ownedRelicIDs.Contains(_currentRelic.name))
                    runData.ownedRelicIDs.Add(_currentRelic.name);

                // Sync object vào championSetup trong RAM (để Inventory sidebar hiện ngay)
                if (runData.championSetup != null
                    && runData.championSetup.ownedRelics != null
                    && !runData.championSetup.ownedRelics.Contains(_currentRelic))
                {
                    runData.championSetup.ownedRelics.Add(_currentRelic);
                    Debug.Log($"[RelicEvent] Synced relic '{_currentRelic.relicName}' → championSetup.ownedRelics.");
                }
            }
            else if (_currentTrinket != null)
            {
                // Lưu ID (cho Save/Load JSON)
                if (!runData.ownedTrinketIDs.Contains(_currentTrinket.name))
                    runData.ownedTrinketIDs.Add(_currentTrinket.name);

                // Sync object vào championSetup trong RAM (để Inventory sidebar hiện ngay)
                if (runData.championSetup != null
                    && runData.championSetup.ownedTrinkets != null
                    && !runData.championSetup.ownedTrinkets.Contains(_currentTrinket))
                {
                    runData.championSetup.ownedTrinkets.Add(_currentTrinket);
                    Debug.Log($"[RelicEvent] Synced trinket '{_currentTrinket.trinketName}' → championSetup.ownedTrinkets.");
                }
            }

            GameManager.Instance?.SaveGame();
        }

        private void FinishEvent()
        {
            MapManager.Instance?.SetEventInProgress(false, null);
            Hide();
            _onComplete?.Invoke();
        }

        private void OnPeekMap()
        {
            _canvasGroup.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
        }
    }
}

