using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using ProjectM.Cards;

namespace ProjectM.Managers
{
    /// <summary>
    /// Quản lý vòng đời wave địch cho 1 stage:
    ///   - Spawn wave đầu tiên trước prep phase
    ///   - Theo dõi khi toàn bộ địch chết → spawn wave tiếp theo ở lượt sau
    ///   - Fill ô trống sau X battle tick từ pool của wave kế tiếp
    ///   - Fire event khi stage hoàn thành
    /// Gắn vào một GameObject trong scene (ví dụ: BattleManager object).
    /// </summary>
    public class EnemyWaveManager : MonoBehaviour
    {
        public static EnemyWaveManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────
        [Header("Stage Config")]
        [Tooltip("Kéo asset StageData của stage này vào đây")]
        public StageData stageData;

        [Header("Spawn Animation")]
        [Tooltip("Offset X ban đầu (pixels, ngoài màn hình bên phải)")]
        public float spawnOffsetX = 1600f;
        [Tooltip("Thời gian mỗi thẻ bay vào vị trí (giây)")]
        public float spawnFlyDuration = 0.30f;
        [Tooltip("Delay giữa các thẻ spawn lần lượt (giây)")]
        public float spawnStagger = 0.12f;

        // ─────────────────────────────────────────────────────────────
        // Runtime state
        // ─────────────────────────────────────────────────────────────
        private int  _currentWaveIndex    = -1;
        private int  _waveSpawnCountdown   = 0;  // Đếm ngược lượt trước khi spawn wave mới (0 = không có wave đang chờ)
        private bool _stageCleared        = false;

        // Battle tick counter — tăng mỗi khi BattleManager xử lý 1 thẻ (speed giảm)
        private int _battleTickCount = 0;

        // Theo dõi tick khi mỗi ô địch trở nên trống
        // Key = EnemySlotId, Value = _battleTickCount tại thời điểm slot bị trống
        private readonly Dictionary<EnemySlotId, int> _emptySlotSinceTick = new();

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ════════════════════════════════════════════════════════════
        // PUBLIC API — Gọi bởi BattleManager
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Gọi khi bắt đầu scene chiến đấu — spawn wave đầu tiên.
        /// BattleManager yield coroutine này trước khi tiếp tục prep phase.
        /// </summary>
        public IEnumerator StartStage()
        {
            if (stageData == null || stageData.waves == null || stageData.waves.Count == 0)
            {
                Debug.LogWarning("[WaveManager] Không có StageData hoặc không có wave nào được cấu hình!");
                yield break;
            }

            yield return StartCoroutine(AdvanceToNextWave());
        }

        // Được set trong OnTurnStart() — ProcessTurn đọc để quyết định có skip combat không
        public bool WaveJustSpawned { get; private set; } = false;

        /// <summary>
        /// Gọi ĐẦU MỖI LƯỢT (trong ProcessTurn) để kiểm tra pending wave spawn.
        /// WaveJustSpawned = true nếu lượt này có wave mới spawn ra → ProcessTurn sẽ skip combat.
        /// </summary>
        public IEnumerator OnTurnStart()
        {
            WaveJustSpawned = false;

            if (_waveSpawnCountdown <= 0 || _stageCleared) yield break;

            _waveSpawnCountdown--;
            if (_waveSpawnCountdown == 0)
            {
                // Countdown về 0 → bản đồ đã sạch hoàn toàn → spawn wave mới
                WaveJustSpawned = true;
                yield return StartCoroutine(AdvanceToNextWave());
            }
        }

        /// <summary>
        /// Gọi sau mỗi card.OnTurnTickRoutine() — tăng bộ đếm và kiểm tra fill.
        /// </summary>
        public void OnBattleTick()
        {
            if (_stageCleared) return;
            _battleTickCount++;
            TryFillEmptySlots();
        }

        /// <summary>
        /// Gọi bởi CardBattle.DieSequence() khi 1 thẻ địch chết.
        /// </summary>
        public void OnEnemyDied(CardBattle deadEnemy)
        {
            if (_stageCleared) return;

            // Ghi lại slot nào vừa trở nên trống
            var slot = deadEnemy.transform.parent?.GetComponent<CardDropZone>();
            if (slot != null)
            {
                EnemySlotId? slotId = BattleGrid.Instance?.GetSlotIdOf(slot);
                if (slotId.HasValue)
                    _emptySlotSinceTick[slotId.Value] = _battleTickCount;
            }

            // Kiểm tra sau 1 frame (Destroy chưa xong ngay)
            StartCoroutine(CheckWaveCleared());
        }

        // ════════════════════════════════════════════════════════════
        // WAVE LOGIC
        // ════════════════════════════════════════════════════════════

        private IEnumerator CheckWaveCleared()
        {
            yield return null; // Đợi Destroy() hoàn tất
            yield return null; // Buffer thêm 1 frame cho chắc

            if (_stageCleared || BattleGrid.Instance == null) yield break;

            var remaining = BattleGrid.Instance.GetAllEnemyCards();
            if (remaining.Count > 0) yield break; // Còn quái → chưa clear

            int nextWave = _currentWaveIndex + 1;
            if (nextWave >= stageData.waves.Count)
            {
                // Hết tất cả wave → thắng stage
                OnStageCleared();
            }
            else
            {
                // Đặt countdown = 2: lượt N+1 giảm xuống 1, lượt N+2 mới thực sự spawn
                // Đảm bảo animation chết (dissolve) của tất cả quái wave cũ đã kết thúc và sàn đã sạch
                _waveSpawnCountdown = 2;
                Debug.Log($"[WaveManager] 🌊 Wave {_currentWaveIndex + 1} đã bị tiêu diệt! " +
                          $"Wave {nextWave + 1} sẽ xuất hiện sau 2 lượt.");
            }
        }

