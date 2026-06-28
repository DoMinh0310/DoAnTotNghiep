using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Giảm chỉ số Speed đếm ngược hiện tại của mục tiêu trong chu kỳ lượt này.
    /// Khi speed đếm ngược chạm 0 và ra đòn xong, speed tự động quay về chỉ số gốc (cardData.speed).
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Reduce Speed")]
    public class SkillOverride_ReduceSpeed : SkillOverrideBase
    {
        [Tooltip("Số lượng speed đếm ngược muốn giảm đi")]
        public int reduceAmount = 2;

        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (targets == null) yield break;

            foreach (var target in targets)
            {
                if (target == null || target.IsDead) continue;
                target.ReduceCurrentSpeed(reduceAmount);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
