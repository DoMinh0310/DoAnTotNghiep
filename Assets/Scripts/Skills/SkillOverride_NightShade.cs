using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Night_shade: Upon use, physical attacks will also apply Decay +1, for the rest of the battle
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Night Shade")]
    public class SkillOverride_NightShade : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (caster == null) yield break;

            Debug.Log($"[Night Shade] ☠️ {caster.Data?.cardName} bao phủ bóng đêm! Các đòn đánh vật lý từ nay gây thêm +1 Decay.");
            caster.bonusDecayOnPhysicalAttack += 1;

            yield break;
        }
    }
}
