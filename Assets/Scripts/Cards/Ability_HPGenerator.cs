using UnityEngine;
using ProjectM.Skills;

namespace ProjectM.Cards
{
    [CreateAssetMenu(fileName = "Ability_HPGenerator", menuName = "Project M/Cards/Abilities/HP Generator")]
    public class Ability_HPGenerator : CardAbilityBase
    {
        [Tooltip("Tên file Asset (SkillData) của thẻ kẹo muốn ném lên tay (vd: Rainbow_candy_mix)")]
        public string itemToGenerate = "Rainbow_candy_mix";

        public override void OnFragileHeartTriggered(CardBattle card)
        {
            if (SkillHandManager.Instance != null && !string.IsNullOrEmpty(itemToGenerate))
            {
                Debug.Log($"[HP Generator] Sinh ra thẻ {itemToGenerate} ném lên tay!");
                // Khởi chạy hàm IEnumerator thông qua StartCoroutine của CardBattle
                card.StartCoroutine(SkillHandManager.Instance.AddSpecificSkillToHand(itemToGenerate));
            }
            else
            {
                Debug.LogWarning("[HP Generator] Không tìm thấy SkillHandManager hoặc tên thẻ bị trống!");
            }
        }
    }
}
