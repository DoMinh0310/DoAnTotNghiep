using UnityEngine;
using System.Collections;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Base class để tạo hiệu ứng skill đặc biệt.
    /// Khi một skill cần logic mà 4 loại hiệu ứng chuẩn không đáp ứng được:
    ///   1. Tạo script mới kế thừa SkillOverrideBase (ví dụ: SkillOverride_TwinStrike.cs)
    ///   2. Override phương thức Execute()
    ///   3. Tạo asset từ script đó (CreateAssetMenu)
    ///   4. Gán asset vào SkillData.customOverride
    ///
    /// Ví dụ:
    ///   [CreateAssetMenu(menuName = "Project M/Skills/Overrides/TwinStrike")]
    ///   public class SkillOverride_TwinStrike : SkillOverrideBase { ... }
    /// </summary>
    public abstract class SkillOverrideBase : ScriptableObject
    {
        /// <summary>
        /// Implement toàn bộ logic đặc biệt của skill tại đây.
        /// caster  = thẻ tướng đang dùng skill
        /// targets = mảng mục tiêu (đã được SkillExecutor resolve dựa vào targetType)
        /// </summary>
        public abstract IEnumerator Execute(CardBattle caster, CardBattle[] targets);
    }
}
