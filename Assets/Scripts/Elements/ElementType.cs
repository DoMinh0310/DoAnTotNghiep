namespace ProjectM.Elements
{
    /// <summary>
    /// Danh sách nguyên tố trong game.
    /// Thêm nguyên tố mới vào đây và tạo class implement IElementalEffect tương ứng.
    /// </summary>
    public enum ElementType
    {
        Bleed,    // 0: Chảy máu — phát nổ trước khi địch đánh
        Decay,    // 1: Phân rã — gây sát thương theo thời gian sau khi địch đánh
        Frost,    // 2: Băng 
        Chain,    // 3: Dây chuyền
        None      // 4: Không có nguyên tố
    }
}
