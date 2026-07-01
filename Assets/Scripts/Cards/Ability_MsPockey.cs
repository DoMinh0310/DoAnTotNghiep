using UnityEngine;

namespace ProjectM.Cards
{
    [CreateAssetMenu(fileName = "Ability_MsPockey", menuName = "Project M/Cards/Abilities/Ms Pockey")]
    public class Ability_MsPockey : CardAbilityBase
    {
        [Tooltip("Hệ số nhân sát thương phản lại khi dùng Thorn Heart (2 = 200%)")]
        public int reflectionMultiplier = 2;

        public override int GetThornMultiplier()
        {
            return reflectionMultiplier;
        }
    }
}
