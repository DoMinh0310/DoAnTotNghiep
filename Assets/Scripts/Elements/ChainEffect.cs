using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectM.Cards;
using ProjectM.Managers;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Dây Chuyền (Chain) — Cơ chế mới:
    ///
    /// TÍCH LŨY: Stack Chain được cộng thủ công từ đòn đánh/thẻ skill có hiệu ứng Chain.
    ///
    /// KÍCH HOẠT: Ngay TRƯỚC KHI kẻ địch tấn công (TurnStart), nếu:
    ///            số Stack Chain >= HP HIỆN TẠI của kẻ địch đó.
    ///
    /// KHI NỔ:
    ///   - Kẻ địch chính nhận sát thương = số Stack Chain đang có.
    ///   - Tất cả kẻ địch KHÁC trong cùng hàng nhận:
    ///       + 1/2 sát thương (làm tròn xuống)
    ///       + 1 stack của mỗi nguyên tố mà kẻ địch chính đang mang (Bleed, Decay, Frost, Chain)
    ///   - Stack Chain của kẻ địch chính reset về 0.
    /// </summary>
    public class ChainEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Chain;

        private const float HitDelay = 0.08f;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            int stacks = handler.GetStacks(ElementType.Chain);
            if (stacks <= 0) yield break;

            var cardBattle = handler.CardBattle;
            if (cardBattle == null || cardBattle.IsDead) yield break;

            // Chain chỉ trigger khi stack >= HP HIỆN TẠI của kẻ địch
            int currentHP = cardBattle.CurrentHP;
            if (stacks < currentHP) yield break;

            float resist = cardBattle.Data?.GetElementalResistance(ElementType.Chain) ?? 0f;

            Debug.Log($"[Chain] ⚡ {cardBattle.Data?.cardName}: {stacks} stack ≥ HP hiện tại {currentHP} → Chain nổ!");

            // Reset stack trước khi gây damage
            handler.SetStacks(ElementType.Chain, 0);

            // --- Gây sát thương cho kẻ địch chính ---
            handler.ApplyElementalDamage(stacks, resist);
            yield return new WaitForSeconds(HitDelay * 2f);

            if (cardBattle.IsDead) yield break;

            // --- Thu thập tất cả stack nguyên tố kẻ địch chính đang mang ---
            var spreadStacks = new Dictionary<ElementType, int>
            {
                { ElementType.Bleed,  handler.GetStacks(ElementType.Bleed) > 0  ? 1 : 0 },
                { ElementType.Decay,  handler.GetStacks(ElementType.Decay) > 0  ? 1 : 0 },
                { ElementType.Frost,  handler.GetStacks(ElementType.Frost) > 0  ? 1 : 0 },
                { ElementType.Chain,  1 }, // Chain luôn lan 1 stack
            };

            // --- Tính sát thương lan truyền (1/2, làm tròn XUỐNG) ---
            int spreadDamage = Mathf.FloorToInt(stacks * 0.5f);

            // --- Lan sang các kẻ địch khác trong cùng hàng ---
            var grid = BattleGrid.Instance;
            if (grid == null) yield break;

            List<CardBattle> rowEnemies = grid.GetEnemiesInSameRow(cardBattle);
            foreach (var enemy in rowEnemies)
            {
                if (enemy == cardBattle || enemy == null || enemy.IsDead) continue;

                var enemyHandler = enemy.GetComponent<ElementalHandler>();
                if (enemyHandler == null) continue;

                float enemyResist = enemy.Data?.GetElementalResistance(ElementType.Chain) ?? 0f;

                Debug.Log($"[Chain] ⚡ Lan sang {enemy.Data?.cardName}: " +
                          $"{spreadDamage} sát thương + các stack nguyên tố");

                // Gây sát thương lan truyền
                if (spreadDamage > 0)
                    enemyHandler.ApplyElementalDamage(spreadDamage, enemyResist);

                yield return new WaitForSeconds(HitDelay);

                // Lan các stack nguyên tố (chỉ lan 1 stack mỗi loại)
                foreach (var kv in spreadStacks)
                {
                    if (kv.Value <= 0) continue;
                    if (!enemy.IsDead)
                        yield return enemyHandler.AddStacks(kv.Key, kv.Value);
                }

                yield return new WaitForSeconds(HitDelay);
            }
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            yield break; // Chain không có hiệu ứng cuối lượt
        }

        public IEnumerator OnAfterPlayerAction(ElementalHandler handler)
        {
            yield break; // Chain không trigger sau lượt đồng minh
        }
    }
}
