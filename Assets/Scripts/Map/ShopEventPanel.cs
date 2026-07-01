using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using ProjectM.Skills;
using DG.Tweening;

namespace ProjectM.Map
{
    // ════════════════════════════════════════════════════════════════════
    // SHOP EVENT PANEL
    // ════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Shop Event — Người chơi mua Skill cards (hàng trên) và Relic/Trinket (hàng dưới).
    /// Hàng dưới có 4 item: 2 Relic + 2 Trinket. Khi click vào item hàng dưới sẽ mở ItemDetailPanel.
    /// </summary>
    public class ShopEventPanel : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ══════════════════════════════════════════════════════════════

        [Header("Core References")]
        public CanvasGroup panelCanvasGroup;
        public GameObject skillCardPrefab;
        public GameObject trinketItemPrefab; // Dùng chung hiển thị kệ cho cả Trinket lẫn Relic

        [Header("Keyword Coloring")]
        public Skills.KeywordDatabase keywordDatabase;

        [Header("Layout Containers")]
        public Transform skillRowContainer;
        public Transform trinketRowContainer;

        [Header("Skill Row Settings (Top)")]
        public int skillSlotCount = 3;
        public float skillCardScale = 0.5f;
        public float skillCardSpacing = 320f;
        public int skillBasePrice = 30;
        public int skillPriceVariance = 10;

        [Header("Bottom Row Settings (2 Relics + 2 Trinkets)")]
        public float bottomItemSpacing = 260f;
        public int trinketBasePrice = 25;
        public int trinketPriceVariance = 10;
        public int relicBasePrice = 45;
        public int relicPriceVariance = 15;

        [Header("Price Tag")]
        public GameObject priceTagPrefab;
        public Sprite coinSprite;
        public Color priceAffordableColor = new Color(1f, 0.85f, 0.1f);
        public Color priceExpensiveColor = new Color(0.9f, 0.3f, 0.3f);

        [Header("Item Detail Popup (Screenshot 1 & 2)")]
        public GameObject itemDetailPanelRoot;
        public GameObject detailBgOverlay;
        public Button detailCloseButton;
        public Image detailItemIcon;
        public TextMeshProUGUI detailItemName;
        public GameObject detailSkillPreviewRoot;
        public GameObject detailDescBox;
        public TextMeshProUGUI detailDescText;
        public Button detailBuyButton;
        public TextMeshProUGUI detailBuyText;

        [Header("Fly Animation")]
        public RectTransform bagIconTarget;
        public float flyDuration = 0.45f;

        [Header("Buttons")]
        public Button backButton;

        [Header("Animation")]
        public float fadeInDuration = 0.3f;

        [Header("Item Pools — Kéo các asset vào đây")]
        public List<SkillData> skillPool = new List<SkillData>();
        public List<TrinketData> trinketPool = new List<TrinketData>();
        public List<RelicData> relicPool = new List<RelicData>();

        // ══════════════════════════════════════════════════════════════
        // RUNTIME PRIVATE
        // ══════════════════════════════════════════════════════════════

        private System.Action _onComplete;

        private class ShopSlot
        {
            public GameObject itemGO;
            public GameObject priceTagGO;
            public ScriptableObject data; // SkillData, TrinketData, hoặc RelicData
            public int price;
            public bool sold;
            public bool isSkill;
            public bool isRelic;
        }

        private List<ShopSlot> _slots = new List<ShopSlot>();
        private ShopSlot _activePopupSlot;

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

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            // Setup nút trong Detail Popup
            if (detailBgOverlay != null)
            {
                var bgBtn = detailBgOverlay.GetComponent<Button>();
                if (bgBtn == null) bgBtn = detailBgOverlay.AddComponent<Button>();
                bgBtn.transition = Selectable.Transition.None;
                bgBtn.onClick.AddListener(CloseDetailPopup);
            }
            if (detailCloseButton != null) detailCloseButton.onClick.AddListener(CloseDetailPopup);
            if (detailBuyButton != null)
            {
                detailBuyButton.onClick.RemoveAllListeners();
                detailBuyButton.onClick.AddListener(OnPopupBuyClicked);
            }

