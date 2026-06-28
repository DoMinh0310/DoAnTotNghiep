using System.Collections;

namespace ProjectM.Elements
{
    /// <summary>
    /// Interface mà mọi nguyên tố phải implement.
    /// Để thêm nguyên tố mới: tạo class mới implement interface này,
    /// sau đó đăng ký nó trong ElementalHandler._registeredEffects.
    /// </summary>
    public interface IElementalEffect
    {
        /// <summary>Loại nguyên tố của effect này.</summary>
        ElementType Type { get; }

        /// <summary>
        /// Gọi ngay khi stack mới được cộng vào.
        /// Dùng để cập nhật trạng thái nội bộ.
        /// </summary>
        IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks);

        /// <summary>
        /// Gọi ngay TRƯỚC KHI đơn vị địch bắt đầu lượt tấn công.
        /// Chain kích hoạt tại đây (khi stacks >= HP hiện tại).
        /// Frost giảm counter tại đây.
        /// </summary>
        IEnumerator OnTurnStart(ElementalHandler handler);

        /// <summary>
        /// Gọi ngay SAU KHI đơn vị địch kết thúc lượt tấn công.
        /// Decay tick tại đây (gây damage rồi tăng stack lên 1).
        /// </summary>
        IEnumerator OnTurnEnd(ElementalHandler handler);

        /// <summary>
        /// Gọi sau khi tất cả action của đồng minh trong 1 lượt hoàn thành,
        /// ngay TRƯỚC KHI đến lượt địch đánh tiếp theo.
        /// Bleed nổ tại đây (dựa trên sát thương vật lý tích lũy từ lượt đồng minh).
        /// Các nguyên tố khác có thể để yield break.
        /// </summary>
        IEnumerator OnAfterPlayerAction(ElementalHandler handler);
    }
}
