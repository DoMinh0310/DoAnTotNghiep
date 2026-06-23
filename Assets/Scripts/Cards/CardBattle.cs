using UnityEngine;
using System.Collections;
using ProjectM.Elements;

namespace ProjectM.Cards
{
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
        public CardData Data      => cardData;

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
        public void Initialize(CardData data, bool playerCard)
        {
            cardData      = data;
            isPlayerCard  = playerCard;
            isDead        = false;

            currentHP      = data.health;
            currentCounter = data.speed;
            currentUlt     = 0;

            RefreshAllUI();
            Debug.Log($"[CardBattle] {data.cardName} khởi tạo xong. HP={currentHP}, Speed={currentCounter}");
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

            // 2. GIẢM SPEED — bỏ qua nếu đang bị đóng băng
            bool frozen = elemental != null && elemental.IsSpeedFrozen;
            if (frozen)
            {
                Debug.Log($"[CardBattle] 🧊 {cardData?.cardName} bị đóng băng — Speed giữ nguyên ở {currentCounter}");
            }
            else
            {
                yield return StartCoroutine(AnimateCounterTick());
            }

            // 3. TẤN CÔNG (Nếu đủ speed VÀ không bị đóng băng)
            if (!frozen && currentCounter <= 0)
            {
                yield return StartCoroutine(AttackSequence());
            }

            if (isDead) yield break;

            // 4. ELEMENTAL (Venom, Scorch...): Kích hoạt SAU KHI xử lý lượt xong
            // Đợi một khoảng delay để tách bạch với hit flash thường (tránh flash chồng chập)
            if (elemental != null)
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
                currentCounter--;
                UpdateCounterUI();
                yield break;
            }

            // Scale cả container cha (chứa icon đồng hồ + text) chứ không phải chỉ text
            Transform counterContainer = display.speedText.transform.parent;
            if (counterContainer == null) counterContainer = display.speedText.transform;

            RectTransform containerRect = counterContainer.GetComponent<RectTransform>();
            Vector3 originalScale = containerRect != null ? containerRect.localScale : Vector3.one;

            // Lấy tốc độ tổng thể dựa trên số lượng tướng trên sàn
            float speedMult = Managers.BattleManager.GlobalAnimationSpeed;
            if (speedMult < 1f) speedMult = 1f;

            // Phase 1: Phóng to (0.1s) - giữ màu gốc
            float duration = 0.1f / speedMult;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (containerRect != null)
                    containerRect.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, elapsed / duration);
                yield return null;
            }

            // Cập nhật số tại đỉnh animation
            currentCounter--;
            UpdateCounterUI();

            // Phase 2: Thu nhỏ về bình thường (0.15s) - giữ màu gốc
            duration = 0.15f / speedMult;
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (containerRect != null)
                    containerRect.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, elapsed / duration);
                yield return null;
            }

            if (containerRect != null) containerRect.localScale = originalScale;
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
            int totalDamage = cardData.attack + _pendingAttackBonus;
            _pendingAttackBonus = 0;

            // Truyền TakeDamage vào onImpact — sẽ được gọi đúng lúc lunge chạm đích
            // PlayHitAnim của target bắt đầu tức thì, song song với Phase 3 (return) của attacker
            yield return StartCoroutine(battleAnim.PlayAttackAnim(attackDirection, onImpact: () =>
            {
                if (!target.IsDead)
                {
                    if (totalDamage != cardData.attack)
                        Debug.Log($"[CardBattle] {cardData.cardName} đánh tăng cường! {cardData.attack} + {totalDamage - cardData.attack} bonus = {totalDamage}");
                    target.TakeDamage(totalDamage);
                }
            }));

            // Tăng ult sau khi đánh
            currentUlt = Mathf.Min(currentUlt + 1, cardData.ult);
            UpdateUltUI();

            ResetCounter();
        }

     
        // Nhận sát thương & Hồi máu
      
        /// <summary>Được gọi bởi thẻ tấn công để gây sát thương lên thẻ này.</summary>
        public void TakeDamage(int amount)
        {
            if (isDead || cardData == null) return;

            currentHP -= amount;
            currentHP  = Mathf.Max(0, currentHP);
            UpdateHealthUI();

            if (currentHP <= 0)
            {
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
            currentHP = Mathf.Min(currentHP + amount, cardData.health);
            UpdateHealthUI();
            Debug.Log($"[CardBattle] {cardData.cardName} hồi +{amount} HP → {currentHP}/{cardData.health}");
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

            // Animation chết (chỉ chạy nếu battleAnim tồn tại)
            if (battleAnim != null)
                yield return StartCoroutine(battleAnim.PlayDeathAnim());

            // Báo cho BattleGrid để dồn hàng
            Managers.BattleGrid grid = FindAnyObjectByType<Managers.BattleGrid>();
            if (grid != null) grid.OnCardDied(this);

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
        }

        private void UpdateHealthUI()
        {
            if (display != null && display.healthText != null)
                display.healthText.text = currentHP.ToString();
        }

        private void UpdateCounterUI()
        {
            if (display != null && display.speedText != null)
                display.speedText.text = currentCounter.ToString();
        }

        private void UpdateUltUI()
        {
            if (display != null && display.ultText != null)
                display.ultText.text = $"{currentUlt}/{cardData.ult}";
        }
    }
}
