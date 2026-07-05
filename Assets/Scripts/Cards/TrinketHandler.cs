using UnityEngine;
using System.Collections;
using ProjectM.Skills;
using ProjectM.Managers;
using ProjectM.Elements;

namespace ProjectM.Cards
{
    /// <summary>
    /// Gắn lên Card Prefab cùng với CardBattle.
    /// Quản lý nội tại (passives) của các Trinket trang bị trên thẻ tướng.
    /// </summary>
    public class TrinketHandler : MonoBehaviour
    {
        private TrinketData _trinket;
        private CardBattle  _card;

        public TrinketData EquippedTrinket => _trinket;

        /// <summary>Trang bị Trinket mới khi bắt đầu combat.</summary>
        public void EquipTrinket(TrinketData trinket)
        {
            _trinket = trinket;
            _card    = GetComponent<CardBattle>();

            if (_trinket == null || _card == null) return;

            // Áp dụng các chỉ số cộng thêm tĩnh (Stat Boost)
            if (_trinket.bonusHP > 0)
            {
                _card.HealHP(_trinket.bonusHP);
            }
            if (_trinket.bonusATK > 0)
            {
                _card.AddPermanentAttack(_trinket.bonusATK);
            }
            if (_trinket.speedReduction > 0)
            {
                _card.ReduceCurrentSpeed(_trinket.speedReduction);
            }

            Debug.Log($"[TrinketHandler] {gameObject.name}: Trang bị Trinket '{_trinket.trinketName}'");
        }

        public void UnequipTrinket()
        {
            _trinket = null;
        }

        /// <summary>Được gọi bởi CardBattle mỗi khi thẻ này thực hiện xong 1 đòn tấn công.
        /// lastTarget: mục tiêu vừa bị tấn công (dùng để áp hiệu ứng lên đúng kẻ thù).
        /// actualDamage: sát thương thực tế đã gây ra (dùng để scale stack Bleed/Chain).
        /// </summary>
        public void OnCardAttacked(CardBattle lastTarget = null, int actualDamage = 0)
        {
            if (_trinket == null || _card == null || _card.IsDead) return;

            string tName = (_trinket.name + " " + _trinket.trinketName).ToLower();

            // 1. Flash light: each time attack, Draw +1
            if (tName.Contains("flash") || tName.Contains("light"))
            {
                Debug.Log($"[Trinket] 🔦 Flash light kích hoạt! Rút +1 lá bài.");
                if (SkillHandManager.Instance != null)
                    StartCoroutine(SkillHandManager.Instance.DrawOneCardIfNeeded());
            }

            // 2. Bow: each time attack, front ally's DMG +1
            if (tName.Contains("bow"))
            {
                var frontAlly = BattleGrid.Instance?.GetFrontAllyInRow(_card);
                if (frontAlly != null)
                {
                    Debug.Log($"[Trinket] 🏹 Bow kích hoạt! Tăng +1 DMG vĩnh viễn cho đồng minh phía trước '{frontAlly.Data?.cardName}'");
                    frontAlly.AddPermanentAttack(1);
                }
            }

            // 3. Heart locket: each time attack: if HP < HP max, +1 HP; if HP = HP max, +1 DMG
            if (tName.Contains("heart") || tName.Contains("locket"))
            {
                int maxHP = _card.Data != null ? _card.Data.health : 100;
                if (_card.CurrentHP < maxHP)
                {
                    Debug.Log($"[Trinket] 💖 Heart locket: HP ({_card.CurrentHP}) < Max ({maxHP}) -> Hồi +1 HP");
                    _card.HealHP(1);
                }
                else
                {
                    Debug.Log($"[Trinket] 💖 Heart locket: HP đầy -> Tăng +1 DMG vĩnh viễn");
                    _card.AddPermanentAttack(1);
                }
            }

            // 4. Conditioner: each time attack, apply Frost +1 to the attacked target
            if (tName.Contains("conditioner"))
            {
                if (lastTarget != null && !lastTarget.IsDead)
                {
                    var handler = lastTarget.GetComponent<ElementalHandler>();
                    if (handler != null)
                    {
                        StartCoroutine(handler.AddStacks(ElementType.Frost, 1));
                        Debug.Log($"[Trinket] ❄️ Conditioner kích hoạt! Áp Frost +1 lên '{lastTarget.Data?.cardName}'");
                    }
                }
            }
            // ── ElementalPassive: Áp stack nguyên tố lên mục tiêu khi tấn công ──
            if (_trinket.effectType == TrinketEffectType.ElementalPassive &&
                _trinket.elementTrigger == TrinketTrigger.OnAttack &&
                _trinket.elementType != ElementType.None &&
                lastTarget != null && !lastTarget.IsDead)
            {
                var handler = lastTarget.GetComponent<ElementalHandler>();
                if (handler != null)
                {
                    // Bleed và Chain scale theo sát thương thực tế
                    // Frost và Decay dùng giá trị cố định từ config trinket
                    bool scaleWithDamage = _trinket.elementType == ElementType.Bleed ||
                                          _trinket.elementType == ElementType.Chain;
                    int stacksToApply = scaleWithDamage
                        ? Mathf.Max(1, actualDamage)
                        : _trinket.elementStackPerTrigger;

                    StartCoroutine(handler.AddStacks(_trinket.elementType, stacksToApply));
                    Debug.Log($"[Trinket] ✨ {_trinket.trinketName} kích hoạt! Áp {stacksToApply} stack {_trinket.elementType} lên '{lastTarget.Data?.cardName}'");
                }
            }
        }

