using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Map
{
    /// <summary>
    /// Định nghĩa một "ô" (slot) trên map.
    /// Mỗi slot có vị trí cố định trên canvas, kiểu loại (combat/random),
    /// và danh sách các slot có thể đi đến tiếp theo.
    /// </summary>
    [Serializable]
    public class MapSlotDefinition
    {
        [Tooltip("Vị trí local trên MapContent (X: 0-1920, Y: âm, tính từ trên xuống)")]
        public Vector2 position;

        [Tooltip("Route: 0 = Trái, 1 = Giữa, 2 = Phải")]
        public int routeIndex;

        [Tooltip("Nếu true: slot này luôn là Battle, không random")]
        public bool isCombat;

        [Tooltip("Nếu true: slot này là Boss cuối cùng")]
        public bool isBoss;

        [Tooltip("Tên file StageData trong Resources/Stages (vd: StageData_1). Bỏ trống hệ thống sẽ tự động gán.")]
        public string customStageID;

        [Tooltip("Chỉ số (index) các slot có thể đến từ slot này")]
        public List<int> nextSlots = new List<int>();
    }
}
