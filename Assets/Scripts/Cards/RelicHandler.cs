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
        [Header("UI References")]
        [Tooltip("Nút bấm đè lên Icon Relic (kéo thả Button vào đây)")]
        public UnityEngine.UI.Button relicButton;
        public UnityEngine.UI.Image relicIconImage; // Tùy chọn: Để nháy sáng khi sẵn sàng
        [Tooltip("Chữ số hiển thị cooldown của Relic")]
        public TMPro.TMP_Text cooldownText;

        // ── Runtime state ─────────────────────────────────────────────────
        private RelicData _relic;
        private int       _currentCooldown;

        /// <summary>True nếu Relic đã đếm xong và đang ĐỢI người chơi bấm.</summary>
        public bool IsReadyToSpawn { get; private set; } = false;

        /// <summary>True nếu skill đã được spawn nhưng người chơi chưa dùng.
        /// Countdown TẠM DỪNG khi pending = true.</summary>
        public bool SkillPending { get; private set; } = false;

        /// <summary>Relic hiện đang trang bị (null = trống).</summary>
        public RelicData EquippedRelic => _relic;

        /// <summary>Countdown hiện tại (để UI hiển thị).</summary>
        public int CurrentCooldown => _currentCooldown;

        // ── Public API ────────────────────────────────────────────────────

        private void Start()
        {
            if (relicButton != null)
            {
                relicButton.onClick.AddListener(OnClickRelicIcon);
                // Khóa nút ban đầu
                relicButton.interactable = false;
            }
        }

        private void UpdateCooldownUI()
        {
            if (cooldownText != null)
            {
                if (_relic == null)
                {
                    cooldownText.text = "";
                }
                else
                {
                    cooldownText.text = IsReadyToSpawn ? "!" : _currentCooldown.ToString();
                    // Đẩy Text lên hiển thị trên cùng để không bị Icon đè lên (như ảnh báo cáo)
                    cooldownText.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>Trang bị Relic mới. Có thể gọi lại để thay đổi.</summary>
        public void EquipRelic(RelicData relic)
        {
            _relic           = relic;
            _currentCooldown = relic != null ? relic.cycleSpeed : 0;
            SkillPending     = false;
            IsReadyToSpawn   = false;
            
            if (relicButton != null) relicButton.interactable = false;
            
            // Hiện icon của Relic lên (nếu có kéo Image vào Inspector)
            if (relicIconImage != null && relic != null)
            {
                relicIconImage.sprite = relic.icon;
                relicIconImage.gameObject.SetActive(true);
            }
            
            UpdateCooldownUI();
            
            Debug.Log($"[RelicHandler] {gameObject.name}: Trang bị Relic '{relic?.relicName ?? "none"}'");
        }

        /// <summary>Tháo Relic hiện tại (chỉ cho phép ngoài combat / trong Inventory).</summary>
        public void UnequipRelic()
        {
            Debug.Log($"[RelicHandler] {gameObject.name}: Tháo Relic '{_relic?.relicName}'");
            _relic           = null;
            _currentCooldown = 0;
            SkillPending     = false;
            IsReadyToSpawn   = false;
            
            if (relicButton != null) relicButton.interactable = false;
            if (relicIconImage != null) relicIconImage.gameObject.SetActive(false);
            
            UpdateCooldownUI();
        }

        /// <summary>
        /// Gọi bởi BattleManager mỗi khi người chơi bấm chuông (đầu ProcessTurn).
        /// Đếm ngược và cho phép bấm khi đủ lượt.
        /// </summary>
        public void OnBellPressed()
        {
            if (_relic == null)            return;
            if (SkillPending)              return; // Chưa dùng skill cũ → không đếm
            if (IsReadyToSpawn)            return; // Đang chờ bấm → không đếm tiếp

            if (_relic.possibleSkills == null || _relic.possibleSkills.Count == 0)
            {
                Debug.LogWarning($"[RelicHandler] Relic '{_relic.relicName}' chưa config possibleSkills!");
                return;
            }

            _currentCooldown--;
            Debug.Log($"[RelicHandler] {_relic.relicName} countdown: {_currentCooldown}/{_relic.cycleSpeed}");

            if (_currentCooldown <= 0)
            {
                // Thay vì SpawnRelicSkill() tự động, giờ ta chờ người chơi bấm
                IsReadyToSpawn = true;
                if (relicButton != null)
                {
                    relicButton.interactable = true;
                    // TODO: Gọi DOTween nhấp nháy Image ở đây nếu thích (phát sáng UI)
                    if (relicIconImage != null)
                        relicIconImage.color = new Color(1f, 1f, 1f, 1f); // Sáng bừng lên
                }
                Debug.Log($"[RelicHandler] {_relic.relicName} ĐÃ SẴN SÀNG! Chờ người chơi click...");
            }
            
            UpdateCooldownUI();
        }

        /// <summary>
        /// Người chơi chủ động click vào Icon Relic.
        /// </summary>
        public void OnClickRelicIcon()
        {
            if (!IsReadyToSpawn || _relic == null) return;
            
            SpawnRelicSkill();
        }

        /// <summary>
        /// Gọi bởi SkillHandManager khi skill từ Relic này được dùng xong.
        /// → Reset pending, countdown tiếp tục lượt sau.
        /// </summary>
        public void OnRelicSkillUsed()
        {
            SkillPending     = false;
            IsReadyToSpawn   = false;
            _currentCooldown = _relic != null ? _relic.cycleSpeed : 0;
            Debug.Log($"[RelicHandler] {_relic?.relicName}: Skill đã dùng, reset countdown = {_currentCooldown}");
            
            UpdateCooldownUI();
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

            IsReadyToSpawn   = false;
            SkillPending     = true;
            _currentCooldown = _relic.cycleSpeed; // Reset ngay để UI không hiện số âm
            
            // Khóa nút lại sau khi đã spawn
            if (relicButton != null) relicButton.interactable = false;
            if (relicIconImage != null) relicIconImage.color = new Color(0.6f, 0.6f, 0.6f, 1f); // Hơi tối đi

            // 4. Nếu dùng thẻ thành công, Spawn 1 Skill ngẫu nhiên từ Relic
            var skillToSpawn = _relic.possibleSkills[Random.Range(0, _relic.possibleSkills.Count)];
            handManager.SpawnRelicSkillCard(skillToSpawn, this);
            Debug.Log($"[RelicHandler] ★ Relic '{_relic.relicName}' kích hoạt! Spawn '{skillToSpawn.skillName}' vào tay.");
            
            UpdateCooldownUI();
        }
    }
}
