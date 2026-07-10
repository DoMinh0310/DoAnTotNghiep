using System.Collections;
using UnityEngine;

namespace ProjectM.Elements
{
    /// <summary>
    /// Nguyên tố Nhắm Bắn (Aim):
    /// - Tích lũy stack khi được truyền vào.
    /// - Nổ gây sát thương vật lý ở đầu lượt của địch (sau 1 lượt), sát thương = số stack.
    /// - Reset stack về 0 sau khi nổ.
    /// </summary>
    public class AimEffect : IElementalEffect
    {
        public ElementType Type => ElementType.Aim;

        public IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks)
        {
            // Không làm gì ngay lập tức khi được gắn Aim
            yield break;
        }

        public IEnumerator OnTurnStart(ElementalHandler handler)
        {
            yield break;
        }

        public IEnumerator OnTurnEnd(ElementalHandler handler)
        {
            yield break;
        }

        public IEnumerator OnAfterPlayerAction(ElementalHandler handler)
        {
            int stacks = handler.GetStacks(ElementType.Aim);
            if (stacks <= 0) yield break;

            // Nếu đang trong thời gian đếm ngược (trì hoãn 1 lượt) thì giảm đi và chưa nổ
            if (handler.aimTurnDelay > 0)
            {
                handler.aimTurnDelay--;
                Debug.Log($"[Aim] 🎯 Đang đếm ngược, Aim sẽ nổ vào cuối lượt người chơi tiếp theo.");
                yield break;
            }

            var cardBattle = handler.CardBattle;
            
            // Sát thương = số stack Aim
            int totalDamage = stacks;

            // Aim nổ xong thì biến mất
            handler.SetStacks(ElementType.Aim, 0);

            if (cardBattle == null || cardBattle.IsDead) yield break;

            // Kháng tính (Aim có thể dùng kháng Aim hoặc mặc định 0)
            float resist = cardBattle.Data?.GetElementalResistance(ElementType.Aim) ?? 0f;

            Debug.Log($"[Aim] 🎯 {cardBattle.Data?.cardName}: Aim nổ! " +
                      $"Sát thương={totalDamage} (kháng {resist * 100}%). Stack reset về 0.");

            // Dùng tạm hiệu ứng nổ (có thể thay thế bằng vfx_Aim nếu game có)
            if (Managers.BattleManager.Instance != null && Managers.BattleManager.Instance.bleedHitVfxPrefab != null)
            {
                GameObject vfx = GameObject.Instantiate(Managers.BattleManager.Instance.bleedHitVfxPrefab, cardBattle.transform.position, Quaternion.identity, cardBattle.transform);
                vfx.transform.localPosition = new Vector3(0, 0, -50f);
                foreach(var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play(true);
                GameObject.Destroy(vfx, 2f);
            }

            // Gây sát thương Aim
            handler.ApplyElementalDamage(totalDamage, resist);

            int actualDamage = Mathf.CeilToInt(totalDamage * (1f - Mathf.Clamp01(resist)));
            if (actualDamage > 0 && !cardBattle.IsDead)
            {
                // TÍCH THÊM số stack Bleed = với lượng sát thương vừa gây ra (không dùng pending damage để tránh double-dip)
                yield return handler.CardBattle.StartCoroutine(handler.AddStacks(ElementType.Bleed, actualDamage));
            }

            // Đợi 0.5 giây để tách biệt rõ ràng pha nổ của Aim và pha nổ của Bleed
            yield return new WaitForSeconds(0.5f);
        }
    }
}
