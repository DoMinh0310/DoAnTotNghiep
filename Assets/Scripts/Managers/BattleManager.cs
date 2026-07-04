using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using ProjectM.Inventory;
using DG.Tweening;

namespace ProjectM.Managers
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Kéo nút chuông (End Turn button) vào đây")]
        public Button endTurnButton;

        [Tooltip("Kéo HandRegion (chứa tướng trên tay) vào đây")]
        [SerializeField] private Transform championHandRegion;

        [Tooltip("Prefab thẻ tướng để spawn vào tay (Card_Prefab)")]
        [SerializeField] private GameObject championPrefab;

        [Header("Settings")]
        [Tooltip("Delay nhỏ (giây) giữa mỗi thẻ tấn công để dễ theo dõi")]
        public float delayBetweenCards = 0.3f;
        
        [Header("VFX Settings")]
        [Tooltip("Prefab hiệu ứng đòn đánh thường")]
        public GameObject normalHitVfxPrefab;
        [Tooltip("Prefab hiệu ứng Particle chạy khi mục tiêu dính Bleed từ Skill")]
        public GameObject bleedHitVfxPrefab;
        [Tooltip("Prefab hiệu ứng Particle chạy khi mục tiêu dính Frost từ Skill")]
        public GameObject frostHitVfxPrefab;
        [Tooltip("Prefab hiệu ứng Particle chạy khi mục tiêu dính Chain từ Skill")]
        public GameObject chainHitVfxPrefab;
        [Tooltip("Prefab hiệu ứng Particle chạy khi mục tiêu dính Decay từ Skill")]
        public GameObject decayHitVfxPrefab;

        // Trạng thái
        private bool isTurnProcessing = false;
        public bool IsTurnProcessing => isTurnProcessing;
        [Tooltip("Số lượt trôi qua")]
        public int turnCount = 0;

        private List<Cards.CardData> _buildingDrawPile = new();

        [Header("Animation Tuning")]
        // Giai đoạn chuẩn bị: lượt free để đặt tướng, chưa có combat
        private bool isPreparationPhase = true;
        public bool IsPreparationPhase => isPreparationPhase;

        /// <summary>True trong lúc animation deal skill hoặc người chơi đang dùng thẻ — khóa mọi input của người chơi.</summary>
        public static bool IsInputLocked { get; set; } = false;

        /// <summary>True nếu đang bị khóa input hoặc đang trong combat phase (IsTurnProcessing = true).</summary>
        public static bool IsInputBlocked => IsInputLocked || (Instance != null && Instance.IsTurnProcessing);

        // ══════════════════════════════════════════
        // KHỞI TẠO
        // ══════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Phát nhạc nền Combat
            AudioManager.Instance?.PlayBattleMusic();

            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(() => EndTurn(drawCard: true)); // Chuông = draw + combat
            else
                Debug.LogWarning("[BattleManager] Chưa gán End Turn Button!");

            // Khóa nút chuông — chỉ mở sau khi địch spawn và deal skill xong
            SetEndTurnButtonInteractable(false);

            // Bắt đầu stage: spawn địch trước, sau đó mới prep phase
            StartCoroutine(InitBattleSequence());
        }

        private IEnumerator InitBattleSequence()
        {
            // 1. Spawn tướng từ ChampionSetup vào HandRegion
            yield return StartCoroutine(SpawnChampionsFromSetup());

            // 2. Spawn wave quái địch đầu tiên
            EnemyWaveManager waveManager = EnemyWaveManager.Instance;
            if (waveManager != null)
                yield return StartCoroutine(waveManager.StartStage());

            // 3. Vẫn khóa nút chuông và túi đồ — chờ người chơi đặt tướng lên sàn
            SetEndTurnButtonInteractable(false);
            InventoryManager.Instance?.SetBagIconInteractable(false);
            BattleDebugger.Log("⚔️ Quái đã xuất hiện! Giai đoạn CHUẨN BỊ bắt đầu — Đặt tất cả tướng lên sàn!");
        }

        /// <summary>
        /// Đọc ChampionSetup, spawn từng thẻ tướng vào HandRegion,
        /// rồi gán CardData + RelicHandler tương ứng.
        /// </summary>
        private IEnumerator SpawnChampionsFromSetup()
        {
            var skillHand = Skills.SkillHandManager.Instance;
            if (skillHand == null || skillHand.championSetup == null)
            {
                Debug.LogWarning("[BattleManager] Chưa gán ChampionSetup vào SkillHandManager!");
                yield break;
            }

            // Nạp Building Deck
            _buildingDrawPile.Clear();
            if (skillHand.championSetup.buildingDeck != null)
            {
                _buildingDrawPile.AddRange(skillHand.championSetup.buildingDeck);
                ShuffleList(_buildingDrawPile);
            }

            if (championPrefab == null)
            {
                Debug.LogWarning("[BattleManager] Chưa gán Champion Prefab (Card_Prefab) vào BattleManager!");
                yield break;
            }

            if (championHandRegion == null)
            {
                Debug.LogWarning("[BattleManager] Chưa gán Champion Hand Region!");
                yield break;
            }

            // Xoá tất cả tướng cũ đặt sẵn trong scene (nếu có)
            foreach (Transform child in championHandRegion)
                Destroy(child.gameObject);

            var setup = skillHand.championSetup;
            BattleDebugger.Log($"[BattleManager] Spawning {setup.champions.Count} tướng từ '{setup.name}'...");

            foreach (var entry in setup.champions)
            {
                if (entry.championData == null) { Debug.LogWarning("[BattleManager] ChampionEntry thiếu championData!"); continue; }

                // Spawn prefab vào HandRegion
                var go = Instantiate(championPrefab, championHandRegion);
                go.name = $"Champion_{entry.championData.cardName}";

                // Load hiển thị thẻ (ảnh, chỉ số, khung)
                var display = go.GetComponentInChildren<CardDisplay>(true);
                if (display != null)
                {
                    display.LoadData(entry.championData);

                    // Override chỉ số nếu có bonus từ Smith Event
                    if (GameManager.Instance?.RunData != null)
                    {
                        var bonus = GameManager.Instance.RunData.GetChampionBonus(entry.championData.name);
                        if (bonus.attackBonus != 0 && display.attackText != null)
                            display.attackText.text = (entry.championData.attack + bonus.attackBonus).ToString();
                        if (bonus.healthBonus != 0 && display.healthText != null)
                            display.healthText.text = (entry.championData.health + bonus.healthBonus).ToString();
                    }
                }
                else
                    Debug.LogWarning("[BattleManager] Card_Prefab thiếu CardDisplay!");

                // Cập nhật giao diện của 2 ô trang bị (Relic/Trinket) trên thẻ tướng
                var rSlot = go.GetComponentInChildren<RelicSlotUI>(true);
                if (rSlot != null) rSlot.Setup(entry.equippedRelic, -1);

                var tSlot = go.GetComponentInChildren<TrinketSlotUI>(true);
                if (tSlot != null) tSlot.Setup(entry.equippedTrinket, -1);

                // Gắn và init Relic Logic
                var relicHandler = go.GetComponent<RelicHandler>() ?? go.AddComponent<RelicHandler>();
                if (entry.equippedRelic != null)
                    relicHandler.EquipRelic(entry.equippedRelic);

                // Gắn và init Trinket Logic
                var trinketHandler = go.GetComponent<TrinketHandler>() ?? go.AddComponent<TrinketHandler>();
                if (entry.equippedTrinket != null)
                {
                    trinketHandler.EquipTrinket(entry.equippedTrinket);

                    // Cập nhật phần Abilities của thẻ tướng để hiển thị mô tả nội tại Trinket
                    if (display != null)
                        display.UpdateTrinketDescription(entry.equippedTrinket);
                }

                BattleDebugger.Log($"  ✅ '{entry.championData.cardName}' | Relic: {entry.equippedRelic?.relicName ?? "none"} | Trinket: {entry.equippedTrinket?.trinketName ?? "none"}");
            }

            // Nhường 1 frame để PlayerHand.UpdateHandLayout() xếp bài đúng vị trí
            yield return null;
        }

        /// <summary>
        /// Đồng bộ lại trang bị (Relic, Trinket) cho tất cả các tướng đang có trong trận đấu (trên tay và trên bàn cờ)
        /// theo dữ liệu mới nhất từ ChampionSetup. Gọi khi người chơi thay đổi trang bị trong Inventory.
        /// </summary>
        public void SyncChampionEquipments()
        {
            var setup = Skills.SkillHandManager.Instance?.championSetup;
            if (setup == null) return;

            var allCards = new List<Cards.CardBattle>();
            if (championHandRegion != null)
                allCards.AddRange(championHandRegion.GetComponentsInChildren<Cards.CardBattle>(true));
            if (BattleGrid.Instance != null)
                allCards.AddRange(BattleGrid.Instance.GetAllPlayerCards());

            foreach (var entry in setup.champions)
            {
                if (entry == null || entry.championData == null) continue;

                var card = allCards.Find(c => c != null && c.GetComponentInChildren<Cards.CardDisplay>(true)?.cardData == entry.championData);
                if (card == null) continue;

                var go = card.gameObject;
                var rSlot = go.GetComponentInChildren<Inventory.RelicSlotUI>(true);
                if (rSlot != null) rSlot.Setup(entry.equippedRelic, -1);

                var tSlot = go.GetComponentInChildren<Inventory.TrinketSlotUI>(true);
                if (tSlot != null) tSlot.Setup(entry.equippedTrinket, -1);

                var relicHandler = go.GetComponent<Cards.RelicHandler>() ?? go.AddComponent<Cards.RelicHandler>();
                if (entry.equippedRelic != null)
                    relicHandler.EquipRelic(entry.equippedRelic);
                else
                    relicHandler.UnequipRelic();

                var trinketHandler = go.GetComponent<Cards.TrinketHandler>() ?? go.AddComponent<Cards.TrinketHandler>();
                if (entry.equippedTrinket != null)
                    trinketHandler.EquipTrinket(entry.equippedTrinket);
                else
                    trinketHandler.UnequipTrinket();

                var display = go.GetComponentInChildren<Cards.CardDisplay>(true);
                if (display != null)
                    display.UpdateTrinketDescription(entry.equippedTrinket);
            }
        }


        // ══════════════════════════════════════════
        // END TURN (Điểm vào chính)
        // ══════════════════════════════════════════

        /// <summary>
        /// drawCard = false : chỉ chạy combat, KHÔNG rút bài (dùng skill / đặt tướng).
        /// drawCard = true  : rút 1 lá trước rồi chạy combat (bấm chuông).
        /// </summary>
        public void EndTurn(bool drawCard = false)
        {
            if (isTurnProcessing) { BattleDebugger.Warn("Đang xử lý lượt, vui lòng chờ..."); return; }

            if (isPreparationPhase)
            {
                isPreparationPhase = false;
                InventoryManager.Instance?.SetBagIconInteractable(true);
                BattleDebugger.Log("✅ Giai đoạn chuẩn bị kết thúc. Bắt đầu chiến đấu!");
                StartCoroutine(ProcessTurn(drawCard));
                return;
            }

            StartCoroutine(ProcessTurn(drawCard));
        }

        /// <summary>
        /// Được gọi bởi CardDragHandler khi người chơi thả bài thành công lên sàn.
        /// </summary>
        public void OnCardPlayedToBoard()
        {
            // Trong prep phase: kiểm tra xem đã đặt hết tướng chưa
            if (isPreparationPhase)
            {
                BattleDebugger.Log("🛡️ Tướng được đặt lên sàn (Prep Phase).");
                CheckAllChampionsPlaced();
                return;
            }
            BattleDebugger.Log("Người chơi thả bài → Kết thúc lượt.");
            EndTurn();
        }

        /// <summary>
        /// Đếm champion còn lại trong tay. Nếu 0 → bắt đầu deal skill.
        /// </summary>
        private void CheckAllChampionsPlaced()
        {
            if (championHandRegion == null)
            {
                Debug.LogWarning("[BattleManager] championHandRegion chưa được gán trong Inspector!");
                return;
            }

            int championsInHand = 0;
            foreach (Transform child in championHandRegion)
            {
                var display = child.GetComponentInChildren<Cards.CardDisplay>();
                if (display?.cardData?.cardType == Cards.CardType.Champion)
                    championsInHand++;
            }

            Debug.Log($"[BattleManager] 🛡️ Còn {championsInHand} tướng trong tay.");

            if (championsInHand == 0)
            {
                BattleDebugger.Log("✅ Tất cả tướng đã được đặt! Bắt đầu deal skill...");
                isPreparationPhase = false;
                InventoryManager.Instance?.SetBagIconInteractable(true);
                IsInputLocked = true;
                StartCoroutine(DealSkillsAndBeginCombat());
            }
        }

        private IEnumerator DealSkillsAndBeginCombat()
        {
            // Phát các thẻ skill ra tay sau khi tướng đã được đặt hết
            var skillHand = Skills.SkillHandManager.Instance;
            if (skillHand != null)
                yield return StartCoroutine(skillHand.DealSkillsWithAnimation());

            IsInputLocked = false;
            SetEndTurnButtonInteractable(true);
            BattleDebugger.Log("⚔️ Chiến đấu bắt đầu! Bấm chuông để kết thúc lượt.");
        }

        // ══════════════════════════════════════════
        // XỬ LÝ LƯỢT
        // ══════════════════════════════════════════

        public static float GlobalAnimationSpeed { get; private set; } = 1f;

        private IEnumerator ProcessTurn(bool drawCard = false)
        {
            isTurnProcessing = true;
            IsInputLocked = true; // Bắt đầu lock chuột khi lượt đang chạy
            turnCount++;
            Debug.Log($"[BattleManager] ══ Lượt {turnCount} bắt đầu ══");

            // Tắt nút chuông trong lúc xử lý
            SetEndTurnButtonInteractable(false);

            // ── Kiểm tra pending wave spawn (quái wave mới xuất hiện đầu lượt) ──
            EnemyWaveManager waveManager = EnemyWaveManager.Instance;
            if (waveManager != null)
                yield return StartCoroutine(waveManager.OnTurnStart());

            // ── Rút bài — chỉ khi bấm chuông (drawCard = true) ──
            // CỰC KỲ QUAN TRỌNG: Phải đặt trước block kiểm tra WaveJustSpawned ở dưới.
            // Nếu không, khi wave mới spawn ra và skip combat, việc rút bài/refill bài trên tay
            // cũng sẽ bị skip, khiến người chơi phải bấm chuông thêm 1 lượt nữa mới có bài.
            var skillHand = Skills.SkillHandManager.Instance;
            if (drawCard)
            {
                if (skillHand != null)
                    yield return StartCoroutine(skillHand.DrawOneCardIfNeeded());
                yield return StartCoroutine(DrawBuildingIfNeeded());
            }

            // Nếu lượt này vừa spawn wave mới → SKIP toàn bộ combat
            if (waveManager != null && waveManager.WaveJustSpawned)
            {
                BattleDebugger.Log($"🌊 Lượt {turnCount}: Wave mới spawn — Bỏ qua combat lượt này!");
                isTurnProcessing = false;
                IsInputLocked = false; // Mở lại chuột
                SetEndTurnButtonInteractable(true);
                yield break;
            }

            BattleGrid grid = BattleGrid.Instance;
            if (grid == null)
            {
                Debug.LogError("[BattleManager] Không tìm thấy BattleGrid!");
                isTurnProcessing = false;
                IsInputLocked = false; // Mở lại chuột
                SetEndTurnButtonInteractable(true);
                yield break;
            }

            // ── Tính toán tốc độ animation động (nhiều thẻ = chạy nhanh hơn) ──
            List<CardBattle> enemyCards = grid.GetAllEnemyCards();
            List<CardBattle> playerCards = grid.GetAllPlayerCards();
            List<CardBattle> allCardsOrdered = grid.GetAllCardsInAttackOrder();
            
            int totalCards = enemyCards.Count + playerCards.Count;
            GlobalAnimationSpeed = Mathf.Clamp(totalCards / 3f, 1f, 3f); // 3 thẻ = x1, 6 thẻ = x2, 9 thẻ = x3
            float currentDelay = delayBetweenCards / GlobalAnimationSpeed;

            // ── Phase 1: COMBAT (Theo thứ tự slot do thiết kế quy định) ──
            BattleDebugger.Log($"══ Lượt {turnCount} — Bắt đầu giao tranh ══");
            foreach (CardBattle card in allCardsOrdered)
            {
                if (card == null || card.IsDead) continue;
                yield return StartCoroutine(card.OnTurnTickRoutine());
                waveManager?.OnBattleTick();
                yield return new WaitForSeconds(currentDelay);
            }

            BattleDebugger.Log($"══ Lượt {turnCount} kết thúc ══");

            // ── Phase 2: BLEED NỔ (sau khi tất cả đã hành động xong) ──
            // Trigger cho tất cả các thẻ (cả người chơi lẫn kẻ địch) để Bleed tích lũy phát nổ
            BattleDebugger.Log($"══ Lượt {turnCount} — Phase 2: Nguyên tố hậu kỳ (Bleed...) ══");
            foreach (CardBattle card in playerCards)
            {
                if (card == null || card.IsDead) continue;
                var elemental = card.GetComponent<ProjectM.Elements.ElementalHandler>();
                if (elemental != null)
                    yield return StartCoroutine(elemental.TriggerAfterPlayerAction());
            }
            foreach (CardBattle card in enemyCards)
            {
                if (card == null || card.IsDead) continue;
                var enemyElemental = card.GetComponent<ProjectM.Elements.ElementalHandler>();
                if (enemyElemental != null)
                    yield return StartCoroutine(enemyElemental.TriggerAfterPlayerAction());
            }

            foreach (CardBattle card in playerCards)
            {
                if (card != null && !card.IsDead) card.OnTurnEnded();
            }
            foreach (CardBattle card in enemyCards)
            {
                if (card != null && !card.IsDead) card.OnTurnEnded();
            }

            isTurnProcessing = false;
            IsInputLocked = false; // Xong lượt, thả lại chuột cho người chơi
            SetEndTurnButtonInteractable(true);
        }


        // ══════════════════════════════════════════
        // ULT (Người chơi kích hoạt thủ công)
        // ══════════════════════════════════════════

        /// <summary>
        /// Gắn hàm này vào nút Ult của thẻ nào đó trên UI.
        /// Truyền vào thẻ đang được chọn để kích hoạt Ult.
        /// </summary>
        public void TryActivateUlt(CardBattle card)
        {
            if (isTurnProcessing)
            {
                Debug.Log("[BattleManager] Đang xử lý lượt, không thể dùng Ult lúc này!");
                return;
            }

            if (card != null && card.TryUseUlt())
            {
                // Dùng Ult cũng tính là 1 lượt trôi qua
                EndTurn();
            }
        }

        // ══════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════

        private void SetEndTurnButtonInteractable(bool interactable)
        {
            if (endTurnButton != null)
                endTurnButton.interactable = interactable;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private IEnumerator DrawBuildingIfNeeded()
        {
            if (_buildingDrawPile.Count == 0 || championHandRegion == null || championPrefab == null) yield break;

            // Kiểm tra xem hand có bị đầy không (giới hạn 3 công trình trên tay)
            if (championHandRegion.childCount >= 3) yield break;

            var data = _buildingDrawPile[0];
            _buildingDrawPile.RemoveAt(0);

            var go = Instantiate(championPrefab, championHandRegion);
            go.name = $"Building_{data.cardName}";

            var display = go.GetComponentInChildren<Cards.CardDisplay>(true);
            if (display != null)
                display.LoadData(data);

            // Ẩn 2 ô trang bị (Relic/Trinket) cho thẻ công trình vì không xài tới
            var rSlot = go.GetComponentInChildren<RelicSlotUI>(true);
            if (rSlot != null) rSlot.gameObject.SetActive(false);

            var tSlot = go.GetComponentInChildren<TrinketSlotUI>(true);
            if (tSlot != null) tSlot.gameObject.SetActive(false);

            // Bật animation rút bài (thu nhỏ -> phóng to)
            go.transform.localScale = Vector3.zero;
            go.transform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
