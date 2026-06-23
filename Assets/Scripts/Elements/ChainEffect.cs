using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectM.Cards;
using ProjectM.Managers;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Dây chuyền (Chain).
    /// - Cộng dồn stack như Bleed.
    /// - Khi đủ threshold: gây nhiều đòn liên tiếp (threshold lần, mỗi lần 1 damage) lên mục tiêu.
    /// - Sau đó LAN 1 stack dây chuyền sang tất cả kẻ địch KHÁC trong cùng hàng.
    /// - Kích hoạt TurnStart (trước khi địch đánh), giống Bleed.
    /// </summary>
    public class ChainEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Chain;

        // Delay nhỏ giữa mỗi đòn (giây) để tạo cảm giác multi-hit
        private const float HitDelay = 0.08f;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            // Chain không trigger ngay khi cộng stack — chỉ trigger đầu lượt
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            int stacks = handler.GetStacks(ElementType.Chain);
            if (stacks <= 0) yield break;

            var cardBattle = handler.CardBattle;
            if (cardBattle?.Data == null) yield break;

            int threshold = cardBattle.Data.GetElementalThreshold(ElementType.Chain);
            float resist  = cardBattle.Data.GetElementalResistance(ElementType.Chain);

            if (stacks < threshold) yield break;

            // ── Kích hoạt! ──
            Debug.Log($"[Chain] ⚡ {cardBattle.Data.cardName}: {stacks} stack ≥ ngưỡng {threshold} → Phóng điện {threshold} đòn!");

            // Reset stack trước khi gây damage (tránh double trigger)
            handler.SetStacks(ElementType.Chain, 0);

            // Gây threshold lần đòn, mỗi lần 1 damage (trừ kháng)
            for (int i = 0; i < threshold; i++)
            {
                if (cardBattle.IsDead) break;
                int actualDmg = Mathf.CeilToInt(1f * (1f - Mathf.Clamp01(resist)));
                if (actualDmg > 0)
                {
                    Debug.Log($"[Chain] ⚡ Đòn {i + 1}/{threshold} → {cardBattle.Data.cardName}: -{actualDmg}");
                    cardBattle.TakeDamage(actualDmg);
                }
                yield return new WaitForSeconds(HitDelay);
            }

            // ── Lan 1 stack sang tất cả kẻ địch KHÁC trong cùng hàng ──
            var grid = BattleGrid.Instance;
            if (grid == null) yield break;

            List<CardBattle> rowEnemies = grid.GetEnemiesInSameRow(cardBattle);
            foreach (var enemy in rowEnemies)
            {
                if (enemy == cardBattle || enemy == null || enemy.IsDead) continue;

                var enemyHandler = enemy.GetComponent<ElementalHandler>();
                if (enemyHandler == null) continue;

                Debug.Log($"[Chain] ⚡ Lan 1 stack dây chuyền sang {enemy.Data?.cardName}");
                yield return new WaitForSeconds(HitDelay);
                yield return enemyHandler.AddStacks(ElementType.Chain, 1);
            }
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            yield break; // Chain không có hiệu ứng cuối lượt
        }
    }
}
