using UnityEngine;
using System.Collections;
using ProjectM.Skills;

namespace ProjectM.Cards
{
    /// <summary>
    /// Gắn lên Card Prefab cùng với CardBattle.
    /// Quản lý countdown của Relic và spawn skill khi đủ lượt.
    ///
    /// FLOW:
    ///   1. BattleManager gọi OnBellPressed() mỗi khi người chơi bấm chuông (combat phase).
    ///   2. Nếu không có skill đang chờ dùng → đếm ngược _currentCooldown.
    ///   3. Khi _currentCooldown <= 0 → SpawnRelicSkill() → _skillPending = true.
    ///   4. Khi người chơi dùng xong skill đó → SkillHandManager gọi OnRelicSkillUsed()
    ///      → _skillPending = false → countdown tiếp tục.
    /// </summary>
    public class RelicHandler : MonoBehaviour
    {
        // ── Runtime state ─────────────────────────────────────────────────
        private RelicData _relic;
        private int       _currentCooldown;

        /// <summary>True nếu skill đã được spawn nhưng người chơi chưa dùng.
        /// Countdown TẠM DỪNG khi pending = true.</summary>
        public bool SkillPending { get; private set; } = false;

        /// <summary>Relic hiện đang trang bị (null = trống).</summary>
        public RelicData EquippedRelic => _relic;

        /// <summary>Countdown hiện tại (để UI hiển thị).</summary>
        public int CurrentCooldown => _currentCooldown;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Trang bị Relic mới. Có thể gọi lại để thay đổi.</summary>
        public void EquipRelic(RelicData relic)
        {
            _relic           = relic;
            _currentCooldown = relic != null ? relic.cycleSpeed : 0;
            SkillPending     = false;
            Debug.Log($"[RelicHandler] {gameObject.name}: Trang bị Relic '{relic?.relicName ?? "none"}'");
        }

        /// <summary>Tháo Relic hiện tại (chỉ cho phép ngoài combat / trong Inventory).</summary>
        public void UnequipRelic()
        {
            Debug.Log($"[RelicHandler] {gameObject.name}: Tháo Relic '{_relic?.relicName}'");
            _relic           = null;
            _currentCooldown = 0;
            SkillPending     = false;
        }

        /// <summary>
        /// Gọi bởi BattleManager mỗi khi người chơi bấm chuông (đầu ProcessTurn).
        /// Đếm ngược và spawn skill khi đủ lượt.
        /// </summary>
        public void OnBellPressed()
        {
            if (_relic == null)            return;
            if (SkillPending)              return; // Chưa dùng skill cũ → không đếm
            if (_relic.spawnedSkill == null)
            {
                Debug.LogWarning($"[RelicHandler] Relic '{_relic.relicName}' chưa config spawnedSkill!");
                return;
            }

            _currentCooldown--;
            Debug.Log($"[RelicHandler] {_relic.relicName} countdown: {_currentCooldown}/{_relic.cycleSpeed}");

            if (_currentCooldown <= 0)
                SpawnRelicSkill();
        }

        /// <summary>
        /// Gọi bởi SkillHandManager khi skill từ Relic này được dùng xong.
        /// → Reset pending, countdown tiếp tục lượt sau.
        /// </summary>
        public void OnRelicSkillUsed()
        {
            SkillPending     = false;
            _currentCooldown = _relic != null ? _relic.cycleSpeed : 0;
            Debug.Log($"[RelicHandler] {_relic?.relicName}: Skill đã dùng, reset countdown = {_currentCooldown}");
        }

        // ── Private ───────────────────────────────────────────────────────

        private void SpawnRelicSkill()
        {
            var handManager = SkillHandManager.Instance;
            if (handManager == null)
            {
                Debug.LogError("[RelicHandler] Không tìm thấy SkillHandManager.Instance!");
                return;
            }

            SkillPending     = true;
            _currentCooldown = _relic.cycleSpeed; // Reset ngay để UI không hiện số âm

            // Spawn thẻ skill, đánh dấu nguồn gốc từ relic này
            handManager.SpawnRelicSkillCard(_relic.spawnedSkill, this);
            Debug.Log($"[RelicHandler] ★ Relic '{_relic.relicName}' kích hoạt! Spawn '{_relic.spawnedSkill.skillName}' vào tay.");
        }
    }
}
