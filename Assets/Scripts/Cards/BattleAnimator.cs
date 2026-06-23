using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace ProjectM.Cards
{
    /// <summary>
    /// Xử lý toàn bộ animation chiến đấu bằng DOTween.
    /// Gắn vào Card Prefab cùng với CardBattle.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class BattleAnimator : MonoBehaviour
    {
        [Header("Attack Animation")]
        [Tooltip("Khoảng cách lùi về sau (windup, pixels)")]
        public float attackWindupDistance = 45f;
        [Tooltip("Thời gian lùi về sau (giây)")]
        public float attackWindupTime = 0.10f;
        [Tooltip("Khoảng cách lao về phía địch (pixels)")]
        public float attackLungeDistance = 130f;
        [Tooltip("Thời gian lao tới (giây)")]
        public float attackLungeTime = 0.14f;
        [Tooltip("Góc xoay trục Y khi tấn công (lật nhẹ phối cảnh, độ)")]
        public float attackYFlip = 22f;
        [Tooltip("Góc cúi về phía địch khi tấn công (độ)")]
        public float attackForwardTilt = 14f;
        [Tooltip("Thời gian quay về vị trí gốc (giây)")]
        public float attackReturnTime = 0.50f;
        [Tooltip("Biên độ rung lắc lò xo khi trở về (càng nhỏ rung càng nhẹ)")]
        public float attackReturnElasticAmplitude = 0.4f;
        [Tooltip("Chu kỳ rung lắc lò xo (càng lớn nhịp rung càng thưa)")]
        public float attackReturnElasticPeriod = 0.6f;

        [Header("Hit Animation")]
        [Tooltip("Khoảng cách lùi ra sau khi bị đánh (pixels)")]
        public float hitRecoilDistance = 30f;
        [Tooltip("Thời gian lùi + bật lại khi bị đánh (giây) — để rất nhỏ (~0.05) để gần tức thì")]
        public float hitRecoilDuration = 0.05f;
        [Tooltip("Thời gian flash đỏ mờ dần sau khi bị đánh (giây)")]
        public float hitFlashDuration = 0.3f;

        [Header("Death Animation (Dissolve)")]
        [Tooltip("Thời gian hiệu ứng tan biến (giây)")]
        public float dissolveDuration = 1.0f;
        [Tooltip("Độ mạnh của scale-punch lúc chết (ví dụ 1.18 = phóng to 18%)")]
        public float deathPunchScale = 1.18f;
        [Tooltip("Thời gian scale-punch (giây)")]
        public float deathPunchTime = 0.12f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Image hitOverlay;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup   = GetComponent<CanvasGroup>();
            CreateHitOverlay();
        }

        private void OnDestroy()
        {
            rectTransform?.DOKill();
            canvasGroup?.DOKill();
            hitOverlay?.DOKill();
        }

        private void CreateHitOverlay()
        {
            var overlayObj = new GameObject("HitOverlay");
            overlayObj.transform.SetParent(transform, false);
            hitOverlay = overlayObj.AddComponent<Image>();
            hitOverlay.color = new Color(1f, 0f, 0f, 0f);
            hitOverlay.raycastTarget = false;

            var overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayObj.transform.SetAsLastSibling();
        }

        // ─────────────────────────────────────────────────
        // ANIMATION 1: Tấn công — lao ra + lùi về
        // onImpact: callback gọi đúng lúc va chạm (đỉnh lunge) — dùng để deal damage tức thì
        // ─────────────────────────────────────────────────
        public IEnumerator PlayAttackAnim(Vector2 direction, System.Action onImpact = null)
        {
            Vector2 origin    = rectTransform.anchoredPosition;
            Vector2 windupPos = origin - direction * attackWindupDistance;
            Vector2 lungePos  = origin + direction * attackLungeDistance;

            // Y-flip: lật nhẹ theo hướng tấn công để tạo phối cảnh 3D
            float yRot  =  direction.x * attackYFlip;
            // Z-tilt: cúi về phía địch (đỉnh thẻ ngả về hướng tấn công)
            float zTilt = -direction.x * attackForwardTilt;

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.attackClip);

            // ── Phase 1: Wind-up — lùi nhẹ ra sau ──
            yield return rectTransform
                .DOAnchorPos(windupPos, attackWindupTime)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            // ── Phase 2: Lao ra tấn công — vị trí + xoay Y + cúi Z cùng lúc ──
            Sequence lungeSeq = DOTween.Sequence();
            lungeSeq.Join(
                rectTransform.DOAnchorPos(lungePos, attackLungeTime)
                    .SetEase(Ease.OutQuint)
            );
            lungeSeq.Join(
                rectTransform.DOLocalRotate(new Vector3(0f, yRot, zTilt), attackLungeTime)
                    .SetEase(Ease.OutQuad)
            );
            yield return lungeSeq.WaitForCompletion();

            // ★ Va chạm! Gọi onImpact ngay khi thẻ ở đỉnh lunge, trước khi quay về
            // TakeDamage → PlayHitAnim của target sẽ bắt đầu đúng lúc này (zero delay)
            onImpact?.Invoke();

            // ── Phase 3: Quay về dạng lò xo — dao động rồi ổn định ──
            // PlayHitAnim của target chạy song song trong lúc Phase 3 này diễn ra
            Sequence returnSeq = DOTween.Sequence();
            returnSeq.Join(
                rectTransform.DOAnchorPos(origin, attackReturnTime)
                    .SetEase(Ease.OutElastic, attackReturnElasticAmplitude, attackReturnElasticPeriod)
            );
            returnSeq.Join(
                rectTransform.DOLocalRotate(Vector3.zero, attackReturnTime * 0.4f)
                    .SetEase(Ease.OutBack)
            );
            yield return returnSeq.WaitForCompletion();

            // Đảm bảo reset hoàn toàn về gốc
            rectTransform.anchoredPosition = origin;
            rectTransform.localRotation    = Quaternion.identity;
        }

        // ─────────────────────────────────────────────────
        // ANIMATION 2: Bị đánh — flash đỏ + lùi nhẹ
        // ─────────────────────────────────────────────────
        public IEnumerator PlayHitAnim(Vector2 recoilDirection)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.hitClip);

            Vector2 origin = rectTransform.anchoredPosition;
            Vector2 recoil = origin + recoilDirection * hitRecoilDistance;

            // Flash đỏ xuất hiện cùng lúc với recoil
            hitOverlay.color = new Color(1f, 0f, 0f, 0.65f);

            // Recoil: lùi + bật lại (hitRecoilDuration rất ngắn — gần tức thì)
            Sequence recoilSeq = DOTween.Sequence();
            recoilSeq.Append(rectTransform.DOAnchorPos(recoil, hitRecoilDuration).SetEase(Ease.OutQuad));
            recoilSeq.Append(rectTransform.DOAnchorPos(origin, hitRecoilDuration).SetEase(Ease.InQuad));
            yield return recoilSeq.WaitForCompletion();

            // Flash mờ dần (chạy sau recoil)
            yield return hitOverlay
                .DOFade(0f, hitFlashDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            hitOverlay.color = new Color(1f, 0f, 0f, 0f);
        }

        // ─────────────────────────────────────────────────
        // ANIMATION 3: Chết — scale punch nhỏ → dissolve tại chỗ
        // ─────────────────────────────────────────────────
        public IEnumerator PlayDeathAnim()
        {
            if (rectTransform == null) yield break;

            transform.SetAsLastSibling();

            // ── Bước 1: Scale punch (cảm giác "bị đòn chí mạng") ──
            Vector3 originScale = rectTransform.localScale;
            Vector3 punchScale  = originScale * deathPunchScale;

            yield return rectTransform
                .DOScale(punchScale, deathPunchTime)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            yield return rectTransform
                .DOScale(originScale, deathPunchTime)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();

            // ── Bước 2: Dissolve tất cả UIEffect có Transition bật ──
            var uiEffects    = GetComponentsInChildren<Coffee.UIEffects.UIEffect>(true);
            bool hasDissolve = false;

            Sequence dissolveSeq = DOTween.Sequence();

            foreach (var fx in uiEffects)
            {
                if (fx.transitionFilter != Coffee.UIEffects.TransitionFilter.None)
                {
                    hasDissolve = true;
                    fx.transitionRate = 0f;

                    dissolveSeq.Join(
                        DOTween.To(
                            () => fx.transitionRate,
                            x  => fx.transitionRate = x,
                            1f,
                            dissolveDuration
                        ).SetEase(Ease.InCubic)
                    );
                }
            }

            // Fallback nếu không có UIEffect → fade CanvasGroup bình thường
            if (!hasDissolve)
            {
                dissolveSeq.Join(
                    canvasGroup.DOFade(0f, dissolveDuration).SetEase(Ease.InQuad)
                );
            }
            else
            {
                // Fade CanvasGroup ở 30% cuối để tránh thẻ "pop" biến mất đột ngột
                dissolveSeq.Insert(
                    dissolveDuration * 0.7f,
                    canvasGroup.DOFade(0f, dissolveDuration * 0.3f).SetEase(Ease.InQuad)
                );
            }

            yield return dissolveSeq.WaitForCompletion();

            canvasGroup.alpha = 0f;

            // Reset về gốc để tái sử dụng object nếu cần
            rectTransform.localScale    = originScale;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}
