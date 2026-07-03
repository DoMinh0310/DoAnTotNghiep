using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ProjectM.Elements;

namespace ProjectM.Cards
{
    public enum HeartType
    {
        Normal,
        Fragile,
        Doom,
        Thorn
    }

    /// <summary>
    /// "Não chiến đấu" của mỗi lá bài.
    /// Lưu trạng thái runtime (HP, Counter, Ult) và xử lý logic tấn công/nhận damage/chết.
    /// Gắn vào Card Prefab cùng với CardDisplay và BattleAnimator.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    [RequireComponent(typeof(BattleAnimator))]
    public class CardBattle : MonoBehaviour
    {
        // ─────────────────────────────────────────────────
        // Trạng thái Runtime (hiển thị trong Inspector để debug)
        // ─────────────────────────────────────────────────
        [Header("Runtime State (Read Only - Inspect Only)")]
        [SerializeField] private int currentHP;
        [SerializeField] private int currentCounter;
        [SerializeField] private int currentUlt;
        [SerializeField] private bool isPlayerCard;
        [SerializeField] private bool isDead;

        [Header("Elemental Timing")]
        [Tooltip("Độ trễ (giây) trước khi sát thương nguyên tố (độc/lửa...) kích hoạt sau lượt đánh.\n" +
                 "Tăng lên để tách bạch với hit flash thường, tránh 2 flash chồng lên nhau.")]
        [SerializeField] private float elementalDamageDelay = 0.4f;

        // Bonus tạm thời từ skill AttackBuff — tiêu thụ sau 1 đòn đánh
        private int _pendingAttackBonus = 0;

        // Buff từ kỹ năng Winter Flavor & Night Shade
        public bool applyFrostOnNextAttack = false;
        public int bonusDecayOnPhysicalAttack = 0;

        // Giáp Khiên & Phản Dame (Thorns)
        public int currentShield = 0;
        public bool hasThorns = false;

        public bool hasSecondPulse = false;
        public bool secondPulseTriggeredThisTurn = false;

        public int permanentAttackBonus = 0;
        
        // Cơ chế Heart đặc biệt cho Building (Fragile, Doom, Thorn)
        public HeartType currentHeartType = HeartType.Normal;
        public event System.Action OnFragileHeartTriggered;

        // HP Tối đa sau khi đã tính toán các loại Bonus (như Smith Event)
        public int maxHP;

        // Dữ liệu gốc từ ScriptableObject
        private CardData cardData;

        // References đến các component khác trên cùng GameObject
        private CardDisplay display;
        private BattleAnimator battleAnim;
        private ElementalHandler elemental; // Nullable — card không bắt buộc phải có nguyên tố
        private TargetHighlight _targetHighlight; // Nullable — chỉ hiện khi được chọn làm mục tiêu skill

        // Thuộc tính public để các script khác đọc
        public bool IsPlayerCard  => isPlayerCard;
        public bool IsDead        => isDead;
        public int  CurrentHP     => currentHP;
        public int  CurrentUlt    => currentUlt;
        public CardData Data => cardData;
        public CardDisplay Display => display;

        public void OnTurnEnded()
        {
            if (hasSecondPulse && secondPulseTriggeredThisTurn)
            {
                hasSecondPulse = false;
                secondPulseTriggeredThisTurn = false;
                UpdateHealthUI(); // Cập nhật lại UI bỏ icon Second Pulse
                Debug.Log($"[SecondPulse] {cardData?.cardName} đã mất Second Pulse sau khi turn kết thúc.");
            }
        }

        // ─────────────────────────────────────────────────
        // Khởi tạo
        // ─────────────────────────────────────────────────
        private void Awake()
        {
            display         = GetComponent<CardDisplay>();
            battleAnim      = GetComponent<BattleAnimator>();
            elemental       = GetComponent<ElementalHandler>();
            _targetHighlight = GetComponentInChildren<TargetHighlight>(includeInactive: true);
            
            // Tự động thêm ElementalHandler nếu prefab chưa có (VD: quái vật)
            if (elemental == null)
            {
                elemental = gameObject.AddComponent<ElementalHandler>();
            }
        }

