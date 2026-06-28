using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectM.Skills
{
    // ════════════════════════════════════════════════════════════════════
    // TEXT FORMATTER — Static Utility
    // ════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Lớp tiện ích tĩnh xử lý text trước khi gán vào TextMeshPro.
    /// Tự động quét description, tìm từ khóa trong KeywordDatabase
    /// và bọc thẻ màu TMP inline (không cần Style Sheet).
    ///
    /// Cách dùng:
    ///   myTMPText.text = TextFormatter.Process(rawDescription, keywordDb);
    /// </summary>
    public static class TextFormatter
    {
        // Cache Regex để không build lại nhiều lần (tối ưu hiệu năng)
        private static Dictionary<string, Regex> _regexCache = new Dictionary<string, Regex>();

        // ──────────────────────────────────────────────────────────────
        // MAIN ENTRY POINT
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Quét rawText, tìm từ khóa trong database và bọc thẻ màu TMP xung quanh.
        /// Trả về text đã được xử lý, sẵn sàng gán vào TMP component.
        /// </summary>
        /// <param name="rawText">Text gốc (viết bình thường trong ScriptableObject).</param>
        /// <param name="db">KeywordDatabase. Null → trả về text gốc.</param>
        public static string Process(string rawText, KeywordDatabase db)
        {
            if (string.IsNullOrEmpty(rawText)) return rawText;
            if (db == null || db.keywords == null || db.keywords.Count == 0) return rawText;

            string result = rawText;

            foreach (var entry in db.keywords)
            {
                if (string.IsNullOrEmpty(entry.keyword)) continue;

                // Lấy Regex từ cache hoặc tạo mới
                string cacheKey = entry.keyword + "|" + entry.caseInsensitive;
                if (!_regexCache.TryGetValue(cacheKey, out Regex rx))
                {
                    var options = entry.caseInsensitive
                        ? RegexOptions.IgnoreCase
                        : RegexOptions.None;

                    // Escape ký tự đặc biệt, thêm word boundary để tránh khớp nhầm
                    string pattern = Regex.Escape(entry.keyword);
                    rx = new Regex(pattern, options);
                    _regexCache[cacheKey] = rx;
                }

                // Build thẻ mở/đóng từ Color + Bold + Italic
                string openTag  = BuildOpenTag(entry);
                string closeTag = BuildCloseTag(entry);

                // Thay thế — bảo vệ các từ khóa đã được bọc thẻ rồi
                result = rx.Replace(result, m =>
                {
                    // Kiểm tra ký tự ngay trước match: nếu là '>' thì đang nằm trong thẻ TMP, bỏ qua
                    int idx = m.Index;
                    if (idx > 0 && result[idx - 1] == '>') return m.Value;

                    return openTag + m.Value + closeTag;
                });
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Build thẻ mở TMP inline từ thuộc tính của KeywordEntry.
        /// VD: <color=#FFD700FF><b>
        /// </summary>
        private static string BuildOpenTag(KeywordEntry entry)
        {
            var sb = new System.Text.StringBuilder();

            // Dùng RGB 6 chữ số (#RRGGBB) thay vì RGBA để tránh lỗi thanh trượt Alpha = 0 trong Unity làm chữ vô hình
            sb.Append("<color=#");
            sb.Append(ColorUtility.ToHtmlStringRGB(entry.color));
            sb.Append(">");

            if (entry.bold)   sb.Append("<b>");
            if (entry.italic) sb.Append("<i>");

            return sb.ToString();
        }

        /// <summary>
        /// Build thẻ đóng tương ứng, theo thứ tự ngược lại với thẻ mở.
        /// </summary>
        private static string BuildCloseTag(KeywordEntry entry)
        {
            var sb = new System.Text.StringBuilder();

            if (entry.italic) sb.Append("</i>");
            if (entry.bold)   sb.Append("</b>");
            sb.Append("</color>");

            return sb.ToString();
        }

        /// <summary>
        /// Xoá cache Regex — gọi khi reload domain trong Editor nếu cần.
        /// </summary>
        public static void ClearCache() => _regexCache.Clear();
    }
}
