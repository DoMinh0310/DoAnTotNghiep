using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using ProjectM.Elements;

namespace ProjectM.Skills
{
    /// <summary>
    /// Eraser: Tẩy ngẫu nhiên 1 loại hiệu ứng bất lợi đang có trên đồng minh.
    /// (Phiên bản "ít ma thuật" hơn của Magic Eraser).
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Eraser")]
    public class SkillOverride_Eraser : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (targets == null || targets.Length == 0) yield break;

            foreach (var card in targets)
            {
                if (card == null || card.IsDead) continue;

                var elemental = card.GetComponent<ElementalHandler>();
                if (elemental == null) continue;

                // 1. Thu thập danh sách các loại debuff đang có trên mục tiêu
                List<ElementType> activeDebuffs = new List<ElementType>();

                foreach (ElementType type in System.Enum.GetValues(typeof(ElementType)))
                {
                    if (type == ElementType.None) continue;

                    int stacks = elemental.GetStacks(type);
                    if (stacks > 0)
                    {
                        activeDebuffs.Add(type);
                    }
                }

                // 2. Nếu có debuff, chọn ngẫu nhiên 1 loại để tẩy sạch
                if (activeDebuffs.Count > 0)
                {
                    ElementType randomDebuff = activeDebuffs[Random.Range(0, activeDebuffs.Count)];
                    int stacksRemoved = elemental.GetStacks(randomDebuff);

                    elemental.SetStacks(randomDebuff, 0);

                    // Nếu tẩy trúng Bleed, cần xóa luôn sát thương ngầm tích lũy
                    if (randomDebuff == ElementType.Bleed)
                    {
                        elemental.ConsumePendingBleedDamage();
                    }

                    Debug.Log($"[Eraser] ✨ Đã chọn ngẫu nhiên và tẩy sạch {stacksRemoved} stack [{randomDebuff}] khỏi '{card.Data?.cardName}'!");
                }
                else
                {
                    Debug.Log($"[Eraser] '{card.Data?.cardName}' đang rất khỏe mạnh, không có debuff nào để tẩy.");
                }
            }

            yield break;
        }
    }
}