        /// <summary>
        /// Gọi bởi BattleGrid sau khi đặt bài vào slot để nạp stats chiến đấu.
        /// </summary>
        public void Initialize(CardData data, bool playerCard, int smithAtkBonus = 0, int smithHpBonus = 0)
        {
            cardData      = data;
            isPlayerCard  = playerCard;
            isDead        = false;

            maxHP          = data.health + smithHpBonus;
            currentHP      = maxHP;
            currentCounter = data.speed;
            currentUlt     = 0;
            applyFrostOnNextAttack = false;
            bonusDecayOnPhysicalAttack = 0;
            currentShield = 0;
            hasThorns = false;
            hasSecondPulse= false;
            currentHeartType = data.defaultHeartType; // Khởi tạo loại tim từ data
            permanentAttackBonus = smithAtkBonus;

            RefreshAllUI();
            Debug.Log($"[CardBattle] {data.cardName} khởi tạo xong. HP={currentHP}, Speed={currentCounter}");

            if (cardData.customAbility != null)
            {
                cardData.customAbility.OnSpawn(this);
            }
        }

        // ─────────────────────────────────────────────────
        // Hệ thống lượt
        // ─────────────────────────────────────────────────

        /// <summary>
        /// BattleManager gọi và yield hàm này để đợi animation xong mới xử lý thẻ tiếp theo.
        /// Trừ counter, khi về 0 thì tấn công (và chờ animation kết thúc).
        /// </summary>
        public IEnumerator OnTurnTickRoutine()
        {
            if (isDead) yield break;

            // 1. TurnStart effects (Scorch nổ, Ice giảm counter, Lightning nổ...)
            //    Ice sẽ giảm Ice Counter TRONG bước này trước khi ta kiểm tra IsSpeedFrozen
            if (elemental != null)
                yield return StartCoroutine(elemental.TriggerTurnStart());

            if (isDead) yield break;

            // 2. GIẢM SPEED — bỏ qua nếu đang bị đóng băng hoặc là thẻ bất động
            bool frozen = elemental != null && elemental.IsSpeedFrozen;
            bool isStaticTarget = cardData != null && cardData.speed <= 0; // Bất động: Speed trong data = 0

            if (isStaticTarget)
            {
                // Không làm gì cả, không đếm ngược
            }
            else if (frozen)
            {
                Debug.Log($"[CardBattle] 🧊 {cardData?.cardName} bị đóng băng — Speed giữ nguyên ở {currentCounter}");
            }
            else
            {
                // Tick Relic cùng lúc giảm speed (giống hệt cơ chế speed của tướng)
                var relicHandler = GetComponent<RelicHandler>();
                relicHandler?.OnCardTurnTick();

                if (currentCounter > 0)
                {
                    // Chỉ chạy animation tick nếu counter vẫn còn > 0
                    // Nếu counter đã về 0 từ trước (VD: Cat Ears, skill mid-turn), bỏ qua bước giảm
                    // và rơi thẳng vào pha tấn công bên dưới
                    yield return StartCoroutine(AnimateCounterTick());
                }
            }

            // 3. TẤN CÔNG HOẶC FRAGILE (Nếu đủ speed, không đóng băng, và không phải thẻ bất động)
            if (!isStaticTarget && !frozen && currentCounter <= 0)
            {
                if (currentHeartType == HeartType.Fragile)
                {
                    Debug.Log($"[Fragile Heart] 🧊 {cardData?.cardName} kích hoạt kỹ năng đặc biệt và tự mất 1 HP!");
                    if (cardData?.customAbility != null)
                    {
                        cardData.customAbility.OnFragileHeartTriggered(this);
                    }
                    OnFragileHeartTriggered?.Invoke();
                    TakeDamage(1, null); // Tự mất 1 máu
                    ResetCounter();
                }
                else
                {
                    // Chỉ lao lên tấn công nếu có sức mạnh cơ bản lớn hơn 0
                    if (cardData != null && cardData.attack > 0)
                    {
                        yield return StartCoroutine(AttackSequence());
                    }
                    else
                    {
                        Debug.Log($"[CardBattle] {cardData?.cardName} đếm giờ xong nhưng Attack = 0, bỏ qua pha tấn công.");
                        ResetCounter();
                    }
                }
            }

            if (isDead) yield break;

            // 4. ELEMENTAL (Venom, Scorch...): Kích hoạt SAU KHI xử lý lượt xong
            // Đợi một khoảng delay để tách bạch với hit flash thường (tránh flash chồng chập)
            if (elemental != null && elemental.HasAnyActiveEffect())
            {
                if (elementalDamageDelay > 0f)
                    yield return new WaitForSeconds(elementalDamageDelay);
                yield return StartCoroutine(elemental.TriggerTurnEnd());
            }
        }