        private IEnumerator AdvanceToNextWave()
        {
            _currentWaveIndex++;
            _emptySlotSinceTick.Clear(); // Reset tracking ô trống cho wave mới

            if (_currentWaveIndex >= stageData.waves.Count) yield break;

            WaveConfig wave = stageData.waves[_currentWaveIndex];
            Debug.Log($"[WaveManager] ⚔️ {wave.waveName} bắt đầu " +
                      $"({_currentWaveIndex + 1}/{stageData.waves.Count}) — " +
                      $"Số quái: {wave.spawnList.Count}");

            yield return StartCoroutine(SpawnWaveAnimated(wave));
        }

        // ════════════════════════════════════════════════════════════
        // SPAWN ANIMATION
        // ════════════════════════════════════════════════════════════

        private IEnumerator SpawnWaveAnimated(WaveConfig wave)
        {
            if (wave.spawnList == null || wave.spawnList.Count == 0) yield break;

            BattleGrid grid = BattleGrid.Instance;
            if (grid == null) yield break;

            // Sắp xếp theo thứ tự slot index (Top0→Top1→Top2→Bot0→Bot1→Bot2)
            var sorted = wave.spawnList
                .Where(e => e.cardPrefab != null && e.cardData != null)
                .OrderBy(e => (int)e.slotId)
                .ToList();

            foreach (var entry in sorted)
            {
                CardDropZone slot = grid.GetEnemySlotById(entry.slotId);
                if (slot == null) continue;

                // Bỏ qua nếu slot đã có quái còn sống
                // (Không dùng GetComponentInChildren mà kiểm tra IsDead — thẻ đang dissolve vẫn còn trên scene)
                CardBattle existing = slot.GetComponentInChildren<CardBattle>();
                if (existing != null && !existing.IsDead) continue;

                // Spawn + animation
                CardBattle spawned = slot.SpawnEnemyCard(entry.cardPrefab, entry.cardData);
                if (spawned != null)
                    AnimateFlyIn(spawned.GetComponent<RectTransform>());

                yield return new WaitForSeconds(spawnStagger);
            }

            // Đợi animation cuối cùng kết thúc
            yield return new WaitForSeconds(spawnFlyDuration + 0.1f);
        }

        private void AnimateFlyIn(RectTransform rect)
        {
            if (rect == null) return;

            // Ghi lại vị trí đích (đang là Vector2.zero vì vừa được snap vào slot)
            Vector2 targetPos = rect.anchoredPosition;

            // Đặt vị trí ban đầu ra ngoài màn hình bên phải
            rect.anchoredPosition = new Vector2(spawnOffsetX, targetPos.y);

            // Phát âm thanh spawn (dùng lại cardFlipClip như lúc tướng được lật)
            // Lược bỏ theo yêu cầu: AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardFlipClip);

            // Bay vào với Ease.OutBack để có cảm giác nảy nhẹ khi "hạ cánh"
            rect.DOAnchorPos(targetPos, spawnFlyDuration)
                .SetEase(Ease.OutBack);
        }

        // ════════════════════════════════════════════════════════════
        // FILL LOGIC
        // ════════════════════════════════════════════════════════════

        private void TryFillEmptySlots()
        {
            // Fill pool lấy từ WAVE TIẾP THEO
            int fillWaveIndex = _currentWaveIndex + 1;
            if (fillWaveIndex >= stageData.waves.Count) return;

            WaveConfig fillWave = stageData.waves[fillWaveIndex];
            if (fillWave.fillPool == null || fillWave.fillPool.Count == 0) return;
            if (fillWave.fillDelayTurns <= 0) return;

            BattleGrid grid = BattleGrid.Instance;
            if (grid == null) return;

            // Lấy danh sách ô địch trống hiện tại
            var emptySlots = grid.GetEmptyEnemySlots();
            if (emptySlots.Count == 0) return;

            // Sắp xếp fill pool theo priority
            var fillPool = fillWave.fillPool
                .Where(f => f.cardPrefab != null && f.cardData != null)
                .OrderBy(f => f.priority)
                .ToList();

            int fillIdx = 0;
            foreach (var (slotId, slot) in emptySlots)
            {
                if (fillIdx >= fillPool.Count) break;

                // Kiểm tra slot đã trống đủ lâu chưa
                if (!_emptySlotSinceTick.TryGetValue(slotId, out int emptySinceTick)) continue;
                if (_battleTickCount - emptySinceTick < fillWave.fillDelayTurns) continue;

                // Fill ô này!
                var entry = fillPool[fillIdx++];
                Debug.Log($"[WaveManager] 🔄 Fill slot {slotId} sau {_battleTickCount - emptySinceTick} tick " +
                          $"→ {entry.cardData.cardName}");

                CardBattle spawned = slot.SpawnEnemyCard(entry.cardPrefab, entry.cardData);
                if (spawned != null)
                    AnimateFlyIn(spawned.GetComponent<RectTransform>());

                _emptySlotSinceTick.Remove(slotId); // Reset tracking
            }
        }

        // ════════════════════════════════════════════════════════════
        // STAGE CLEAR
        // ════════════════════════════════════════════════════════════

        private void OnStageCleared()
        {
            _stageCleared = true;
            Debug.Log("[WaveManager] 🏆 Tất cả wave đã bị tiêu diệt! Stage hoàn thành!");

            // TODO: Kết nối với GameManager để chuyển sang Map
            // GameManager.Instance?.OnStageClear();
        }
    }
}
