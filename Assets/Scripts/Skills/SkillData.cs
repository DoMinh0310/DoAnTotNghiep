using UnityEngine;
using ProjectM.Elements;

namespace ProjectM.Skills
{
    public enum SkillTargetType
    {
        SingleEnemy = 0, // Kéo vào 1 kẻ địch cụ thể
        AllEnemies  = 1, // Tự động áp lên toàn bộ địch
        EnemyRow    = 2, // Kéo vào 1 kẻ địch → áp lên toàn bộ hàng chứa kẻ đó
        Self        = 3, // Tự động áp lên bản thân
        SingleAlly  = 4, // Kéo vào 1 đồng minh cụ thể
        EmptySlot   = 5, // Kéo vào ô trống (dành cho thẻ Summon)
        AllyRow     = 6, // Kéo vào 1 đồng minh → áp lên toàn bộ hàng chứa người đó
    }

    public enum SkillEffectType
    {
        Damage       = 0, // Gây sát thương trực tiếp
        ApplyElement = 1, // Cộng stack nguyên tố vào mục tiêu
        HealTarget   = 2, // Hồi máu cho mục tiêu (thay thế HealSelf)
        AttackBuff   = 3, // Tăng sát thương đòn thường tiếp theo của bản thân
        AddShield    = 4, // Nhận khiên chặn sát thương trước khi mất máu
        ApplyThorns  = 5, // Bật phản lại 50% sát thương khi bị đánh
        ReduceSpeed  = 6, // Giảm speed đếm ngược hiện tại trong chu kỳ lượt
    }

    /// <summary>
    /// ScriptableObject lưu toàn bộ config của 1 thẻ skill.
    /// Tạo skill mới: chuột phải trong Project → Create → Project M → Skills → Skill Data
    /// KHÔNG cần tạo thêm code — chỉ cần tạo asset và điền Inspector.
    /// Trường hợp skill có hiệu ứng đặc biệt: gán customOverride.
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_New", menuName = "Project M/Skills/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillName = "New Skill";
        [TextArea(2, 3)]
        public string description = "Mô tả hiệu ứng của skill.";
        public Sprite artwork;

        [Header("Visual Tuning")]
        [Tooltip("Dùng để căn chỉnh lại vị trí ảnh (nếu ảnh gốc bị lệch tâm). X=trái/phải, Y=lên/xuống.")]
        public Vector2 artOffset = Vector2.zero;
        [Tooltip("Dùng để phóng to/thu nhỏ ảnh gốc (mặc định = 1). Phóng to lên nếu ảnh gốc quá nhỏ.")]
        public float artScale = 1f;

        [Header("Targeting")]
        [Tooltip("Cách chọn mục tiêu khi dùng thẻ này")]
        public SkillTargetType targetType = SkillTargetType.SingleEnemy;

        [Header("Effect")]
        [Tooltip("Loại hiệu ứng của skill này")]
        public SkillEffectType effectType = SkillEffectType.Damage;

        [Tooltip("Giá trị hiệu ứng: sát thương / số stack / lượng hồi máu / attack bonus")]
        public int effectValue = 5;

        [Header("Element (chỉ điền khi Effect Type = ApplyElement)")]
        public ElementType elementType = ElementType.None;

        [Header("Custom Override")]
        [Tooltip("Chỉ điền cho skill đặc biệt cần logic riêng. Để trống = dùng logic chuẩn.")]
        public SkillOverrideBase customOverride;

        [Header("Exhaust")]
        [Tooltip("Nếu tích chọn, thẻ này sẽ bị hủy (Exhaust) sau khi sử dụng và KHÔNG rơi vào Discard Pile.")]
        public bool isExhaust = false;
    }
}
