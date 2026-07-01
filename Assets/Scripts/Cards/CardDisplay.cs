using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectM.Skills;

namespace ProjectM.Cards 
{
    public class CardDisplay : MonoBehaviour
    {
        [Header("Card Data")]
        public CardData cardData;

        [Header("Keyword Coloring")]
        [Tooltip("Kéo asset KeywordDatabase vào đây để tự động nhuộm màu từ khoá trong description.")]
        public Skills.KeywordDatabase keywordDatabase;

        [Header("UI References")]
        public GameObject cardBack;     // Layer 1
        public GameObject cardFront;    // Group chứa từ Layer 2 đến 6
        
        [Header("Front Elements")]
        public Image cardFrameImage;    // Layer 2 (Khung bài)
        public Image characterArtImage; // Layer 3 (Nằm trong Mask Layer 2)
        public TextMeshProUGUI nameText; // Layer 6
        public TextMeshProUGUI attackText; // Layer 6 (Nằm dưới SwordIcon)
        public TextMeshProUGUI healthText; // Layer 6 (Nằm dưới HeartIcon)
        public TextMeshProUGUI speedText; // Layer 6 (Nằm dưới SpeedIcon)
        public TextMeshProUGUI ultText;
        public TextMeshProUGUI abilitiesText; // Layer 6 (Hiển thị skill/nội tại)
        
        [Header("Shield Display")]
        [Tooltip("Image icon trái tim bình thường (HP > 2/3).")]
        public Image heartIconImage;          // Icon trái tim bên cạnh số HP
        
        [Tooltip("Sprite trái tim bình thường (HP > 2/3).")]
        public Sprite normalHeartSprite;
        
        [Tooltip("Sprite trái tim khi HP <= 2/3.")]
        public Sprite heart2Sprite;
        
        [Tooltip("Sprite trái tim khi HP <= 1/3 (sẽ đập thình thịch).")]
        public Sprite heart3Sprite;

        [Tooltip("Sprite trái tim giáp — dùng cho buff Second Pulse (ưu tiên cao nhất).")]
        public Sprite shieldHeartSprite;

        [Header("Special Hearts")]
        public Sprite fragileHeartSprite;
        public Sprite doomHeartSprite;
        public Sprite thornHeartSprite;

        [Header("Separate Shield Display")]
        public GameObject shieldIconObj;
        public TextMeshProUGUI shieldText;

        [Header("Elemental UI")]
        public GameObject frostIconObj;
        public TextMeshProUGUI frostText;
        
        public GameObject bleedIconObj;
        public TextMeshProUGUI bleedText;
        
        public GameObject decayIconObj;
        public TextMeshProUGUI decayText;
        
        public GameObject chainIconObj;
        public TextMeshProUGUI chainText;

        private bool _isInitialized = false;

