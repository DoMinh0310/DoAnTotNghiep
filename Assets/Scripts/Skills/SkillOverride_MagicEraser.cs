using UnityEngine;
using System.Collections;
using ProjectM.Cards;
using ProjectM.Elements;
using ProjectM.Managers;

namespace ProjectM.Skills
{
    /// <summary>
    /// Magic Eraser: Remove all elemental debuffs (Bleed, Frost, Decay, Chain)
    /// from ALL player cards currently on the battlefield.
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Magic Eraser")]
    public class SkillOverride_MagicEraser : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            var grid = BattleGrid.Instance;
            if (grid == null) yield break;

            var playerCards = grid.GetAllPlayerCards();
            if (playerCards == null || playerCards.Count == 0) yield break;

            int totalCleansed = 0;

            foreach (var card in playerCards)
            {
                if (card == null || card.IsDead) continue;

                var elemental = card.GetComponent<ElementalHandler>();
                if (elemental == null) continue;

                // Xóa toàn bộ stack của mọi loại nguyên tố
                foreach (ElementType type in System.Enum.GetValues(typeof(ElementType)))
                {
                    if (type == ElementType.None) continue;

                    int stacks = elemental.GetStacks(type);
                    if (stacks > 0)
                    {
                        elemental.SetStacks(type, 0);
                        totalCleansed += stacks;
                        Debug.Log($"[Magic Eraser] ✨ Xóa {stacks} stack {type} khỏi '{card.Data?.cardName}'");
                    }
                }
            }

            if (totalCleansed > 0)
                Debug.Log($"[Magic Eraser] ✅ Đã xóa tổng cộng {totalCleansed} stack debuff khỏi đồng minh!");
            else
                Debug.Log("[Magic Eraser] Không có debuff nào cần xóa.");

            yield break;
        }
    }
}
