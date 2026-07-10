using UnityEngine;
using ProjectM.Elements;
using System.Collections;

namespace ProjectM.Cards
{
    /// <summary>
    /// Nội tại của công trình Nail Launcher.
    /// Không có tốc độ đánh cơ bản, nhưng phụ họa (bắn bồi) 1 stack Bleed mỗi khi có đồng minh dán Bleed lên địch.
    /// Dùng HeartType = Fragile.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_NailLauncher", menuName = "Project M/Cards/Abilities/Nail Launcher")]
    public class Ability_NailLauncher : CardAbilityBase
    {
        public override void OnSpawn(CardBattle card)
        {
            // Ép buộc thẻ này phải mang tim Fragile (tránh người dùng quên set trong Inspector)
            card.currentHeartType = HeartType.Fragile;
            
            // Gắn Helper Component vào để quản lý sự kiện 
            card.gameObject.AddComponent<NailLauncherHelper>();
        }

        public override void OnFragileHeartTriggered(CardBattle card)
        {
            // Nail Launcher tự mất máu khi bị tấn công do cơ chế Fragile Heart gốc ở CardBattle.
            // Có thể thêm hiệu ứng vỡ kính ở đây nếu thích.
        }
    }

    /// <summary>
    /// Component theo dõi sự kiện AddStack toàn cục để bắn bồi.
    /// Tự động bị dọn dẹp khi thẻ (gameObject) chết.
    /// </summary>
    public class NailLauncherHelper : MonoBehaviour
    {
        private CardBattle _myCard;

        // Dùng biến static để chặn đệ quy vô tận nếu có NHIỀU Nail Launcher trên bàn.
        // Bất cứ Nail Launcher nào đang bắn bồi, các Nail Launcher khác sẽ KHÔNG trigger event từ cú bắn đó.
        public static bool IsNailLauncherShooting = false;

        private void Awake()
        {
            _myCard = GetComponent<CardBattle>();
            ElementalHandler.OnStacksAddedGlobal += HandleStacksAdded;
        }

        private void OnDestroy()
        {
            ElementalHandler.OnStacksAddedGlobal -= HandleStacksAdded;
        }

        private void HandleStacksAdded(CardBattle target, ElementType element, int amount)
        {
            // Nếu Nail Launcher đã chết thì không làm gì
            if (_myCard == null || _myCard.IsDead) return;
            
            // Chặn đệ quy vô tận (Nail Launcher tự kích hoạt lẫn nhau)
            if (IsNailLauncherShooting) return;

            // Nếu nguyên tố được áp dụng là Bleed VÀ nạn nhân là kẻ địch
            if (element == ElementType.Bleed && target != null && !target.IsPlayerCard)
            {
                Debug.Log($"[Nail Launcher] 🚀 {_myCard.Data?.cardName} phát hiện đồng minh vừa gắn Bleed lên {target.Data?.cardName}, phụ họa thêm 1 stack!");
                StartCoroutine(ShootExtraBleed(target));
            }
        }

        private IEnumerator ShootExtraBleed(CardBattle target)
        {
            // Delay 1 chút cho giống kiểu bắn bồi (chứ không nổ cùng 1 lúc với đòn chính)
            yield return new WaitForSeconds(0.2f);

            var elemental = target.GetComponent<ElementalHandler>();
            if (elemental != null && !target.IsDead)
            {
                IsNailLauncherShooting = true;

                // Thêm hiệu ứng VFX bắn đinh nếu có (Dùng tạm hiệu ứng Hit vật lý)
                if (Managers.BattleManager.Instance != null && Managers.BattleManager.Instance.normalHitVfxPrefab != null)
                {
                    GameObject vfx = Instantiate(Managers.BattleManager.Instance.normalHitVfxPrefab, target.transform.position, Quaternion.identity, target.transform);
                    vfx.transform.localPosition = new Vector3(0, 0, -50f);
                    foreach(var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play(true);
                    Destroy(vfx, 2f);
                }

                // Gây 1 stack Bleed
                yield return StartCoroutine(elemental.AddStacks(ElementType.Bleed, 1));
                
                IsNailLauncherShooting = false;
            }
        }
    }
}
