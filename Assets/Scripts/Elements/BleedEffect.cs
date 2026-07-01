using System.Collections;
using UnityEngine;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Chảy Máu (Bleed) — Cơ chế mới:
    ///
    /// TÍCH LŨY: Khi kẻ địch đang có ít nhất 1 stack Bleed, mọi đòn đánh VẬT LÝ
    ///           (không mang nguyên tố, ElementType.None) vào kẻ địch đó sẽ cộng thêm
    ///           số stack bằng với sát thương thực tế đã gây ra.
    ///           Việc này được xử lý bởi CardBattle khi gọi NotifyPhysicalDamageReceived().
    ///
    /// KÍCH HOẠT: Sau khi toàn bộ action của đồng minh trong 1 lượt hoàn tất,
    ///            kẻ địch sẽ bị nổ Bleed với sát thương bằng tổng pending damage.
    ///            Stack Bleed và pending damage đều reset về 0 sau khi nổ.
    ///
    /// Lưu ý: Stack Bleed ở đây đóng vai trò "bộ nhớ" cho biết kẻ địch đang bị Bleed.
    ///         Bản thân pending damage mới là thứ quyết định sát thương thực tế.
    /// </summary>
    public class BleedEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Bleed;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            // Bleed không trigger ngay khi cộng stack
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            // Bleed không trigger đầu lượt địch
            yield break;
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            // Bleed không trigger cuối lượt địch
            yield break;
        }

        public IEnumerator OnAfterPlayerAction(ElementalHandler handler)
        {
            int stacks = handler.GetStacks(ElementType.Bleed);
            if (stacks <= 0) yield break;

            int pendingDamage = handler.ConsumePendingBleedDamage();
            if (pendingDamage <= 0)
            {
                // Không có đòn vật lý nào vào kẻ địch này lượt này, Bleed không nổ
                yield break;
            }

            var cardBattle = handler.CardBattle;
            if (cardBattle == null || cardBattle.IsDead) yield break;

            float resist = cardBattle.Data?.GetElementalResistance(ElementType.Bleed) ?? 0f;

            // Bleed nổ: sát thương = tổng pending vật lý, sau đó reset stack về 0
            Debug.Log($"[Bleed] 🩸 {cardBattle.Data?.cardName}: Bleed nổ! " +
                      $"Sát thương = {pendingDamage} (kháng {resist * 100}%). Stack reset về 0.");

            handler.SetStacks(ElementType.Bleed, 0);
            
            if (Managers.BattleManager.Instance != null && Managers.BattleManager.Instance.bleedHitVfxPrefab != null)
            {
                GameObject vfx = GameObject.Instantiate(Managers.BattleManager.Instance.bleedHitVfxPrefab, cardBattle.transform.position, Quaternion.identity, cardBattle.transform);
                vfx.transform.localPosition = new Vector3(0, 0, -50f);
                foreach(var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play(true);
                GameObject.Destroy(vfx, 2f);
            }

            handler.ApplyElementalDamage(pendingDamage, resist);

            yield break;
        }
    }
}
