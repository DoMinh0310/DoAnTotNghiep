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
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Inspector ─────────────────────────────────────────────────
        [Header("Targeting Arrow")]
        [SerializeField] private GameObject targetingArrowPrefab;

        [Header("Skill Activation Animation")]
        [Tooltip("Độ co rút lại khi bắt đầu chuẩn bị (0.1 = nhỏ đi 10%)")]
        public float animShrinkAmount = 0.1f;
        [Tooltip("Khoảng cách lùi lại để lấy đà (px)")]
        public float animWindupDistance = 80f;
        [Tooltip("Khoảng cách lao lên tấn công (px)")]
        public float animLungeDistance = 100f;
        
        [Tooltip("Thời gian bẻ phẳng thẻ (giây)")]
        public float animStraightenTime = 0.15f;
        [Tooltip("Thời gian lùi lấy đà (giây)")]
        public float animWindupTime = 0.4f;
        [Tooltip("Thời gian lao lên (giây)")]
        public float animLungeTime = 0.3f;
        [Tooltip("Thời gian quay về vị trí ban đầu (giây)")]
        public float animReturnTime = 0.35f;

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

            // Vị trí tâm thẻ (start) và cursor (end)
            Vector2 startScreen = GetCardCenterScreenPos();
            Vector2 mouseScreen = Mouse.current.position.ReadValue();

            // Card dưới chuột
            CardBattle cardUnder = GetCardUnderScreenPos(mouseScreen);
            bool isValid = cardUnder != null && IsValidTarget(cardUnder, targetType);

            // Cập nhật arrow — AllEnemies chỉ show target icon khi cursor trên thẻ địch
            bool overEnemyForArrow = targetType == SkillTargetType.AllEnemies && cardUnder != null && !cardUnder.IsPlayerCard;
            bool showTargetIcon = isValid || overEnemyForArrow;

            // Khóa nam châm (Magnetic Snap): Nếu đang trên thẻ mục tiêu hợp lệ, khóa điểm cuối vào tâm thẻ đó
            Vector2 endScreen = (showTargetIcon && cardUnder != null) ? GetCardCenterScreenPos(cardUnder) : mouseScreen;
            _arrow?.SetPositions(startScreen, endScreen, showTargetIcon);

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

            // Chặn click nếu chuột đang nằm trong khu vực tay bài (Hand Region)
            if (IsPointerOverHandRegion(mouseScreen))
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
            if (Managers.BattleManager.IsInputBlocked) return;
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
            if (Managers.BattleManager.IsInputBlocked) return;

            // Nếu là thẻ Summon (EmptySlot), KHÔNG click-to-target mà dùng Drag-and-Drop
            if (_executor?.Data != null && _executor.Data.targetType == SkillTargetType.EmptySlot) return;

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
        // DRAG & DROP (Dành riêng cho thẻ Summon / EmptySlot)
        // ════════════════════════════════════════════════════════════════
        public static CardDropZone targetDropZoneForSummon; // Pass slot cho SkillOverride_Summon
        private Transform _previousParent;
        private bool _isDragTargeting;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_executor?.Data == null) return;
            if (Managers.BattleManager.IsInputBlocked) return;

            // Đảm bảo _mainCanvas không bị null (do Awake có thể chạy lúc vừa Instantiate chưa có Parent)
            if (_mainCanvas == null)
            {
                Transform curr = transform.parent;
                while (curr != null)
                {
                    _mainCanvas = curr.GetComponent<Canvas>();
                    if (_mainCanvas != null) break;
                    curr = curr.parent;
                }
            }

            // Nếu đang ở click-targeting mode (click mũi tên), tắt nó đi
            if (_isTargeting) ExitTargetingMode();

            if (_executor.Data.targetType == SkillTargetType.EmptySlot)
            {
                // Physical Drag (dành cho Summon)
                isAnySkillTargeting = true;
                _previousParent = transform.parent;
                _siblingIndex = transform.GetSiblingIndex();

                transform.DOKill();
                _rectTransform.DOKill();

                if (_mainCanvas != null) transform.SetParent(_mainCanvas.transform);
                else transform.SetParent(transform.root);

                transform.SetAsLastSibling();
                transform.localRotation = Quaternion.identity;

                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.alpha = 0.8f;
                
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardPickUpClip);
            }
            else
            {
                // Virtual Drag (Targeting Arrow)
                _isDragTargeting = true;
                EnterTargetingMode();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_executor?.Data == null) return;
            if (Managers.BattleManager.IsInputBlocked)
            {
                if (_isDragTargeting) { _isDragTargeting = false; ExitTargetingMode(); }
                else CancelDrag();
                return;
            }

            if (_executor.Data.targetType == SkillTargetType.EmptySlot)
            {
                transform.position = eventData.position;
            }
            // Ngược lại (Virtual Drag): không di chuyển thẻ, Update() sẽ tự vẽ mũi tên
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_executor?.Data == null) return;
            if (Managers.BattleManager.IsInputBlocked)
            {
                if (_isDragTargeting) { _isDragTargeting = false; ExitTargetingMode(); }
                else CancelDrag();
                return;
            }
            
            if (_executor.Data.targetType == SkillTargetType.EmptySlot)
            {
                // Kết thúc Physical Drag
                isAnySkillTargeting = false;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;

                AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardDropClip);

                PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                CardDropZone dropZone = null;
                foreach (var result in results)
                {
                    var zone = result.gameObject.GetComponentInParent<CardDropZone>();
                    if (zone != null && zone.transform.childCount == 0)
                    {
                        Managers.BattleGrid grid = Managers.BattleGrid.Instance;
                        // Bắt buộc phải thả vào ô của Player, không cho thả vào ô Enemy
                        if (grid != null && !grid.IsEnemyZone(zone) && grid.GetCardInSlot(zone) == null)
                        {
                            dropZone = zone;
                            break;
                        }
                    }
                }

                if (dropZone != null)
                {
                    targetDropZoneForSummon = dropZone;
                    StartCoroutine(ActivateSkill(null));
                }
                else CancelDrag();
            }
            else
            {
                // Kết thúc Virtual Drag
                if (!_isDragTargeting) return;
                _isDragTargeting = false;

                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                if (IsPointerOverHandRegion(mouseScreen))
                {
                    ExitTargetingMode();
                    return;
                }

                CardBattle targetCard = GetCardUnderScreenPos(mouseScreen);

                if (targetCard != null && IsValidTarget(targetCard, _executor.Data.targetType))
                {
                    ExitTargetingMode();
                    StartCoroutine(ActivateSkill(targetCard));
                }
                else
                {
                    ExitTargetingMode();
                }
            }
        }

        private void CancelDrag()
        {
            if (_previousParent != null)
            {
                transform.SetParent(_previousParent);
                transform.SetSiblingIndex(_siblingIndex);
            }
            isAnySkillTargeting = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            // Gọi SkillHandManager cập nhật lại vị trí thẻ (nếu thẻ vừa bị kéo vật lý)
            if (_executor != null && _executor.Data != null && _executor.Data.targetType == SkillTargetType.EmptySlot)
            {
                SkillHandManager.Instance?.RepositionAllCards();
            }
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

            // Nhích thẻ lên 10 pixel theo trục Y so với vị trí hiện tại (hover) để làm nổi bật
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                float currentY = _rectTransform.anchoredPosition.y;
                _rectTransform.DOAnchorPosY(currentY + 10f, 0.15f).SetEase(Ease.OutCubic);
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
            
            // Ép thẻ nguồn thu nhỏ lại và trả về đúng base position
            GetComponent<ProjectM.Cards.CardHoverHandler>()?.ForceStopHover();
        }

        // ════════════════════════════════════════════════════════════════
        // ACTIVATE
        // ════════════════════════════════════════════════════════════════
        private IEnumerator ActivateSkill(CardBattle draggedTarget)
        {
            if (_isActivating) yield break;
            _isActivating = true;
            
            // Khóa chuột ngay lập tức khi bắt đầu dùng thẻ
            Managers.BattleManager.IsInputLocked = true;

            var grid = BattleGrid.Instance;
            if (grid == null) { 
                _isActivating = false; 
                Managers.BattleManager.IsInputLocked = false; 
                yield break; 
            }

            var playerCards = grid.GetAllPlayerCards();
            CardBattle caster = playerCards.Count > 0 ? playerCards[0] : null;

            List<CardBattle> allEnemies = grid.GetAllEnemyCards();
            List<CardBattle> targets    = _executor.ResolveTargets(draggedTarget, caster, allEnemies);

            // Bỏ qua check target count nếu là loại thả vào ô trống (EmptySlot) vì loại này không target vào thẻ bài
            if (targets.Count == 0 && _executor.Data.targetType != SkillTargetType.EmptySlot)
            {
                BattleDebugger.Warn($"[Skill] '{_executor.Data?.skillName}': Không tìm thấy mục tiêu!");
                _isActivating = false;
                Managers.BattleManager.IsInputLocked = false;
                yield break;
            }

            var names = string.Join(", ", targets.ConvertAll(t => t.Data?.cardName ?? "?"));
            BattleDebugger.Log($"✨ '{_executor.Data?.skillName}' → [{names}]");

            // Tắt đường mũi tên (Bezier line) ngay lập tức
            if (_arrow != null) 
            { 
                Destroy(_arrow.gameObject); 
                _arrow = null; 
            }

            // Dừng mọi animation đang chạy trên thẻ (do HoverHandler hoặc HandLayout)
            _rectTransform.DOKill();
            transform.DOKill();

            // Ép thẻ nguồn thu nhỏ lại để huỷ hiệu ứng hover đang làm to lá bài
            var hover = GetComponent<ProjectM.Cards.CardHoverHandler>();
            if (hover != null) Destroy(hover);

            if (_executor.Data.targetType == SkillTargetType.EmptySlot)
            {
                // Ẩn lá bài đi ngay lập tức để tạo cảm giác nó "biến" thành mô hình công trình
                _canvasGroup.alpha = 0f;
            }

            if (_executor.Data.targetType != SkillTargetType.EmptySlot)
            {
                // 1. Tính toán hướng mục tiêu
                Vector2 direction = Vector2.up; 
                if (targets.Count > 0 && targets[0] != null)
                {
                    // Vector hướng từ thẻ skill bay tới vị trí mục tiêu
                    Vector3 targetPos = targets[0].transform.position;
                    Vector3 cardPos = transform.position;
                    direction = (targetPos - cardPos).normalized;
                }
                else if (_executor.Data.targetType == SkillTargetType.Self)
                {
                    direction = Vector2.down; // Buff bản thân thì hướng xuống dưới
                }

                // 2. Tách thẻ ra khỏi HandLayout để BỎ HOÀN TOÀN GÓC NGHIÊNG (panning vòng cung)
                if (_mainCanvas != null) transform.SetParent(_mainCanvas.transform);
                else transform.SetParent(transform.root);
                transform.SetAsLastSibling();
                
                // 3. Tính toán thông số
                Vector3 originalScale = _rectTransform.localScale;
                Vector3 shrunkScale = new Vector3(originalScale.x - animShrinkAmount, originalScale.y - animShrinkAmount, originalScale.z);
                Vector2 originPos = _rectTransform.anchoredPosition;
                Vector2 windupPos = originPos - direction * animWindupDistance;  
                Vector2 lungePos = originPos + direction * animLungeDistance;  

                AudioManager.Instance?.PlaySFX(AudioManager.Instance.attackClip);

                // TẠO CHUỖI ANIMATION 3 BƯỚC
                Sequence animSeq = DOTween.Sequence();
                
                // Bước 1: Thu nhỏ lại và bẻ phẳng thẻ về góc (0,0,0)
                animSeq.Append(_rectTransform.DOScale(shrunkScale, animStraightenTime).SetEase(Ease.OutQuad));
                animSeq.Join(_rectTransform.DORotate(Vector3.zero, animStraightenTime).SetEase(Ease.OutQuad));
                
                // Bước 2: Lùi lại lấy đà
                animSeq.Append(_rectTransform.DOAnchorPos(windupPos, animWindupTime).SetEase(Ease.OutCubic));

                // Bước 3: Lao thẳng lên
                animSeq.Append(_rectTransform.DOAnchorPos(lungePos, animLungeTime).SetEase(Ease.OutBack, overshoot: 1.5f));
                
                // Bước 4: Quay về vị trí cũ và nảy nảy (Elastic)
                animSeq.Append(_rectTransform.DOAnchorPos(originPos, animReturnTime).SetEase(Ease.OutElastic, amplitude: 0.8f, period: 0.5f));
                animSeq.Join(_rectTransform.DOScale(originalScale, animReturnTime).SetEase(Ease.OutElastic, amplitude: 0.8f, period: 0.5f));

                yield return animSeq.WaitForCompletion();
                    
                // Phát âm thanh va chạm (hit) khi chạm đích!
                if (_executor?.Data != null && _executor.Data.elementType != ProjectM.Elements.ElementType.None)
                {
                    AudioManager.Instance?.PlayElementDamageSFX(_executor.Data.elementType);
                }
                else
                {
                    AudioManager.Instance?.PlaySFX(AudioManager.Instance.hitClip);
                }
            }
            // -----------------------

            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(_executor.Execute(caster, targets));

            BattleDebugger.Log($"✅ '{_executor.Data?.skillName}' dùng xong.");
            SkillHandManager.Instance?.OnSkillUsed(this);
            BattleManager.Instance?.EndTurn(drawCard: false);
            
            // Mở lại chuột. Note: EndTurn sẽ lập tức khóa lại qua ProcessTurn, 
            // set false ở đây để phòng hờ trường hợp không gọi được ProcessTurn.
            Managers.BattleManager.IsInputLocked = false;
        }

        // ════════════════════════════════════════════════════════════════
        // HIGHLIGHT HELPERS
        // ════════════════════════════════════════════════════════════════
        private void ApplyHoverHighlight(CardBattle card, SkillTargetType targetType)
        {
            if (card == null) return;
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

        private static bool IsValidTarget(CardBattle card, SkillTargetType targetType)
        {
            if (card == null || card.IsDead) return false;
            var dropZone = card.transform.parent != null ? card.transform.parent.GetComponent<CardDropZone>() : null;
            if (dropZone == null) return false;

            return targetType switch
            {
                SkillTargetType.SingleAlly  =>  card.IsPlayerCard,
                SkillTargetType.SingleEnemy => !card.IsPlayerCard,
                SkillTargetType.EnemyRow    => !card.IsPlayerCard,
                _                           => false,
            };
        }

        private bool IsPointerOverHandRegion(Vector2 screenPos)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (var r in results)
            {
                if (r.gameObject.GetComponentInParent<SkillHandManager>() != null ||
                    r.gameObject.GetComponentInParent<PlayerHand>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        // ════════════════════════════════════════════════════════════════
        // POSITION HELPERS
        // ════════════════════════════════════════════════════════════════
        private Vector2 GetCardCenterScreenPos(CardBattle card = null)
        {
            RectTransform targetRt = card != null ? card.GetComponent<RectTransform>() : _rectTransform;
            if (targetRt == null) return Vector2.zero;

            // Nếu là thẻ mục tiêu và có TargetHighlight, lấy luôn tâm của TargetHighlight để đường Bezier ghim chính xác 100% vào tâm động
            if (card != null)
            {
                var th = card.GetComponentInChildren<TargetHighlight>(true);
                if (th != null) targetRt = th.GetComponent<RectTransform>();
            }

            if (_mainCanvas == null) _mainCanvas = GetComponentInParent<Canvas>();

            var corners = new Vector3[4];
            targetRt.GetWorldCorners(corners);
            // corners: [0]=bottomLeft [1]=topLeft [2]=topRight [3]=bottomRight
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            if (_mainCanvas != null && _mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Với Overlay, World Coordinates đã chính xác là Screen Coordinates
                return center;
            }

            return RectTransformUtility.WorldToScreenPoint(_mainCanvas?.worldCamera, center);
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

        private CardDropZone GetEmptyDropZoneUnderScreenPos(Vector2 screenPos)
        {
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results     = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            foreach (var r in results)
            {
                var zone = r.gameObject.GetComponentInParent<CardDropZone>();
                if (zone != null && zone.isPlayerZone && zone.transform.childCount == 0)
                {
                    Managers.BattleGrid grid = Managers.BattleGrid.Instance;
                    if (grid != null && grid.GetCardInSlot(zone) == null) return zone;
                }
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