            if (itemDetailPanelRoot != null) itemDetailPanelRoot.SetActive(false);

            gameObject.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════════════════════════

        public void Open(System.Action onComplete)
        {
            _onComplete = onComplete;

            ClearSlots();
            if (itemDetailPanelRoot != null) itemDetailPanelRoot.SetActive(false);

            SpawnSkillRow();
            SpawnBottomRow();

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
            if (itemDetailPanelRoot != null) itemDetailPanelRoot.SetActive(false);

            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.DOFade(1f, fadeInDuration);

            RefreshPriceColors();
        }

        // ══════════════════════════════════════════════════════════════
        // SPAWN ITEMS
        // ══════════════════════════════════════════════════════════════

        private void SpawnSkillRow()
        {
            if (skillPool == null || skillPool.Count == 0 || skillCardPrefab == null) return;

            var picked = PickRandom(skillPool, skillSlotCount);
            int count = picked.Count;
            float actualSpacing = skillCardSpacing + 40f; // Tăng khoảng cách 40px theo yêu cầu
            float totalWidth = (count - 1) * actualSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                var data = picked[i];
                int price = Mathf.Max(1, skillBasePrice + Random.Range(-skillPriceVariance, skillPriceVariance + 1));

                var go = Instantiate(skillCardPrefab, skillRowContainer);
                go.SetActive(true); // Đảm bảo Prefab luôn được bật nếu lỡ bị tắt trong thư mục
                go.transform.localScale = Vector3.zero;

                var display = go.GetComponentInChildren<CardDisplay>();
                if (display != null) display.LoadSkillData(data);

                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(startX + i * actualSpacing, 0f);
                go.transform.localEulerAngles = Vector3.zero;

                go.transform.DOScale(skillCardScale, 0.35f).SetEase(Ease.OutBack).SetDelay(i * 0.07f);

                // Tính toán yOffset dựa trên kích thước thực tế của thẻ (sau khi scale)
                float cardHeight = rt.rect.height > 0 ? rt.rect.height : 450f;
                float yOffset = -(cardHeight / 2f * skillCardScale) - 40f; // Dịch xuống dưới thêm 20px (từ 20 -> 40)

                var priceGO = SpawnPriceTag(go, price, rt.anchoredPosition, yOffset);

                var slot = new ShopSlot
                {
                    itemGO = go, priceTagGO = priceGO, data = data,
                    price = price, sold = false, isSkill = true, isRelic = false
                };
                _slots.Add(slot);

                SetupSlotInteraction(slot, skillCardScale);
            }
        }

        private void SpawnBottomRow()
        {
            var runData = GameManager.Instance?.RunData;
            var unownedRelics = relicPool != null ? relicPool.FindAll(r => r != null && (runData == null || !runData.ownedRelicIDs.Contains(r.name))) : new List<RelicData>();
            var unownedTrinkets = trinketPool != null ? trinketPool.FindAll(t => t != null && (runData == null || !runData.ownedTrinketIDs.Contains(t.name))) : new List<TrinketData>();

            // Bốc 2 Relic và 2 Trinket chưa sở hữu
            var pickedRelics = PickRandom(unownedRelics, 2);
            var pickedTrinkets = PickRandom(unownedTrinkets, 2);

            var combined = new List<ScriptableObject>();
            combined.AddRange(pickedRelics);
            combined.AddRange(pickedTrinkets);

            // Trộn ngẫu nhiên vị trí 4 item
            for (int i = 0; i < combined.Count; i++)
            {
                int rnd = Random.Range(0, combined.Count);
                var temp = combined[i];
                combined[i] = combined[rnd];
                combined[rnd] = temp;
            }

            int count = combined.Count;
            float totalWidth = (count - 1) * bottomItemSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                var data = combined[i];
                bool isRelicItem = data is RelicData;
                int baseP = isRelicItem ? relicBasePrice : trinketBasePrice;
                int varP  = isRelicItem ? relicPriceVariance : trinketPriceVariance;
                int price = Mathf.Max(1, baseP + Random.Range(-varP, varP + 1));

                Sprite icon = isRelicItem ? ((RelicData)data).icon : ((TrinketData)data).icon;
                string dName = isRelicItem ? ((RelicData)data).relicName : ((TrinketData)data).trinketName;

                // ── Tạo item slot hoàn toàn từ scratch — không dùng trinketItemPrefab
                //    để tránh Awake() của script Inventory tự gọi SetActive(false)
                var go = new GameObject($"ShopItem_{dName}", typeof(RectTransform));
                go.transform.SetParent(trinketRowContainer, false);

                // Kích thước 100×100 px, căn giữa
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(150f, 150f);
                rt.anchoredPosition = new Vector2(startX + i * bottomItemSpacing, 60f);

                // Vùng nhận click (trong suốt, nhưng vẫn là Raycast target)
                var hitImg = go.AddComponent<Image>();
                hitImg.color = Color.clear;

                // Child: Icon Image (stretch fill toàn bộ slot)
                var iconGO = new GameObject("Icon", typeof(RectTransform));
                iconGO.transform.SetParent(go.transform, false);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                var iconImg = iconGO.AddComponent<Image>();
                if (icon != null)
                {
                    iconImg.sprite = icon;
                    iconImg.preserveAspect = true;
                    iconImg.color = Color.white;
                }
                else
                {
                    // Fallback: hiện tên nếu chưa gán icon
                    iconGO.SetActive(false);

                    var textGO = new GameObject("NameText", typeof(RectTransform));
                    textGO.transform.SetParent(go.transform, false);
                    var textRT = textGO.GetComponent<RectTransform>();
                    textRT.anchorMin = Vector2.zero;
                    textRT.anchorMax = Vector2.one;
                    textRT.offsetMin = Vector2.zero;
                    textRT.offsetMax = Vector2.zero;

                    var tmp = textGO.AddComponent<TextMeshProUGUI>();
                    tmp.text = dName;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                    tmp.fontSize = 18f;
                    tmp.color = isRelicItem ? new Color(1f, 0.85f, 0.3f) : new Color(0.5f, 0.9f, 1f);
                }

                // Animation xuất hiện
                go.transform.localScale = Vector3.zero;
                go.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(i * 0.07f);

                // Giá tiền ngay bên dưới item (sizeDelta.y / 2 + 25px padding)
                float yOffset = -(rt.sizeDelta.y / 2f) - 40f;
                var priceGO = SpawnPriceTag(go, price, rt.anchoredPosition, yOffset);

                var slot = new ShopSlot
                {
                    itemGO = go, priceTagGO = priceGO, data = data,
                    price = price, sold = false, isSkill = false, isRelic = isRelicItem
                };
                _slots.Add(slot);

                SetupSlotInteraction(slot, 1f);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // PRICE TAG
        // ══════════════════════════════════════════════════════════════

        private GameObject SpawnPriceTag(GameObject parentItem, int price, Vector2 itemAnchoredPos, float yOffset)
        {
            if (priceTagPrefab == null) return null;

            var priceGO = Instantiate(priceTagPrefab, parentItem.transform.parent);
            var priceRT = priceGO.GetComponent<RectTransform>();

            // Dùng offset truyền vào để ghim chính xác xuống dưới đáy
            priceRT.anchoredPosition = new Vector2(itemAnchoredPos.x, itemAnchoredPos.y + yOffset);

            var images = priceGO.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("coin") || img.gameObject.name.ToLower().Contains("icon"))
                {
                    if (coinSprite != null) img.sprite = coinSprite;
                    break;
                }
            }

            var priceText = priceGO.GetComponentInChildren<TextMeshProUGUI>();
            if (priceText != null)
            {
                priceText.text = price.ToString();
                int gold = GameManager.Instance?.RunData?.gold ?? 0;
                priceText.color = gold >= price ? priceAffordableColor : priceExpensiveColor;
            }

            priceGO.transform.localScale = Vector3.zero;
            priceGO.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);

            return priceGO;
        }

        private void RefreshPriceColors()
        {
            int gold = GameManager.Instance?.RunData?.gold ?? 0;
            foreach (var slot in _slots)
            {
                if (slot.sold || slot.priceTagGO == null) continue;
                var priceText = slot.priceTagGO.GetComponentInChildren<TextMeshProUGUI>();
                if (priceText != null)
                    priceText.color = gold >= slot.price ? priceAffordableColor : priceExpensiveColor;
            }

            // Nếu popup đang mở cũng cập nhật nút mua trong popup
            if (itemDetailPanelRoot != null && itemDetailPanelRoot.activeSelf && _activePopupSlot != null)
            {
                if (detailBuyText != null) detailBuyText.color = gold >= _activePopupSlot.price ? priceAffordableColor : priceExpensiveColor;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // INTERACTION
        // ══════════════════════════════════════════════════════════════

        private void SetupSlotInteraction(ShopSlot slot, float baseScale)
        {
            SetupHoverOnGO(slot.itemGO, slot, baseScale);
            if (slot.priceTagGO != null) SetupHoverOnGO(slot.priceTagGO, slot, baseScale);

            var btn = slot.itemGO.GetComponent<Button>();
            if (btn == null) btn = slot.itemGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            btn.onClick.AddListener(() => OnSlotClicked(slot));

            if (slot.priceTagGO != null)
            {
                var priceBtn = slot.priceTagGO.GetComponent<Button>();
                if (priceBtn == null) priceBtn = slot.priceTagGO.AddComponent<Button>();
                priceBtn.transition = Selectable.Transition.None;
                priceBtn.onClick.AddListener(() => OnSlotClicked(slot));
            }
        }

        private void SetupHoverOnGO(GameObject go, ShopSlot slot, float baseScale)
        {
            var trigger = go.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            float hoverScale = baseScale * 1.1f;

            var enterE = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enterE.callback.AddListener(_ =>
            {
                if (slot.sold) return;
                slot.itemGO.transform.DOScale(hoverScale, 0.15f).SetEase(Ease.OutQuad);
                if (slot.priceTagGO != null) slot.priceTagGO.transform.DOScale(1.1f, 0.15f).SetEase(Ease.OutQuad);
            });

            var exitE = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exitE.callback.AddListener(_ =>
            {
                if (slot.sold) return;
                slot.itemGO.transform.DOScale(baseScale, 0.15f).SetEase(Ease.OutQuad);
                if (slot.priceTagGO != null) slot.priceTagGO.transform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);
            });

            trigger.triggers.Add(enterE);
            trigger.triggers.Add(exitE);
        }

        // ══════════════════════════════════════════════════════════════
        // CLICK ROUTING
        // ══════════════════════════════════════════════════════════════

        private void OnSlotClicked(ShopSlot slot)
        {
            if (slot.sold) return;

            // Chặn click kệ khi detail popup đang mở (tránh mua nhầm khi click ngoài popup)
            if (itemDetailPanelRoot != null && itemDetailPanelRoot.activeSelf) return;

            if (slot.isSkill)
            {
                var runData = GameManager.Instance?.RunData;
                if (runData != null && runData.gold < slot.price)
                {
                    // Rung thẻ nếu không đủ tiền
                    slot.itemGO.transform.DOKill(true);
                    slot.itemGO.transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 20, 90f);
                    return;
                }

                // Thẻ skill → Mua trực tiếp ngay trên kệ
                ExecutePurchase(slot);
            }
            else
            {
                // Relic hoặc Trinket → Mở bảng chi tiết ItemDetailPanel
                ShowDetailPopup(slot);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ITEM DETAIL POPUP LOGIC (Screenshot 1 & 2)
        // ══════════════════════════════════════════════════════════════

        private void ShowDetailPopup(ShopSlot slot)
        {
            _activePopupSlot = slot;
            if (itemDetailPanelRoot == null) return;

            Sprite icon = slot.isRelic ? ((RelicData)slot.data).icon : ((TrinketData)slot.data).icon;
            string dName = slot.isRelic ? ((RelicData)slot.data).relicName : ((TrinketData)slot.data).trinketName;
            string rawDesc = slot.isRelic ? ((RelicData)slot.data).description : ((TrinketData)slot.data).description;

            if (detailItemIcon != null) detailItemIcon.sprite = icon;
            if (detailItemName != null)
            {
                detailItemName.text = dName;
                detailItemName.color = slot.isRelic ? new Color(1f, 0.85f, 0.3f) : new Color(0.5f, 0.9f, 1f);
            }

            if (detailDescBox != null) detailDescBox.SetActive(true);
            if (detailDescText != null)
                detailDescText.text = Skills.TextFormatter.Process(rawDesc, keywordDatabase);

            // Xử lý kĩ năng preview của Relic
            if (detailSkillPreviewRoot != null)
            {
                bool showSkill = slot.isRelic && ((RelicData)slot.data).possibleSkills != null && ((RelicData)slot.data).possibleSkills.Count > 0;
                detailSkillPreviewRoot.SetActive(showSkill);

                if (showSkill && skillCardPrefab != null)
                {
                    foreach (Transform c in detailSkillPreviewRoot.transform) Destroy(c.gameObject);

                    var relic = (RelicData)slot.data;
                    for (int i = 0; i < relic.possibleSkills.Count; i++)
                    {
                        var sk = relic.possibleSkills[i];
                        if (sk == null) continue;

                        var go = Instantiate(skillCardPrefab, detailSkillPreviewRoot.transform);
                        go.transform.localScale = Vector3.one * 0.4f;
                        go.transform.localPosition = Vector3.zero;

                        var disp = go.GetComponentInChildren<CardDisplay>();
                        if (disp != null) disp.LoadSkillData(sk);
                    }
                }
            }

            // Nút mua trong popup
            int gold = GameManager.Instance?.RunData?.gold ?? 0;
            if (detailBuyText != null)
            {
                detailBuyText.text = $"Buy ({slot.price} G)";
                detailBuyText.color = gold >= slot.price ? priceAffordableColor : priceExpensiveColor;
            }

            itemDetailPanelRoot.SetActive(true);
            var cg = itemDetailPanelRoot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.DOFade(1f, 0.2f);
            }
        }

        private void CloseDetailPopup()
        {
            if (itemDetailPanelRoot == null || !itemDetailPanelRoot.activeSelf) return;

            var cg = itemDetailPanelRoot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.15f).OnComplete(() => itemDetailPanelRoot.SetActive(false));
            }
            else
            {
                itemDetailPanelRoot.SetActive(false);
            }
            _activePopupSlot = null;
        }

        private void OnPopupBuyClicked()
        {
            if (_activePopupSlot == null || _activePopupSlot.sold) return;

            var runData = GameManager.Instance?.RunData;
            if (runData == null) return;

            if (runData.gold < _activePopupSlot.price)
            {
                // Không đủ tiền → rung nút Buy
                if (detailBuyButton != null)
                    detailBuyButton.transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 20, 90f);
                return;
            }

            // Đủ tiền → mua
            ShopSlot boughtSlot = _activePopupSlot;
            CloseDetailPopup();
            ExecutePurchase(boughtSlot);
        }

        // ══════════════════════════════════════════════════════════════
        // PURCHASE EXECUTION
        // ══════════════════════════════════════════════════════════════

        private void ExecutePurchase(ShopSlot slot)
        {
            var runData = GameManager.Instance?.RunData;
            if (runData == null || slot.sold) return;

            slot.sold = true;
            runData.gold -= slot.price;
            runData.goldSpent += slot.price;

            // Phát tiếng mua hàng (trừ tiền)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.buyItemClip);

            if (slot.isSkill)
            {
                runData.playerDeckIDs.Add(slot.data.name);
                // Sync vào supportDeck trong RAM
                if (runData.championSetup != null && slot.data is Skills.SkillData sd)
                    runData.championSetup.supportDeck.Add(sd);
                Debug.Log($"[Shop] Mua Skill '{slot.data.name}' giá {slot.price} vàng.");
            }
            else if (slot.isRelic)
            {
                if (!runData.ownedRelicIDs.Contains(slot.data.name))
                    runData.ownedRelicIDs.Add(slot.data.name);
                // Sync vào ownedRelics trong RAM
                if (runData.championSetup != null && slot.data is RelicData rd
                    && !runData.championSetup.ownedRelics.Contains(rd))
                    runData.championSetup.ownedRelics.Add(rd);
                Debug.Log($"[Shop] Mua Relic '{slot.data.name}' giá {slot.price} vàng.");
            }
            else
            {
                if (!runData.ownedTrinketIDs.Contains(slot.data.name))
                    runData.ownedTrinketIDs.Add(slot.data.name);
                // Sync vào ownedTrinkets trong RAM
                if (runData.championSetup != null && slot.data is Skills.TrinketData td
                    && !runData.championSetup.ownedTrinkets.Contains(td))
                    runData.championSetup.ownedTrinkets.Add(td);
                Debug.Log($"[Shop] Mua Trinket '{slot.data.name}' giá {slot.price} vàng.");
            }


            GameManager.Instance?.SaveGame();
            RefreshPriceColors();

            if (slot.priceTagGO != null)
            {
                slot.priceTagGO.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(slot.priceTagGO));
            }

            FlyItemToBag(slot);
        }

        private void FlyItemToBag(ShopSlot slot)
        {
            GameObject itemGO = slot.itemGO;
            if (bagIconTarget == null) { Destroy(itemGO); return; }

            var cg = itemGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = itemGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) itemGO.transform.SetParent(canvas.transform, true);

            Vector3 target = bagIconTarget.position;
            itemGO.transform.DOKill();
            itemGO.transform.DOScale(0f, flyDuration).SetEase(Ease.InCubic);
            itemGO.transform.DOLocalRotate(new Vector3(0, 0, 180), flyDuration, RotateMode.LocalAxisAdd);
            itemGO.transform.DOJump(target, 120f, 1, flyDuration).SetEase(Ease.InCubic)
                .OnComplete(() => {
                    Destroy(itemGO);
                    if (!slot.isSkill) 
                    {
                        ProjectM.Inventory.InventoryManager.Instance?.OpenInventory();
                    }
                });
        }

        private void OnBackClicked()
        {
            panelCanvasGroup.DOFade(0f, 0.25f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                if (_onComplete != null)
                {
                    var cb = _onComplete;
                    _onComplete = null;
                    cb.Invoke();
                }
            });
        }

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot.itemGO != null) Destroy(slot.itemGO);
                if (slot.priceTagGO != null) Destroy(slot.priceTagGO);
            }
            _slots.Clear();

            if (skillRowContainer != null) foreach (Transform c in skillRowContainer) Destroy(c.gameObject);
            if (trinketRowContainer != null) foreach (Transform c in trinketRowContainer) Destroy(c.gameObject);
        }

        private List<T> PickRandom<T>(List<T> pool, int count)
        {
            var result = new List<T>();
            if (pool == null || pool.Count == 0) return result;

            var available = new List<T>(pool);
            count = Mathf.Min(count, available.Count);

            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, available.Count);
                result.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return result;
        }
    }
}
