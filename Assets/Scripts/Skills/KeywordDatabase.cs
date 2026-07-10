using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectM.Skills
{
    // ════════════════════════════════════════════════════════════════════
    // KEYWORD ENTRY
    // ════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Một từ khóa thuật ngữ trong game và màu hiển thị tương ứng.
    /// Không cần TMP Style Sheet — mọi thứ cấu hình ngay tại đây.
    /// </summary>
    [Serializable]
    public class KeywordEntry
    {
        [Tooltip("Từ khóa xuất hiện trong description.\n" +
                 "VD: 'Sharp pencil', 'Frost', 'Rainbow candy mix'...")]
        public string keyword;

        [Tooltip("Màu hiển thị khi từ khóa này xuất hiện trong text.\n" +
                 "Chọn màu qua bảng Color Picker bình thường.")]
        public Color color = Color.white;

        [Tooltip("In đậm từ khóa này không?")]
        public bool bold = false;

        [Tooltip("In nghiêng từ khóa này không?")]
        public bool italic = false;

        [Tooltip("Nếu bật: 'FROST', 'frost' hay 'Frost' đều được xử lý giống nhau.")]
        public bool caseInsensitive = true;

        [TextArea(2, 4)]
        [Tooltip("Phần giải thích chi tiết cho từ khóa này (Sẽ hiển thị trên bảng Tooltip khi hover).")]
        public string explanation = "";
    }

    // ════════════════════════════════════════════════════════════════════
    // KEYWORD DATABASE (ScriptableObject)
    // ════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Bộ từ điển thuật ngữ toàn cục — quản lý màu sắc của từng từ khóa.
    ///
    /// Tạo asset: chuột phải Project → Create → Project M → Text → Keyword Database
    /// Kéo asset này vào ô Keyword Database trong CardDisplay Prefab.
    ///
    /// Muốn đổi màu toàn bộ từ khóa "Frost"? → Sửa dòng Frost trong list này là xong.
    /// </summary>
    [CreateAssetMenu(fileName = "KeywordDatabase", menuName = "Project M/Text/Keyword Database")]
    public class KeywordDatabase : ScriptableObject
    {
        [Tooltip("Danh sách từ khóa. Đặt từ DÀI HƠN ở TRÊN từ ngắn hơn để tránh nhầm lẫn.\n" +
                 "VD: 'Frost Berry' phải đặt trên 'Frost' trong danh sách.")]
        public List<KeywordEntry> keywords = new List<KeywordEntry>();
    }
}
