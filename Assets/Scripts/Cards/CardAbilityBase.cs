using UnityEngine;

namespace ProjectM.Cards
{
    /// <summary>
    /// Base class cho các kỹ năng/nội tại riêng biệt của từng thẻ Building.
    /// Giống như SkillOverrideBase, nhưng gắn vào CardData của thẻ nằm trên bàn cờ.
    /// </summary>
    public abstract class CardAbilityBase : ScriptableObject
    {
        public virtual void OnSpawn(CardBattle card) {}
        
        public virtual void OnFragileHeartTriggered(CardBattle card) {}
        
        public virtual void OnCardDestroyed(CardBattle card, CardBattle attacker) {}
        
        /// <summary>
        /// Hệ số nhân sát thương phản lại của Thorn Heart. Mặc định là 1 (100%).
        /// Thể Ms_Pockey có thể override trả về 2 (200%).
        /// </summary>
        public virtual int GetThornMultiplier() { return 1; }

        /// <summary>
        /// Gọi ngay sau khi thẻ thực hiện xong đòn đánh (kể cả có trúng hay không).
        /// </summary>
        public virtual void OnAttack(CardBattle card, CardBattle target) {}
    }
}
