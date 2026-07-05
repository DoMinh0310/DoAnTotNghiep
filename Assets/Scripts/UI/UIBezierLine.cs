using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    /// <summary>
    /// Vẽ một đường cong mượt trong UI Canvas bằng cách sinh mesh từ các điểm Bezier.
    /// Thêm vào SkillTargetingArrowPrefab cùng với Image component riêng (cho arrowhead).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIBezierLine : MaskableGraphic
    {
        private Vector2[] _points;

        [SerializeField] private float lineWidth = 18f;

        private bool _fadeAtEnd = false;
        public bool FadeAtEnd { get => _fadeAtEnd; set { if (_fadeAtEnd != value) { _fadeAtEnd = value; SetVerticesDirty(); } } }

        public float LineWidth { get => lineWidth; set { lineWidth = value; SetVerticesDirty(); } }

        /// <summary>
        /// Cập nhật các điểm của đường (trong local space của canvas cha).
        /// Gọi mỗi frame từ SkillTargetingArrow.
        /// </summary>
        public void SetPoints(Vector2[] canvasLocalPoints)
        {
            _points = canvasLocalPoints;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points == null || _points.Length < 2) return;

            int n = _points.Length;

            // BƯỚC 1: Sinh ra các cặp đỉnh (Miter Joints) cho lưới liên tục
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);

                // Tính toán Alpha và Width tại điểm này
                float a = Mathf.InverseLerp(0f, 0.7f, t);
                a = Mathf.Max(0.05f, a);

                // Khi đang khóa vào thẻ mục tiêu, làm cho phần đầu của đường Bezier (gần tâm thẻ) mờ dần về 0 để tạo hiệu ứng tan biến mềm mại vào trong tâm thẻ
                if (_fadeAtEnd && t > 0.5f)
                {
                    float fadeOut = Mathf.InverseLerp(1f, 0.5f, t);
                    a *= fadeOut;
                }

                Color c = new Color(color.r, color.g, color.b, a);

                // Đường kẻ giữ nguyên độ rộng đều đặn và thon gọn từ gốc đến ngọn, không bị phình to dần
                float w = lineWidth;

                // Tính toán vector hướng của đường cong (bao gồm trước và sau để lấy trung bình)
                Vector2 dirPrev = i > 0 ? (_points[i] - _points[i - 1]).normalized : Vector2.zero;
                Vector2 dirNext = i < n - 1 ? (_points[i + 1] - _points[i]).normalized : Vector2.zero;

                Vector2 dir = (dirPrev + dirNext).normalized;
                if (dir == Vector2.zero) dir = dirNext != Vector2.zero ? dirNext : dirPrev;
                if (dir == Vector2.zero) dir = Vector2.right; // Fallback an toàn

                // Tính Miter Joint (Khớp nối liền mạch không bị gãy góc)
                Vector2 perp = new Vector2(-dir.y, dir.x);
                float miterDot = 1f;
                if (i > 0 && i < n - 1)
                {
                    Vector2 perpNext = new Vector2(-dirNext.y, dirNext.x);
                    miterDot = Vector2.Dot(perp, perpNext);
                    if (miterDot < 0.5f) miterDot = 0.5f; // Giới hạn độ nhọn để tránh vỡ lưới (Miter spike)
                }
                perp = perp / miterDot; // Bù đắp độ dày khi đường cong gập lại

                // Sinh 2 điểm (trái, phải)
                vh.AddVert(_points[i] - perp * (w * 0.5f), c, new Vector2(0, t));
                vh.AddVert(_points[i] + perp * (w * 0.5f), c, new Vector2(1, t));
            }

            // BƯỚC 2: Nối các cặp đỉnh thành một dải Triangle Strip nguyên khối
            for (int i = 0; i < n - 1; i++)
            {
                int index = i * 2;
                // Vẽ 2 tam giác nối điểm hiện tại và điểm kế tiếp để tạo thành 1 đốt sống liền mạch
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index + 1, index + 3, index + 2);
            }
        }
    }
}
