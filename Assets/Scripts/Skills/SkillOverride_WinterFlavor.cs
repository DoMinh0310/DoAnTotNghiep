using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Winter_flavor: Upon use, next attack also apply frost stacks = number of physical damage dealt
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Winter Flavor")]
    public class SkillOverride_WinterFlavor : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (caster == null) yield break;

            Debug.Log($"[Winter Flavor] ❄️ {caster.Data?.cardName} uống hương vị mùa đông! Đòn đánh vật lý tiếp theo sẽ kèm Frost theo sát thương.");
            caster.applyFrostOnNextAttack = true;

            yield break;
        }
    }
}
