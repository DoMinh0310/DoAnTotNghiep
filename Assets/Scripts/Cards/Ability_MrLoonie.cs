using UnityEngine;
using ProjectM.Elements;

namespace ProjectM.Cards
{
    [CreateAssetMenu(fileName = "Ability_MrLoonie", menuName = "Project M/Cards/Abilities/Mr Loonie")]
    public class Ability_MrLoonie : CardAbilityBase
    {
        public int chainStacksToApply = 5;

        public override void OnCardDestroyed(CardBattle card, CardBattle attacker)
        {
            if (attacker != null && !attacker.IsDead)
            {
                var elemental = attacker.GetComponent<ElementalHandler>();
                if (elemental != null)
                {
                    Debug.Log($"[Mr Loonie] Bị tiêu diệt! Trừng phạt {attacker.Data?.cardName} bằng {chainStacksToApply} stack Chain.");
                    card.StartCoroutine(elemental.AddStacks(ElementType.Chain, chainStacksToApply));
                }
            }
        }
    }
}
