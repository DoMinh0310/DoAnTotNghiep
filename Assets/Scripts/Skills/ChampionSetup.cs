using UnityEngine;
using System.Collections.Generic;
using ProjectM.Cards;

namespace ProjectM.Skills
{
    /// <summary>
    /// Cặp dữ liệu: 1 thẻ tướng + Relic + Trinket đang trang bị.
    /// Dùng trong ChampionSetup để config deck của 1 run.
    /// </summary>
    [System.Serializable]
    public class ChampionEntry
    {
        [Tooltip("CardData của thẻ tướng")]
        public CardData championData;

        [Tooltip("Relic đang trang bị (có thể tháo ở Inventory). Để trống = không có.")]
        public RelicData equippedRelic;

        [Tooltip("Trinket đang trang bị (KHÔNG thể tháo ngoài Shop). Để trống = không có.")]
        public TrinketData equippedTrinket;
    }

    /// <summary>
    /// ScriptableObject lưu toàn bộ deck của 1 run.
    /// Bao gồm: danh sách tướng (+ equipment) và pool bài hỗ trợ.
    ///
    /// Tạo: chuột phải Project → Create → Project M → Champion Setup
    ///
    /// UNITY SETUP:
    ///   1. Tạo asset ChampionSetup cho mỗi stage/run.
    ///   2. Kéo vào SkillHandManager.championSetup.
    ///   3. Điền danh sách champions và supportDeck.
    /// </summary>
    [CreateAssetMenu(fileName = "ChampionSetup_New", menuName = "Project M/Champion Setup")]
    public class ChampionSetup : ScriptableObject
    {
        [Header("Champions & Equipment")]
        [Tooltip("Danh sách tướng trong run này, mỗi tướng kèm Relic và Trinket trang bị.")]
        public List<ChampionEntry> champions = new();

        [Header("Support Deck (Draw Pile)")]
        [Tooltip("Pool bài hỗ trợ ban đầu — sẽ được xáo và deal 6 lá đầu lượt đầu tiên.")]
        public List<SkillData> supportDeck = new();

        [Header("Player Owned Collection (Run Inventory)")]
        [Tooltip("Toàn bộ Relic người chơi sở hữu trong run này (kể cả đang trang bị). Kéo-thả trong Inventory để đổi cho tướng.")]
        public List<RelicData> ownedRelics = new();

        [Tooltip("Toàn bộ Trinket người chơi sở hữu trong run này. Chỉ đổi được tại Shop.")]
        public List<TrinketData> ownedTrinkets = new();
    }
}