        /// <summary>Được gọi bởi CardBattle mỗi khi thẻ này bị tấn công bởi kẻ địch.</summary>
        public void OnCardTakeDamage(CardBattle attacker)
        {
            if (_trinket == null || _card == null || _card.IsDead) return;

            string tName = (_trinket.name + " " + _trinket.trinketName).ToLower();

            // 5. Cat ears: each time being attacked, SPD -1 (tiến độ hành động tăng 1)
            if (tName.Contains("cat") || tName.Contains("ear"))
            {
                Debug.Log($"[Trinket] 🐱 Cat ears kích hoạt! SPD -1 (giảm 1 đếm ngược lượt).");
                _card.ReduceCurrentSpeed(1);
            }

            // 6. Pocket Spikes: each time hit, add 1 Tooth_pick skill card to hand
            if (tName.Contains("pocket") || tName.Contains("spike"))
            {
                Debug.Log($"[Trinket] 🦷 Pocket Spikes kích hoạt! Thêm 1 thẻ Tooth_pick lên tay.");
                if (SkillHandManager.Instance != null)
                    StartCoroutine(SkillHandManager.Instance.AddSpecificSkillToHand("Tooth Pick"));
            }

            // 7. Survival Guide: each time hit, add 1 Plaster skill card to hand
            if (tName.Contains("survival") || tName.Contains("guide"))
            {
                Debug.Log($"[Trinket] 🩹 Survival Guide kích hoạt! Thêm 1 thẻ Plaster lên tay.");
                if (SkillHandManager.Instance != null)
                    StartCoroutine(SkillHandManager.Instance.AddSpecificSkillToHand("Plaster"));
            }
            // ── ElementalPassive: Áp stack nguyên tố lên kẻ tấn công mình ──
            if (_trinket.effectType == TrinketEffectType.ElementalPassive &&
                _trinket.elementTrigger == TrinketTrigger.OnHit &&
                _trinket.elementType != ElementType.None &&
                attacker != null && !attacker.IsDead)
            {
                var handler = attacker.GetComponent<ElementalHandler>();
                if (handler != null)
                {
                    StartCoroutine(handler.AddStacks(_trinket.elementType, _trinket.elementStackPerTrigger));
                    Debug.Log($"[Trinket] ✨ {_trinket.trinketName} kích hoạt! Áp {_trinket.elementStackPerTrigger} stack {_trinket.elementType} lên kẻ tấn công '{attacker.Data?.cardName}'");
                }
            }
        }
    }
}
