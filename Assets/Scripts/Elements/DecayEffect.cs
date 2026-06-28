using System.Collections;
using UnityEngine;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Phân Rã (Decay) — Cơ chế mới (Leo thang vô hạn):
    ///
    /// Sau khi kẻ địch kết thúc lượt tấn công (TurnEnd):
    ///   - Gây sát thương = số stack Decay hiện tại
    ///   - Sau đó tăng số stack lên 1
    ///   - Vòng tiếp theo: stack cao hơn → sát thương cao hơn → stack lại tăng → ...
    ///
    /// Ví dụ: 3 stack → gây 3 sát → tăng thành 4 → gây 4 sát → tăng thành 5 → ...
    ///
    /// Không có giới hạn trên và không bao giờ tự tắt trừ khi kẻ địch chết.
    /// </summary>
    public class DecayEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Decay;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            Debug.Log($"[Decay] ☠️ {handler.gameObject.name} bị phân rã! Tổng stack: {totalStacks}");
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            // Decay không trigger trước khi địch đánh
            yield break;
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            int stacks = handler.GetStacks(ElementType.Decay);
            if (stacks <= 0) yield break;

            var cardBattle = handler.CardBattle;
            if (cardBattle == null || cardBattle.IsDead) yield break;

            float resist = cardBattle.Data?.GetElementalResistance(ElementType.Decay) ?? 0f;

            // 1. Gây sát thương bằng số stack hiện tại
            Debug.Log($"[Decay] ☠️ {cardBattle.Data?.cardName} nhận {stacks} sát thương Phân Rã " +
                      $"(kháng {resist * 100}%)");
            handler.ApplyElementalDamage(stacks, resist);

            if (cardBattle.IsDead) yield break;

            // 2. Tăng stack lên 1 (leo thang)
            handler.SetStacks(ElementType.Decay, stacks + 1);
            Debug.Log($"[Decay] ☠️ {cardBattle.Data?.cardName}: Stack Decay tăng lên {stacks + 1}");

            yield break;
        }

        public IEnumerator OnAfterPlayerAction(ElementalHandler handler)
        {
            // Decay không trigger sau lượt đồng minh
            yield break;
        }
    }
}