        /// <summary>
        /// Hiệu ứng pulse + đổi màu khi counter giảm xuống 1 đơn vị.
        /// </summary>
        private IEnumerator AnimateCounterTick()
        {
            if (display?.speedText == null)
            {
                if (currentCounter > 0) currentCounter--;
                UpdateCounterUI();
                yield break;
            }

            Transform counterContainer = display.speedText.transform.parent;
            if (counterContainer == null) counterContainer = display.speedText.transform;

            RectTransform containerRect = counterContainer.GetComponent<RectTransform>();
            
            // Dừng nhịp đập (pulse) nếu đang chạy để tránh xung đột
            if (_speedPulseTween != null)
            {
                _speedPulseTween.Kill();
                _speedPulseTween = null;
            }
            if (containerRect != null) containerRect.localScale = Vector3.one;

            Vector3 originalScale = Vector3.one;

            if (containerRect != null)
            {
                // Dùng DOTween làm hiệu ứng nảy (PunchScale) cho biểu tượng đồng hồ nhanh hơn
                Sequence seq = DOTween.Sequence();
                
                // Phóng to nhanh (tốn 0.04s)
                seq.Append(containerRect.DOScale(originalScale * 1.4f, 0.04f).SetEase(Ease.OutQuad));
                
                // Cập nhật số ở đỉnh của animation
                seq.AppendCallback(() => {
                    if (currentCounter > 0) 
                    {
                        currentCounter--;
                        AudioManager.Instance?.PlaySFX(AudioManager.Instance.speedReduceClip);
                    }
                    if (display != null && display.speedText != null)
                        display.speedText.text = currentCounter.ToString();
                });
                
                // Thu nhỏ về bình thường (tốn 0.06s)
                seq.Append(containerRect.DOScale(originalScale, 0.06f).SetEase(Ease.OutBack));
                
                // Mọi thứ hoàn tất xong xuôi mới bật nhịp đập pulse (nếu có)
                seq.OnComplete(() => {
                    UpdateCounterUI(); 
                });

                seq.SetLink(gameObject); // Báo cho DOTween tự hủy anim khi thẻ bài bị Destroy

                yield return seq.WaitForCompletion();
            }
            else
            {
                if (currentCounter > 0) currentCounter--;
                UpdateCounterUI();
            }
        }


        // Tấn công
      
