using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Kích hoạt Phản Sát Thương (Thorns) lên mục tiêu. Khi bị kẻ địch đánh trúng, phản lại 50% sát thương nhận vào.
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Thorns")]
    public class SkillOverride_Thorns : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (targets == null) yield break;

            foreach (var target in targets)
            {
                if (target == null || target.IsDead) continue;
                target.EnableThorns();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
