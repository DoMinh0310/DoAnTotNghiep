using UnityEngine;

namespace ProjectM.Cards.Abilities
{
    [CreateAssetMenu(fileName = "New Heal On Attack", menuName = "Project M/Abilities/Heal On Attack")]
    public class HealOnAttackAbility : CardAbilityBase
    {
        [Tooltip("Số lượng HP sẽ hồi lại sau mỗi đòn đánh")]
        public int healAmount = 2;

        public override void OnAttack(CardBattle card, CardBattle target)
        {
            if (card != null && !card.IsDead && healAmount > 0)
            {
                card.Heal(healAmount);
                Debug.Log($"[HealOnAttack] 💚 {card.Data?.cardName} đã hồi {healAmount} HP sau khi tấn công!");
            }
        }
    }
}
