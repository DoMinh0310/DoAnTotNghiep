using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using ProjectM.Skills;
using ProjectM.Cards;
using ProjectM.Managers;
using ProjectM.Map;

namespace ProjectM.Inventory
{
    /// <summary>
    /// Quản lý UI Inventory Panel.
    /// Chứa logic animation mở/đóng, populate thẻ tướng/skill/relic/trinket, và xử lý event Drag-Drop relic.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        /// <summary>
        /// Event bắn ra khi Inventory mở (true) hoặc đóng (false).
        /// Các UI khác (như bảng Vàng) có thể lắng nghe để tự động ẩn/hiện.
        /// </summary>
        public static event System.Action<bool> OnInventoryToggled;

        [Header("UI References")]
        public CanvasGroup inventoryPanel;
        public RectTransform circleReveal;  // Image vòng tròn trắng để làm mask animation
        public RectTransform bagIcon;       // Nút túi đồ ở combat UI
        public Button closeButton;

        [Header("Containers")]
        public Transform championRow;       // Hàng trên cùng (chứa Tướng + Slot trang bị)
        public Transform skillGrid;         // Lưới chứa Support Deck
        public Transform activeSkillRow;    // Hàng dưới cùng (chứa Active Skill từ Relic)
        public Transform relicSidebar;      // Cột trái (chứa Collection Relic)
        public Transform trinketSidebar;    // Cột phải (chứa Collection Trinket)

        [Header("Prefabs")]
        public GameObject cardPrefab;       // Dùng cho Tướng
        public GameObject skillPrefab;      // Dùng cho Skill
        public RelicSlotUI   relicSlotPrefab;
        public TrinketSlotUI trinketSlotPrefab;

        [Header("Sidebar Settings")]
        [Tooltip("Số slot tối thiểu luôn hiển thị trong sidebar (bần còn lại là placeholder trống).")]
        public int sidebarMinSlots = 6;
        [Tooltip("Kích thước của mỗi slot (dagger, pixel).")]
        public Vector2 slotSize = new Vector2(100f, 100f);
        [Tooltip("Màu nền của slot trống trong sidebar.")]
        public Color emptySlotColor = new Color(0.15f, 0.10f, 0.05f, 0.55f);

        [Header("Map Scene - Card Database")]
        [Tooltip("Kéo tất cả SkillData asset vào đây để Inventory có thể tra cứu theo ID khi ở Map Scene.")]
        public List<SkillData> allSkillAssets = new List<SkillData>();
        [Tooltip("Kéo tất cả CardData asset vào đây để Inventory có thể tra cứu theo ID khi ở Map Scene.")]
        public List<CardData> allCardAssets = new List<CardData>();