        private IEnumerator AttackSequence()
        {
            Managers.BattleGrid grid = FindAnyObjectByType<Managers.BattleGrid>();
            if (grid == null)
            {
                Debug.LogWarning("[CardBattle] Không tìm thấy BattleGrid, hủy tấn công.");
                ResetCounter();
                yield break;
            }

            CardBattle target = grid.GetAttackTarget(this);
            if (target == null)
            {
                Debug.Log($"[CardBattle] {cardData.cardName} không tìm được mục tiêu, bỏ lượt.");
                ResetCounter();
                yield break;
            }

            Debug.Log($"[CardBattle] {cardData?.cardName ?? "?"} tấn công {target.Data?.cardName} gây {cardData?.attack} sát thương!");

            Vector2 attackDirection = isPlayerCard ? Vector2.right : Vector2.left;

            // Tính sát thương trước để dùng trong callback
            int totalDamage = cardData.attack + permanentAttackBonus + _pendingAttackBonus;
            _pendingAttackBonus = 0;

            // Truyền TakeDamage vào onImpact — sẽ được gọi đúng lúc lunge chạm đích
            // PlayHitAnim của target bắt đầu tức thì, song song với Phase 3 (return) của attacker
            // Một đòn đánh là "vật lý" khi chính cái thẻ này không đang mang stack nguyên tố nào cả
            // (tức là nó không phải là đòn nguyên tố của bản thân thẻ, mà là đòn thường)
            bool isPhysicalAttack = elemental == null ||
                (elemental.GetStacks(ProjectM.Elements.ElementType.Bleed) == 0 &&
                 elemental.GetStacks(ProjectM.Elements.ElementType.Decay) == 0 &&
                 elemental.GetStacks(ProjectM.Elements.ElementType.Frost) == 0 &&
                 elemental.GetStacks(ProjectM.Elements.ElementType.Chain) == 0);
            yield return StartCoroutine(battleAnim.PlayAttackAnim(attackDirection, onImpact: () =>
            {
                if (!target.IsDead)
                {
                    // Các nguyên tố chỉ áp stack, không gây sát thương vật lý trực tiếp
                    bool isStackOnlyAttack = cardData != null &&
                        (cardData.innateAttackElement == ProjectM.Elements.ElementType.Decay ||
                         cardData.innateAttackElement == ProjectM.Elements.ElementType.Chain);

                    if (!isStackOnlyAttack)
                    {
                        if (totalDamage != cardData.attack)
                            Debug.Log($"[CardBattle] {cardData.cardName} đánh tăng cường! {cardData.attack} + {totalDamage - cardData.attack} bonus = {totalDamage}");
                        target.TakeDamage(totalDamage, this);

                        // Bật VFX đòn đánh thường (nếu có)
                        if (Managers.BattleManager.Instance != null && Managers.BattleManager.Instance.normalHitVfxPrefab != null)
                        {
                            GameObject vfx = Instantiate(Managers.BattleManager.Instance.normalHitVfxPrefab, target.transform.position, Quaternion.identity, target.transform);
                            vfx.transform.localPosition = new Vector3(0, 0, -50f); 
                            vfx.transform.localScale = Vector3.one;
                            foreach(var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play(true);
                            Destroy(vfx, 2f);
                        }
                    }

                    var targetElemental = target.GetComponent<ProjectM.Elements.ElementalHandler>();

                    // Nếu thẻ này có Đòn đánh nguyên tố nội tại (Innate Attack Element)
                    if (cardData.innateAttackElement != ProjectM.Elements.ElementType.None && targetElemental != null)
                    {
                        StartCoroutine(targetElemental.AddStacks(cardData.innateAttackElement, totalDamage));
                        Debug.Log($"[InnateElement] {cardData.cardName} đánh đòn {cardData.innateAttackElement}, áp {totalDamage} stack lên {target.Data?.cardName}");
                    }
                    // Đòn đánh vật lý thuần túy (không có nội tại nguyên tố)
                    else if (isPhysicalAttack && !isStackOnlyAttack)
                    {
                        targetElemental?.NotifyPhysicalDamageReceived(totalDamage);

                        // Kỹ năng Winter Flavor
                        if (applyFrostOnNextAttack && targetElemental != null)
                        {
                            applyFrostOnNextAttack = false;
                            
                            if (Managers.BattleManager.Instance?.frostHitVfxPrefab != null)
                            {
                                GameObject vfx = Instantiate(Managers.BattleManager.Instance.frostHitVfxPrefab, target.transform.position, Quaternion.identity, target.transform);
                                vfx.transform.localPosition = new Vector3(0, 0, -50f); 
                                vfx.transform.localScale = Vector3.one;
                                foreach(var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play(true);
                                Destroy(vfx, 2f);
                            }

                            StartCoroutine(targetElemental.AddStacks(ProjectM.Elements.ElementType.Frost, totalDamage));
                            Debug.Log($"[Winter Flavor] ❄️ {cardData.cardName} áp {totalDamage} Frost lên {target.Data?.cardName}");
                        }

                        // Kỹ năng Night Shade
                        if (bonusDecayOnPhysicalAttack > 0 && targetElemental != null)
                        {
                            if (Managers.BattleManager.Instance?.decayHitVfxPrefab != null)
                            {
                                GameObject vfx = Instantiate(Managers.BattleManager.Instance.decayHitVfxPrefab, target.transform.position, Quaternion.identity, target.transform);
                                vfx.transform.localPosition = new Vector3(0, 0, -50f); 
                                vfx.transform.localScale = Vector3.one;
                                foreach(var ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play(true);
                                Destroy(vfx, 2f);
                            }

                            StartCoroutine(targetElemental.AddStacks(ProjectM.Elements.ElementType.Decay, bonusDecayOnPhysicalAttack));
                            Debug.Log($"[Night Shade] ☠️ {cardData.cardName} áp {bonusDecayOnPhysicalAttack} Decay lên {target.Data?.cardName}");
                        }
                    }
                }
                GetComponent<TrinketHandler>()?.OnCardAttacked(target, totalDamage);
            }));

            // Tăng ult sau khi đánh
            currentUlt = Mathf.Min(currentUlt + 1, cardData.ult);
            UpdateUltUI();

            // Kích hoạt nội tại sau khi đánh (nếu có)
            if (cardData != null && cardData.customAbility != null)
            {
                cardData.customAbility.OnAttack(this, target);
            }

            ResetCounter();
        }

     
        // Nhận sát thương & Hồi máu
      
        public void Heal(int amount)
        {
            if (isDead || amount <= 0) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            UpdateHealthUI();
        }

        /// <summary>Được gọi bởi thẻ tấn công để gây sát thương lên thẻ này.</summary>
        public void TakeDamage(int amount, CardBattle attacker = null)
        {
            if (isDead || cardData == null) return;
            GetComponent<TrinketHandler>()?.OnCardTakeDamage(attacker);
            if (amount <= 0) return;

            if (hasSecondPulse)
            {
                if (!secondPulseTriggeredThisTurn)
                {
                    amount = 1;
                    secondPulseTriggeredThisTurn = true;
                    Debug.Log($"[SecondPulse] {cardData.cardName} kích hoạt Second Pulse! Nhận đúng 1 sát thương đòn này, miễn nhiễm các đòn đánh kế tiếp trong turn.");
                }
                else
                {
                    amount = 0;
                    Debug.Log($"[SecondPulse] {cardData.cardName} được miễn nhiễm đòn đánh nhờ Second Pulse!");
                }
            }

            if (amount <= 0) return;

            // Thống kê: Bất kỳ sát thương nào gây lên địch đều tính là sát thương do người chơi gây ra (chém, phép, độc...)
            if (!this.isPlayerCard && ProjectM.GameManager.Instance?.RunData != null)
            {
                ProjectM.GameManager.Instance.RunData.totalDamageDealt += amount;
            }

            int remainingDamage = amount;

            // 1. Cơ chế Khiên (Shield): Hấp thụ sát thương trước khi mất máu
            if (currentShield > 0)
            {
                int absorbed = Mathf.Min(currentShield, remainingDamage);
                currentShield -= absorbed;
                remainingDamage -= absorbed;
                Debug.Log($"[Shield] 🛡️ {cardData.cardName} dùng khiên chặn {absorbed} sát thương (khiên còn: {currentShield})");
            }

            if (remainingDamage > 0)
            {
                currentHP -= remainingDamage;
                currentHP  = Mathf.Max(0, currentHP);
            }
            UpdateHealthUI();

            // 2. Cơ chế Phản Dame (Thorns): Phản lại 50% sát thương khi bị đánh trúng
            if (hasThorns && attacker != null && !attacker.IsDead && attacker != this)
            {
                int reflected = Mathf.RoundToInt(amount * 0.5f);
                if (reflected > 0)
                {
                    Debug.Log($"[Thorns] 🌵 {cardData.cardName} phản lại {reflected} (50%) sát thương lên {attacker.Data?.cardName}!");
                    attacker.TakeDamage(reflected);
                }
            }

            // 3. Cơ chế Thorn Heart: Phản lại sát thương (mặc định 100%, có thể override)
            if (currentHeartType == HeartType.Thorn && attacker != null && !attacker.IsDead && attacker != this)
            {
                int multiplier = (cardData != null && cardData.customAbility != null) ? cardData.customAbility.GetThornMultiplier() : 1;
                int reflected = amount * multiplier;
                if (reflected > 0)
                {
                    Debug.Log($"[Thorn Heart] 🥀 {cardData.cardName} phản lại {reflected} ({multiplier*100}%) sát thương lên {attacker.Data?.cardName}!");
                    attacker.TakeDamage(reflected);
                }
            }

            if (currentHP <= 0)
            {
                if (cardData != null && cardData.customAbility != null)
                {
                    cardData.customAbility.OnCardDestroyed(this, attacker);
                }

                // 4. Cơ chế Doom Heart: Phát nổ gây x2 sát thương (MaxHP) khi chết
                if (currentHeartType == HeartType.Doom && attacker != null && attacker != this)
                {
                    int doomDamage = (maxHP > 0 ? maxHP : cardData.health) * 2;
                    Debug.Log($"[Doom Heart] 💀 {cardData.cardName} phát nổ, gây {doomDamage} sát thương lên {attacker.Data?.cardName}!");
                    attacker.TakeDamage(doomDamage);
                }
                
                StartCoroutine(DieSequence());
            }
            else
            {
                // Animation bị đánh: lùi ra sau (ngược chiều tấn công của kẻ địch)
                if (battleAnim != null)
                {
                    Vector2 recoilDir = isPlayerCard ? Vector2.left : Vector2.right;
                    StartCoroutine(battleAnim.PlayHitAnim(recoilDir));
                }
            }
        }

        /// <summary>
        /// Hồi máu cho đơn vị này (gọi bởi SkillExecutor khi dùng skill HealSelf).
        /// Không vượt quá HP tối đa.
        /// </summary>
        public void HealHP(int amount)
        {
            if (isDead || amount <= 0) return;

            // Fragile Heart và Doom Heart không thể được hồi máu dưới mọi hình thức
            if (currentHeartType == HeartType.Fragile || currentHeartType == HeartType.Doom)
            {
                Debug.Log($"[Heal Blocked] 🚫 {cardData?.cardName} mang tim {currentHeartType} nên không thể được hồi máu!");
                return;
            }

            currentHP = Mathf.Min(currentHP + amount, maxHP);
            UpdateHealthUI();
            Debug.Log($"[CardBattle] {cardData.cardName} hồi +{amount} HP → {currentHP}/{maxHP}");
        }

        /// <summary>
        /// Thêm bonus damage cho đòn thường TIẾP THEO (gọi bởi SkillExecutor khi dùng skill AttackBuff).
        /// Bonus sẽ bị tiêu thụ hoàn toàn sau 1 đòn đánh.
        /// </summary>
        public void AddAttackBonus(int bonus)
        {
            if (bonus <= 0) return;
            _pendingAttackBonus += bonus;
            Debug.Log($"[CardBattle] {cardData.cardName} nhận +{bonus} attack bonus (tổng: {_pendingAttackBonus})");
        }

        /// <summary>Thêm giáp khiên cho đơn vị.</summary>
        public void AddShield(int amount)
        {
            if (isDead || amount <= 0) return;
            currentShield += amount;
            UpdateHealthUI();
            Debug.Log($"[Shield] 🛡️ {cardData?.cardName} nhận +{amount} khiên (tổng: {currentShield})");
        }

        /// <summary>Kích hoạt phản dame 50% khi bị đánh.</summary>
        public void EnableThorns()
        {
            if (isDead) return;
            hasThorns = true;
            Debug.Log($"[Thorns] 🌵 {cardData?.cardName} đã kích hoạt phản dame 50%!");
        }

        /// <summary>Kích hoạt buff Second Pulse.</summary>
        public void AddSecondPulse()
        {
            if (isDead) return;
            hasSecondPulse = true;
            secondPulseTriggeredThisTurn = false;
            UpdateHealthUI();
            Debug.Log($"[SecondPulse] {cardData?.cardName} đã nhận buff Second Pulse!");
        }

        /// <summary>
        /// Giảm speed đếm ngược hiện tại của mục tiêu trong chu kỳ lượt này.
        /// Khi speed chạm 0 và ra đòn xong, speed sẽ tự động quay về chỉ số gốc (cardData.speed).
        /// </summary>
        public void ReduceCurrentSpeed(int amount)
        {
            if (isDead || amount <= 0) return;
            currentCounter -= amount;
            currentCounter = Mathf.Max(0, currentCounter);
            UpdateCounterUI();
            Debug.Log($"[Speed Buff] ⚡ {cardData?.cardName} giảm {amount} speed đếm ngược -> còn {currentCounter}");

            // Kích hoạt tấn công ngay lập tức nếu speed về 0
            // Điều kiện: thẻ đã được đặt lên sàn (có parent là CardDropZone)
            bool isPlacedOnField = GetComponentInParent<Cards.CardDropZone>() != null;
            if (currentCounter == 0 && isPlacedOnField &&
                Managers.BattleManager.Instance != null)
            {
                Debug.Log($"[CardBattle] ⚔️ {cardData?.cardName} được giảm speed về 0, LẬP TỨC TẤN CÔNG!");
                // Reset counter NGAY LẬP TỨC trước khi animation chạy để tránh race condition:
                // nếu người chơi bấm chuông trong lúc animation đang chạy, ProcessTurn sẽ thấy
                // counter đã được reset (không còn = 0) và không kích hoạt thêm đòn thứ 2.
                currentCounter = cardData.speed;
                UpdateCounterUI();
                StartCoroutine(AttackSequence());
            }
        }

        /// <summary>Tăng sát thương đòn thường vĩnh viễn trong trận chiến.</summary>
        public void AddPermanentAttack(int amount)
        {
            if (isDead || amount <= 0) return;
            permanentAttackBonus += amount;
            UpdateAttackUI();
            Debug.Log($"[CardBattle] {cardData?.cardName} nhận +{amount} ATK vĩnh viễn (tổng bonus vĩnh viễn: {permanentAttackBonus})");
        }

        // ── Target Highlight (dùng khi skill đang chọn mục tiêu) ──

        /// <summary>Bật icon mục tiêu trên thẻ này. Gọi bởi SkillDragHandler.</summary>
        public void ShowTargetHighlight() => _targetHighlight?.Show();

        /// <summary>Tắt icon mục tiêu trên thẻ này. Gọi bởi SkillDragHandler.</summary>
        public void HideTargetHighlight() => _targetHighlight?.Hide();

        // Chết

        private IEnumerator DieSequence()
        {
            if (isDead) yield break;
            isDead = true;

            string name = cardData?.cardName ?? "Unknown";
            Debug.Log($"[CardBattle] {name} đã chết!");

            // Thông báo cho WaveManager nếu đây là thẻ địch
            if (!isPlayerCard)
                Managers.EnemyWaveManager.Instance?.OnEnemyDied(this);

            // Animation bị đánh (giật lùi) 1 lần cuối trước khi tan biến
            if (battleAnim != null)
            {
                Vector2 recoilDir = isPlayerCard ? Vector2.left : Vector2.right;
                yield return StartCoroutine(battleAnim.PlayHitAnim(recoilDir));
                
                // Animation chết (tan biến)
                yield return StartCoroutine(battleAnim.PlayDeathAnim());
            }

            // Báo cho BattleGrid để dồn hàng
            Managers.BattleGrid grid = FindAnyObjectByType<Managers.BattleGrid>();
            if (grid != null) grid.OnCardDied(this);

            // ── Kiểm tra điều kiện thua (toàn bộ thẻ người chơi chết) ──
            if (isPlayerCard && grid != null)
            {
                // grid.GetAllPlayerCards() sẽ lọc bỏ những thẻ isDead = true
                var remainingPlayers = grid.GetAllPlayerCards();
                if (remainingPlayers.Count == 0)
                {
                    Debug.Log("[CardBattle] 💀 Toàn bộ tướng của người chơi đã chết! Bại trận.");
                    var resultPanel = FindAnyObjectByType<ProjectM.UI.CombatResultPanel>();
                    if (resultPanel != null)
                    {
                        resultPanel.ShowLoss();
                    }
                    else
                    {
                        // Fallback
                        ProjectM.GameManager.Instance?.DeleteSave();
                        ProjectM.GameManager.Instance?.LoadMenuScene();
                    }
                }
            }

            // Dọn dẹp tất cả các hiệu ứng DOTween đang chạy dở trên object này trước khi xóa
            transform.DOKill();
            Destroy(gameObject);
        }


        // Ult (Kỹ năng đặc biệt - Người chơi kích hoạt tay)

        /// <summary>
        /// Người chơi bấm nút Ult của thẻ này để kích hoạt kỹ năng.
        /// Trả về true nếu kích hoạt thành công, false nếu chưa đủ điểm ult.
        /// </summary>
        public bool TryUseUlt()
        {
            if (isDead) return false;
            if (currentUlt < cardData.ult)
            {
                Debug.Log($"[CardBattle] {cardData.cardName} chưa đủ Ult ({currentUlt}/{cardData.ult})");
                return false;
            }

            Debug.Log($"[CardBattle] {cardData.cardName} kích hoạt Ult!");
            currentUlt = 0;
            UpdateUltUI();

            return true;
        }

       
        // Các hàm nội bộ
     
        private void ResetCounter()
        {
            currentCounter = cardData.speed;
            UpdateCounterUI();
        }

        private void RefreshAllUI()
        {
            UpdateHealthUI();
            UpdateCounterUI();
            UpdateUltUI();
            UpdateAttackUI();
        }

        private DG.Tweening.Tween _heartPulseTween;

        private void UpdateHealthUI()
        {
            if (display == null) return;

            bool hasShield = currentShield > 0;

            // ── Số HP ──────────────────────────────────────────
            if (display.healthText != null)
            {
                display.healthText.text = currentHP.ToString();
            }

            // ── Shield Icon riêng biệt ─────────────────────────────────────
            if (display.shieldIconObj != null)
            {
                display.shieldIconObj.SetActive(hasShield);
            }
            if (display.shieldText != null && hasShield)
            {
                display.shieldText.text = currentShield.ToString();
            }

            // ── Swap icon trái tim ─────────────────────────────────────
            if (display.heartIconImage != null)
            {
                if (hasSecondPulse && display.shieldHeartSprite != null)
                {
                    display.heartIconImage.sprite = display.shieldHeartSprite;
                    display.heartIconImage.color = Color.white;
                    StopHeartPulse();
                }
                else 
                {
                    display.heartIconImage.color = Color.white;
                    
                    if (currentHeartType == HeartType.Fragile && display.fragileHeartSprite != null)
                    {
                        display.heartIconImage.sprite = display.fragileHeartSprite;
                    }
                    else if (currentHeartType == HeartType.Doom && display.doomHeartSprite != null)
                    {
                        display.heartIconImage.sprite = display.doomHeartSprite;
                    }
                    else if (currentHeartType == HeartType.Thorn && display.thornHeartSprite != null)
                    {
                        display.heartIconImage.sprite = display.thornHeartSprite;
                    }
                    else
                    {
                        int maxHealthBase = maxHP > 0 ? maxHP : cardData.health;
                        float hpPercent = maxHealthBase > 0 ? (float)currentHP / maxHealthBase : 0f;

                        if (hpPercent <= 0.34f && display.heart3Sprite != null)
                        {
                            display.heartIconImage.sprite = display.heart3Sprite;
                            StartHeartPulse();
                        }
                        else if (hpPercent <= 0.67f && display.heart2Sprite != null)
                        {
                            display.heartIconImage.sprite = display.heart2Sprite;
                            StopHeartPulse();
                        }
                        else if (display.normalHeartSprite != null)
                        {
                            display.heartIconImage.sprite = display.normalHeartSprite;
                            StopHeartPulse();
                        }
                        else
                        {
                            StopHeartPulse();
                        }
                    }
                }
            }
        }

        private void StartHeartPulse()
        {
            if (_heartPulseTween != null && _heartPulseTween.IsActive() && _heartPulseTween.IsPlaying()) 
                return; // Đã chạy rồi
            
            if (display?.heartIconImage == null) return;

            // Reset scale trước khi anim
            display.heartIconImage.transform.localScale = Vector3.one;
            _heartPulseTween = display.heartIconImage.transform.DOScale(1.2f, 0.4f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        private void StopHeartPulse()
        {
            if (_heartPulseTween != null)
            {
                _heartPulseTween.Kill();
                _heartPulseTween = null;
            }
            if (display?.heartIconImage != null)
            {
                display.heartIconImage.transform.localScale = Vector3.one;
            }
        }

        private DG.Tweening.Tween _speedPulseTween;

        private void UpdateCounterUI()
        {
            if (display != null && display.speedText != null)
            {
                display.speedText.text = currentCounter.ToString();
                
                Transform counterContainer = display.speedText.transform.parent;
                if (counterContainer == null) counterContainer = display.speedText.transform;

                if (currentCounter == 1)
                {
                    // Chỉ bật nhịp đập nếu chưa chạy
                    if (_speedPulseTween == null || !_speedPulseTween.IsActive())
                    {
                        _speedPulseTween = counterContainer.DOScale(Vector3.one * 1.15f, 0.35f)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine)
                            .SetLink(gameObject); // Báo cho DOTween tự hủy anim khi thẻ bài bị Destroy
                    }
                }
                else
                {
                    if (_speedPulseTween != null)
                    {
                        _speedPulseTween.Kill();
                        _speedPulseTween = null;
                        counterContainer.localScale = Vector3.one;
                    }
                }
            }
        }

        private void UpdateUltUI()
        {
            if (display != null && display.ultText != null)
                display.ultText.text = $"{currentUlt}/{cardData.ult}";
        }

        private void UpdateAttackUI()
        {
            if (display != null && display.attackText != null)
            {
                int baseAtk = cardData != null ? cardData.attack : 0;
                display.attackText.text = (baseAtk + permanentAttackBonus).ToString();
            }
        }
    }
}
