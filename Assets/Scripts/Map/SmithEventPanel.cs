using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using DG.Tweening;

namespace ProjectM.Map
{
    // ════════════════════════════════════════════════════════════════════
    // SMITH EVENT PANEL
    // ════════════════════════════════════════════════════════════════════
    public class SmithEventPanel : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ══════════════════════════════════════════════════════════════

        [Header("Core References")]
        public CanvasGroup panelCanvasGroup;
        public Transform championContainer;
        public GameObject cardPrefab;

        [Header("Phase 1 Layout")]
        public float cardSpawnScale = 0.5f;
        public float cardSpacing = 350f;
        public float yRandomRange = 60f;

        public RectTransform bagIconTarget;
        public float flyDuration = 0.5f;

        [Header("Phase 2 — Upgrade Buttons")]
        public GameObject upgradePhaseRoot;
        public float selectedCardScale = 0.6f;

        public Button atkUpgradeButton;
        public Button hpUpgradeButton;
        public TextMeshProUGUI atkButtonLabel;
        public TextMeshProUGUI hpButtonLabel;

        [Header("Texts")]
        public TextMeshProUGUI headerText;

        [Header("Buttons")]
        public Button backButton;

        [Header("Animation")]
        public float fadeInDuration = 0.3f;

        // ══════════════════════════════════════════════════════════════
        // RUNTIME PRIVATE
        // ══════════════════════════════════════════════════════════════

        private System.Action _onComplete;
        private List<ChampionCardEntry> _spawnedCards = new List<ChampionCardEntry>();

        private class ChampionCardEntry
        {
            public GameObject go;
            public CardData   cardData;
            public int        clickCount; // 0=bình thường, 1=focused, 2=confirmed
            public Tween      bobbingTween;
        }

        private ChampionCardEntry _focusedEntry;
        private GameObject        _selectedCardGO;
        private CardData          _selectedCardData;

