using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ProjectM.Cards;
using ProjectM.Managers;
using ProjectM.UI;

namespace ProjectM.Skills
{
    /// <summary>
    /// Skill targeting — Click-to-Mode (không drag thẻ).
    ///
    /// LUỒNG:
    ///   1. Hover vào thẻ → focus (lên top layer)
    ///   2. Click vào thẻ đó → vào Targeting Mode (spawn arrow, theo chuột)
    ///   3. Di chuột → mũi tên bezier theo cursor; card hợp lệ sáng icon
    ///   4. Click vào mục tiêu hợp lệ → kích hoạt skill
    ///   5. Click vào vùng trống / thẻ không hợp lệ → cancel
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(SkillExecutor))]
    public class SkillDragHandler : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ── Inspector ─────────────────────────────────────────────────
        [Header("Targeting Arrow")]
        [SerializeField] private GameObject targetingArrowPrefab;

        // ── References ────────────────────────────────────────────────
        private SkillExecutor  _executor;
        private CanvasGroup    _canvasGroup;
        private Canvas         _mainCanvas;
        private RectTransform  _rectTransform;

        // ── Targeting runtime ─────────────────────────────────────────
        private SkillTargetingArrow _arrow;
        private CardBattle          _hoveredTarget;
        private List<CardBattle>    _preHighlighted  = new();

        // ── State ─────────────────────────────────────────────────────
        public static bool isAnySkillFocused   = false;
        public static bool isAnySkillTargeting = false;
        private bool _isTargeting   = false;
        private bool _isActivating  = false;
        private bool _isHoverFocused = false; // đánh dấu thẻ này có đang giữ focus lock hay không
        private int  _siblingIndex;
        private bool _suppressNextClick = false; // chặn re-enter sau khi cancel
        private float _targetingEnterTime = -1f;  // thời điểm vào targeting mode
        [SerializeField] private float _targetingClickCooldown = 0.25f; // (giây) thời gian tối thiểu trước khi chấp nhận click mới

        // ════════════════════════════════════════════════════════════════
        private void Awake()
        {
            _executor      = GetComponentInChildren<SkillExecutor>(includeInactive: true);
            _canvasGroup   = GetComponent<CanvasGroup>();
            _mainCanvas    = GetComponentInParent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();

            foreach (var cdh in GetComponentsInChildren<CardDragHandler>(includeInactive: true))
                cdh.enabled = false;
        }

        // ════════════════════════════════════════════════════════════════
        // UPDATE — cập nhật mũi tên khi đang targeting
        // ════════════════════════════════════════════════════════════════
        private void Update()
        {
            if (!_isTargeting || _isActivating || _executor?.Data == null) return;

            var targetType = _executor.Data.targetType;

            // Vị trí đỉnh thẻ (start) và cursor (end)
            Vector2 startScreen = GetCardTopScreenPos();
            Vector2 mouseScreen  = Mouse.current.position.ReadValue();

            // Card dưới chuột
            CardBattle cardUnder = GetCardUnderScreenPos(mouseScreen);
            bool isValid = cardUnder != null && IsValidTarget(cardUnder, targetType);

            // Cập nhật arrow — AllEnemies chỉ show target icon khi cursor trên thẻ địch
            bool overEnemyForArrow = targetType == SkillTargetType.AllEnemies && cardUnder != null && !cardUnder.IsPlayerCard;
            bool showTargetIcon = isValid || overEnemyForArrow;
            _arrow?.SetPositions(startScreen, mouseScreen, showTargetIcon);

            // Cập nhật hover highlight (trừ AllEnemies đã pre-highlight)
            if (targetType != SkillTargetType.AllEnemies)
            {
                if (isValid)
                {
                    if (_hoveredTarget != cardUnder)
                    {
                        ClearHoverHighlight(targetType);
                        _hoveredTarget = cardUnder;
                        ApplyHoverHighlight(cardUnder, targetType);
                    }
                }
                else if (_hoveredTarget != null)
                {
                    ClearHoverHighlight(targetType);
                    _hoveredTarget = null;
                }
            }

            // ── Detect click ──
            // Dùng time-based cooldown thay vì mouse-release guard.
            // Bất kể người chơi click nhanh hay chậm, phải chờ ít nhất _targetingClickCooldown
            // giây sau khi vào targeting mode mới chấp nhận click chọn mục tiêu.
            if (Time.realtimeSinceStartup - _targetingEnterTime < _targetingClickCooldown) return;

            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            // Guard quan trọng: nếu click ngay trên thẻ skill đang targeting → luôn cancel.
            // Không có guard này, RaycastAll sẽ xuyên qua thẻ skill và tìm thấy thẻ tướng
            // nằm phía sau, khiến skill bị kích hoạt nhầm mà không có mục tiêu nào được chọn.
            Camera uiCam = _mainCanvas != null && _mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                           ? _mainCanvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mouseScreen, uiCam))
            {
                _suppressNextClick = true;
                ExitTargetingMode();
                return;
            }

            if (targetType == SkillTargetType.AllEnemies)
            {
                // AllEnemies: chỉ kích hoạt khi cursor đang hover trên ít nhất 1 thẻ địch
                bool overEnemy = cardUnder != null && !cardUnder.IsPlayerCard;
                if (overEnemy)
                {
                    ExitTargetingMode();
                    StartCoroutine(ActivateSkill(null));
                }
                else
                {
                    // Click vào vùng trống → cancel
                    _suppressNextClick = true;
                    ExitTargetingMode();
                }
                return;
            }

            if (isValid && cardUnder != null)
            {
                // Click vào mục tiêu hợp lệ → kích hoạt
                var target = cardUnder;
                ExitTargetingMode();
                StartCoroutine(ActivateSkill(target));
            }
            else
            {
                // Click vào vùng trống / không hợp lệ → cancel
                _suppressNextClick = true;
                ExitTargetingMode();
            }
        }

        // ════════════════════════════════════════════════════════════════
        // HOVER
        // ════════════════════════════════════════════════════════════════
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isActivating || _isTargeting) return;
            // Nếu có thẻ khác đang focus hoặc đang targeting → block hoàn toàn
            if (isAnySkillFocused || isAnySkillTargeting) return; 

            isAnySkillFocused = true;
            _isHoverFocused   = true;
            _siblingIndex     = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isTargeting) return; // đang targeting → giữ focus
            if (!_isHoverFocused) return; // thẻ này không giữ focus → không làm gì cả

            transform.SetSiblingIndex(_siblingIndex);
            isAnySkillFocused = false;
            _isHoverFocused   = false;
        }

        // ════════════════════════════════════════════════════════════════
        // CLICK
        // ════════════════════════════════════════════════════════════════
        public void OnPointerClick(PointerEventData eventData)
        {
            // Chỉ nhận chuột trái
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // Nếu một thẻ KHÁC đang targeting thì bỏ qua click này để tránh hiện 2 mũi tên
            if (isAnySkillTargeting && !_isTargeting) return;

            // Chặn click ngay sau khi vừa cancel targeting
            if (_suppressNextClick) { _suppressNextClick = false; return; }
            if (_isActivating) return;

            // Đang targeting → click vào thẻ chính là cancel
            if (_isTargeting) { ExitTargetingMode(); return; }

            if (_executor?.Data == null) return;

            var t = _executor.Data.targetType;
            if (t == SkillTargetType.Self)
            {
                StartCoroutine(ActivateSkill(null));
                return;
            }

            // Vào targeting mode
            EnterTargetingMode();
        }

        // ════════════════════════════════════════════════════════════════
        // TARGETING MODE ENTER / EXIT
        // ════════════════════════════════════════════════════════════════
        private void EnterTargetingMode()
        {
            if (_isTargeting || _executor?.Data == null) return;
            _isTargeting        = true;
            isAnySkillTargeting = true;
            _targetingEnterTime = Time.realtimeSinceStartup; // Bắt đầu đếm cooldown

            // Spawn arrow
            if (targetingArrowPrefab != null)
            {
                var root = GetRootCanvas();
                Transform parent = root != null ? root.transform : transform.root;
                var go = Instantiate(targetingArrowPrefab, parent);
                go.transform.SetAsLastSibling();
                _arrow = go.GetComponent<SkillTargetingArrow>();

                if (_arrow == null)
                    Debug.LogWarning("[SkillDragHandler] SkillTargetingArrow không tìm thấy trên prefab!");
            }
            else
            {
                Debug.LogWarning("[SkillDragHandler] targetingArrowPrefab chưa gán trong Inspector!");
            }

            // Pre-highlight AllEnemies ngay khi vào mode
            _preHighlighted.Clear();
            if (_executor.Data.targetType == SkillTargetType.AllEnemies)
            {
                var grid = BattleGrid.Instance;
                if (grid != null)
                {
                    _preHighlighted = grid.GetAllEnemyCards();
                    foreach (var c in _preHighlighted) c.ShowTargetHighlight();
                }
            }

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardPickUpClip);
        }

        private void ExitTargetingMode()
        {
            _isTargeting        = false;
            isAnySkillTargeting = false;
            isAnySkillFocused   = false;
            _isHoverFocused     = false;

            // Destroy arrow
            if (_arrow != null) { Destroy(_arrow.gameObject); _arrow = null; }

            // Tắt tất cả highlight
            foreach (var c in _preHighlighted) c.HideTargetHighlight();
            _preHighlighted.Clear();
            if (_hoveredTarget != null && _executor?.Data != null)
                ClearHoverHighlight(_executor.Data.targetType);
            _hoveredTarget = null;

            // Trả thẻ về layer cũ
            transform.SetSiblingIndex(_siblingIndex);
            
            // Ép thẻ nguồn thu nhỏ lại
            GetComponent<ProjectM.Cards.CardHoverHandler>()?.ForceStopHover();
        }

        // ════════════════════════════════════════════════════════════════
        // ACTIVATE
        // ════════════════════════════════════════════════════════════════
        private IEnumerator ActivateSkill(CardBattle draggedTarget)
        {
            if (_isActivating) yield break;
            _isActivating = true;

            var grid = BattleGrid.Instance;
            if (grid == null) { _isActivating = false; yield break; }

            var playerCards = grid.GetAllPlayerCards();
            CardBattle caster = playerCards.Count > 0 ? playerCards[0] : null;

            List<CardBattle> allEnemies = grid.GetAllEnemyCards();
            List<CardBattle> targets    = _executor.ResolveTargets(draggedTarget, caster, allEnemies);

            if (targets.Count == 0)
            {
                BattleDebugger.Warn($"[Skill] '{_executor.Data?.skillName}': Không tìm thấy mục tiêu!");
                _isActivating = false;
                yield break;
            }

            var names = string.Join(", ", targets.ConvertAll(t => t.Data?.cardName ?? "?"));
            BattleDebugger.Log($"✨ '{_executor.Data?.skillName}' → [{names}]");

            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(_executor.Execute(caster, targets));

            BattleDebugger.Log($"✅ '{_executor.Data?.skillName}' dùng xong.");
            SkillHandManager.Instance?.OnSkillUsed(this);
            BattleManager.Instance?.EndTurn(drawCard: false);
        }

        // ════════════════════════════════════════════════════════════════
        // HIGHLIGHT HELPERS
        // ════════════════════════════════════════════════════════════════
        private void ApplyHoverHighlight(CardBattle card, SkillTargetType targetType)
        {
            if (targetType == SkillTargetType.EnemyRow)
            {
                var row = BattleGrid.Instance?.GetEnemiesInSameRow(card);
                if (row != null) foreach (var c in row) c.ShowTargetHighlight();
                else card.ShowTargetHighlight();
            }
            else card.ShowTargetHighlight();
        }

        private void ClearHoverHighlight(SkillTargetType targetType)
        {
            if (_hoveredTarget == null) return;
            if (targetType == SkillTargetType.EnemyRow)
            {
                var row = BattleGrid.Instance?.GetEnemiesInSameRow(_hoveredTarget);
                if (row != null) foreach (var c in row) c.HideTargetHighlight();
                else _hoveredTarget.HideTargetHighlight();
            }
            else _hoveredTarget.HideTargetHighlight();
        }

        private static bool IsValidTarget(CardBattle card, SkillTargetType targetType) => targetType switch
        {
            SkillTargetType.SingleAlly  =>  card.IsPlayerCard,
            SkillTargetType.SingleEnemy => !card.IsPlayerCard,
            SkillTargetType.EnemyRow    => !card.IsPlayerCard,
            _                           => false,
        };

        // ════════════════════════════════════════════════════════════════
        // POSITION HELPERS
        // ════════════════════════════════════════════════════════════════
        private Vector2 GetCardTopScreenPos()
        {
            if (_rectTransform == null) return Vector2.zero;
            var corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);
            // corners: [0]=bottomLeft [1]=topLeft [2]=topRight [3]=bottomRight
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            return RectTransformUtility.WorldToScreenPoint(_mainCanvas?.worldCamera, topCenter);
        }

        private CardBattle GetCardUnderScreenPos(Vector2 screenPos)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results     = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (var r in results)
            {
                var card = r.gameObject.GetComponentInParent<CardBattle>();
                if (card != null) return card;
            }
            return null;
        }

        private Canvas GetRootCanvas()
        {
            var canvases = GetComponentsInParent<Canvas>(true);
            if (canvases == null || canvases.Length == 0) return null;
            foreach (var c in canvases)
                if (c.isRootCanvas) return c;
            return canvases[canvases.Length - 1];
        }
    }
}
