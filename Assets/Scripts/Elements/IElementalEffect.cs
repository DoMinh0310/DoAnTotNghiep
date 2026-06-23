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
        /// Dùng để cập nhật trạng thái nội bộ (ví dụ: DecayEffect cập nhật dotDuration).
        /// </summary>
        IEnumerator OnStackAdded(ElementalHandler handler, int totalStacks);

        /// <summary>
        /// Gọi ngay TRƯỚC KHI đơn vị bắt đầu lượt của mình (trước khi trừ speed).
        /// Bleed kích hoạt tại đây nếu đủ stack.
        /// </summary>
        IEnumerator OnTurnStart(ElementalHandler handler);

        /// <summary>
        /// Gọi ngay SAU KHI đơn vị kết thúc lượt của mình (ngay cả khi không đánh).
        /// Decay tích/kích hoạt tại đây.
        /// </summary>
        IEnumerator OnTurnEnd(ElementalHandler handler);
    }
}
