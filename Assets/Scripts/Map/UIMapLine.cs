using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.Map
{
    /// <summary>
    /// Vẽ đường đứt đoạn (dashed line), uốn cong (bezier), và có hiệu ứng trôi (animation)
    /// nối giữa các node trên bản đồ. Không cần dùng texture đặc biệt!
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIMapLine : MaskableGraphic
    {
        [Header("Line Settings")]
        public Vector2 startPoint;
        public Vector2 endPoint;
        public float lineWidth = 8f;
        
        [Tooltip("Độ cong của đường. Randomize nhẹ để map trông tự nhiên.")]
        public float curveOffset = 50f; 

        [Header("Dotted Animation")]
        public float dashLength = 20f;
        public float gapLength = 15f;

        private float _offset = 0f;

        public void Setup(Vector2 start, Vector2 end)
        {
            startPoint = start;
            endPoint = end;
            
            // Tạo độ cong ngẫu nhiên (nếu đường đi chéo, bẻ cong nhẹ ra ngoài)
            float sign = (start.x > end.x) ? 1f : -1f;
            curveOffset = Random.Range(30f, 70f) * sign;
            
            // Random offset để các đường không bị đồng bộ nhịp đứt y hệt nhau
            _offset = Random.Range(0f, dashLength + gapLength);

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (startPoint == endPoint) return;

            // Tính control point để bẻ cong đường thẳng
            Vector2 mid = (startPoint + endPoint) * 0.5f;
            Vector2 dir = (endPoint - startPoint).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector2 controlPoint = mid + normal * curveOffset;

            // Lấy độ dài xấp xỉ của đường cong
            float approxLength = Vector2.Distance(startPoint, controlPoint) + Vector2.Distance(controlPoint, endPoint);
            
            // Chia nhỏ đường cong thành nhiều đoạn nhỏ (mỗi đoạn ~2-3 pixels) để vẽ nét đứt mượt
            int resolution = Mathf.CeilToInt(approxLength / 3f);
            if (resolution < 5) resolution = 5;

            Vector2[] curvePoints = new Vector2[resolution + 1];
            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                // Công thức Bezier bậc 2
                curvePoints[i] = Vector2.Lerp(Vector2.Lerp(startPoint, controlPoint, t), Vector2.Lerp(controlPoint, endPoint, t), t);
            }

            float currentDist = _offset;
            float cycle = dashLength + gapLength;

            Vector2 lastPos = curvePoints[0];

            // Bắt đầu sinh các mảnh Quad dọc theo đường cong
            for (int i = 1; i <= resolution; i++)
            {
                Vector2 p = curvePoints[i];
                float distStep = Vector2.Distance(lastPos, p);
                
                // Kiểm tra xem đoạn này có nằm trong phần Dash (phải vẽ) hay Gap (bỏ qua)
                float mod = currentDist % cycle;
                if (mod < 0) mod += cycle;

                bool shouldDraw = mod < dashLength;

                if (shouldDraw)
                {
                    // Hướng cong tại điểm này
                    Vector2 d = (p - lastPos).normalized;
                    if (d == Vector2.zero) d = Vector2.up;
                    Vector2 perp = new Vector2(-d.y, d.x); // Vuông góc

                    int vCount = vh.currentVertCount;

                    // Thêm 4 đỉnh tạo thành 1 hình chữ nhật nhỏ
                    vh.AddVert(lastPos - perp * (lineWidth * 0.5f), color, Vector2.zero);
                    vh.AddVert(lastPos + perp * (lineWidth * 0.5f), color, Vector2.zero);
                    vh.AddVert(p - perp * (lineWidth * 0.5f), color, Vector2.zero);
                    vh.AddVert(p + perp * (lineWidth * 0.5f), color, Vector2.zero);

                    vh.AddTriangle(vCount, vCount + 1, vCount + 2);
                    vh.AddTriangle(vCount + 1, vCount + 3, vCount + 2);
                }

                currentDist += distStep;
                lastPos = p;
            }
        }
    }
}
