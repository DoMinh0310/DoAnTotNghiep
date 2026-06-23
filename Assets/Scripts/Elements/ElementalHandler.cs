using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;

namespace ProjectM.Elements
{
    /// <summary>
    /// MonoBehaviour quản lý toàn bộ stack nguyên tố trên một đơn vị (thẻ tướng hoặc kẻ địch).
    /// Gắn vào Card_Prefab và Skill_Prefab cùng với CardBattle.
    /// CardBattle sẽ gọi TriggerBeforeAttack() và TriggerAfterAttack() vào đúng thời điểm.
    /// </summary>
    public class ElementalHandler : MonoBehaviour
    {
        // ── Danh sách tất cả nguyên tố được đăng ký ─────────────────────
        // Khi thêm nguyên tố mới: tạo class implement IElementalEffect và thêm vào đây.
        private static readonly List<IElementalEffect> RegisteredEffects = new()
        {
            new BleedEffect(),
            new DecayEffect(),
            new FrostEffect(),
            new ChainEffect(),
        };

        // ── Dữ liệu runtime ──────────────────────────────────────────────
        // Số stack hiện tại của từng nguyên tố
        private readonly Dictionary<ElementType, int> _stacks = new();

        // Số lượt DoT còn lại (dùng cho Decay và các DoT khác sau này)
        private readonly Dictionary<ElementType, int> _dotDuration = new();

        // ── References ───────────────────────────────────────────────────
        private CardBattle _cardBattle;
        public CardBattle CardBattle => _cardBattle;

        // ════════════════════════════════════════════════════════════════
        private void Awake()
        {
            _cardBattle = GetComponent<CardBattle>();
        }

        // ════════════════════════════════════════════════════════════════
        // PUBLIC API — Thêm Stack
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Cộng thêm stack nguyên tố vào đơn vị này.
        /// Resistance KHÔNG ảnh hưởng số stack được cộng — chỉ ảnh hưởng sát thương khi kích hoạt.
        /// </summary>
        public IEnumerator AddStacks(ElementType element, int amount)
        {
            if (amount <= 0) yield break;

            _stacks[element] = GetStacks(element) + amount;
            Debug.Log($"[Elemental] {gameObject.name} nhận {amount} stack {element} → Tổng: {_stacks[element]}");

            // Cập nhật UI ngay khi stack thay đổi
            _cardBattle.GetComponent<ProjectM.Cards.CardDisplay>()?.UpdateElementalUI(this);

            var effect = GetEffect(element);
            if (effect != null)
                yield return StartCoroutine(effect.OnStackAdded(this, _stacks[element]));
        }

        // ════════════════════════════════════════════════════════════════
        // PUBLIC API — Trigger Points (gọi bởi CardBattle)
        // ════════════════════════════════════════════════════════════════

        /// <summary>Gọi trong CardBattle TRƯỚC KHI đơn vị bắt đầu lượt (trừ speed).</summary>
        public IEnumerator TriggerTurnStart()
        {
            foreach (var effect in RegisteredEffects)
                yield return StartCoroutine(effect.OnTurnStart(this));
        }

        /// <summary>Gọi trong CardBattle SAU KHI đơn vị kết thúc lượt (dù có đánh hay không).</summary>
        public IEnumerator TriggerTurnEnd()
        {
            if (_cardBattle.IsDead) yield break;
            foreach (var effect in RegisteredEffects)
                yield return StartCoroutine(effect.OnTurnEnd(this));
        }

        // ════════════════════════════════════════════════════════════════
        // GETTERS / SETTERS
        // ════════════════════════════════════════════════════════════════
        public int GetStacks(ElementType element)
            => _stacks.TryGetValue(element, out int v) ? v : 0;

        public void SetStacks(ElementType element, int amount)
        {
            _stacks[element] = Mathf.Max(0, amount);
            _cardBattle.GetComponent<ProjectM.Cards.CardDisplay>()?.UpdateElementalUI(this);
        }

        public int GetDotDuration(ElementType element)
            => _dotDuration.TryGetValue(element, out int v) ? v : 0;

        public void SetDotDuration(ElementType element, int duration)
        {
            _dotDuration[element] = Mathf.Max(0, duration);
            _cardBattle.GetComponent<ProjectM.Cards.CardDisplay>()?.UpdateElementalUI(this);
        }

        /// <summary>
        /// Trả về true nếu đơn vị này đang bị đóng băng (Frost Counter > 0).
        /// CardBattle dùng để bỏ qua bước giảm speed trong lượt.
        /// </summary>
        public bool IsSpeedFrozen => GetStacks(ElementType.Frost) > 0;

        // ════════════════════════════════════════════════════════════════
        // DAMAGE HELPER
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tính và gây sát thương nguyên tố với kháng tính.
        /// resistance = 0 → full damage, 0.5 → 50% (làm tròn lên), 1 → miễn dịch.
        /// </summary>
        public void ApplyElementalDamage(int baseDamage, float resistance)
        {
            int actualDamage = Mathf.CeilToInt(baseDamage * (1f - Mathf.Clamp01(resistance)));
            if (actualDamage <= 0)
            {
                Debug.Log($"[Elemental] {gameObject.name} miễn dịch với sát thương này!");
                return;
            }
            Debug.Log($"[Elemental] {gameObject.name} nhận {actualDamage} sát thương nguyên tố (gốc {baseDamage}, kháng {resistance * 100}%)");
            _cardBattle.TakeDamage(actualDamage);
        }

        // ════════════════════════════════════════════════════════════════
        private IElementalEffect GetEffect(ElementType type)
        {
            foreach (var e in RegisteredEffects)
                if (e.Type == type) return e;
            return null;
        }
    }
}
