using UnityEngine;
using System.Collections;
using ProjectM.Cards;
using ProjectM.Elements;

namespace ProjectM.Skills
{
    /// <summary>
    /// Magic Eraser: Remove all elemental debuffs (Bleed, Frost, Decay, Chain)
    /// from the targeted ally card.
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Magic Eraser")]
    public class SkillOverride_MagicEraser : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (targets == null || targets.Length == 0) yield break;

            int totalCleansed = 0;

            foreach (var card in targets)
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

                // Clear luôn sát thương Bleed vật lý ngầm tích lũy (nếu có)
                elemental.ConsumePendingBleedDamage();
            }

            if (totalCleansed > 0)
                Debug.Log($"[Magic Eraser] ✅ Đã xóa tổng cộng {totalCleansed} stack debuff khỏi đồng minh được chỉ định!");
            else
                Debug.Log("[Magic Eraser] Mục tiêu không có debuff nào cần xóa.");

            yield break;
        }
    }
}
