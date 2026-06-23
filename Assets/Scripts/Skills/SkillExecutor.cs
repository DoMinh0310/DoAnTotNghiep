using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectM.Cards;
using ProjectM.Elements;

namespace ProjectM.Skills
{
    /// <summary>
    /// Gắn vào Skill_Prefab cùng với CardDisplay.
    /// Nhận SkillData từ bên ngoài (ví dụ: SkillRewardUI), thực thi hiệu ứng
    /// khi người chơi xác nhận mục tiêu (gọi bởi SkillDragHandler).
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        public SkillData Data { get; private set; }

        // ══════════════════════════════════════════
        /// <summary>Khởi tạo skill với dữ liệu từ ScriptableObject.</summary>
        public void Init(SkillData data)
        {
            Data = data;
            if (data == null) return;

            // Hiển thị tên, artwork, mô tả lên CardDisplay
            var display = GetComponentInChildren<Cards.CardDisplay>();
            if (display != null)
                display.LoadSkillData(data);
            else
                Debug.LogWarning($"[SkillExecutor] Không tìm thấy CardDisplay trên Skill_Prefab để hiển thị '{data.skillName}'!");
        }

        // ══════════════════════════════════════════
        /// <summary>
        /// Thực thi hiệu ứng skill.
        /// Gọi bởi SkillDragHandler sau khi người chơi chọn xong mục tiêu.
        /// </summary>
        public IEnumerator Execute(CardBattle caster, List<CardBattle> targets)
        {
            if (Data == null) yield break;

            // ── Custom override: ưu tiên tuyệt đối ──
            if (Data.customOverride != null)
            {
                yield return StartCoroutine(Data.customOverride.Execute(caster, targets.ToArray()));
                yield break;
            }

            // ── Logic chuẩn ──
            switch (Data.effectType)
            {
                // ─ Gây sát thương ────────────────────────────────────────
                case SkillEffectType.Damage:
                    foreach (var target in targets)
                    {
                        if (target == null || target.IsDead) continue;
                        Debug.Log($"[Skill] {Data.skillName} → {target.Data?.cardName}: -{Data.effectValue} HP");
                        target.TakeDamage(Data.effectValue);
                        yield return new WaitForSeconds(0.15f); // Delay nhỏ nếu multi-target
                    }
                    break;

                // ─ Cộng stack nguyên tố ──────────────────────────────────
                case SkillEffectType.ApplyElement:
                    foreach (var target in targets)
                    {
                        if (target == null || target.IsDead) continue;
                        var elemental = target.GetComponent<ElementalHandler>();
                        if (elemental == null)
                        {
                            Debug.LogWarning($"[Skill] {target.Data?.cardName} không có ElementalHandler!");
                            continue;
                        }
                        Debug.Log($"[Skill] {Data.skillName} → {target.Data?.cardName}: +{Data.effectValue} stack {Data.elementType}");
                        yield return StartCoroutine(elemental.AddStacks(Data.elementType, Data.effectValue));
                        yield return new WaitForSeconds(0.15f);
                    }
                    break;

                // ─ Hồi máu cho mục tiêu ─────────────────────────────────────
                case SkillEffectType.HealTarget:
                    foreach (var target in targets)
                    {
                        if (target == null || target.IsDead) continue;
                        Debug.Log($"[Skill] {Data.skillName}: Hồi +{Data.effectValue} HP cho {target.Data?.cardName}");
                        target.HealHP(Data.effectValue);
                        yield return new WaitForSeconds(0.15f);
                    }
                    break;

                // ─ Tăng sát thương đòn thường tiếp theo ─────────────────
                case SkillEffectType.AttackBuff:
                    Debug.Log($"[Skill] {Data.skillName}: +{Data.effectValue} attack cho đòn thường tiếp theo của {caster.Data?.cardName}");
                    caster.AddAttackBonus(Data.effectValue);
                    break;
            }
        }

        // ══════════════════════════════════════════
        // HELPERS — Resolve danh sách target theo TargetType
        // ══════════════════════════════════════════

        /// <summary>
        /// Tự động resolve target cho skill theo TargetType.
        /// SkillDragHandler gọi hàm này sau khi người chơi kéo vào mục tiêu.
        /// </summary>
        public List<CardBattle> ResolveTargets(CardBattle draggedTarget, CardBattle caster, List<CardBattle> allEnemies)
        {
            var result = new List<CardBattle>();
            switch (Data.targetType)
            {
                case SkillTargetType.SingleEnemy:
                case SkillTargetType.SingleAlly:
                    if (draggedTarget != null)
                        result.Add(draggedTarget);
                    break;

                case SkillTargetType.AllEnemies:
                    result.AddRange(allEnemies);
                    break;

                case SkillTargetType.EnemyRow:
                    // Lấy toàn bộ kẻ địch trong cùng hàng với mục tiêu được kéo vào
                    if (draggedTarget != null)
                    {
                        var grid = UnityEngine.Object.FindAnyObjectByType<Managers.BattleGrid>();
                        if (grid != null)
                        {
                            var rowEnemies = grid.GetEnemiesInSameRow(draggedTarget);
                            if (rowEnemies != null)
                                result.AddRange(rowEnemies);
                        }
                        else
                        {
                            // Fallback nếu không tìm được BattleGrid: chỉ đánh mục tiêu đó
                            UnityEngine.Debug.LogWarning("[SkillExecutor] Không tìm thấy BattleGrid, fallback → SingleEnemy");
                            result.Add(draggedTarget);
                        }
                    }
                    break;

                case SkillTargetType.Self:
                    result.Add(caster);
                    break;
            }
            return result;
        }
    }
}
