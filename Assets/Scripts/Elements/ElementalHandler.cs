using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;

namespace ProjectM.Elements
{
    /// <summary>
    /// MonoBehaviour quản lý toàn bộ stack nguyên tố trên một đơn vị.
    /// Gắn vào Card_Prefab và Skill_Prefab cùng với CardBattle.
    /// 
    /// LUỒNG GỌI (quan trọng — thứ tự phải đúng):
    ///   1. BattleManager tick từng thẻ địch theo thứ tự tốc độ
    ///   2. Trước khi thẻ địch đánh → TriggerBeforeEnemyAttack() (Chain/Bleed nổ tại đây)
    ///   3. Thẻ địch đánh (hoặc bị bỏ qua vì Frost)
    ///   4. Sau khi thẻ địch đánh → TriggerAfterEnemyAttack() (Decay tick tại đây)
    ///   5. Sau khi tất cả action của Player xong → BattleManager gọi TriggerAfterPlayerAction()
    ///      trên tất cả thẻ địch đang bị Bleed (Bleed nổ tại đây).
    /// </summary>
    public class ElementalHandler : MonoBehaviour
    {
        // ── Danh sách tất cả nguyên tố được đăng ký ─────────────────────
        private static readonly List<IElementalEffect> RegisteredEffects = new()
        {
            new BleedEffect(),
            new DecayEffect(),
            new FrostEffect(),
            new ChainEffect(),
        };

        // ── Dữ liệu runtime ──────────────────────────────────────────────
        private readonly Dictionary<ElementType, int> _stacks = new();

        // Bleed: tích lũy sát thương vật lý trong 1 lượt action của ĐỒNG MINH
        // (Được cộng dồn khi có đòn đánh vật lý vào unit này, reset sau mỗi lần Bleed nổ)
        private int _pendingBleedDamage = 0;

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
        /// </summary>
        public IEnumerator AddStacks(ElementType element, int amount)
        {
            if (amount <= 0) yield break;

            _stacks[element] = GetStacks(element) + amount;
            Debug.Log($"[Elemental] {gameObject.name} nhận {amount} stack {element} → Tổng: {_stacks[element]}");

            _cardBattle.GetComponent<ProjectM.Cards.CardDisplay>()?.UpdateElementalUI(this);

            var effect = GetEffect(element);
            if (effect != null)
                yield return StartCoroutine(effect.OnStackAdded(this, _stacks[element]));
        }

        // ════════════════════════════════════════════════════════════════
        // PUBLIC API — Trigger Points
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gọi TRƯỚC KHI đơn vị địch bắt đầu lượt tấn công.
        /// Chain và Bleed (phía địch) kích hoạt tại đây.
        /// </summary>
        public IEnumerator TriggerTurnStart()
        {
            foreach (var effect in RegisteredEffects)
                yield return StartCoroutine(effect.OnTurnStart(this));
        }

        /// <summary>
        /// Gọi SAU KHI đơn vị kết thúc lượt tấn công.
        /// Decay tick tại đây.
        /// </summary>
        public IEnumerator TriggerTurnEnd()
        {
            if (_cardBattle.IsDead) yield break;
            foreach (var effect in RegisteredEffects)
                yield return StartCoroutine(effect.OnTurnEnd(this));
        }

        /// <summary>
        /// Gọi bởi BattleManager SAU KHI tất cả action của ĐỒNG MINH trong 1 lượt đã hoàn thành
        /// (tức là trước khi đến lượt địch đánh tiếp).
        /// Bleed phía địch nổ tại đây nếu có stack tích lũy.
        /// </summary>
        public IEnumerator TriggerAfterPlayerAction()
        {
            if (_cardBattle.IsDead) yield break;
            yield return StartCoroutine(GetEffect(ElementType.Bleed).OnAfterPlayerAction(this));
        }

        /// <summary>
        /// Được gọi bởi CardBattle khi một đòn đánh VẬT LÝ (không có nguyên tố) vào đơn vị này.
        /// Nếu đơn vị này đang có Bleed, sát thương đó sẽ được tích vào _pendingBleedDamage.
        /// </summary>
        /// <param name="physicalDamage">Lượng sát thương vật lý thực tế đã nhận.</param>
        public void NotifyPhysicalDamageReceived(int physicalDamage)
        {
            if (GetStacks(ElementType.Bleed) <= 0) return;
            _pendingBleedDamage += physicalDamage;
            Debug.Log($"[Bleed] {gameObject.name} nhận đòn vật lý {physicalDamage} khi đang Bleed → " +
                      $"Pending bleed damage: {_pendingBleedDamage}");
        }

        /// <summary>Lấy và reset lượng sát thương Bleed đang chờ nổ.</summary>
        public int ConsumePendingBleedDamage()
        {
            int val = _pendingBleedDamage;
            _pendingBleedDamage = 0;
            return val;
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

        /// <summary>
        /// Backward-compat cho CardDisplay: trả về số stack Decay hiện tại.
        /// Với cơ chế mới, Decay dùng stack thay vì dotDuration riêng.
        /// </summary>
        public int GetDotDuration(ElementType element)
            => GetStacks(element);

        /// <summary>
        /// Trả về true nếu đơn vị này đang bị đóng băng (Frost Counter > 0).
        /// </summary>
        public bool IsSpeedFrozen => GetStacks(ElementType.Frost) > 0;

        /// <summary>
        /// Trả về true nếu đơn vị này đang có bất kỳ stack nguyên tố nào.
        /// </summary>
        public bool HasAnyActiveEffect()
        {
            foreach (var kvp in _stacks)
            {
                if (kvp.Value > 0) return true;
            }
            return false;
        }

        // ════════════════════════════════════════════════════════════════
        // DAMAGE HELPER
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tính và gây sát thương nguyên tố với kháng tính.
        /// resistance = 0 → full damage, 0.5 → 50%, 1 → miễn dịch.
        /// </summary>
        public void ApplyElementalDamage(int baseDamage, float resistance)
        {
            int actualDamage = Mathf.CeilToInt(baseDamage * (1f - Mathf.Clamp01(resistance)));
            if (actualDamage <= 0)
            {
                Debug.Log($"[Elemental] {gameObject.name} miễn dịch với sát thương này!");
                return;
            }
            Debug.Log($"[Elemental] {gameObject.name} nhận {actualDamage} sát thương nguyên tố " +
                      $"(gốc {baseDamage}, kháng {resistance * 100}%)");
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
