using System.Collections;
using UnityEngine;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Băng (Frost).
    /// - Khi nhận stack: Frost Counter += stack (cộng dồn).
    /// - Khi Frost Counter > 0: Speed của đơn vị bị ĐÓNG BĂNG (không giảm).
    /// - Mỗi TurnStart: Frost Counter giảm 1. Khi Frost Counter về 0 → Speed tiếp tục bình thường.
    /// </summary>
    public class FrostEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Frost;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            // Stack Frost chính là Frost Counter — đã được cộng dồn trong ElementalHandler
            Debug.Log($"[Frost] ❄️ {handler.gameObject.name} bị đóng băng! Frost Counter: {totalStacks}");
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            int frostCounter = handler.GetStacks(ElementType.Frost);
            if (frostCounter <= 0) yield break;

            // Giảm Frost Counter đi 1 mỗi lượt
            int newCounter = frostCounter - 1;
            handler.SetStacks(ElementType.Frost, newCounter);

            if (newCounter > 0)
                Debug.Log($"[Frost] ❄️ {handler.gameObject.name} vẫn đóng băng! Frost Counter còn: {newCounter}");
            else
                Debug.Log($"[Frost] ❄️ {handler.gameObject.name} đã tan băng! Speed tiếp tục đếm từ lượt sau.");

            yield break;
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            yield break; // Frost không có hiệu ứng cuối lượt
        }
    }
}
