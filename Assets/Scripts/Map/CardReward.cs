using UnityEngine;
using System.Collections.Generic;
using ProjectM.Cards;

namespace ProjectM.Map
{
    /// <summary>
    /// ScriptableObject cấu hình sự kiện nhận thưởng thẻ (Card Reward).
    /// Tạo trong Unity: Right-click Project → Create → Project M → Card Reward Config
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardReward", menuName = "Project M/Map/Card Reward Config")]
    public class CardReward : ScriptableObject
    {
        [Header("Cấu hình Pool Thẻ")]
        [Tooltip("Kéo các thẻ Kỹ năng (Skill) và Tiện ích (Utilities) vào đây.")]
        public List<ProjectM.Skills.SkillData> skillPool = new();

        [Tooltip("Kéo các thẻ Công trình (Building) vào đây.")]
        public List<CardData> buildingPool = new();

        [Header("Số lượng thẻ hiển thị để chọn")]
        [Range(1, 5)]
        public int numberOfChoices = 3;

        [Header("Cho phép bỏ qua không nhận thẻ?")]
        public bool allowSkip = false;

        /// <summary>
        /// Trả về danh sách ngẫu nhiên gồm numberOfChoices thẻ từ hai Pool.
        /// CHỈ lấy SkillData, hoặc CardData có CardType là Building.
        /// </summary>
        public List<ScriptableObject> GetRandomChoices(System.Random rng = null)
        {
            var validCards = new List<ScriptableObject>();

            // Gom chung SkillData vào mảng hợp lệ
            if (skillPool != null)
            {
                foreach (var skill in skillPool)
                {
                    if (skill != null) validCards.Add(skill);
                }
            }

            // Gom chung Building vào mảng hợp lệ (nhớ lọc kỹ nhỡ tay kéo nhầm Tướng)
            if (buildingPool != null)
            {
                foreach (var card in buildingPool)
                {
                    if (card != null && card.cardType == CardType.Building)
                    {
                        validCards.Add(card);
                    }
                }
            }

            if (validCards.Count == 0)
            {
                Debug.LogWarning("[CardReward] Cả hai Pool đều trống hoặc không có thẻ hợp lệ! Hãy thêm thẻ vào.");
                return new List<ScriptableObject>();
            }

            rng ??= new System.Random();

            if (validCards.Count < numberOfChoices)
            {
                Debug.LogWarning($"[CardReward] Tổng Pool chỉ có {validCards.Count} thẻ hợp lệ " +
                                 $"nhưng cần {numberOfChoices}. Sẽ hiển thị tất cả thẻ có sẵn.");
            }

            // ── Trộn ngẫu nhiên và lọc trùng ───────────────────────────
            var picked = new List<ScriptableObject>();
            
            while (picked.Count < numberOfChoices && validCards.Count > 0)
            {
                int r = rng.Next(0, validCards.Count);
                var selectedCard = validCards[r];
                
                picked.Add(selectedCard);
                
                // Cực kỳ quan trọng: Xóa TẤT CẢ các bản sao của thẻ này khỏi validCards
                // để đảm bảo 3 ô lựa chọn không bao giờ hiện 2 thẻ giống nhau!
                validCards.RemoveAll(c => c == selectedCard);
            }

            return picked;
        }
    }
}



