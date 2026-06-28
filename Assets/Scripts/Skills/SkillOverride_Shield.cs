using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Kích hoạt Khiên (Shield) lên mục tiêu. Khiên chặn sát thương trước khi trừ vào máu và không tự mất.
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Shield")]
    public class SkillOverride_Shield : SkillOverrideBase
    {
        public int shieldAmount = 10;

        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (targets == null) yield break;

            foreach (var target in targets)
            {
                if (target == null || target.IsDead) continue;
                target.AddShield(shieldAmount);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
