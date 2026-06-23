namespace ProjectM.Map
{
    /// <summary>
    /// Trạng thái hiển thị của 1 node trên map.
    /// Tách ra file riêng để MapManager.cs có thể dùng trực tiếp không cần qualify.
    /// </summary>
    public enum NodeState
    {
        Hidden,     // Chưa reveal (scale = 0, không tương tác)
        Revealed,   // Đã hiện nhưng chưa phải lượt chọn
        Active,     // Đang có thể click (hiện mũi tên bounce)
        Completed,  // Đã hoàn thành (mờ + dấu tick)
        Dimmed      // Bị loại vì player chọn nhánh kia
    }
}
