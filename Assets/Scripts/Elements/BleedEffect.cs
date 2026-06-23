using System.Collections;
using UnityEngine;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Chảy máu (Bleed).
    /// Cộng dồn stack. Khi stack >= ngưỡng kích hoạt (lấy từ CardData của đơn vị bị nhiễm):
    ///   - Phát nổ ngay TRƯỚC KHI đơn vị đó tấn công
    ///   - Gây sát thương = ngưỡng kích hoạt (sau khi trừ kháng, làm tròn lên)
    ///   - Reset stack về 0, stack thừa mất hết
    /// </summary>
    public class BleedEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Bleed;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            // Bleed không trigger ngay khi cộng stack — chỉ trigger trước khi đơn vị đánh
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            int stacks = handler.GetStacks(ElementType.Bleed);
            if (stacks <= 0) yield break;

            var cardBattle = handler.CardBattle;
            if (cardBattle?.Data == null) yield break;

            int threshold  = cardBattle.Data.GetElementalThreshold(ElementType.Bleed);
            float resist   = cardBattle.Data.GetElementalResistance(ElementType.Bleed);

            if (stacks < threshold) yield break;

            // ── Kích hoạt! ──
            Debug.Log($"[Bleed] 🩸 {cardBattle.Data.cardName}: {stacks} stack ≥ ngưỡng {threshold} → Xuất huyết!");

            // Reset stack về 0 (overflow mất hết)
            handler.SetStacks(ElementType.Bleed, 0);

            // Gây sát thương = ngưỡng, trừ kháng
            handler.ApplyElementalDamage(threshold, resist);

            // TODO: Thêm visual effect máu ở đây (VFX, shake camera...)
            yield break;
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            yield break; // Bleed không có hiệu ứng sau khi đánh
        }
    }
}