        // ══════════════════════════════════════════════════════════════
        // UNITY
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null) panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (upgradePhaseRoot != null) upgradePhaseRoot.SetActive(false);

            if (atkUpgradeButton != null) atkUpgradeButton.onClick.AddListener(() => OnUpgradeChosen(1, 0));
            if (hpUpgradeButton  != null) hpUpgradeButton.onClick.AddListener(() => OnUpgradeChosen(0, 1));
            if (backButton       != null) backButton.onClick.AddListener(OnBackClicked);

            // Click vào nền panel (khoảng trống) → bỏ chọn thẻ đang focus
            var bgBtn = GetComponent<Button>();
            if (bgBtn == null) bgBtn = gameObject.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None; // Không đổi màu khi click
            bgBtn.onClick.AddListener(OnBackgroundClicked);

            gameObject.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════════════════════════

        public void Open(System.Action onComplete)
        {
            _onComplete = onComplete;
            _focusedEntry = null;
            _selectedCardData = null;
            _spawnedCards.Clear();

            if (headerText != null) headerText.text = "Choose one character to upgrade";
            if (upgradePhaseRoot != null) upgradePhaseRoot.SetActive(false);

            PopulateChampions();

            gameObject.SetActive(true);
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.DOFade(1f, fadeInDuration);

            MapManager.Instance?.SetEventInProgress(true, Reopen);
        }

        public void Reopen()
        {
            gameObject.SetActive(true);
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.DOFade(1f, fadeInDuration);
        }

        // ══════════════════════════════════════════════════════════════
        // PHASE 1 — Spawn champion cards
        // ══════════════════════════════════════════════════════════════

        private void PopulateChampions()
        {
            foreach (Transform child in championContainer) Destroy(child.gameObject);

            var champions = GetChampionCards();
            if (champions.Count == 0)
            {
                Debug.LogWarning("[SmithEvent] Không tìm thấy thẻ Tướng nào trong RunData!");
                return;
            }

            int count = champions.Count;
            float totalWidth = (count - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                var cardData = champions[i];
                var go = Instantiate(cardPrefab, championContainer);
                go.transform.localPosition = Vector3.zero;

                var display = go.GetComponentInChildren<CardDisplay>();
                if (display != null)
                {
                    display.cardData = cardData;
                    display.LoadData(cardData);
                }

                float xPos = startX + i * cardSpacing;
                float yBase = 0f; // Tất cả thẻ cùng độ cao gốc
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(xPos, yBase);

                // Thẳng đứng hoàn toàn, không nghiêng
                go.transform.localEulerAngles = Vector3.zero;

                // Hiệu ứng xuất hiện
                go.transform.localScale = Vector3.zero;
                go.transform.DOScale(cardSpawnScale, 0.4f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(i * 0.08f);

                var entry = new ChampionCardEntry { go = go, cardData = cardData, clickCount = 0 };
                _spawnedCards.Add(entry);

                // Bobbing: biên độ và tốc độ khác nhau → cảm giác random, nhưng baseline Y đồng nhất
                float bobAmplitude = Random.Range(18f, 32f);   // Biên độ lên xuống (px)
                float bobDuration  = Random.Range(1.1f, 2.5f); // Tốc độ mỗi thẻ khác nhau

                // Bắt đầu từ vị trí ngẫu nhiên trong chu kỳ để không đồng bộ ngay từ đầu
                float startY = yBase + Random.Range(-bobAmplitude, bobAmplitude);
                rt.anchoredPosition = new Vector2(xPos, startY);

                entry.bobbingTween = rt.DOAnchorPosY(yBase + bobAmplitude, bobDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                AddHoverEffect(go, entry);

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                btn.onClick.AddListener(() => OnChampionClicked(entry));
            }
        }

        private void AddHoverEffect(GameObject go, ChampionCardEntry entry)
        {
            var trigger = go.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enterEvent = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterEvent.callback.AddListener(_ =>
            {
                // Chỉ scale lên, KHÔNG dừng bobbing — bobbing vẫn chạy ngầm
                if (entry.clickCount == 0)
                {
                    go.transform.DOScale(cardSpawnScale * 1.12f, 0.15f).SetEase(Ease.OutQuad);
                    go.transform.SetAsLastSibling();
                }
            });

            var exitEvent = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitEvent.callback.AddListener(_ =>
            {
                // Scale về bình thường, bobbing vẫn chạy
                if (entry.clickCount == 0)
                    go.transform.DOScale(cardSpawnScale, 0.15f).SetEase(Ease.OutQuad);
            });

            trigger.triggers.Add(enterEvent);
            trigger.triggers.Add(exitEvent);
        }

        // ══════════════════════════════════════════════════════════════
        // PHASE 1 — Click logic (2 lần click)
        // ══════════════════════════════════════════════════════════════

        private void OnChampionClicked(ChampionCardEntry entry)
        {
            // Bấm lần nào cũng phát tiếng chọn thẻ
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardSelectClip);

            if (entry.clickCount == 0)
            {
                // Click 1: Focus
                if (_focusedEntry != null && _focusedEntry != entry)
                    UnfocusEntry(_focusedEntry);

                _focusedEntry = entry;
                entry.clickCount = 1;

                entry.bobbingTween?.Pause(); // Dừng nhấp nhô khi focus

                entry.go.transform.DOKill(true);
                entry.go.transform.DOScale(selectedCardScale, 0.2f).SetEase(Ease.OutBack);
                entry.go.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);

                foreach (var other in _spawnedCards)
                {
                    if (other == entry) continue;
                    var cg = other.go.GetComponent<CanvasGroup>();
                    if (cg == null) cg = other.go.AddComponent<CanvasGroup>();
                    cg.DOFade(0.35f, 0.2f);
                    cg.blocksRaycasts = false;
                }
            }
            else if (entry.clickCount == 1)
            {
                // Click 2: Confirm
                entry.clickCount = 2;
                _selectedCardData = entry.cardData;
                _selectedCardGO   = entry.go;

                panelCanvasGroup.blocksRaycasts = false;

                foreach (var other in _spawnedCards)
                {
                    if (other == entry) continue;
                    other.bobbingTween?.Kill();
                    FlyCardToBagAndDestroy(other.go);
                }

                StartCoroutine(TransitionToPhase2(entry));
            }
        }

        private void UnfocusEntry(ChampionCardEntry entry)
        {
            entry.clickCount = 0;
            entry.bobbingTween?.Play(); // Tiếp tục nhấp nhô

            entry.go.transform.DOKill(true);
            entry.go.transform.DOScale(cardSpawnScale, 0.15f).SetEase(Ease.OutQuad);

            var cg = entry.go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(1f, 0.15f);
                cg.blocksRaycasts = true;
            }
        }

        private void FlyCardToBagAndDestroy(GameObject cardGO)
        {
            if (bagIconTarget == null) { Destroy(cardGO); return; }

            var cg = cardGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) cardGO.transform.SetParent(canvas.transform, true);

            Vector3 targetPos = bagIconTarget.position;
            cardGO.transform.DOKill();
            cardGO.transform.DOScale(0f, flyDuration).SetEase(Ease.InCubic);
            cardGO.transform.DOLocalRotate(new Vector3(0, 0, 180), flyDuration, RotateMode.LocalAxisAdd);
            cardGO.transform.DOJump(targetPos, 150f, 1, flyDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() => Destroy(cardGO));
        }

        private IEnumerator TransitionToPhase2(ChampionCardEntry entry)
        {
            yield return new WaitForSeconds(0.2f);

            var rt = entry.go.GetComponent<RectTransform>();
            entry.bobbingTween?.Kill();

            // Di chuyển mượt mà về chính giữa ChampionContainer (0,0)
            rt.DOAnchorPos(Vector2.zero, 0.4f).SetEase(Ease.OutCubic);
            entry.go.transform.DOScale(selectedCardScale, 0.4f).SetEase(Ease.OutBack);
            entry.go.transform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad);

            yield return new WaitForSeconds(0.45f);

            EnterPhase2();
            panelCanvasGroup.blocksRaycasts = true;
        }

        // ══════════════════════════════════════════════════════════════
        // PHASE 2 — Upgrade selection
        // ══════════════════════════════════════════════════════════════

        private void EnterPhase2()
        {
            if (headerText != null) headerText.text = "Choose an upgrade";

            if (_selectedCardData != null)
            {
                if (atkButtonLabel != null) atkButtonLabel.text = "+1";
                if (hpButtonLabel  != null) hpButtonLabel.text  = "+1";
            }

            if (upgradePhaseRoot != null)
            {
                upgradePhaseRoot.SetActive(true);

                var cg = upgradePhaseRoot.GetComponent<CanvasGroup>();
                if (cg == null) cg = upgradePhaseRoot.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
                cg.DOFade(1f, 0.3f);
            }

            // Gắn hover pulse sau khi upgradePhaseRoot đã active
            SetupUpgradeButtonHover();
        }

        // Tìm GameObject con theo tên trong cây
        private Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void SetupUpgradeButtonHover()
        {
            if (_selectedCardGO == null) return;

            var display = _selectedCardGO.GetComponentInChildren<CardDisplay>();

            Transform heartIconOnCard = display != null ? FindDeep(display.transform, "HeartIcon")  : null;
            Transform swordIconOnCard = display != null ? FindDeep(display.transform, "SwordIcon") : null;

            TextMeshProUGUI cardHpText  = display?.healthText;
            TextMeshProUGUI cardAtkText = display?.attackText;

            // Tính giá trị hiện tại (sau bonus đã có từ trước)
            var existing = GameManager.Instance?.RunData?.GetChampionBonus(_selectedCardData?.name ?? "")
                           ?? new ChampionStatBonus();
            int currentHp  = (_selectedCardData?.health  ?? 0) + existing.healthBonus;
            int currentAtk = (_selectedCardData?.attack   ?? 0) + existing.attackBonus;

            if (hpUpgradeButton != null)
                AddUpgradeHover(hpUpgradeButton.gameObject,  hpUpgradeButton.transform,
                                heartIconOnCard, cardHpText,
                                currentHp.ToString(), (currentHp + 1).ToString());

            if (atkUpgradeButton != null)
                AddUpgradeHover(atkUpgradeButton.gameObject, atkUpgradeButton.transform,
                                swordIconOnCard, cardAtkText,
                                currentAtk.ToString(), (currentAtk + 1).ToString());
        }

        private void AddUpgradeHover(
            GameObject      btnGO,
            Transform       btnIcon,
            Transform       cardIcon,
            TextMeshProUGUI cardStatText,
            string          originalValue,
            string          upgradedValue)
        {
            var trigger = btnGO.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = btnGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            trigger.triggers.Clear();

            Color normalColor    = Color.white;
            Color highlightColor = new Color(0.45f, 0.85f, 1f); // Xanh biển nhạt

            Coroutine btnCoroutine  = null;
            Coroutine iconCoroutine = null;

            // ── Pointer Enter ──
            var enterE = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterE.callback.AddListener(_ =>
            {
                // Heartbeat nút
                if (btnCoroutine != null) StopCoroutine(btnCoroutine);
                btnCoroutine = StartCoroutine(HeartbeatLoop(btnIcon, 1.22f));

                // Heartbeat icon trên thẻ
                if (cardIcon != null)
                {
                    if (iconCoroutine != null) StopCoroutine(iconCoroutine);
                    iconCoroutine = StartCoroutine(HeartbeatLoop(cardIcon, 1.28f));
                }

                // Hiển thị số sau khi cộng + đổi màu xanh
                if (cardStatText != null)
                {
                    cardStatText.text = upgradedValue;
                    cardStatText.DOColor(highlightColor, 0.15f);
                }
            });

            // ── Pointer Exit ──
            var exitE = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitE.callback.AddListener(_ =>
            {
                // Dừng heartbeat, về scale gốc
                if (btnCoroutine  != null) { StopCoroutine(btnCoroutine);  btnCoroutine  = null; }
                if (iconCoroutine != null) { StopCoroutine(iconCoroutine); iconCoroutine = null; }

                btnIcon.DOKill();
                btnIcon.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);

                if (cardIcon != null)
                {
                    cardIcon.DOKill();
                    cardIcon.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);
                }

                // Khôi phục số gốc + màu trắng
                if (cardStatText != null)
                {
                    cardStatText.text = originalValue;
                    cardStatText.DOColor(normalColor, 0.15f);
                }
            });

            trigger.triggers.Add(enterE);
            trigger.triggers.Add(exitE);
        }

        // Nhịp tim: beat1 (to) → nhỏ → chờ 0.25s → beat2 (to hơn chút) → nhỏ → chờ 1s → lặp
        private IEnumerator HeartbeatLoop(Transform target, float peakScale)
        {
            float beat   = 0.12f; // Thời gian mỗi nửa beat
            float gap    = 0.22f; // Khoảng cách giữa 2 lần đập
            float pause  = 1.0f; // Nghỉ giữa 2 nhịp tim

            while (true)
            {
                // Lần đập 1 (lớn hơn)
                yield return target.DOScale(peakScale, beat).SetEase(Ease.OutQuad).WaitForCompletion();
                yield return target.DOScale(1f, beat).SetEase(Ease.InQuad).WaitForCompletion();

                yield return new WaitForSeconds(gap);

                // Lần đập 2 (nhỏ hơn lần 1 chút — giống nhịp tim "lub-dub")
                yield return target.DOScale(peakScale * 0.85f, beat).SetEase(Ease.OutQuad).WaitForCompletion();
                yield return target.DOScale(1f, beat).SetEase(Ease.InQuad).WaitForCompletion();

                yield return new WaitForSeconds(pause);
            }
        }

        private void OnUpgradeChosen(int atkBonus, int hpBonus)
        {
            if (_selectedCardData == null) return;

            // Phát tiếng khi chọn nâng cấp
            if (atkBonus > 0)
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.upgradeATKClip);
            else if (hpBonus > 0)
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.upgradeHPClip);

            var runData = GameManager.Instance?.RunData;
            if (runData != null)
            {
                runData.AddChampionBonus(_selectedCardData.name, atkBonus, hpBonus);
                string upgradeText = atkBonus > 0 ? $"ATK +{atkBonus}" : $"HP +{hpBonus}";
                Debug.Log($"[SmithEvent] {_selectedCardData.cardName} nhận {upgradeText}!");
            }

            if (_selectedCardGO != null)
            {
                _selectedCardGO.transform
                    .DOScale(selectedCardScale * 1.2f, 0.15f).SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _selectedCardGO.transform
                            .DOScale(0f, 0.3f).SetEase(Ease.InBack)
                            .OnComplete(() => Destroy(_selectedCardGO));
                    });
            }

            CloseAndComplete();
        }

        private void CloseAndComplete()
        {
            panelCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                _onComplete?.Invoke();
                MapManager.Instance?.SetEventInProgress(false, null);
            });
        }

        private void OnBackClicked()
        {
            panelCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                MapManager.Instance?.SetEventInProgress(true, Reopen);
            });
        }

        private void OnBackgroundClicked()
        {
            // Chỉ xử lý khi có thẻ đang focus (click 1) và chưa vào Phase 2
            if (_focusedEntry == null) return;

            UnfocusEntry(_focusedEntry);
            _focusedEntry = null;

            // Khôi phục lại toàn bộ các thẻ còn lại
            foreach (var other in _spawnedCards)
            {
                var cg = other.go.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOFade(1f, 0.2f);
                    cg.blocksRaycasts = true;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private List<CardData> GetChampionCards()
        {
            var result = new List<CardData>();
            var runData = GameManager.Instance?.RunData;
            if (runData == null) return result;

            ProjectM.Inventory.InventoryManager.Instance?.EnsureChampionSetup();
            if (runData.championSetup != null)
            {
                foreach (var entry in runData.championSetup.champions)
                    if (entry?.championData != null) result.Add(entry.championData);
                return result;
            }

            var allCards = ProjectM.Inventory.InventoryManager.Instance?.allCardAssets;
            if (allCards == null) return result;

            var lookup = new Dictionary<string, CardData>();
            foreach (var c in allCards)
                if (c != null) lookup[c.name] = c;

            foreach (var id in runData.playerDeckIDs)
            {
                if (lookup.TryGetValue(id, out var card) && card.cardType == CardType.Champion)
                    result.Add(card);
            }
            return result;
        }
    }
}
