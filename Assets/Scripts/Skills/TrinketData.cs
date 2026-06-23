using UnityEngine;
using ProjectM.Elements;

namespace ProjectM.Skills
{
    /// <summary>
    /// Loại hiệu ứng passive của Trinket.
    /// </summary>
    public enum TrinketEffectType
    {
        StatBoost,          // Tăng chỉ số: HP, ATK, Speed
        ElementalPassive,   // Cung cấp stack nguyên tố mỗi lượt hoặc khi trigger
        Conditional,        // Hiệu ứng có điều kiện (OnHit, OnKill, OnTurnStart...)
    }

    /// <summary>
    /// Điều kiện kích hoạt của Trinket loại Conditional.
    /// </summary>
    public enum TrinketTrigger
    {
        None,
        OnAttack,       // Sau khi thẻ gắn tấn công
        OnHit,          // Khi thẻ gắn nhận sát thương
        OnKill,         // Khi thẻ gắn tiêu diệt kẻ địch
        OnTurnStart,    // Đầu mỗi lượt
        OnTurnEnd,      // Cuối mỗi lượt
    }

    /// <summary>
    /// Cấu hình cho 1 Trinket (trang bị Passive).
    /// Tạo: chuột phải Project → Create → Project M → Equipment → Trinket Data
    ///
    /// Trinket gắn vào thẻ tướng là VĨNH VIỄN (không thể tháo ngoài shop).
    /// </summary>
    [CreateAssetMenu(fileName = "Trinket_New", menuName = "Project M/Equipment/Trinket Data")]
    public class TrinketData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Tên hiển thị của Trinket")]
        public string trinketName = "New Trinket";

        [TextArea(2, 4)]
        [Tooltip("Mô tả hiệu ứng (hiển thị khi hover)")]
        public string description = "Mô tả trinket.";

        public Sprite icon;

        [Header("Effect Type")]
        public TrinketEffectType effectType = TrinketEffectType.StatBoost;

        // ── StatBoost fields ──────────────────────────────────────────────
        [Header("Stat Boost (effectType = StatBoost)")]
        [Tooltip("Lượng HP bonus cộng thêm khi gắn Trinket")]
        public int bonusHP = 0;
        [Tooltip("Lượng ATK bonus cộng thêm khi gắn Trinket")]
        public int bonusATK = 0;
        [Tooltip("Giảm speed counter đi N (speed thấp hơn = đánh nhanh hơn). Số âm = tăng speed delay.")]
        public int speedReduction = 0;

        // ── Elemental Passive fields ──────────────────────────────────────
        [Header("Elemental Passive (effectType = ElementalPassive)")]
        [Tooltip("Nguyên tố sẽ được áp lên kẻ địch")]
        public ElementType elementType = ElementType.None;

        [Tooltip("Số stack nguyên tố áp mỗi lần trigger")]
        public int elementStackPerTrigger = 1;

        [Tooltip("Trigger của elemental passive")]
        public TrinketTrigger elementTrigger = TrinketTrigger.OnAttack;

        // ── Conditional fields ────────────────────────────────────────────
        [Header("Conditional (effectType = Conditional)")]
        [Tooltip("Điều kiện kích hoạt hiệu ứng")]
        public TrinketTrigger conditionalTrigger = TrinketTrigger.None;

        [Tooltip("Loại hiệu ứng khi điều kiện kích hoạt")]
        public SkillEffectType conditionalEffect = SkillEffectType.HealTarget;

        [Tooltip("Giá trị của hiệu ứng (heal / damage / stack...)")]
        public int conditionalValue = 1;

        [Header("Rarity")]
        public Cards.CardRarity rarity = Cards.CardRarity.Common;
    }
}
