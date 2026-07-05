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
            if (targets == null || targets.Length == 0 || targets[0] == null) yield break;

            CardBattle targetAlly = targets[0];
            Debug.Log($"[Night Shade] ☠️ {targetAlly.Data?.cardName} bao phủ bóng đêm! Các đòn đánh vật lý từ nay gây thêm +1 Decay.");
            targetAlly.bonusDecayOnPhysicalAttack += 1;

            yield break;
        }
    }
}
