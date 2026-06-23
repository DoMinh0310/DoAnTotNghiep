using UnityEngine;

namespace ProjectM.Skills
{
    /// <summary>
    /// Cấu hình cho 1 Relic (trang bị Active).
    /// Tạo: chuột phải Project → Create → Project M → Equipment → Relic Data
    ///
    /// Relic sẽ đếm ngược mỗi lượt chuông bấm.
    /// Khi countdown về 0 → spawn thẻ skill vào tay người chơi.
    /// Nếu thẻ skill đó chưa được dùng → countdown TẠM DỪNG cho đến khi dùng xong.
    /// </summary>
    [CreateAssetMenu(fileName = "Relic_New", menuName = "Project M/Equipment/Relic Data")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Tên hiển thị của Relic")]
        public string relicName = "New Relic";

        [TextArea(2, 4)]
        [Tooltip("Mô tả hiệu ứng của Relic (hiển thị khi hover)")]
        public string description = "Mô tả relic.";

        public Sprite icon;

        [Header("Relic Mechanic")]
        [Tooltip("Số lượt chuông cần bấm để kích hoạt (spawn skill). Tương tự speed của thẻ.")]
        [Min(1)]
        public int cycleSpeed = 3;

        [Tooltip("Thẻ skill sẽ được spawn vào tay người chơi khi Relic kích hoạt.")]
        public SkillData spawnedSkill;

        [Header("Rarity & Visuals")]
        public Cards.CardRarity rarity = Cards.CardRarity.Common;
    }
}
