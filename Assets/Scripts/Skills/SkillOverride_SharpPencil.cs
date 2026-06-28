using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Sharp pencil: Hit a row with equal DMG to owner’s
    /// Gán Target Type = EnemyRow trong SkillData inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Project M/Skills/Overrides/Sharp Pencil")]
    public class SkillOverride_SharpPencil : SkillOverrideBase
    {
        public override IEnumerator Execute(CardBattle caster, CardBattle[] targets)
        {
            if (caster == null || targets == null) yield break;

            // Lấy đúng chỉ số tấn công hiện tại của thẻ chủ sở hữu
            int ownerDamage = (caster.Data != null ? caster.Data.attack : 5) + caster.permanentAttackBonus;
            Debug.Log($"[Sharp Pencil] ✏️ {caster.Data?.cardName} phóng bút chì vào hàng địch, gây {ownerDamage} sát thương lên mỗi mục tiêu!");

            foreach (var target in targets)
            {
                if (target == null || target.IsDead) continue;
                target.TakeDamage(ownerDamage);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
