using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// Quản lý đường cong Bezier từ thẻ skill đến cursor.
    ///
    /// SETUP prefab (SkillTargetingArrowPrefab):
    ///   Root (RectTransform stretch-fill, SkillTargetingArrow.cs, CanvasGroup)
    ///   ├── Line  (UIBezierLine.cs + CanvasRenderer, raycastTarget = OFF)
    ///   └── ArrowHead (Image hình tam giác, raycastTarget = OFF)
    ///
    /// Gán Line → field "Bezier Line", ArrowHead → field "Arrow Head".
    /// </summary>
    public class SkillTargetingArrow : MonoBehaviour
    {
        [Header("Line")]
        [SerializeField] private UIBezierLine bezierLine;
        [SerializeField] private Color lineColor = new Color(1f, 0.45f, 0.1f, 1f);
        [SerializeField] private float lineWidth = 22f;
        [SerializeField] private int   segmentCount = 24;
        [SerializeField] private float curveHeight  = 160f;

        [Header("ArrowHead")]
        [SerializeField] private RectTransform arrowHead;

        // ── Runtime ──────────────────────────────────────────────────────
        private Canvas        _canvas;
        private RectTransform _canvasRect;

        // Caching optimization
        private Vector2[] _bezierPoints;
        private Vector2   _lastEnd;
        private bool      _lastValidTarget;

        // ════════════════════════════════════════════════════════════════
        private void Awake()
        {
            // Cập phát sẵn mảng để tránh tạo rác (Garbage) mỗi frame làm tụt FPS
            _bezierPoints = new Vector2[segmentCount + 1];

            // ── Tự động xóa TargetIcon cũ nếu vẫn còn thừa trong prefab ──
            foreach (Transform child in transform)
            {
                if (child.name.Contains("TargetIcon"))
                {
                    Destroy(child.gameObject);
                }
            }

            // ── Tự động thêm Canvas để render đè lên các thẻ ──
            var myCanvas = gameObject.GetComponent<Canvas>();
            if (myCanvas == null) myCanvas = gameObject.AddComponent<Canvas>();
            myCanvas.overrideSorting = true;
            myCanvas.sortingOrder    = 100; // Đảm bảo luôn nằm trên cùng

            // ── Tìm root canvas ──
            _canvas = FindRootCanvas();
            _canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;

            // ── Đảm bảo ROOT của prefab stretch-fill để tọa độ khớp canvas ──
            var rootRt = GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.anchorMin        = Vector2.zero;
                rootRt.anchorMax        = Vector2.one;
                rootRt.offsetMin        = Vector2.zero;
                rootRt.offsetMax        = Vector2.zero;
                rootRt.anchoredPosition = Vector2.zero;
            }

            // ── Setup UIBezierLine ──
            if (bezierLine != null)
            {
                bezierLine.color     = lineColor;
                bezierLine.LineWidth = lineWidth;

                // Stretch-fill
                var lr = bezierLine.GetComponent<RectTransform>();
                if (lr != null)
                {
                    lr.anchorMin        = Vector2.zero;
                    lr.anchorMax        = Vector2.one;
                    lr.offsetMin        = Vector2.zero;
                    lr.offsetMax        = Vector2.zero;
                    lr.anchoredPosition = Vector2.zero;
                }

                // Tắt raycast để không chặn click
                bezierLine.raycastTarget = false;
            }
            else
            {
                Debug.LogWarning("[SkillTargetingArrow] 'Bezier Line' chưa được gán trong Inspector!");
            }

            // ── ArrowHead: ẩn ngay cho đến khi SetPositions được gọi ──
            if (arrowHead != null)
            {
                // TỰ ĐỘNG FIX: Nếu người dùng kéo Prefab từ Project Window vào thay vì dùng child object
                if (!arrowHead.gameObject.scene.IsValid())
                {
                    arrowHead = Instantiate(arrowHead, transform);
                    arrowHead.name = "ArrowHead (Auto-Spawned)";
                }

                arrowHead.gameObject.SetActive(false);
                var img = arrowHead.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
            else
            {
                Debug.LogWarning("[SkillTargetingArrow] 'Arrow Head' chưa được gán trong Inspector!");
            }

            if (_canvasRect == null)
                Debug.LogError("[SkillTargetingArrow] Không tìm thấy Root Canvas! Prefab phải là con của Canvas.");
        }

        // ════════════════════════════════════════════════════════════════
        /// <summary>Cập nhật đường cong mỗi frame. Gọi từ SkillDragHandler.Update.</summary>
        public void SetPositions(Vector2 startScreen, Vector2 endScreen, bool hasValidTarget)
        {
            var rootRt = GetComponent<RectTransform>();
            if (rootRt == null) return;

            // Overlay canvas không dùng camera
            Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRt, startScreen, cam, out Vector2 start)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRt, endScreen, cam, out Vector2 end)) return;

            // Optimization: Nếu chuột không di chuyển và state không đổi -> Bỏ qua tính toán lại
            if (Vector2.Distance(_lastEnd, end) < 0.5f && _lastValidTarget == hasValidTarget)
                return;

            _lastEnd = end;
            _lastValidTarget = hasValidTarget;

            // Khoảng cách giữa điểm bắt đầu và chuột
            float dist = Vector2.Distance(start, end);

            // Điều chỉnh chiều cao cung theo hướng cursor.
            // Vấn đề: nếu cursor ở phía TRÊN điểm xuất phát, điểm control bị đẩy lên
            // cao hơn cả endpoint, khiến bezier vẽ vung lỗi quá endpoint rồi quay xuống.
            // Giải pháp: giảm chiều cao cung về 0 khi cursor thẳng lên → đường thẳng.
            Vector2 dirToEnd        = dist > 0.01f ? (end - start) / dist : Vector2.up;
            float   upDot           = Vector2.Dot(dirToEnd, Vector2.up); // +1 = thẳng lên, -1 = thẳng xuống
            float   heightMult      = 1f - Mathf.Clamp01(upDot);         // 0 khi lên, 1 khi xuống
            float   dynamicCurveHeight = Mathf.Min(curveHeight, dist * 0.35f) * heightMult;

            // Điểm kiểm soát: giữa đường lệch lên trên
            Vector2 mid     = (start + end) * 0.5f;
            Vector2 control = mid + Vector2.up * dynamicCurveHeight;

            // Tính tiếp tuyến dự kiến để giật lùi điểm kết thúc
            Vector2 approxTangent = BezierTangent(start, control, end, 0.98f).normalized;
            Vector2 lineEnd = end;
            float arrowLength = 80f; // Default fallback

            if (arrowHead != null && arrowHead.gameObject.scene.IsValid() || arrowHead != null)
            {
                arrowLength = arrowHead.rect.height;
                // Cắt ngắn đường kẻ để nó kết thúc vừa khít với mặt đáy của mũi tên
                lineEnd = end - approxTangent * (arrowLength * 0.6f);
            }

            // Sinh điểm Bezier dùng mảng cấp sẵn
            if (_bezierPoints == null || _bezierPoints.Length != segmentCount + 1)
                _bezierPoints = new Vector2[segmentCount + 1];

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                _bezierPoints[i] = Bezier(start, control, lineEnd, t); // Vẽ đến lineEnd
            }

            // Cập nhật line
            bezierLine?.SetPoints(_bezierPoints);

            // Cập nhật arrowhead
            if (arrowHead != null)
            {
                arrowHead.gameObject.SetActive(true);

                arrowHead.anchorMin = new Vector2(0.5f, 0.5f);
                arrowHead.anchorMax = new Vector2(0.5f, 0.5f);
                arrowHead.pivot     = new Vector2(0.5f, 0.5f);

                // Lấy hướng của đoạn thẳng cuối cùng vừa được vẽ
                Vector2 actualDir = (_bezierPoints[segmentCount] - _bezierPoints[segmentCount - 1]).normalized;
                // Dự phòng: nếu tiếp tuyến suy biến (2 điểm trùng nhau), dùng hướng tổng thể
                if (actualDir.sqrMagnitude < 0.01f)
                    actualDir = (end - start).normalized;

                // Gắn mũi tên sao cho ĐÁY của nó khớp chuẩn 100% với lineEnd
                // Tâm mũi tên sẽ tiến lên phía trước dọc theo hướng actualDir
                arrowHead.anchoredPosition = lineEnd + actualDir * (arrowLength * 0.35f);

                // Mũi tên gốc chỉ lên trên, nên ta trừ đi 90 độ
                float angle = Mathf.Atan2(actualDir.y, actualDir.x) * Mathf.Rad2Deg - 90f;
                arrowHead.localRotation = Quaternion.Euler(0f, 0f, angle);

                var img = arrowHead.GetComponent<Image>();
                if (img != null)
                    img.color = lineColor; // Luôn đặc (Alpha = 1) và đồng bộ màu với đường vẽ
            }
        }

        // ════════════════════════════════════════════════════════════════
        // MATH
        // ════════════════════════════════════════════════════════════════
        private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector2 BezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }

        private Canvas FindRootCanvas()
        {
            var canvases = GetComponentsInParent<Canvas>(true);
            if (canvases == null || canvases.Length == 0) return null;
            foreach (var c in canvases)
                if (c.isRootCanvas) return c;
            return canvases[canvases.Length - 1];
        }
    }
}
