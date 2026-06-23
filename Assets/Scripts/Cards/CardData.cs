using UnityEngine;
using System.Collections.Generic;
using ProjectM.Elements;

namespace ProjectM.Cards
{
    public enum CardType  { Champion, Building, Skill, Utilities, Enemy }
    public enum CardRarity{ Common, Uncommon, Rare, Legendary }

    /// <summary>Cấu hình kháng tính và ngưỡng kích hoạt cho từng nguyên tố.</summary>
    [System.Serializable]
    public class ElementalConfig
    {
        public ElementType element;

        [Tooltip("Số stack cần để kích hoạt hiệu ứng (Scorch). Venom dùng số lượt DoT.")]
        public int triggerThreshold = 5;

        [Range(0f, 1f)]
        [Tooltip("0 = không kháng, 0.5 = kháng 50%%, 1 = miễn dịch hoàn toàn")]
        public float resistance = 0f;
    }

    [CreateAssetMenu(fileName = "NewCard", menuName = "Project M/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        public string cardName;
        public string id;
        [TextArea(2, 4)]
        public string abilities;

        [Header("Stats")]
        public int attack;
        public int health;
        public int speed;
        [Tooltip("Số năng lượng (manas) tướng sẽ tiêu thụ để dùng kĩ năng tối thượng")]
        public int ult;

        [Header("Card System")]
        public CardType  cardType  = CardType.Champion;
        public CardRarity rarity   = CardRarity.Common;
        [Tooltip("Chi phí năng lượng để đánh thẻ này trong lượt")]
        public int cost = 1;
        [TextArea(2, 4)]
        [Tooltip("Mô tả hiệu ứng khi đánh thẻ (hiển thị trên card UI)")]
        public string description = "";

        [Header("Art Assets")]
        public Sprite characterArt;  // Ảnh nhân vật (đã có)
        public Sprite cardArtwork;   // Ảnh minh họa của thẻ kỹ năng
        public Sprite cardFrame;     // Khung viền thẻ (tùy rarity, để null dùng default)
        
        [Header("Visual Tuning")]
        [Tooltip("Dùng để căn chỉnh lại vị trí ảnh (nếu ảnh gốc bị lệch tâm). X=trái/phải, Y=lên/xuống.")]
        public Vector2 artOffset = Vector2.zero;
        [Tooltip("Dùng để phóng to/thu nhỏ ảnh gốc (mặc định = 1). Phóng to lên nếu ảnh gốc quá nhỏ.")]
        public float artScale = 1f;

        [Header("Elemental Config")]
        [Tooltip("Cấu hình kháng tính và ngưỡng kích hoạt cho từng nguyên tố. Để trống = dùng giá trị mặc định.")]
        public List<ElementalConfig> elementalConfigs;

        public int GetElementalThreshold(ElementType element)
        {
            if (elementalConfigs == null) return 5;
            foreach (var c in elementalConfigs)
                if (c.element == element) return c.triggerThreshold > 0 ? c.triggerThreshold : 5;
            return 5;
        }

        /// <summary>Trả về % kháng tính nguyên tố (0 = không kháng, 1 = miễn dịch).</summary>
        public float GetElementalResistance(ElementType element)
        {
            if (elementalConfigs == null) return 0f;
            foreach (var c in elementalConfigs)
                if (c.element == element) return c.resistance;
            return 0f;
        }

        /// <summary>Trả về màu accent theo rarity để CardUI tô màu viền/glow.</summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                CardRarity.Common    => new Color(0.75f, 0.75f, 0.75f),
                CardRarity.Uncommon  => new Color(0.20f, 0.80f, 0.35f),
                CardRarity.Rare      => new Color(0.25f, 0.55f, 1.00f),
                CardRarity.Legendary => new Color(1.00f, 0.80f, 0.15f),
                _                   => Color.white
            };
        }
    }
}