        private void Awake()
        {
            EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (_isInitialized) return;

            if (characterArtImage == null)
            {
                // Card_Prefab dùng tên "CharacterArt", Skill_Prefab dùng "SkillArt" — thử cả hai
                Transform artTransform = FindChildByName(transform, "CharacterArt")
                                      ?? FindChildByName(transform, "SkillArt");
                if (artTransform != null)
                    characterArtImage = artTransform.GetComponent<Image>();
            }

            if (cardFrameImage == null)
            {
                Transform frameTransform = FindChildByName(transform, "CardFrame");
                if (frameTransform != null)
                    cardFrameImage = frameTransform.GetComponent<Image>();
            }

            if (heartIconImage == null)
            {
                Transform heartTransform = FindChildByName(transform, "HeartIcon");
                if (heartTransform != null)
                    heartIconImage = heartTransform.GetComponent<Image>();
            }

            if (abilitiesText == null)
            {
                Transform abilitiesTransform = FindChildByName(transform, "AbilitiesText");
                if (abilitiesTransform != null)
                    abilitiesText = abilitiesTransform.GetComponent<TextMeshProUGUI>();
            }

            // Hàm nội bộ để đảm bảo reference phải nằm trong CÙNG prefab instance
            GameObject ValidateRef(GameObject obj, string childName)
            {
                if (obj != null && obj.transform.IsChildOf(this.transform)) return obj;
                var found = FindChildByName(transform, childName);
                return found != null ? found.gameObject : null;
            }

            // Tự động tìm icon nguyên tố theo tên nếu chưa kéo vào hoặc kéo nhầm Prefab từ Project
            frostIconObj = ValidateRef(frostIconObj, "FrostIcon");
            bleedIconObj = ValidateRef(bleedIconObj, "BleedIcon");
            decayIconObj = ValidateRef(decayIconObj, "DecayIcon");
            chainIconObj = ValidateRef(chainIconObj, "ChainIcon");
            shieldIconObj = ValidateRef(shieldIconObj, "ShieldIcon");

            // Tự động tìm Text bên trong từng icon
            if (frostText == null) frostText = frostIconObj?.GetComponentInChildren<TextMeshProUGUI>();
            if (bleedText == null) bleedText = bleedIconObj?.GetComponentInChildren<TextMeshProUGUI>();
            if (decayText == null) decayText = decayIconObj?.GetComponentInChildren<TextMeshProUGUI>();
            if (chainText == null) chainText = chainIconObj?.GetComponentInChildren<TextMeshProUGUI>();

            // Ẩn hết ngay khi Awake — trước khi LoadData() được gọi
            HideAllElementalIcons();

            _isInitialized = true;
        }

