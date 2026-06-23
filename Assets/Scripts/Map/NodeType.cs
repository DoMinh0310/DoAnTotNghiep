namespace ProjectM.Map
{
    /// <summary>
    /// Tất cả loại event có thể xuất hiện trên Map.
    /// </summary>
    public enum NodeType
    {
        Battle,     // Lần combat bắt buộc (icon đầu lâu)
        Boss,       // Lần combat cuối - Boss
        Card,       // Nhận thẻ bài mới
        Relic,      // Nhận Relic (vật phẩm bị động)
        Resource,   // Nhận tài nguyên (gold/material)
        Sacrifice,  // Đánh đổi HP lấy thứ gì đó
        Smith,      // Nâng cấp thẻ bài
        Shop        // Cửa hàng (sẽ implement sau)
    }
}