        private ChampionSetup _setup;
        private bool _isOpen = false;
        // Danh sách placeholder slot (chỉ hiện khi đang drag)
        private readonly System.Collections.Generic.List<GameObject> _placeholders
            = new System.Collections.Generic.List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (inventoryPanel != null)
            {
                inventoryPanel.alpha = 0;
                inventoryPanel.gameObject.SetActive(false);
            }

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseInventory);
                
            // Tự động tìm Button trên túi đồ và gắn lệnh Mở
            if (bagIcon != null)
            {
                var bagBtn = bagIcon.GetComponent<Button>();
                if (bagBtn != null)
                    bagBtn.onClick.AddListener(OpenInventory);
                else
                    Debug.LogWarning("[InventoryManager] Bag Icon không có component Button! Hãy Add Component -> Button vào Inventory_Bag nhé.");
            }
        }

        private void Start()
        {
            // Khởi tạo tham chiếu tới setup
            if (SkillHandManager.Instance != null)
            {
                _setup = SkillHandManager.Instance.championSetup;
            }
            else if (GameManager.Instance?.RunData?.championSetup != null)
            {
                // Đang ở Map Scene — dùng ChampionSetup được lưu trong RunData
                _setup = GameManager.Instance.RunData.championSetup;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ANIMATION MỞ / ĐÓNG
        // ══════════════════════════════════════════════════════════════════

        public void OpenInventory()
        {
            if (ProjectM.Managers.BattleManager.IsInputBlocked) return;
            if (_isOpen || inventoryPanel == null || circleReveal == null) return;
            
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.inventoryToggleClip);
            
            // LUÔN cập nhật lại _setup từ dữ liệu runtime thực tế (tránh lỗi do gán cứng trong Inspector)
            if (SkillHandManager.Instance != null)
                _setup = SkillHandManager.Instance.championSetup;
            else if (GameManager.Instance?.RunData?.championSetup != null)
                _setup = GameManager.Instance.RunData.championSetup;

            _isOpen = true;
            inventoryPanel.gameObject.SetActive(true);
            inventoryPanel.alpha = 0f;

            OnInventoryToggled?.Invoke(true); // Báo cho các UI khác biết túi đồ đang mở

            // Không can thiệp vào vị trí của CircleReveal nữa.
            // Hãy để Unity giữ nguyên vị trí mà bạn đã kéo thả trong Editor.

            circleReveal.localScale = Vector3.zero;
            circleReveal.gameObject.SetActive(true);

            // Chạy DOTween Sequence: Vòng tròn to ra (Wipe) + Panel mờ hiện lên (Fade)
            Sequence seq = DOTween.Sequence();
            seq.Append(circleReveal.DOScale(new Vector3(50f, 50f, 1f), 0.45f).SetEase(Ease.OutCirc));
            seq.Insert(0.1f, inventoryPanel.DOFade(1f, 0.3f));
            seq.OnComplete(() => {
                circleReveal.gameObject.SetActive(false);
                PopulateInventory();
            });
        }

        public void CloseInventory()
        {
            if (!_isOpen || inventoryPanel == null || circleReveal == null) return;
            _isOpen = false;

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.inventoryToggleClip);

            OnInventoryToggled?.Invoke(false); // Báo cho các UI khác biết túi đồ đã đóng

            circleReveal.gameObject.SetActive(true);
            
            Sequence seq = DOTween.Sequence();
            seq.Append(circleReveal.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InCirc));
            seq.Insert(0.1f, inventoryPanel.DOFade(0f, 0.25f));
            seq.OnComplete(() => {
                inventoryPanel.gameObject.SetActive(false);
                ClearInventory();
            });
        }

        // ══════════════════════════════════════════════════════════════════
        // POPULATE DỮ LIỆU
        // ══════════════════════════════════════════════════════════════════

        private void PopulateInventory()
        {
            // Nếu đang ở Map Scene (không có SkillHandManager) thì đọc dữ liệu từ RunData
            if (_setup == null)
            {
                PopulateFromRunData();
                return;
            }

            ClearInventory();

            _placeholders.Clear();

            // 1. Sidebar — deduplicate và chỉ hiện Relic/Trinket CHƯA được trang bị
            var equippedRelics   = new System.Collections.Generic.HashSet<RelicData>();
            var equippedTrinkets = new System.Collections.Generic.HashSet<ProjectM.Skills.TrinketData>();
            foreach (var c in _setup.champions)
            {
                if (c.equippedRelic   != null) equippedRelics.Add(c.equippedRelic);
                if (c.equippedTrinket != null) equippedTrinkets.Add(c.equippedTrinket);
            }

            int relicCount = 0;
            var seenRelics     = new System.Collections.Generic.HashSet<RelicData>();
            var seenRelicNames = new System.Collections.Generic.HashSet<string>(); // chống trùng theo tên
            foreach (var relic in _setup.ownedRelics)
            {
                if (relic == null) continue;
                if (equippedRelics.Contains(relic)) continue;
                if (!seenRelics.Add(relic)) continue;         // trùng reference
                if (!seenRelicNames.Add(relic.relicName)) continue; // trùng tên
                var slot = Instantiate(relicSlotPrefab, relicSidebar);
                // Ép kích thước vừa khung sidebar
                var slotRT = slot.GetComponent<RectTransform>();
                if (slotRT != null) slotRT.sizeDelta = slotSize;

                // FIX: Xóa nền trắng mặc định của clone (set Alpha = 0)
                var rootImg = slot.GetComponent<UnityEngine.UI.Image>();
                if (rootImg != null) rootImg.color = Color.clear;

                slot.Setup(relic, -1);
                relicCount++;
            }
            for (int i = relicCount; i < sidebarMinSlots; i++)
                _placeholders.Add(SpawnEmptyPlaceholder(relicSidebar));

            int trinketCount = 0;
            var seenTrinkets     = new System.Collections.Generic.HashSet<ProjectM.Skills.TrinketData>();
            var seenTrinketNames = new System.Collections.Generic.HashSet<string>();
            foreach (var trinket in _setup.ownedTrinkets)
            {
                if (trinket == null) continue;
                if (equippedTrinkets.Contains(trinket)) continue;
                if (!seenTrinkets.Add(trinket)) continue;
                if (!seenTrinketNames.Add(trinket.trinketName)) continue;
                var slot = Instantiate(trinketSlotPrefab, trinketSidebar);
                var slotRT = slot.GetComponent<RectTransform>();
                if (slotRT != null) slotRT.sizeDelta = slotSize;

                // FIX: Xóa nền trắng mặc định của clone (set Alpha = 0) theo yêu cầu
                var rootImg = slot.GetComponent<UnityEngine.UI.Image>();
                if (rootImg != null) rootImg.color = Color.clear;

                slot.Setup(trinket, -1);
                trinketCount++;
            }
            for (int i = trinketCount; i < sidebarMinSlots; i++)
                _placeholders.Add(SpawnEmptyPlaceholder(trinketSidebar));

            // Placeholder ẩn mặc định, chỉ hiện khi drag
            ShowPlaceholders(false);

            // 2. Champions Row (Tướng + Slot trang bị đang gắn)
            for (int i = 0; i < _setup.champions.Count; i++)
            {
                var champEntry = _setup.champions[i];
                
                // Spawn Thẻ Tướng
                var champGo = Instantiate(cardPrefab, championRow);
                var invDisplay = champGo.AddComponent<InventoryCardDisplay>();

                // Đọc bonus từ Smith Event (nếu có)
                var bonus = GameManager.Instance?.RunData?.GetChampionBonus(champEntry.championData?.name ?? "")
                            ?? new Map.ChampionStatBonus();

                // Lấy mô tả từ Trinket và Relic đang gắn
                string extraAbilities = "";
                if (champEntry.equippedTrinket != null)
                {
                    extraAbilities += champEntry.equippedTrinket.description;
                }
                if (champEntry.equippedRelic != null)
                {
                    if (!string.IsNullOrEmpty(extraAbilities)) extraAbilities += "\n";
                    extraAbilities += champEntry.equippedRelic.description;
                }

                invDisplay.InitChampion(champEntry.championData, bonus.attackBonus, bonus.healthBonus, extraAbilities);

                // Toàn bộ thẻ là drop target cho relic (snap như nam châm)
                champGo.AddComponent<ChampionCardRelicDrop>();

                // Tìm RelicSlotUI trên Card_Prefab — force-active trước khi gọi Setup()
                var rSlot = champGo.GetComponentInChildren<RelicSlotUI>(true);
                if (rSlot != null)
                {
                    rSlot.gameObject.SetActive(true);          // đảm bảo không bị ẩn bởi Awake()
                    // Ép cùng kích thước với TrinketSlot để đồng bộ
                    var rSlotRT = rSlot.GetComponent<RectTransform>();
                    if (rSlotRT != null) rSlotRT.sizeDelta = slotSize;
                    rSlot.Setup(champEntry.equippedRelic, i);
                }

                var tSlot = champGo.GetComponentInChildren<TrinketSlotUI>(true);
                if (tSlot != null)
                {
                    tSlot.gameObject.SetActive(true);
                    var tSlotRT = tSlot.GetComponent<RectTransform>();
                    if (tSlotRT != null) tSlotRT.sizeDelta = slotSize;
                    tSlot.Setup(champEntry.equippedTrinket, i);
                }

                // 3. Active Skills Row (Skill sinh ra từ Relic)
                if (champEntry.equippedRelic != null && champEntry.equippedRelic.possibleSkills != null)
                {
                    foreach (var pSkill in champEntry.equippedRelic.possibleSkills)
                    {
                        if (pSkill == null) continue;
                        var aSkillGo = Instantiate(skillPrefab, activeSkillRow);
                        var skillDisplay = aSkillGo.AddComponent<InventoryCardDisplay>();
                        skillDisplay.InitSkill(pSkill);
                    }
                }
            }

            // 4. Support Deck (Skill Grid - hàng 2)
            foreach (var skill in _setup.supportDeck)
            {
                var skillGo = Instantiate(skillPrefab, skillGrid);
                var skillDisplay = skillGo.AddComponent<InventoryCardDisplay>();
                skillDisplay.InitSkill(skill);
            }
        }

        private void ClearInventory()
        {
            if (championRow != null) foreach (Transform child in championRow) Destroy(child.gameObject);
            if (skillGrid != null) foreach (Transform child in skillGrid) Destroy(child.gameObject);
            if (activeSkillRow != null) foreach (Transform child in activeSkillRow) Destroy(child.gameObject);
            if (relicSidebar != null) foreach (Transform child in relicSidebar) Destroy(child.gameObject);
            if (trinketSidebar != null) foreach (Transform child in trinketSidebar) Destroy(child.gameObject);
        }

        /// <summary>
        /// Hiển thị bộ bài khi ở Map Scene, đọc từ RunData.playerDeckIDs.
        /// </summary>
        private void PopulateFromRunData()
        {
            var runData = GameManager.Instance?.RunData;
            if (runData == null)
            {
                Debug.LogWarning("[Inventory] Không có RunData! Không thể hiển thị bộ bài.");
                return;
            }

            ClearInventory();

            // 1. Khởi tạo các ô trống cho Sidebar (để giữ layout)
            _placeholders.Clear();
            int relicCount = 0; // Tạm thời chưa có dữ liệu relic trong RunData
            int trinketCount = runData.playerTrinketIDs?.Count ?? 0;
            
            for (int i = relicCount; i < sidebarMinSlots; i++)
                _placeholders.Add(SpawnEmptyPlaceholder(relicSidebar));
                
            for (int i = trinketCount; i < sidebarMinSlots; i++)
                _placeholders.Add(SpawnEmptyPlaceholder(trinketSidebar));
                
            ShowPlaceholders(false);

            if (runData.playerDeckIDs == null || runData.playerDeckIDs.Count == 0)
            {
                Debug.Log("[Inventory] Bộ bài đang trống (chưa chọn thẻ nào).");
                return;
            }

            int championIndex = 0;
            foreach (var itemID in runData.playerDeckIDs)
            {
                // Tìm trong SkillData trước
                var skillAsset = allSkillAssets.Find(s => s != null && s.name == itemID);
                if (skillAsset != null)
                {
                    var go = Instantiate(skillPrefab, skillGrid);
                    var display = go.AddComponent<InventoryCardDisplay>();
                    display.InitSkill(skillAsset);
                    continue;
                }

                // Nếu không phải Skill thì tìm trong CardData (Tướng)
                var cardAsset = allCardAssets.Find(c => c != null && c.name == itemID);
                if (cardAsset != null)
                {
                    var go = Instantiate(cardPrefab, championRow); // Đã chuyển sang championRow
                    var display = go.AddComponent<InventoryCardDisplay>();
                    display.InitChampion(cardAsset);

                    // Thêm các slot trang bị rỗng cho Tướng để giống bên Battle
                    go.AddComponent<ChampionCardRelicDrop>();
                    var rSlot = go.GetComponentInChildren<RelicSlotUI>(true);
                    if (rSlot != null)
                    {
                        rSlot.gameObject.SetActive(true);
                        rSlot.Setup(null, championIndex); // Tạm thời null vì RunData chưa lưu Relic trang bị
                    }

                    var tSlot = go.GetComponentInChildren<TrinketSlotUI>(true);
                    if (tSlot != null)
                    {
                        tSlot.gameObject.SetActive(true);
                        tSlot.Setup(null, championIndex); // Tạm thời null
                    }
                    
                    championIndex++;
                    continue;
                }

                Debug.LogWarning($"[Inventory] Không tìm thấy asset nào với tên '{itemID}' trong database. Bạn có đã kéo nó vào 'All Skill Assets' hoặc 'All Card Assets' chưa?");
            }
        }

        /// <summary>
        /// Tạo slot rỗng mang tính thẩm mỹ trong sidebar.
        /// Slot ẩn mặc định — chỉ hiện khi đang drag.
        /// </summary>
        private GameObject SpawnEmptyPlaceholder(Transform parent)
        {
            var go = new GameObject("Slot_Empty");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = slotSize;

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = emptySlotColor;
            img.raycastTarget = true;

            go.AddComponent<RelicSidebarDropZone>();

            go.SetActive(false); // Ẩn mặc định
            return go;
        }

        /// <summary>Hiện hoặc ẩn tất cả placeholder slots.</summary>
        private void ShowPlaceholders(bool show)
        {
            foreach (var ph in _placeholders)
                if (ph != null) ph.SetActive(show);
        }

        // ══════════════════════════════════════════════════════════════════
        // XỬ LÝ SWAP RELIC
        // ══════════════════════════════════════════════════════════════════

        public void OnRelicDragStart(RelicSlotUI sourceSlot)
        {
            ShowPlaceholders(true);

            // Dùng true để tìm CẢ slot đang ẩn (champion slot trống)
            var allSlots = inventoryPanel.GetComponentsInChildren<RelicSlotUI>(true);
            foreach (var slot in allSlots)
            {
                if (slot == sourceSlot) continue;
                slot.gameObject.SetActive(true); // Bật champion slot ẩn thành drop target
                slot.ShowGlow(true);
            }
        }

        public void OnRelicDragEnd()
        {
            // Ẩn placeholder lại
            ShowPlaceholders(false);

            var allSlots = inventoryPanel.GetComponentsInChildren<RelicSlotUI>();
            foreach (var slot in allSlots)
                slot.ShowGlow(false);
        }

        public void HandleRelicDrop(RelicSlotUI source, RelicSlotUI target)
        {
            if (_setup == null) return;

            RelicData relicA = source.currentRelic;
            RelicData relicB = target.currentRelic;

            // Swap data giữa 2 slot
            if (source.championIndex >= 0)
                _setup.champions[source.championIndex].equippedRelic = relicB;
            
            if (target.championIndex >= 0)
                _setup.champions[target.championIndex].equippedRelic = relicA;

            // Đảm bảo cả 2 relic đều còn trong ownedRelics để khi unequip thì tự động hiện lại sidebar
            if (relicA != null && !_setup.ownedRelics.Contains(relicA))
                _setup.ownedRelics.Add(relicA);
            if (relicB != null && !_setup.ownedRelics.Contains(relicB))
                _setup.ownedRelics.Add(relicB);

            // Reload lại UI
            PopulateInventory();
        }

        /// <summary>
        /// Tháo Relic khỏi tướng (right-click vào slot của tướng trong Inventory).
        /// Relic sẽ quay về sidebar pool.
        /// </summary>
        public void HandleRelicUnequip(RelicSlotUI source)
        {
            if (_setup == null || source.championIndex < 0) return;

            // Đảm bảo Relic khởi đầu (chưa có trong túi) được tống vào túi trước khi tháo
            if (source.currentRelic != null && !_setup.ownedRelics.Contains(source.currentRelic))
            {
                _setup.ownedRelics.Add(source.currentRelic);
            }

            _setup.champions[source.championIndex].equippedRelic = null;
            Debug.Log($"[Inventory] Đã tháo Relic '{source.currentRelic?.relicName}' khỏi tướng #{source.championIndex}.");

            PopulateInventory();
        }

        // ══════════════════════════════════════════════════════════════════
        // XỬ LÝ SWAP TRINKET
        // ══════════════════════════════════════════════════════════════════

        public void OnTrinketDragStart(TrinketSlotUI sourceSlot)
        {
            ShowPlaceholders(true);

            // Dùng true để tìm CẢ slot đang ẩn (champion slot trống)
            var allSlots = inventoryPanel.GetComponentsInChildren<TrinketSlotUI>(true);
            foreach (var slot in allSlots)
            {
                if (slot == sourceSlot) continue;
                slot.gameObject.SetActive(true); // Bật champion slot ẩn thành drop target
                slot.ShowGlow(true);
            }
        }

        public void OnTrinketDragEnd()
        {
            ShowPlaceholders(false);

            var allSlots = inventoryPanel.GetComponentsInChildren<TrinketSlotUI>();
            foreach (var slot in allSlots)
                slot.ShowGlow(false);

            // Re-sync UI để đảm bảo trinket không mất khi thả vào vùng không hợp lệ
            if (_setup != null)
                PopulateInventory();
        }

        public void HandleTrinketDrop(TrinketSlotUI source, TrinketSlotUI target)
        {
            if (_setup == null) return;

            TrinketData trinketA = source.currentTrinket;
            TrinketData trinketB = target.currentTrinket;

            // Swap data giữa 2 slot
            if (source.championIndex >= 0)
                _setup.champions[source.championIndex].equippedTrinket = trinketB;
            
            if (target.championIndex >= 0)
                _setup.champions[target.championIndex].equippedTrinket = trinketA;

            // Đảm bảo cả 2 trinket đều còn trong ownedTrinkets.
            // Quan trọng: khi trinketB bị đẩy khỏi chỗ của tướng nó phải được trở về sidebar.
            if (trinketA != null && !_setup.ownedTrinkets.Contains(trinketA))
                _setup.ownedTrinkets.Add(trinketA);
            if (trinketB != null && !_setup.ownedTrinkets.Contains(trinketB))
                _setup.ownedTrinkets.Add(trinketB);

            // Reload lại UI
            PopulateInventory();
        }

        public void HandleTrinketUnequip(TrinketSlotUI source)
        {
            if (_setup == null || source.championIndex < 0) return;

            // Đảm bảo Trinket khởi đầu được add vào danh sách túi đồ trước khi tháo
            if (source.currentTrinket != null && !_setup.ownedTrinkets.Contains(source.currentTrinket))
            {
                _setup.ownedTrinkets.Add(source.currentTrinket);
            }

            _setup.champions[source.championIndex].equippedTrinket = null;
            Debug.Log($"[Inventory] Đã tháo Trinket '{source.currentTrinket?.trinketName}' khỏi tướng #{source.championIndex}.");

            PopulateInventory();
        }
    }
}
