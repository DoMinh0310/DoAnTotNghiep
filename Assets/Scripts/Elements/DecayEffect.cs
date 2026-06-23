using System.Collections;
using UnityEngine;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Phân rã (Decay).
    /// Cộng dồn stack. SAU KHI đơn vị bị nhiễm tấn công:
    ///   - Gây sát thương = số lượt DoT còn lại (giảm dần mỗi lượt)
    ///   - Ví dụ: 3 stack → lượt 1: -3, lượt 2: -2, lượt 3: -1 → tổng -6
    /// Nếu nhiễm thêm phân rã khi đang phân rã: dotDuration cộng dồn thêm
    ///   - Ví dụ: đang có duration=2, nhiễm thêm 3 → duration = 2+3 = 5
    /// </summary>
    public class DecayEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Decay;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            // Khi nhận thêm stack phân rã, cộng dồn dotDuration
            // (không phải ghi đè mà cộng thêm để stack cũ không bị mất)
            int addedStacks = totalStacks - handler.GetDotDuration(ElementType.Decay);
            if (addedStacks > 0)
            {
                int newDuration = handler.GetDotDuration(ElementType.Decay) + addedStacks;
                handler.SetDotDuration(ElementType.Decay, newDuration);
                Debug.Log($"[Decay] ☠️ {handler.gameObject.name} bị phân rã! Tổng DoT duration: {newDuration} lượt");
            }
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            yield break; // Decay không trigger trước khi đánh
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            int duration = handler.GetDotDuration(ElementType.Decay);
            if (duration <= 0) yield break;

            var cardBattle = handler.CardBattle;
            if (cardBattle?.Data == null) yield break;

            float resist = cardBattle.Data.GetElementalResistance(ElementType.Decay);

            // Gây sát thương = duration hiện tại
            Debug.Log($"[Decay] ☠️ {cardBattle.Data.cardName} nhận phân rã {duration} sát thương (còn {duration - 1} lượt sau)");
            handler.ApplyElementalDamage(duration, resist);

            // Giảm duration đi 1 mỗi lượt
            handler.SetDotDuration(ElementType.Decay, duration - 1);

            // Giảm stack display theo (để UI có thể hiển thị sau)
            int currentStacks = handler.GetStacks(ElementType.Decay);
            handler.SetStacks(ElementType.Decay, Mathf.Max(0, currentStacks - 1));

            // TODO: Thêm visual effect phân rã ở đây
            yield break;
        }
    }
}