        /// <summary>
        /// Tìm kiếm đệ quy một Transform con có tên chứa chuỗi đích (không phân biệt hoa thường).
        /// Giúp tìm được cả các object bị Unity đổi tên khi duplicate như "HeartIcon (1)".
        /// </summary>
        private Transform FindChildByName(Transform parent, string targetName)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
                Transform found = FindChildByName(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private bool _hasLoadedData = false;

        // Hữu ích cho việc Test: Nếu bạn kéo sẵn CardData vào Inspector và ấn Play, thẻ bài sẽ tự động load.
        private void Start()
        {
            if (cardData != null && !_hasLoadedData)
            {
                LoadData(cardData);
            }
        }


        // Hàm Helper nội bộ để xử lý hiện/ẩn icon
        private void SetStatVisible(ref TextMeshProUGUI txtRef, string iconName, int value)
        {
            if (txtRef == null) 
            {
                Transform icon = FindChildByName(transform, iconName);
                if (icon != null) txtRef = icon.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            bool show = (value > 0);
            if (txtRef != null)
            {
                txtRef.text = value.ToString();
                if (txtRef.transform.parent != null) txtRef.transform.parent.gameObject.SetActive(show);
            }
            else
            {
                Transform icon = FindChildByName(transform, iconName);
                if (icon != null) icon.gameObject.SetActive(show);
            }
        }

        private void SwapHeartIcon(HeartType type)
        {
            if (heartIconImage == null) return;
            switch (type)
            {
                case HeartType.Fragile: if (fragileHeartSprite != null) heartIconImage.sprite = fragileHeartSprite; break;
                case HeartType.Doom:    if (doomHeartSprite != null) heartIconImage.sprite = doomHeartSprite; break;
                case HeartType.Thorn:   if (thornHeartSprite != null) heartIconImage.sprite = thornHeartSprite; break;
                default:                if (normalHeartSprite != null) heartIconImage.sprite = normalHeartSprite; break;
            }
        }

        // Load data từ ScriptableObject lên giao diện
        public void LoadData(CardData data)
        {
            EnsureReferences();
            if (data == null) return;
            
            cardData = data;
            _hasLoadedData = true;
            
            nameText.text = cardData.cardName;
            int currentAtk = cardData.attack;
            int currentHp = cardData.health;

            // Lấy bonus từ RunData nếu có (Upgrade Event)
            if (ProjectM.GameManager.Instance != null && ProjectM.GameManager.Instance.RunData != null)
            {
                var bonus = ProjectM.GameManager.Instance.RunData.GetChampionBonus(cardData.name);
                currentAtk += bonus.attackBonus;
                currentHp += bonus.healthBonus;
            }

            SetStatVisible(ref attackText, "SwordIcon", currentAtk);
            SetStatVisible(ref healthText, "HeartIcon", currentHp);
            SetStatVisible(ref speedText, "SpeedIcon", cardData.speed);
            SetStatVisible(ref ultText, "UltIcon", cardData.ult);
            
            // Ẩn Shield Icon mặc định khi mới load bài
            if (shieldIconObj != null) shieldIconObj.SetActive(false);
            else FindChildByName(transform, "ShieldIcon")?.gameObject.SetActive(false);

            if (abilitiesText != null)
                abilitiesText.text = Skills.TextFormatter.Process(cardData.abilities, keywordDatabase);

            if (cardData.characterArt != null)
            {
                if (characterArtImage != null)
                {
                    characterArtImage.preserveAspect = true;
                    characterArtImage.sprite = cardData.characterArt;

                    // Chỉ override vị trí/scale nếu được set tường minh trong CardData.
                    // Nếu để (0,0) và 1.0 → giữ nguyên vị trí mặc định của Prefab.
                    if (cardData.artOffset != Vector2.zero)
                        characterArtImage.rectTransform.anchoredPosition = cardData.artOffset;
                    if (!Mathf.Approximately(cardData.artScale, 1f))
                        characterArtImage.rectTransform.localScale = Vector3.one * cardData.artScale;
                }
                else
                {
                    Debug.LogWarning($"[CardDisplay] Lá bài {cardData.cardName} có ảnh nhưng bạn chưa kéo UI Image vào ô Character Art Image trong Inspector!");
                }
            }

            // Tự động đổi khung bài (Card Frame) nếu có
            if (cardData.cardFrame != null && cardFrameImage != null)
            {
                cardFrameImage.sprite = cardData.cardFrame;
            }
            
            // Mặc định lật mặt trước khi load data
            if (cardFront != null) cardFront.SetActive(true);
            if (cardBack != null) cardBack.SetActive(false);

            // Ẩn toàn bộ icon nguyên tố khi khởi tạo — chỉ hiện khi có hiệu ứng thực sự
            HideAllElementalIcons();

            // QUAN TRỌNG: Dọn dẹp tất cả các icon bị duplicate (bản sao dư thừa)
            // Dùng .Contains("99") thay vì == "99" để đề phòng TextMeshPro tự chèn zero-width space hoặc ký tự ẩn
            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (tmp.text != null && tmp.text.Contains("99"))
                {
                    if (tmp.transform.parent != null)
                        tmp.transform.parent.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Cập nhật text nội tại khi Trinket thay đổi
        /// </summary>
        public void UpdateTrinketDescription(TrinketData trinket)
        {
            if (abilitiesText == null || cardData == null) return;
            
            // Ép Auto Sizing để text tự thu nhỏ lại vừa khung khi có thêm mô tả của Trinket
            abilitiesText.enableAutoSizing = true;
            abilitiesText.fontSizeMin = 10;
            if (abilitiesText.fontSizeMax > 50) abilitiesText.fontSizeMax = 24; 

            if (trinket == null)
            {
                abilitiesText.text = Skills.TextFormatter.Process(cardData.abilities, keywordDatabase);
            }
            else
            {
                string raw = string.IsNullOrWhiteSpace(cardData.abilities)
                    ? trinket.description
                    : cardData.abilities + $"\n{trinket.description}";
                    
                abilitiesText.text = Skills.TextFormatter.Process(raw, keywordDatabase);
                
                // DIAGNOSTIC LOG
                var rect = abilitiesText.rectTransform;
                Debug.Log($"[Diagnostic] Card '{cardData.cardName}' AbilitiesText -> Text: '{abilitiesText.text}', Active: {abilitiesText.gameObject.activeInHierarchy}, Color: {abilitiesText.color}, FontAuto: {abilitiesText.enableAutoSizing}, Overflow: {abilitiesText.overflowMode}, RectSize: {rect.rect.size}, Scale: {rect.localScale}");
            }
        }

        // Bật/tắt để lật bài
        public void SetFaceUp(bool faceUp)
        {
            cardBack.SetActive(!faceUp);
            cardFront.SetActive(faceUp);
        }

        /// <summary>
        /// Animation lật thẻ từ úp → ngửa bằng DOTween (scale X: 1→0, đổi mặt, 0→1).
        /// </summary>
        public System.Collections.IEnumerator FlipToFaceUpRoutine(float halfDuration = 0.1f)
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null) { SetFaceUp(true); yield break; }

            float originalScaleX = rect.localScale.x;

            // Nửa đầu: squish lại (Ease.InQuad — tăng tốc về 0)
            yield return rect
                .DOScaleX(0f, halfDuration)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();

            // Đổi sang mặt ngửa tại điểm giữa
            SetFaceUp(true);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardFlipClip);

            // Nửa sau: mở ra (Ease.OutBack — overshoot nhẹ → cảm giác "bật" ra)
            yield return rect
                .DOScaleX(originalScaleX, halfDuration)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }

        /// <summary>
        /// Load dữ liệu từ SkillData lên giao diện thẻ skill.
        /// Hiển thị tên, mô tả và artwork — ẩn các icon stat của tướng (ATK/HP/SPD).
        /// </summary>
        public void LoadSkillData(Skills.SkillData data)
        {
            EnsureReferences();
            if (data == null) return;

            // Tên skill
            if (nameText != null) nameText.text = data.skillName;

            // Mô tả thay vì abilities
            if (abilitiesText != null)
                abilitiesText.text = Skills.TextFormatter.Process(data.description, keywordDatabase);

            if (data.artwork != null && characterArtImage != null)
            {
                characterArtImage.preserveAspect = true;
                characterArtImage.sprite = data.artwork;

                // Chỉ override nếu được set tường minh
                if (data.artOffset != Vector2.zero)
                    characterArtImage.rectTransform.anchoredPosition = data.artOffset;
                if (!Mathf.Approximately(data.artScale, 1f))
                    characterArtImage.rectTransform.localScale = Vector3.one * data.artScale;
            }

            // Xử lý riêng cho thẻ Summon (Building): Lấy chỉ số từ thẻ buildingData để hiển thị
            CardData buildingData = null;
            if (data.customOverride != null && data.customOverride is SkillOverride_Summon summonOverride)
            {
                buildingData = summonOverride.buildingData;
            }

            if (buildingData != null)
            {
                SetStatVisible(ref attackText, "SwordIcon", buildingData.attack);
                SetStatVisible(ref healthText, "HeartIcon", buildingData.health);
                SetStatVisible(ref speedText, "SpeedIcon", buildingData.speed);
                SetStatVisible(ref ultText, "UltIcon", buildingData.ult);
                
                // Cập nhật đúng loại tim cho Building
                SwapHeartIcon(buildingData.defaultHeartType);
            }
            else
            {
                // Ẩn các text stat của tướng (không cần thiết cho skill)
                if (attackText != null) attackText.transform.parent?.gameObject.SetActive(false);
                else FindChildByName(transform, "SwordIcon")?.gameObject.SetActive(false);

                if (healthText != null) healthText.transform.parent?.gameObject.SetActive(false);
                else FindChildByName(transform, "HeartIcon")?.gameObject.SetActive(false);

                if (speedText  != null) speedText.transform.parent?.gameObject.SetActive(false);
                else FindChildByName(transform, "SpeedIcon")?.gameObject.SetActive(false);

                if (ultText    != null) ultText.transform.parent?.gameObject.SetActive(false);
                else FindChildByName(transform, "UltIcon")?.gameObject.SetActive(false);
            }

            // Tắt shield và elements
            if (shieldIconObj != null) shieldIconObj.SetActive(false);
            else FindChildByName(transform, "ShieldIcon")?.gameObject.SetActive(false);
            HideAllElementalIcons();

            SetFaceUp(true);

            // QUAN TRỌNG: Dọn dẹp tất cả các icon bị duplicate (bản sao dư thừa)
            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (tmp.text != null && tmp.text.Contains("99"))
                {
                    if (tmp.transform.parent != null)
                        tmp.transform.parent.gameObject.SetActive(false);
                }
            }

            // Failsafe cực mạnh cho thẻ Skill: Tắt luôn nguyên cái cột chứa stats (nếu tìm thấy) VÀ nếu không phải thẻ Summon
            Transform statsContainer = null;
            if (attackText != null && attackText.transform.parent != null) statsContainer = attackText.transform.parent.parent;
            else if (healthText != null && healthText.transform.parent != null) statsContainer = healthText.transform.parent.parent;
            
            if (statsContainer != null && buildingData == null)
            {
                // Tắt hoàn toàn container (trừ khi tên nó là CardFront - tránh tắt nhầm cả lá bài)
                if (!statsContainer.name.Contains("Front") && !statsContainer.name.Contains("Card"))
                {
                    statsContainer.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Bật/Tắt icon nguyên tố.
        /// Dùng SetActive để Vertical Layout Group tự động dồn lên/xóa khoảng trống.
        /// </summary>
        private void SetElementalIconVisible(GameObject iconObj, bool visible)
        {
            if (iconObj != null)
            {
                iconObj.SetActive(visible);
            }
        }

        /// <summary>
        /// Ẩn toàn bộ icon nguyên tố. Gọi khi khởi tạo thẻ bài.
        /// </summary>
        public void HideAllElementalIcons()
        {
            SetElementalIconVisible(frostIconObj, false);
            SetElementalIconVisible(bleedIconObj, false);
            SetElementalIconVisible(decayIconObj, false);
            SetElementalIconVisible(chainIconObj, false);
        }

        /// <summary>
        /// Cập nhật hiển thị UI của các icon nguyên tố.
        /// Chỉ hiển thị nếu thẻ là loại Enemy VÀ stack > 0.
        /// </summary>
        public void UpdateElementalUI(ProjectM.Elements.ElementalHandler handler)
        {
            if (handler == null) return;

            // Chỉ thẻ Enemy mới hiển thị icon nguyên tố
            bool isEnemy = cardData != null && cardData.cardType == CardType.Enemy;
            if (!isEnemy)
            {
                HideAllElementalIcons();
                return;
            }

            // Frost
            int frostStacks = handler.GetStacks(ProjectM.Elements.ElementType.Frost);
            SetElementalIconVisible(frostIconObj, frostStacks > 0);
            if (frostText != null) frostText.text = frostStacks.ToString();

            // Bleed
            int bleedStacks = handler.GetStacks(ProjectM.Elements.ElementType.Bleed);
            SetElementalIconVisible(bleedIconObj, bleedStacks > 0);
            if (bleedText != null) bleedText.text = bleedStacks.ToString();

            // Chain
            int chainStacks = handler.GetStacks(ProjectM.Elements.ElementType.Chain);
            SetElementalIconVisible(chainIconObj, chainStacks > 0);
            if (chainText != null) chainText.text = chainStacks.ToString();

            // Decay (Hiển thị số lượt DoT còn lại)
            int decayDuration = handler.GetDotDuration(ProjectM.Elements.ElementType.Decay);
            SetElementalIconVisible(decayIconObj, decayDuration > 0);
            if (decayText != null) decayText.text = decayDuration.ToString();
        }
    }
}
